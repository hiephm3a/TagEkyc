using System.Diagnostics;
using System.Security.Cryptography;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

internal sealed class DurableObjectMinioFixture : IAsyncDisposable
{
    internal const string Image = "minio/minio@sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e";
    private const string McImage = "minio/mc@sha256:eb4ea9884b77704230e2423e9004d2fa738dc272876b9cc41a297d29443b8780";
    private const string ObjectPrefix = "raw-export/c1/v1/";
    private readonly string containerName = $"tagekyc-durable-object-{Guid.NewGuid():N}";
    private readonly string volumeName = $"tagekyc-durable-object-{Guid.NewGuid():N}";
    private readonly string rootAccessKey = RandomCredential(16);
    private readonly string rootSecretKey = RandomCredential(32);
    private readonly Dictionary<ProvisionalObjectCapability, (string Access, string Secret)> credentials =
        Enum.GetValues<ProvisionalObjectCapability>()
            .ToDictionary(capability => capability, _ => (RandomCredential(16), RandomCredential(32)));
    private readonly (string Access, string Secret) cleanupCredential =
        (RandomCredential(16), RandomCredential(32));
    private readonly Dictionary<string, (string Access, string Secret, string PolicyName)>
        postureScenarioCredentials = new(StringComparer.Ordinal);
    private readonly Dictionary<ProvisionalObjectCapability, string> capabilityPolicyDocuments = [];
    private readonly List<string> additionalPolicyNames = [];
    private readonly List<string> buckets = [];
    private AmazonS3Client? cleanupAdmin;

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
            await fixture.CreateBucketAsync(fixture.BucketName).ConfigureAwait(false);
            await fixture.ProvisionCapabilityIdentitiesAsync().ConfigureAwait(false);
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

    internal async Task<string> CreateBucketAsync(bool objectLockEnabled = false)
    {
        var bucketName = $"tagekyc-{Guid.NewGuid():N}";
        await CreateBucketAsync(bucketName, objectLockEnabled).ConfigureAwait(false);
        await ProvisionPostureScenarioIdentityAsync(bucketName).ConfigureAwait(false);
        return bucketName;
    }

    private async Task StartContainerAsync()
    {
        await DockerAsync(
            "run", "-d", "--name", containerName,
            "-e", $"MINIO_ROOT_USER={rootAccessKey}",
            "-e", $"MINIO_ROOT_PASSWORD={rootSecretKey}",
            "-v", $"{volumeName}:/data",
            "-p", "127.0.0.1::9000",
            Image, "server", "/data", "--console-address", ":9001").ConfigureAwait(false);
        var mapping = (await DockerAsync("port", containerName, "9000/tcp").ConfigureAwait(false)).Trim();
        var port = int.Parse(mapping[(mapping.LastIndexOf(':') + 1)..], System.Globalization.CultureInfo.InvariantCulture);
        ServiceUrl = new Uri($"http://127.0.0.1:{port}", UriKind.Absolute);

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
            Policy = $$"""
                {"Version":"2012-10-17","Statement":[{"Effect":"Deny","Principal":{"AWS":["*"]},"Action":["s3:GetObjectTagging"],"Resource":["arn:aws:s3:::{{bucketName}}/{{ObjectPrefix}}*"]}]}
                """,
        }).ConfigureAwait(false);
        buckets.Add(bucketName);
    }

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
            await DockerAsync("rm", "-f", containerName, allowFailure: true).ConfigureAwait(false);
            await DockerAsync("volume", "rm", volumeName, allowFailure: true).ConfigureAwait(false);
        }
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
        var command = string.Join(" ; ", scenarioUsers.Concat(baseUsers).Concat(scenarioPolicies).Concat(basePolicies));
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

    private static string BucketPolicy(string bucketName, params string[] actions) => $$"""
        {"Version":"2012-10-17","Statement":[{"Effect":"Allow","Action":[{{string.Join(',', actions.Select(action => $"\"{action}\""))}}],"Resource":["arn:aws:s3:::{{bucketName}}"]}]}
        """;

    private static string CombinedPolicy(string bucketName, string[] bucketActions, string[] objectActions) => $$"""
        {"Version":"2012-10-17","Statement":[{"Effect":"Allow","Action":[{{string.Join(',', bucketActions.Select(action => $"\"{action}\""))}}],"Resource":["arn:aws:s3:::{{bucketName}}"]},{"Effect":"Allow","Action":[{{string.Join(',', objectActions.Select(action => $"\"{action}\""))}}],"Resource":["arn:aws:s3:::{{bucketName}}/*"]}]}
        """;

    private static string RandomCredential(int bytes) => Convert.ToHexString(RandomNumberGenerator.GetBytes(bytes));
}
