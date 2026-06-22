using System.Diagnostics;
using System.Text.Json;
using TagEkyc.Application.VerificationSessions;

namespace TagEkyc.UnitTests;

public sealed class Tip65EvidenceCanonicalizationTests
{
    [Fact]
    public void Rfc8785_conformance_vectors_are_byte_for_byte()
    {
        const string input = """
            {
              "numbers": [333333333.33333329, 1E30, 4.50, 2e-3, 0.000000000000000000000000001],
              "string": "€$\u000f\nA'B\"\\\\\"/",
              "literals": [null, true, false]
            }
            """;
        const string expected = """{"literals":[null,true,false],"numbers":[333333333.3333333,1e+30,4.5,0.002,1e-27],"string":"€$\u000f\nA'B\"\\\\\"/"}""";

        Assert.Equal(expected, EvidenceCanonicalization.CanonicalizeJson(input));
        Assert.Equal("""{"small":0.00001}""", EvidenceCanonicalization.CanonicalizeJson("""{"small":1e-5}"""));
    }

    [Fact]
    public void Timestamp_format_is_full_precision_utc()
    {
        var timestamp = new DateTimeOffset(2026, 6, 22, 3, 4, 5, 123, TimeSpan.Zero).AddTicks(4567);

        Assert.Equal("2026-06-22T03:04:05.1234567Z", EvidenceCanonicalization.FormatTimestamp(timestamp));
    }

    [Fact]
    public void Deterministic_ids_have_jcs_golden_vectors()
    {
        var completedAt = "2026-06-22T03:04:05.1234567Z";
        var sessionId = "11111111111151118111111111111111";
        var evidenceId = "22222222222252228222222222222222";
        var decisionId = EvidenceCanonicalization.DeterministicGuid("tip-06-decision", new
        {
            SessionId = sessionId,
            EvidenceIds = new[] { evidenceId },
            Result = "Passed",
            AssuranceLevel = "Medium",
            ForceReview = false,
            RequestId = "req-golden",
            CorrelationId = "corr-golden",
            CompletedAt = completedAt,
        });
        var packageId = EvidenceCanonicalization.DeterministicGuid("tip-06-evidence-package", new
        {
            SessionId = sessionId,
            DecisionId = decisionId.ToString("N"),
        });
        var auditId = EvidenceCanonicalization.DeterministicGuid("tip-06-completion-audit", new
        {
            SessionId = sessionId,
            PackageId = packageId.ToString("N"),
        });

        Assert.Equal("b4cc7b99-3e97-58d4-8040-43af8e04f4c3", decisionId.ToString());
        Assert.Equal("a207cfcd-3f03-5b33-9100-a5fad588001a", packageId.ToString());
        Assert.Equal("996ac4c4-7be7-5465-b75b-b6b1d5caf3cb", auditId.ToString());
    }

    [Fact]
    public async Task Independent_node_jcs_reproduces_manifest_package_and_manifest_hashes()
    {
        var vectors = BuildGoldenHashVectors();
        var nodeHashes = await CalculateWithNodeAsync(vectors.NodePayload);

        Assert.Equal(vectors.ManifestBodyHash, nodeHashes["manifestBodyHash"]);
        Assert.Equal(vectors.PackageHash, nodeHashes["packageHash"]);
        Assert.Equal(vectors.ManifestHash, nodeHashes["manifestHash"]);
    }

    [Fact]
    public void Metadata_tuple_classification_accepts_current_and_legacy_and_rejects_unknown()
    {
        Assert.True(EvidenceCanonicalization.IsKnownHashMetadataCombination(
            EvidenceCanonicalization.PackageVersion,
            EvidenceCanonicalization.CanonicalizationScheme,
            EvidenceCanonicalization.HashAlgorithm));
        Assert.True(EvidenceCanonicalization.IsKnownHashMetadataCombination(
            EvidenceCanonicalization.LegacyPackageVersion,
            EvidenceCanonicalization.LegacyCanonicalizationScheme,
            EvidenceCanonicalization.HashAlgorithm));
        Assert.False(EvidenceCanonicalization.IsKnownHashMetadataCombination(
            "evidence-package-v99",
            EvidenceCanonicalization.CanonicalizationScheme,
            EvidenceCanonicalization.HashAlgorithm));
        Assert.False(EvidenceCanonicalization.IsKnownHashMetadataCombination(
            EvidenceCanonicalization.PackageVersion,
            "unknown-jcs",
            EvidenceCanonicalization.HashAlgorithm));
    }

    [Fact]
    public void Hashed_evidence_graphs_do_not_emit_raw_json_numbers()
    {
        const string completedAt = "2026-06-22T03:04:05.1234567Z";
        const string sessionId = "11111111111151118111111111111111";
        const string evidenceId = "22222222222252228222222222222222";
        const string decisionId = "b4cc7b993e9758d4804043af8e04f4c3";
        const string packageId = "a207cfcd3f035b339100a5fad588001a";
        const string auditId = "996ac4c47be75465b75bb6b1d5caf3cb";
        var decisionSeed = new
        {
            SessionId = sessionId,
            EvidenceIds = new[] { evidenceId },
            Result = "Passed",
            AssuranceLevel = "Medium",
            ForceReview = false,
            RequestId = "req-golden",
            CorrelationId = "corr-golden",
            CompletedAt = completedAt,
        };
        var packageIdSeed = new
        {
            SessionId = sessionId,
            DecisionId = decisionId,
        };
        var auditEventIdSeed = new
        {
            SessionId = sessionId,
            PackageId = packageId,
        };
        var completionAuditPayload = new
        {
            SessionId = sessionId,
            DecisionId = decisionId,
            EvidencePackageId = packageId,
            Result = "Passed",
            RequestId = "req-golden",
            CorrelationId = "corr-golden",
            CompletedAt = completedAt,
        };
        var artifactHashSet = new[] { "sha256:artifact" };
        var manifestBody = new
        {
            EvidencePackageId = packageId,
            VerificationSessionId = sessionId,
            PackageVersion = EvidenceCanonicalization.PackageVersion,
            CanonicalizationScheme = EvidenceCanonicalization.CanonicalizationScheme,
            HashAlgorithm = EvidenceCanonicalization.HashAlgorithm,
            EvidenceRefs = new[]
            {
                new
                {
                    Type = "CaptureQuality",
                    Id = evidenceId,
                    VaultRef = (string?)null,
                    ArtifactHash = "sha256:artifact",
                    PayloadHash = "sha256:payload",
                },
            },
            AuditEventRefs = new[]
            {
                new
                {
                    EventId = auditId,
                    EventType = "VERIFICATION_COMPLETED",
                    EventPayloadHash = "sha256:auditpayload",
                },
            },
            ResultRef = decisionId,
            Result = "Passed",
            AssuranceLevel = "Medium",
            RequestId = "req-golden",
            CorrelationId = "corr-golden",
            CreatedAt = completedAt,
        };
        var manifestBodyHash = "sha256:e8aa856e1cc6fd31f085f3ace99ef0ddafc6f8538dd0443def7d4bf91ccc96d4";
        var packageBody = new
        {
            EvidencePackageId = packageId,
            VerificationSessionId = sessionId,
            PackageVersion = EvidenceCanonicalization.PackageVersion,
            CanonicalizationScheme = EvidenceCanonicalization.CanonicalizationScheme,
            HashAlgorithm = EvidenceCanonicalization.HashAlgorithm,
            ManifestBodyHash = manifestBodyHash,
            ResultRef = decisionId,
            EvidenceRefs = new[] { evidenceId },
            Result = "Passed",
            AssuranceLevel = "Medium",
            CreatedAt = completedAt,
        };
        var packageHash = "sha256:0a3cbc9b031c2131822b676a5f40c11b478636e74704732fd558655833c122c0";
        var manifestHashSeed = new
        {
            BodyHash = manifestBodyHash,
            PackageHash = packageHash,
        };

        AssertCanonicalJsonHasNoNumbers(decisionSeed);
        AssertCanonicalJsonHasNoNumbers(packageIdSeed);
        AssertCanonicalJsonHasNoNumbers(auditEventIdSeed);
        AssertCanonicalJsonHasNoNumbers(completionAuditPayload);
        AssertCanonicalJsonHasNoNumbers(artifactHashSet);
        AssertCanonicalJsonHasNoNumbers(manifestBody);
        AssertCanonicalJsonHasNoNumbers(packageBody);
        AssertCanonicalJsonHasNoNumbers(manifestHashSeed);
    }

    [Fact]
    public void Future_fields_do_not_change_old_version_verification_shape()
    {
        var oldShape = new
        {
            EvidencePackageId = "pkg",
            VerificationSessionId = "session",
            PackageVersion = EvidenceCanonicalization.PackageVersion,
            CanonicalizationScheme = EvidenceCanonicalization.CanonicalizationScheme,
            HashAlgorithm = EvidenceCanonicalization.HashAlgorithm,
            ManifestBodyHash = "sha256:body",
            ResultRef = "decision",
            EvidenceRefs = new[] { "evidence-1" },
            Result = "Passed",
            AssuranceLevel = "Medium",
            CreatedAt = "2026-06-22T03:04:05.1234567Z",
        };
        var futureShape = new
        {
            oldShape.EvidencePackageId,
            oldShape.VerificationSessionId,
            oldShape.PackageVersion,
            oldShape.CanonicalizationScheme,
            oldShape.HashAlgorithm,
            oldShape.ManifestBodyHash,
            oldShape.ResultRef,
            oldShape.EvidenceRefs,
            oldShape.Result,
            oldShape.AssuranceLevel,
            oldShape.CreatedAt,
            LaterField = "must-not-enter-old-shape",
        };

        var oldHash = EvidenceCanonicalization.HashCanonical("tip-06-evidence-package", oldShape);
        var futureVerifiedAsOldHash = EvidenceCanonicalization.HashCanonical("tip-06-evidence-package", new
        {
            futureShape.EvidencePackageId,
            futureShape.VerificationSessionId,
            futureShape.PackageVersion,
            futureShape.CanonicalizationScheme,
            futureShape.HashAlgorithm,
            futureShape.ManifestBodyHash,
            futureShape.ResultRef,
            futureShape.EvidenceRefs,
            futureShape.Result,
            futureShape.AssuranceLevel,
            futureShape.CreatedAt,
        });

        Assert.Equal(oldHash, futureVerifiedAsOldHash);
    }

    private static GoldenHashVectors BuildGoldenHashVectors()
    {
        const string completedAt = "2026-06-22T03:04:05.1234567Z";
        const string sessionId = "11111111111151118111111111111111";
        const string evidenceId = "22222222222252228222222222222222";
        const string decisionId = "b4cc7b993e9758d4804043af8e04f4c3";
        const string packageId = "a207cfcd3f035b339100a5fad588001a";
        const string auditId = "996ac4c47be75465b75bb6b1d5caf3cb";
        var manifestBody = new
        {
            EvidencePackageId = packageId,
            VerificationSessionId = sessionId,
            PackageVersion = EvidenceCanonicalization.PackageVersion,
            CanonicalizationScheme = EvidenceCanonicalization.CanonicalizationScheme,
            HashAlgorithm = EvidenceCanonicalization.HashAlgorithm,
            EvidenceRefs = new[]
            {
                new
                {
                    Type = "CaptureQuality",
                    Id = evidenceId,
                    VaultRef = (string?)null,
                    ArtifactHash = "sha256:artifact",
                    PayloadHash = "sha256:payload",
                },
            },
            AuditEventRefs = new[]
            {
                new
                {
                    EventId = auditId,
                    EventType = "VERIFICATION_COMPLETED",
                    EventPayloadHash = "sha256:auditpayload",
                },
            },
            ResultRef = decisionId,
            Result = "Passed",
            AssuranceLevel = "Medium",
            RequestId = "req-golden",
            CorrelationId = "corr-golden",
            CreatedAt = completedAt,
        };
        var manifestBodyHash = EvidenceCanonicalization.HashCanonical("tip-06-manifest-body", manifestBody);
        var packageBody = new
        {
            EvidencePackageId = packageId,
            VerificationSessionId = sessionId,
            PackageVersion = EvidenceCanonicalization.PackageVersion,
            CanonicalizationScheme = EvidenceCanonicalization.CanonicalizationScheme,
            HashAlgorithm = EvidenceCanonicalization.HashAlgorithm,
            ManifestBodyHash = manifestBodyHash,
            ResultRef = decisionId,
            EvidenceRefs = new[] { evidenceId },
            Result = "Passed",
            AssuranceLevel = "Medium",
            CreatedAt = completedAt,
        };
        var packageHash = EvidenceCanonicalization.HashCanonical("tip-06-evidence-package", packageBody);
        var manifestHash = EvidenceCanonicalization.HashCanonical("tip-06-evidence-manifest", new
        {
            BodyHash = manifestBodyHash,
            PackageHash = packageHash,
        });
        var nodePayload = JsonSerializer.Serialize(new
        {
            ManifestBody = ToNodeObject(manifestBody),
            PackageBody = ToNodeObject(packageBody),
            ManifestHashBody = new Dictionary<string, object?>
            {
                ["bodyHash"] = manifestBodyHash,
                ["packageHash"] = packageHash,
            },
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal("sha256:e8aa856e1cc6fd31f085f3ace99ef0ddafc6f8538dd0443def7d4bf91ccc96d4", manifestBodyHash);
        Assert.Equal("sha256:0a3cbc9b031c2131822b676a5f40c11b478636e74704732fd558655833c122c0", packageHash);
        Assert.Equal("sha256:124c36f819308aa86cb5813f63ec11d7bdbc410cbd97b2d5e7f23d6bf60dc2af", manifestHash);

        return new GoldenHashVectors(manifestBodyHash, packageHash, manifestHash, nodePayload);
    }

    private static void AssertCanonicalJsonHasNoNumbers(object value)
    {
        using var document = JsonDocument.Parse(EvidenceCanonicalization.Canonicalize(value));

        AssertNoNumbers(document.RootElement, "$");
    }

    private static void AssertNoNumbers(JsonElement element, string path)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    AssertNoNumbers(property.Value, $"{path}.{property.Name}");
                }

                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    AssertNoNumbers(item, $"{path}[{index}]");
                    index++;
                }

                break;
            case JsonValueKind.Number:
                Assert.Fail($"Hashed evidence graph contains a raw JSON number at {path}.");
                break;
        }
    }

    private static object ToNodeObject(object value)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(json)!;
    }

    private static async Task<IReadOnlyDictionary<string, string>> CalculateWithNodeAsync(string payload)
    {
        const string script = """
            const crypto = require('crypto');
            let input = '';
            process.stdin.on('data', chunk => input += chunk);
            process.stdin.on('end', () => {
              const payload = JSON.parse(input);
              function sort(value) {
                if (Array.isArray(value)) return value.map(sort);
                if (value !== null && typeof value === 'object') {
                  const result = {};
                  for (const key of Object.keys(value).sort()) result[key] = sort(value[key]);
                  return result;
                }
                return value;
              }
              function canonicalize(value) {
                return JSON.stringify(sort(value));
              }
              function hash(label, value) {
                return 'sha256:' + crypto.createHash('sha256').update(label + '\n' + canonicalize(value), 'utf8').digest('hex');
              }
              process.stdout.write(JSON.stringify({
                manifestBodyHash: hash('tip-06-manifest-body', payload.manifestBody),
                packageHash: hash('tip-06-evidence-package', payload.packageBody),
                manifestHash: hash('tip-06-evidence-manifest', payload.manifestHashBody)
              }));
            });
            """;
        using var process = new Process();
        process.StartInfo.FileName = "node";
        process.StartInfo.ArgumentList.Add("-e");
        process.StartInfo.ArgumentList.Add(script);
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.Start();

        await process.StandardInput.WriteAsync(payload);
        process.StandardInput.Close();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        var exited = await Task.Run(() => process.WaitForExit(30_000));
        Assert.True(exited, "Node cross-verification timed out.");
        Assert.True(process.ExitCode == 0, error);

        return JsonSerializer.Deserialize<Dictionary<string, string>>(output)!;
    }

    private sealed record GoldenHashVectors(
        string ManifestBodyHash,
        string PackageHash,
        string ManifestHash,
        string NodePayload);
}
