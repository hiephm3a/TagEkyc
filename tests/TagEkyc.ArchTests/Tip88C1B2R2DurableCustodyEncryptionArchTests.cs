using System.Reflection;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1B2R2DurableCustodyEncryptionArchTests
{
    [Fact]
    public void R2A1_r2_has_no_key_object_role_readiness_or_multipart_implementation()
    {
        foreach (var type in R2Types())
            Assert.False(type.IsPublic || type.IsNestedPublic, type.FullName);

        var production = string.Join('\n', R2ProductionFiles().Select(File.ReadAllText));
        foreach (var forbidden in new[]
        {
            "CREATE ROLE", "CREATE TABLE tagekyc.raw_export_attempt_key_",
            "CREATE TABLE tagekyc.raw_export_provisional_object_",
            "AmazonS3Client", "MinioClient", "CreateMultipartUpload",
            "UploadPart", "CompleteMultipartUpload", "AbortMultipartUpload",
            "RawExportDurableObjectReadiness", "RawExportDurableKeyReadiness",
            "AddTagEkycRawExportR2", "RawExportR2Controller", "MapRawExportR2",
        })
            Assert.DoesNotContain(forbidden, production, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void R2A2_writer_and_verifier_use_only_non_assignable_operation_scoped_capabilities()
    {
        Assert.False(typeof(IAttemptAeadEncryptionOperation)
            .IsAssignableFrom(typeof(IAttemptAeadVerificationOperation)));
        Assert.False(typeof(IAttemptAeadVerificationOperation)
            .IsAssignableFrom(typeof(IAttemptAeadEncryptionOperation)));
        Assert.Equal(
            typeof(Task<AttemptAeadVerificationResult>),
            typeof(IAttemptAeadVerificationOperation).GetMethod(
                nameof(IAttemptAeadVerificationOperation.DecryptAndVerifyBoundedChunkAsync))!.ReturnType);

        var writerParameters = PrimaryConstructorParameterTypes(typeof(RawExportR2EncryptionOrchestrator));
        Assert.Contains(typeof(IAttemptAeadEncryptionOperation), writerParameters);
        Assert.Contains(typeof(IProvisionalObjectWriter), writerParameters);
        Assert.DoesNotContain(typeof(IAttemptAeadVerificationOperation), writerParameters);
        Assert.DoesNotContain(typeof(IProvisionalObjectReconciler), writerParameters);

        var verifierParameters = PrimaryConstructorParameterTypes(typeof(RawExportR2CompletionVerifier));
        Assert.Contains(typeof(IAttemptAeadVerificationOperation), verifierParameters);
        Assert.Contains(typeof(IProvisionalObjectReconciler), verifierParameters);
        Assert.DoesNotContain(typeof(IAttemptAeadEncryptionOperation), verifierParameters);
        Assert.DoesNotContain(typeof(IProvisionalObjectWriter), verifierParameters);

        R2A2AssertNoDirectProviderConstruction(typeof(RawExportR2EncryptionOrchestrator));
        R2A2AssertNoDirectProviderConstruction(typeof(RawExportR2CompletionVerifier));
    }

    [Fact]
    public void R2A3_r2_contracts_and_logs_expose_no_raw_or_key_material()
    {
        var forbidden = new[]
        {
            "PlaintextDigest", "Dek", "WrappedKey", "NonceSeed",
            "Credential", "ProviderReceipt", "CiphertextBytes",
        };
        foreach (var result in new[] { typeof(RawExportR2WriterResult), typeof(RawExportR2VerifierResult) })
        {
            Assert.False(result.IsPublic || result.IsNestedPublic, result.FullName);
            foreach (var property in result.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                Assert.DoesNotContain(forbidden, value => property.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(forbidden, value => result.ToString()!.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        var source = string.Join('\n', R2ProductionFiles().Select(File.ReadAllText));
        foreach (var forbiddenText in new[]
        {
            "Convert.ToHexString(plaintext", "Convert.ToBase64String(plaintext",
            "ILogger<", "LogInformation(", "LogDebug(", "LogTrace(",
        })
            Assert.DoesNotContain(forbiddenText, source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void R2A4_no_production_raw_source_registration_or_runtime_activation_is_added()
    {
        var program = File.ReadAllText(ProjectPath("src/TagEkyc.Api/Program.cs"));
        Assert.DoesNotContain("RawExportR2", program, StringComparison.Ordinal);

        var r2Source = string.Join('\n', R2ProductionFiles().Select(File.ReadAllText));
        Assert.DoesNotContain("AddTagEkycRawExportR2", r2Source, StringComparison.Ordinal);
        Assert.DoesNotContain("RawSourceAdapter", r2Source, StringComparison.Ordinal);
        Assert.DoesNotContain("IServiceCollection", r2Source, StringComparison.Ordinal);

        // C6B's ratified broker/R1 -> R2 handoff supersedes B2-R2's assembly-wide
        // name ban. Only this closed metadata record is public, not an R2 provider/runtime.
        // Homeowner A1 continuation §13 authorizes reconciliation, not activation.
        var publicHandoff = Assert.Single(typeof(AttemptAeadChunkRequest).Assembly.GetTypes()
            .Where(type => type.Name.Contains("RawExportR2", StringComparison.Ordinal)));
        Assert.Equal(typeof(RawExportR2Handoff), publicHandoff);
        Assert.True(publicHandoff.IsSealed);
        Assert.Equal(new[]
        {
            ("AttemptId", typeof(Guid)),
            ("AttemptKeyReservationId", typeof(Guid)),
            ("ExpectedEncryptionAttemptRevision", typeof(long)),
            ("ExpectedFence", typeof(long)),
            ("SourceArtifactId", typeof(Guid)),
        }, publicHandoff.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .Select(property => (property.Name, property.PropertyType)).ToArray());
        Assert.DoesNotContain(R2Types(), type => type.Name.Contains("RawSourceAdapter", StringComparison.Ordinal));
    }

    private static Type[] R2Types() =>
    [
        typeof(RawExportR2EncryptionOrchestrator), typeof(RawExportR2CompletionVerifier),
        typeof(RawExportR2FramedCiphertextStream), typeof(RawExportR2FrameCodec),
        typeof(RawExportR2Repository), typeof(RawExportR2EncryptionRequest),
        typeof(RawExportR2WriterResult), typeof(RawExportR2VerifierResult),
    ];

    private static string[] R2ProductionFiles() =>
        Directory.GetFiles(
            ProjectPath("src/TagEkyc.Infrastructure/RawExport"),
            "RawExportR2*.cs",
            SearchOption.TopDirectoryOnly)
        .Append(ProjectPath(
            "src/TagEkyc.Infrastructure/Persistence/Migrations/20260807120000_Tip88C1B2R2DurableCustodyEncryption.cs"))
        .ToArray();

    private static Type[] PrimaryConstructorParameterTypes(Type type) =>
        type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single().GetParameters().Select(parameter => parameter.ParameterType).ToArray();

    private static void R2A2AssertNoDirectProviderConstruction(Type owner)
    {
        const BindingFlags declared = BindingFlags.Instance | BindingFlags.Static
            | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var methods = owner.GetMethods(declared);
        var execute = Assert.Single(methods, method => method.Name == "ExecuteAsync");
        var executeStateMachine = execute.GetCustomAttribute<
            System.Runtime.CompilerServices.AsyncStateMachineAttribute>();
        Assert.NotNull(executeStateMachine);
        var executeMoveNext = executeStateMachine.StateMachineType.GetMethod(
            "MoveNext",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(executeMoveNext);
        Assert.NotEmpty(executeMoveNext.GetMethodBody()?.GetILAsByteArray() ?? []);

        var executableBodies = new List<MethodBase>();
        executableBodies.AddRange(owner.GetConstructors(declared));
        foreach (var method in methods)
        {
            executableBodies.Add(method);
            var stateMachine = method.GetCustomAttribute<
                System.Runtime.CompilerServices.AsyncStateMachineAttribute>();
            if (stateMachine is null)
                continue;
            var moveNext = stateMachine.StateMachineType.GetMethod(
                "MoveNext",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(moveNext);
            Assert.NotEmpty(moveNext.GetMethodBody()?.GetILAsByteArray() ?? []);
            executableBodies.Add(moveNext);
        }

        var forbiddenContracts = new[]
        {
            typeof(IAttemptKeyReservationProvisioningOperation),
            typeof(IProvisionalObjectWriter),
            typeof(IProvisionalObjectReconciler),
        };
        foreach (var method in executableBodies)
        foreach (var constructedType in R2A2ResolvedConstructions(method))
        {
            var forbidden = forbiddenContracts.FirstOrDefault(
                contract => contract.IsAssignableFrom(constructedType));
            Assert.True(
                forbidden is null,
                $"R2A2_DIRECT_PROVIDER_CONSTRUCTION:{owner.FullName}:"
                + $"{method.DeclaringType?.FullName}.{method.Name}:"
                + $"{constructedType.FullName}:{forbidden?.FullName}");
        }
    }

    private static IReadOnlyList<Type> R2A2ResolvedConstructions(MethodBase method)
    {
        var body = method.GetMethodBody();
        Assert.NotNull(body);
        var il = body.GetILAsByteArray();
        Assert.NotNull(il);
        Assert.NotEmpty(il);
        var constructions = new List<Type>();
        var offset = 0;
        while (offset < il.Length)
        {
            var first = il[offset++];
            var value = first == 0xfe
                ? unchecked((short)(0xfe00 | il[offset++]))
                : (short)first;
            var opcode = R2A2OpCodes[value];
            var operandStart = offset;
            var operandSize = opcode.OperandType switch
            {
                System.Reflection.Emit.OperandType.InlineNone => 0,
                System.Reflection.Emit.OperandType.ShortInlineBrTarget
                    or System.Reflection.Emit.OperandType.ShortInlineI
                    or System.Reflection.Emit.OperandType.ShortInlineVar => 1,
                System.Reflection.Emit.OperandType.InlineVar => 2,
                System.Reflection.Emit.OperandType.InlineI
                    or System.Reflection.Emit.OperandType.InlineBrTarget
                    or System.Reflection.Emit.OperandType.InlineField
                    or System.Reflection.Emit.OperandType.InlineMethod
                    or System.Reflection.Emit.OperandType.InlineSig
                    or System.Reflection.Emit.OperandType.InlineString
                    or System.Reflection.Emit.OperandType.InlineType
                    or System.Reflection.Emit.OperandType.InlineTok
                    or System.Reflection.Emit.OperandType.ShortInlineR => 4,
                System.Reflection.Emit.OperandType.InlineI8
                    or System.Reflection.Emit.OperandType.InlineR => 8,
                System.Reflection.Emit.OperandType.InlineSwitch =>
                    checked(4 + BitConverter.ToInt32(il, offset) * 4),
                _ => throw new InvalidOperationException(
                    $"R2A2_UNSUPPORTED_IL_OPERAND:{opcode.OperandType}"),
            };
            offset = checked(offset + operandSize);
            if (opcode != System.Reflection.Emit.OpCodes.Newobj)
                continue;
            var token = BitConverter.ToInt32(il, operandStart);
            var constructor = method.Module.ResolveMethod(
                token,
                method.DeclaringType?.GetGenericArguments(),
                method.IsGenericMethod ? method.GetGenericArguments() : null)
                as ConstructorInfo;
            Assert.NotNull(constructor);
            Assert.NotNull(constructor.DeclaringType);
            constructions.Add(constructor.DeclaringType);
        }
        return constructions;
    }

    private static readonly IReadOnlyDictionary<short, System.Reflection.Emit.OpCode>
        R2A2OpCodes = typeof(System.Reflection.Emit.OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(System.Reflection.Emit.OpCode))
            .Select(field => (System.Reflection.Emit.OpCode)field.GetValue(null)!)
            .ToDictionary(opcode => opcode.Value);

    private static string ProjectPath(string relative) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../..", relative));
}
