using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TagEkyc.Infrastructure.Persistence.Entities;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1B2SourceFinalizationArchTests
{
    [Fact]
    public void Finalization_surface_is_metadata_only_and_capability_separated()
    {
        var types=new[]{typeof(RawExportSourceFinalizationService),typeof(RawExportSourceCleanupReconciliationService),typeof(RawExportSourceCleanupService)};
        Assert.All(types,t=>Assert.DoesNotContain(t.GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public),f=>f.FieldType.Name.Contains("Aead")||f.FieldType.Name.Contains("Plaintext")));
        var forbidden=new[]{"AttemptAead","IAttemptAead","IKek","KekProvisioning","S3Compatible","Minio","IProvisionalObjectLifecycle","IProvisionalObjectReconciler"};
        var r4r5Types=new[]{typeof(RawExportSourceFinalizationService)}.Concat(typeof(RawExportSourceFinalizationService).GetNestedTypes(BindingFlags.NonPublic));
        var referenced=r4r5Types.SelectMany(t=>t.GetMethods(BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic))
            .SelectMany(ReferencedMembers).Select(x=>x.DeclaringType?.FullName??x.ToString()??string.Empty).ToArray();
        Assert.DoesNotContain(referenced,name=>forbidden.Any(token=>name.Contains(token,StringComparison.Ordinal)));
        var reconciliationReferences=ImplementationMethods(typeof(RawExportSourceCleanupReconciliationService)).SelectMany(ReferencedMembers).ToArray();
        Assert.DoesNotContain(reconciliationReferences,member=>member.DeclaringType==typeof(RawExportSourceCleanupService));
        Assert.DoesNotContain(reconciliationReferences,member=>member.DeclaringType==typeof(IProvisionalObjectLifecycle));
        Assert.DoesNotContain(reconciliationReferences,member=>member.DeclaringType==typeof(RawExportSourceFinalizationRepository)
            && member.Name is "CompleteAsync" or "FinalizeAsync" or "FinalizeKeyAbandonAsync" or "RevokeKeyAsync" or "RequestKeyAbandonAsync"
                or "ReadObjectLifecycleContextAsync" or "MarkObjectCleanupRequiredAsync" or "RecordObjectDeleteAcknowledgedAsync");
        var lifecycleReferences=ImplementationMethods(typeof(RawExportSourceCleanupService)).SelectMany(ReferencedMembers).ToArray();
        Assert.DoesNotContain(lifecycleReferences,member=>member.DeclaringType==typeof(IKekProvisioningRecoveryOperation)
            || member.DeclaringType==typeof(IProvisionalObjectReconciler));
        Assert.DoesNotContain(lifecycleReferences,member=>member.DeclaringType==typeof(RawExportSourceCleanupReconciliationService)
            && member.Name is "ReconcileObjectAsync" or "ReconcileKeyAsync");
        Assert.DoesNotContain(lifecycleReferences,member=>member.DeclaringType==typeof(RawExportSourceFinalizationRepository)
            && member.Name is "ReadObjectReconcileContextAsync" or "RecordObjectAbsenceConfirmedAsync" or "ReadKeyRecoveryContextAsync"
                or "ResolveKeyProviderOutcomeAsync" or "MarkKeyCleanupRequiredAsync" or "ObserveKeyCleanupAsync" or "AcknowledgeKeyCleanupAsync");
        var publication=typeof(RawExportSourcePublicationRow).GetProperties().Select(x=>x.Name).ToArray();
        Assert.DoesNotContain(publication,n=>n.Contains("ObjectKey")||n.Contains("Plaintext")||n.Contains("WrappedDek"));
    }

    private static IEnumerable<MethodInfo> ImplementationMethods(Type type)
    {
        var types=new[]{type}.Concat(type.GetNestedTypes(BindingFlags.Public|BindingFlags.NonPublic).SelectMany(SelfAndNested));
        foreach(var method in types.SelectMany(x=>x.GetMethods(BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)))
        {
            yield return method;
            var stateMachine=method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
            var moveNext=stateMachine?.GetMethod("MoveNext",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
            if(moveNext is not null) yield return moveNext;
        }
    }

    private static IEnumerable<Type> SelfAndNested(Type type)
    {
        yield return type;
        foreach(var nested in type.GetNestedTypes(BindingFlags.Public|BindingFlags.NonPublic).SelectMany(SelfAndNested)) yield return nested;
    }

    [Fact]
    public void Finalization_outcomes_are_closed_and_result_strings_redact()
    {
        Assert.Equal(new[]{"Committed","ExistingMatch","NotFound","SourceRetentionNotAuthorized","StateConflict"},Enum.GetNames<SourceCommitDisposition>().Order());
        var value=new SourceCommitResult(SourceCommitDisposition.Committed,Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),new byte[32],DateTimeOffset.UtcNow,2,1);
        Assert.Contains("<redacted>",value.ToString()); Assert.DoesNotContain(Convert.ToHexString(value.CommitEvidenceDigest!),value.ToString(),StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<MemberInfo> ReferencedMembers(MethodInfo method)
    {
        var body=method.GetMethodBody();if(body is null) yield break;
        var bytes=body.GetILAsByteArray()??[];var module=method.Module;var position=0;
        while(position<bytes.Length)
        {
            ushort value=bytes[position++];if(value==0xfe)value=(ushort)(0xfe00|bytes[position++]);
            if(!OpCodesByValue.TryGetValue(value,out var opcode)) yield break;
            var size=opcode.OperandType switch
            {
                OperandType.InlineNone=>0, OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar=>1,
                OperandType.InlineVar=>2, OperandType.InlineI8 or OperandType.InlineR=>8,
                OperandType.InlineSwitch=>-1, _=>4
            };
            if(size==-1){var count=BitConverter.ToInt32(bytes,position);position+=4+(count*4);continue;}
            if(opcode.OperandType is OperandType.InlineField or OperandType.InlineMethod or OperandType.InlineTok or OperandType.InlineType)
            {
                var token=BitConverter.ToInt32(bytes,position);
                MemberInfo? member=null;try{member=module.ResolveMember(token,method.DeclaringType?.GetGenericArguments(),method.GetGenericArguments());}catch(ArgumentException){}
                if(member is not null) yield return member;
            }
            position+=size;
        }
    }

    private static readonly IReadOnlyDictionary<ushort,OpCode> OpCodesByValue=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static)
        .Where(x=>x.FieldType==typeof(OpCode)).Select(x=>(OpCode)x.GetValue(null)!).ToDictionary(x=>unchecked((ushort)x.Value));
}
