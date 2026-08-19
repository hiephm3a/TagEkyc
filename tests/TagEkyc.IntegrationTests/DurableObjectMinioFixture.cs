using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

internal sealed class DurableObjectMinioFixture : IAsyncDisposable
{
    private const string StartupNotInitializedMessage = "Server not initialized yet, please try again.";
    internal const string Image = "minio/minio@sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e";
    private const string McImage = "minio/mc@sha256:eb4ea9884b77704230e2423e9004d2fa738dc272876b9cc41a297d29443b8780";
    private const string ObjectPrefix = "raw-export/c1/v1/";
    private const string RecipientPackagePrefix = "raw-export/c2-package/v1/";
    private readonly string containerName = $"tagekyc-durable-object-{Guid.NewGuid():N}";
    private readonly string volumeName = $"tagekyc-durable-object-{Guid.NewGuid():N}";
    private readonly string rootAccessKey = RandomCredential(16);
    private readonly string rootSecretKey = RandomCredential(32);
    private readonly Dictionary<ProvisionalObjectCapability, (string Access, string Secret)> credentials =
        Enum.GetValues<ProvisionalObjectCapability>()
            .ToDictionary(capability => capability, _ => (RandomCredential(16), RandomCredential(32)));
    private readonly (string Access, string Secret) cleanupCredential =
        (RandomCredential(16), RandomCredential(32));
    private readonly Dictionary<string, (string Access, string Secret)> recipientPackageCredentials =
        new[] { "writer", "reconciler", "lifecycle", "posture", "delivery-reader" }
            .ToDictionary(name => name, _ => (RandomCredential(16), RandomCredential(32)), StringComparer.Ordinal);
    private readonly Dictionary<string, (string Access, string Secret, string PolicyName)>
        postureScenarioCredentials = new(StringComparer.Ordinal);
    private readonly Dictionary<ProvisionalObjectCapability, string> capabilityPolicyDocuments = [];
    private readonly List<string> additionalPolicyNames = [];
    private readonly List<string> buckets = [];
    private AmazonS3Client? cleanupAdmin;
    private int hostPort;
    private static readonly TimeSpan ProtocolUsabilityTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan ProtocolUsabilityRetryDelay = TimeSpan.FromMilliseconds(250);

    private DurableObjectMinioFixture()
    {
        BucketName = $"tagekyc-{Guid.NewGuid():N}";
    }

    internal string BucketName { get; }
    internal Uri ServiceUrl { get; private set; } = null!;

    internal static async Task<DurableObjectMinioFixture> StartAsync()
    {
        var fixture = new DurableObjectMinioFixture();
        try
        {
            await fixture.StartContainerAsync().ConfigureAwait(false);
            await fixture.ProveStartupRetryPolicyAsync().ConfigureAwait(false);
            await fixture.CreateInitialBucketWhenUsableAsync(fixture.BucketName).ConfigureAwait(false);
            await fixture.ProvisionCapabilityIdentitiesAsync().ConfigureAwait(false);
            await fixture.ProvisionRecipientPackageIdentitiesAsync().ConfigureAwait(false);
            return fixture;
        }
        catch
        {
            await fixture.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    internal ProvisionalObjectCustodyOptions Options(
        ProvisionalObjectCapability capability,
        string? bucketName = null)
    {
        var credential = capability == ProvisionalObjectCapability.PostureProbe
            && bucketName is not null
            && postureScenarioCredentials.TryGetValue(bucketName, out var scenario)
                ? (scenario.Access, scenario.Secret)
                : credentials[capability];
        return new(
            ProvisionalObjectTopology.S3CompatibleDurable,
            capability,
            ServiceUrl,
            bucketName ?? BucketName,
            credential.Access,
            credential.Secret,
            true,
            ProvisionalObjectCustodyOptions.FixedMaximumSinglePartCiphertextBytes,
            ProvisionalObjectCustodyOptions.FixedOperationTimeout,
            true);
    }

    internal async Task RestartAsync()
    {
        cleanupAdmin?.Dispose();
        cleanupAdmin = null;
        await DockerAsync("rm", "-f", containerName).ConfigureAwait(false);
        await StartContainerAsync().ConfigureAwait(false);
    }

    internal AmazonS3Client CreateAdminClient() => NewClient(cleanupCredential);

    internal async Task PutRootObjectAsync(string bucketName, string objectKey, byte[] content)
    {
        using var root = NewRootClient();
        await root.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = new MemoryStream(content, writable: false),
            AutoCloseStream = true,
        }).ConfigureAwait(false);
    }

    internal AmazonS3Client CreateCapabilityClient(
        ProvisionalObjectCapability capability,
        string? bucketName = null)
    {
        var credential = capability == ProvisionalObjectCapability.PostureProbe
            && bucketName is not null
            && postureScenarioCredentials.TryGetValue(bucketName, out var scenario)
                ? (scenario.Access, scenario.Secret)
                : credentials[capability];
        return NewClient(credential);
    }

    internal string CapabilityPolicyDocument(ProvisionalObjectCapability capability) =>
        capabilityPolicyDocuments[capability];

    internal int DistinctCredentialCount() => credentials.Values
        .Append(cleanupCredential)
        .Select(value => Convert.ToHexString(SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{value.Access}\0{value.Secret}"))))
        .Distinct(StringComparer.Ordinal)
        .Count();

    internal RecipientPackageProviderConfiguration RecipientPackageConfiguration() => new(
        "c2-minio-fixture-v1",
        ServiceUrl,
        BucketName,
        true,
        "us-east-1",
        new(recipientPackageCredentials["writer"].Access, recipientPackageCredentials["writer"].Secret),
        new(recipientPackageCredentials["reconciler"].Access, recipientPackageCredentials["reconciler"].Secret),
        new(recipientPackageCredentials["lifecycle"].Access, recipientPackageCredentials["lifecycle"].Secret),
        new(recipientPackageCredentials["posture"].Access, recipientPackageCredentials["posture"].Secret),
        true);

    internal AmazonS3Client CreateRecipientPackageClient(string capability) =>
        NewClient(recipientPackageCredentials[capability]);

    internal RecipientPackageCredential RecipientPackageDeliveryReaderCredential() => new(
        recipientPackageCredentials["delivery-reader"].Access,
        recipientPackageCredentials["delivery-reader"].Secret);

    internal RecipientPackageCredential RecipientPackageCredentialFor(string capability)
    {
        var credential = recipientPackageCredentials[capability];
        return new(credential.Access, credential.Secret);
    }

    internal int RecipientPackageDistinctCredentialCount() => recipientPackageCredentials.Values
        .Select(value => Convert.ToHexString(SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{value.Access}\0{value.Secret}"))))
        .Distinct(StringComparer.Ordinal)
        .Count();

    internal string RecipientPackageDeliveryReaderPolicyDocument() =>
        ObjectPolicyForPrefix(BucketName, RecipientPackagePrefix, "s3:GetObject");

    internal async Task<string> CreateBucketAsync(bool objectLockEnabled = false)
    {
        var bucketName = $"tagekyc-{Guid.NewGuid():N}";
        await CreateBucketAsync(bucketName, objectLockEnabled).ConfigureAwait(false);
        await ProvisionPostureScenarioIdentityAsync(bucketName).ConfigureAwait(false);
        return bucketName;
    }

    private async Task StartContainerAsync()
    {
        var portBinding = hostPort == 0 ? "127.0.0.1::9000" : $"127.0.0.1:{hostPort}:9000";
        await DockerAsync(
            "run", "-d", "--name", containerName,
            "-e", $"MINIO_ROOT_USER={rootAccessKey}",
            "-e", $"MINIO_ROOT_PASSWORD={rootSecretKey}",
            "-v", $"{volumeName}:/data",
            "-p", portBinding,
            Image, "server", "/data", "--console-address", ":9001").ConfigureAwait(false);
        var mapping = (await DockerAsync("port", containerName, "9000/tcp").ConfigureAwait(false)).Trim();
        hostPort = int.Parse(mapping[(mapping.LastIndexOf(':') + 1)..], System.Globalization.CultureInfo.InvariantCulture);
        ServiceUrl = new Uri($"http://127.0.0.1:{hostPort}", UriKind.Absolute);

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using var response = await http.GetAsync(new Uri(ServiceUrl, "/minio/health/ready")).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }

            await Task.Delay(250).ConfigureAwait(false);
        }

        throw new TimeoutException("MINIO_FIXTURE_NOT_READY");
    }

    private async Task CreateInitialBucketWhenUsableAsync(string bucketName)
    {
        using var root = NewRootClient();
        await ExecuteStartupOperationAsync(
            () => root.PutBucketAsync(new PutBucketRequest { BucketName = bucketName }),
            ProtocolUsabilityTimeout,
            ProtocolUsabilityRetryDelay).ConfigureAwait(false);
        await ExecuteStartupOperationAsync(
            () => root.PutBucketPolicyAsync(new PutBucketPolicyRequest
            {
                BucketName = bucketName,
                Policy = BucketDenyTaggingPolicy(bucketName),
            }),
            ProtocolUsabilityTimeout,
            ProtocolUsabilityRetryDelay).ConfigureAwait(false);
        buckets.Add(bucketName);
    }

    private async Task ProveStartupRetryPolicyAsync()
    {
        var recoverableAttempts = 0;
        await ExecuteStartupOperationAsync(
            () =>
            {
                recoverableAttempts++;
                if (recoverableAttempts == 1)
                    throw new AmazonS3Exception(StartupNotInitializedMessage);
                return Task.CompletedTask;
            },
            ProtocolUsabilityTimeout,
            TimeSpan.Zero).ConfigureAwait(false);
        if (recoverableAttempts != 2)
            throw new InvalidOperationException("MINIO_STARTUP_S1_NOT_DISCRIMINATING");

        var persistentAttempts = 0;
        try
        {
            await ExecuteStartupOperationAsync(
                () =>
                {
                    persistentAttempts++;
                    throw new AmazonS3Exception(StartupNotInitializedMessage);
                },
                TimeSpan.FromMilliseconds(10),
                TimeSpan.Zero).ConfigureAwait(false);
            throw new InvalidOperationException("MINIO_STARTUP_S2_DID_NOT_FAIL_CLOSED");
        }
        catch (TimeoutException)
        {
            if (persistentAttempts <= 1)
                throw new InvalidOperationException("MINIO_STARTUP_S2_NOT_DISCRIMINATING");
        }

        var nonStartupAttempts = 0;
        var accessDenied = new AmazonS3Exception("Access Denied")
        {
            ErrorCode = "AccessDenied",
            StatusCode = HttpStatusCode.Forbidden,
        };
        try
        {
            await ExecuteStartupOperationAsync(
                () =>
                {
                    nonStartupAttempts++;
                    throw accessDenied;
                },
                ProtocolUsabilityTimeout,
                ProtocolUsabilityRetryDelay).ConfigureAwait(false);
            throw new InvalidOperationException("MINIO_STARTUP_S3_DID_NOT_FAIL_CLOSED");
        }
        catch (AmazonS3Exception exception) when (ReferenceEquals(exception, accessDenied))
        {
            if (nonStartupAttempts != 1)
                throw new InvalidOperationException("MINIO_STARTUP_S3_RETRIED_NON_STARTUP_FAILURE");
        }
    }

    private static async Task ExecuteStartupOperationAsync(
        Func<Task> operation,
        TimeSpan timeout,
        TimeSpan retryDelay)
    {
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                await operation().ConfigureAwait(false);
                return;
            }
            catch (AmazonS3Exception exception) when (IsStartupNotInitialized(exception))
            {
                if (stopwatch.Elapsed >= timeout)
                    throw new TimeoutException("MINIO_FIXTURE_PROTOCOL_NOT_USABLE", exception);
            }

            if (retryDelay > TimeSpan.Zero)
                await Task.Delay(retryDelay).ConfigureAwait(false);
            else
                await Task.Yield();
        }
    }

    private static bool IsStartupNotInitialized(AmazonS3Exception exception) =>
        string.Equals(exception.Message, StartupNotInitializedMessage, StringComparison.Ordinal);

    private async Task CreateBucketAsync(string bucketName, bool objectLockEnabled = false)
    {
        using var root = NewRootClient();
        await root.PutBucketAsync(new PutBucketRequest
        {
            BucketName = bucketName,
            ObjectLockEnabledForBucket = objectLockEnabled,
        }).ConfigureAwait(false);
        await root.PutBucketPolicyAsync(new PutBucketPolicyRequest
        {
            BucketName = bucketName,
            Policy = BucketDenyTaggingPolicy(bucketName),
        }).ConfigureAwait(false);
        buckets.Add(bucketName);
    }

    private static string BucketDenyTaggingPolicy(string bucketName) => $$"""
        {"Version":"2012-10-17","Statement":[{"Effect":"Deny","Principal":{"AWS":["*"]},"Action":["s3:GetObjectTagging"],"Resource":["arn:aws:s3:::{{bucketName}}/{{ObjectPrefix}}*"]}]}
        """;

    private AmazonS3Client NewRootClient() => NewClient((rootAccessKey, rootSecretKey));

    private AmazonS3Client NewClient((string Access, string Secret) credential) => new(
        new BasicAWSCredentials(credential.Access, credential.Secret),
        new AmazonS3Config
        {
            ServiceURL = ServiceUrl.AbsoluteUri.TrimEnd('/'),
            ForcePathStyle = true,
            MaxErrorRetry = 0,
            AuthenticationRegion = "us-east-1",
        });

    public async ValueTask DisposeAsync()
    {
        try
        {
            cleanupAdmin ??= NewClient(cleanupCredential);
            foreach (var bucketName in buckets.AsEnumerable().Reverse())
            {
                var listed = await cleanupAdmin.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucketName }).ConfigureAwait(false);
                foreach (var item in listed.S3Objects)
                    await cleanupAdmin.DeleteObjectAsync(bucketName, item.Key).ConfigureAwait(false);
                await cleanupAdmin.DeleteBucketAsync(bucketName).ConfigureAwait(false);
            }
            await RemoveCapabilityIdentitiesAsync().ConfigureAwait(false);
        }
        catch
        {
        }
        finally
        {
            cleanupAdmin?.Dispose();
            await CaptureContainerLogsAsync().ConfigureAwait(false);
            await DockerAsync("rm", "-f", containerName, allowFailure: true).ConfigureAwait(false);
            await DockerAsync("volume", "rm", volumeName, allowFailure: true).ConfigureAwait(false);
        }
    }

    private async Task CaptureContainerLogsAsync()
    {
        var directory = Environment.GetEnvironmentVariable("TAGEKYC_DURABLE_OBJECT_LOG_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory))
            return;

        Directory.CreateDirectory(directory);
        var startInfo = new ProcessStartInfo("docker")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("logs");
        startInfo.ArgumentList.Add("--timestamps");
        startInfo.ArgumentList.Add(containerName);
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("DOCKER_LOG_CAPTURE_START_FAILED");
        var standardOutput = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        var standardError = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);
        var path = Path.Combine(directory, $"{containerName}.log");
        await File.WriteAllTextAsync(
            path,
            $"exit={process.ExitCode}{Environment.NewLine}" +
            $"stdout:{Environment.NewLine}{standardOutput}{Environment.NewLine}" +
            $"stderr:{Environment.NewLine}{standardError}").ConfigureAwait(false);
    }

    private static async Task<string> DockerAsync(params string[] arguments) =>
        await DockerAsync(arguments, false).ConfigureAwait(false);

    private static async Task<string> DockerAsync(string[] arguments, bool allowFailure)
    {
        var startInfo = new ProcessStartInfo("docker")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("DOCKER_START_FAILED");
        var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        var error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);
        if (!allowFailure && process.ExitCode != 0)
            throw new InvalidOperationException($"DOCKER_EXIT_{process.ExitCode}: {error}");
        return output;
    }

    private static Task<string> DockerAsync(string command, string argument1, string argument2, bool allowFailure) =>
        DockerAsync([command, argument1, argument2], allowFailure);

    private async Task ProvisionCapabilityIdentitiesAsync()
    {
        var policyDirectory = Path.Combine(Path.GetTempPath(), $"tagekyc-dobj-policies-{Guid.NewGuid():N}");
        Directory.CreateDirectory(policyDirectory);
        try
        {
            var policies = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["writer"] = ObjectPolicy(BucketName, "s3:PutObject"),
                ["reconciler"] = ObjectPolicy(BucketName, "s3:GetObject"),
                ["lifecycle"] = ObjectPolicy(BucketName, "s3:DeleteObject"),
                ["posture"] = BucketPolicy(BucketName,
                    "s3:GetBucketVersioning", "s3:GetBucketObjectLockConfiguration",
                    "s3:GetLifecycleConfiguration", "s3:GetBucketPolicy"),
                ["cleanup"] = CombinedPolicy(BucketName,
                    ["s3:ListBucket", "s3:DeleteBucket", "s3:PutBucketVersioning",
                     "s3:PutLifecycleConfiguration", "s3:PutBucketPolicy"],
                    ["s3:DeleteObject"]),
            };
            capabilityPolicyDocuments[ProvisionalObjectCapability.Writer] = policies["writer"];
            capabilityPolicyDocuments[ProvisionalObjectCapability.Reconciler] = policies["reconciler"];
            capabilityPolicyDocuments[ProvisionalObjectCapability.Lifecycle] = policies["lifecycle"];
            capabilityPolicyDocuments[ProvisionalObjectCapability.PostureProbe] = policies["posture"];
            foreach (var policy in policies)
                await File.WriteAllTextAsync(Path.Combine(policyDirectory, $"{policy.Key}.json"), policy.Value)
                    .ConfigureAwait(false);

            var environment = new List<(string Name, string Value)>
            {
                ("MC_HOST_local", $"http://{rootAccessKey}:{rootSecretKey}@127.0.0.1:9000"),
            };
            var bindings = new[]
            {
                ("writer", credentials[ProvisionalObjectCapability.Writer]),
                ("reconciler", credentials[ProvisionalObjectCapability.Reconciler]),
                ("lifecycle", credentials[ProvisionalObjectCapability.Lifecycle]),
                ("posture", credentials[ProvisionalObjectCapability.PostureProbe]),
                ("cleanup", cleanupCredential),
            };
            foreach (var binding in bindings)
            {
                environment.Add(($"{binding.Item1.ToUpperInvariant()}_ACCESS", binding.Item2.Access));
                environment.Add(($"{binding.Item1.ToUpperInvariant()}_SECRET", binding.Item2.Secret));
            }

            var command = string.Join(" && ", bindings.Select(binding =>
                $"mc admin user add local \"${binding.Item1.ToUpperInvariant()}_ACCESS\" \"${binding.Item1.ToUpperInvariant()}_SECRET\""))
                + " && " + string.Join(" && ", bindings.Select(binding =>
                    $"mc admin policy create local tagekyc-dobj-{binding.Item1} /policies/{binding.Item1}.json"))
                + " && " + string.Join(" && ", bindings.Select(binding =>
                    $"mc admin policy attach local tagekyc-dobj-{binding.Item1} --user \"${binding.Item1.ToUpperInvariant()}_ACCESS\""));
            await RunMcAsync(policyDirectory, environment, command).ConfigureAwait(false);
        }
        finally
        {
            Directory.Delete(policyDirectory, recursive: true);
        }
    }

    private async Task ProvisionRecipientPackageIdentitiesAsync()
    {
        var policyDirectory = Path.Combine(Path.GetTempPath(), $"tagekyc-c2-policies-{Guid.NewGuid():N}");
        Directory.CreateDirectory(policyDirectory);
        try
        {
            var policies = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["writer"] = ObjectPolicyForPrefix(BucketName, RecipientPackagePrefix, "s3:PutObject"),
                ["reconciler"] = ObjectPolicyForPrefix(BucketName, RecipientPackagePrefix, "s3:GetObject"),
                ["lifecycle"] = ObjectPolicyForPrefix(BucketName, RecipientPackagePrefix, "s3:GetObject", "s3:DeleteObject"),
                ["delivery-reader"] = ObjectPolicyForPrefix(BucketName, RecipientPackagePrefix, "s3:GetObject"),
                ["posture"] = BucketPolicy(BucketName,
                    "s3:GetBucketVersioning", "s3:GetBucketObjectLockConfiguration",
                    "s3:GetLifecycleConfiguration", "s3:GetBucketPolicy"),
            };
            foreach (var policy in policies)
                await File.WriteAllTextAsync(Path.Combine(policyDirectory, $"{policy.Key}.json"), policy.Value)
                    .ConfigureAwait(false);

            var environment = new List<(string Name, string Value)>
            {
                ("MC_HOST_local", $"http://{rootAccessKey}:{rootSecretKey}@127.0.0.1:9000"),
            };
            foreach (var binding in recipientPackageCredentials)
            {
                environment.Add(($"C2_{binding.Key.ToUpperInvariant().Replace('-', '_')}_ACCESS", binding.Value.Access));
                environment.Add(($"C2_{binding.Key.ToUpperInvariant().Replace('-', '_')}_SECRET", binding.Value.Secret));
            }
            var command = string.Join(" && ", recipientPackageCredentials.Select(binding =>
                $"mc admin user add local \"$C2_{binding.Key.ToUpperInvariant().Replace('-', '_')}_ACCESS\" \"$C2_{binding.Key.ToUpperInvariant().Replace('-', '_')}_SECRET\""))
                + " && " + string.Join(" && ", policies.Keys.Select(name =>
                    $"mc admin policy create local tagekyc-c2-{name} /policies/{name}.json"))
                + " && " + string.Join(" && ", recipientPackageCredentials.Select(binding =>
                    $"mc admin policy attach local tagekyc-c2-{binding.Key} --user \"$C2_{binding.Key.ToUpperInvariant().Replace('-', '_')}_ACCESS\""));
            await RunMcAsync(policyDirectory, environment, command).ConfigureAwait(false);
        }
        finally
        {
            Directory.Delete(policyDirectory, recursive: true);
        }
    }

    private async Task ProvisionPostureScenarioIdentityAsync(string bucketName)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var posturePolicyName = $"tagekyc-dobj-posture-{suffix}";
        var cleanupPolicyName = $"tagekyc-dobj-cleanup-{suffix}";
        var credential = (Access: RandomCredential(16), Secret: RandomCredential(32));
        var policyDirectory = Path.Combine(Path.GetTempPath(), $"tagekyc-dobj-policies-{suffix}");
        Directory.CreateDirectory(policyDirectory);
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(policyDirectory, "posture.json"),
                BucketPolicy(bucketName,
                    "s3:GetBucketVersioning", "s3:GetBucketObjectLockConfiguration",
                    "s3:GetLifecycleConfiguration", "s3:GetBucketPolicy"))
                .ConfigureAwait(false);
            await File.WriteAllTextAsync(
                Path.Combine(policyDirectory, "cleanup.json"),
                CombinedPolicy(bucketName,
                    ["s3:ListBucket", "s3:DeleteBucket", "s3:PutBucketVersioning",
                     "s3:PutLifecycleConfiguration", "s3:PutBucketPolicy"],
                    ["s3:DeleteObject"]))
                .ConfigureAwait(false);
            var environment = new[]
            {
                ("MC_HOST_local", $"http://{rootAccessKey}:{rootSecretKey}@127.0.0.1:9000"),
                ("SCENARIO_ACCESS", credential.Access),
                ("SCENARIO_SECRET", credential.Secret),
                ("CLEANUP_ACCESS", cleanupCredential.Access),
            };
            var command =
                $"mc admin user add local \"$SCENARIO_ACCESS\" \"$SCENARIO_SECRET\"" +
                $" && mc admin policy create local {posturePolicyName} /policies/posture.json" +
                $" && mc admin policy attach local {posturePolicyName} --user \"$SCENARIO_ACCESS\"" +
                $" && mc admin policy create local {cleanupPolicyName} /policies/cleanup.json" +
                $" && mc admin policy attach local {cleanupPolicyName} --user \"$CLEANUP_ACCESS\"";
            await RunMcAsync(policyDirectory, environment, command).ConfigureAwait(false);
            postureScenarioCredentials[bucketName] =
                (credential.Access, credential.Secret, posturePolicyName);
            additionalPolicyNames.Add(cleanupPolicyName);
        }
        finally
        {
            Directory.Delete(policyDirectory, recursive: true);
        }
    }

    private async Task RemoveCapabilityIdentitiesAsync()
    {
        var environment = new List<(string Name, string Value)>
        {
            ("MC_HOST_local", $"http://{rootAccessKey}:{rootSecretKey}@127.0.0.1:9000"),
        };
        var bindings = new[]
        {
            ("writer", credentials[ProvisionalObjectCapability.Writer]),
            ("reconciler", credentials[ProvisionalObjectCapability.Reconciler]),
            ("lifecycle", credentials[ProvisionalObjectCapability.Lifecycle]),
            ("posture", credentials[ProvisionalObjectCapability.PostureProbe]),
            ("cleanup", cleanupCredential),
        };
        foreach (var binding in bindings)
            environment.Add(($"{binding.Item1.ToUpperInvariant()}_ACCESS", binding.Item2.Access));
        foreach (var binding in recipientPackageCredentials)
            environment.Add(($"C2_{binding.Key.ToUpperInvariant().Replace('-', '_')}_ACCESS", binding.Value.Access));
        var scenarioIndex = 0;
        foreach (var scenario in postureScenarioCredentials.Values)
        {
            environment.Add(($"SCENARIO_{scenarioIndex}_ACCESS", scenario.Access));
            scenarioIndex++;
        }
        var scenarioUsers = postureScenarioCredentials.Values.Select((_, index) =>
            $"mc admin user remove local \"$SCENARIO_{index}_ACCESS\"");
        var baseUsers = bindings.Select(binding =>
            $"mc admin user remove local \"${binding.Item1.ToUpperInvariant()}_ACCESS\"");
        var scenarioPolicies = postureScenarioCredentials.Values.Select(value => value.PolicyName)
            .Concat(additionalPolicyNames)
            .Select(name => $"mc admin policy remove local {name}");
        var basePolicies = bindings.Select(binding =>
            $"mc admin policy remove local tagekyc-dobj-{binding.Item1}");
        var c2Users = recipientPackageCredentials.Select(binding =>
            $"mc admin user remove local \"$C2_{binding.Key.ToUpperInvariant().Replace('-', '_')}_ACCESS\"");
        var c2Policies = recipientPackageCredentials.Keys.Select(name =>
            $"mc admin policy remove local tagekyc-c2-{name}");
        var command = string.Join(" ; ", scenarioUsers.Concat(baseUsers).Concat(c2Users)
            .Concat(scenarioPolicies).Concat(basePolicies).Concat(c2Policies));
        await RunMcAsync(null, environment, command, allowFailure: true).ConfigureAwait(false);
    }

    private async Task RunMcAsync(
        string? policyDirectory,
        IEnumerable<(string Name, string Value)> environment,
        string command,
        bool allowFailure = false)
    {
        var arguments = new List<string> { "run", "--rm", "--network", $"container:{containerName}" };
        foreach (var item in environment)
        {
            arguments.Add("-e");
            arguments.Add($"{item.Name}={item.Value}");
        }
        if (policyDirectory is not null)
        {
            arguments.Add("-v");
            arguments.Add($"{policyDirectory}:/policies:ro");
        }
        arguments.Add("--entrypoint");
        arguments.Add("/bin/sh");
        arguments.Add(McImage);
        arguments.Add("-c");
        arguments.Add(command);
        await DockerAsync(arguments.ToArray(), allowFailure).ConfigureAwait(false);
    }

    private static string ObjectPolicy(string bucketName, string action) => $$"""
        {"Version":"2012-10-17","Statement":[{"Effect":"Allow","Action":["{{action}}"],"Resource":["arn:aws:s3:::{{bucketName}}/{{ObjectPrefix}}*"]}]}
        """;

    private static string ObjectPolicyForPrefix(string bucketName, string prefix, params string[] actions) => $$"""
        {"Version":"2012-10-17","Statement":[{"Effect":"Allow","Action":[{{string.Join(',', actions.Select(action => $"\"{action}\""))}}],"Resource":["arn:aws:s3:::{{bucketName}}/{{prefix}}*"]}]}
        """;

    private static string BucketPolicy(string bucketName, params string[] actions) => $$"""
        {"Version":"2012-10-17","Statement":[{"Effect":"Allow","Action":[{{string.Join(',', actions.Select(action => $"\"{action}\""))}}],"Resource":["arn:aws:s3:::{{bucketName}}"]}]}
        """;

    private static string CombinedPolicy(string bucketName, string[] bucketActions, string[] objectActions) => $$"""
        {"Version":"2012-10-17","Statement":[{"Effect":"Allow","Action":[{{string.Join(',', bucketActions.Select(action => $"\"{action}\""))}}],"Resource":["arn:aws:s3:::{{bucketName}}"]},{"Effect":"Allow","Action":[{{string.Join(',', objectActions.Select(action => $"\"{action}\""))}}],"Resource":["arn:aws:s3:::{{bucketName}}/*"]}]}
        """;

    private static string RandomCredential(int bytes) => Convert.ToHexString(RandomNumberGenerator.GetBytes(bytes));
}
