using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.Application.CaptureRuntime;

public sealed class CaptureRuntimeRotationApplicationService(
    ICaptureRuntimeRotationCompletionAuthenticator authenticator,
    ICaptureRuntimeRotationGateway gateway,
    TimeProvider? timeProvider = null) : ICaptureRuntimeRotationService
{
    public async Task<SessionOperationResult<CaptureRuntimeRotationCompletionResponse>> CompleteRotationAsync(
        CaptureRuntimeSignedRequest signedRequest, Guid rotationId, CaptureRuntimeRotationCompleteRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signedRequest);
        ArgumentNullException.ThrowIfNull(request);
        if (!IsUuidV4(rotationId) || !IsUuidV4(idempotencyKey) || !IsUuidV4(request.CandidateKeyId) ||
            !IsUuidV4(signedRequest.CredentialId) || signedRequest.CredentialGeneration < 1 ||
            exactRequestBody.IsEmpty || exactRequestBody.Length > 16384 ||
            !Decode(request.SuccessorPublicVerifierSpki,91,out var spki) ||
            !Hex(request.SuccessorPublicKeyThumbprint,out var thumbprint) ||
            !Decode(request.SuccessorProof,64,out var proof))
            return Failure(CaptureRuntimeErrorCodes.RequestInvalid,400);

        // Both pure digests exist before the first authenticator call. No database
        // identity, role or target-existence result influences candidate construction.
        var candidates=CreateCandidates(signedRequest.CredentialId,signedRequest.CredentialGeneration,
            rotationId,idempotencyKey,exactRequestBody);
        byte[] selected=[];
        byte[] rotatePreimage=[];
        try
        {
            var authentication=await authenticator.AuthenticateRotationCompletionAsync(
                signedRequest,rotationId,request,candidates,cancellationToken).ConfigureAwait(false);
            if (!authentication.IsSuccess)
                return Failure(authentication.Error!.Code,authentication.Error.StatusCode);
            var result=authentication.Value!;
            var expected=result.Branch switch
            {
                CaptureRuntimeRotationBranch.Predecessor=>candidates.AsPredecessor,
                CaptureRuntimeRotationBranch.Successor=>candidates.AsSuccessor,
                _=>null
            };
            if (expected is null || result.SelectedRequestFingerprint is not {Length:32} ||
                !CryptographicOperations.FixedTimeEquals(expected,result.SelectedRequestFingerprint) ||
                result.Actor.CredentialId!=signedRequest.CredentialId ||
                result.Actor.CredentialGeneration!=signedRequest.CredentialGeneration ||
                result.Actor.CaptureAgentId==Guid.Empty || result.Actor.DeviceInstallationId==Guid.Empty)
                return Failure(CaptureRuntimeErrorCodes.NotReady,503);

            var predecessor=result.Branch==CaptureRuntimeRotationBranch.Predecessor
                ? signedRequest.CredentialGeneration : signedRequest.CredentialGeneration-1;
            rotatePreimage=Encoding.UTF8.GetBytes(string.Join('\n',
                "TAG-EKYC-ROTATE1",rotationId.ToString("N"),result.Actor.CaptureAgentId.ToString("N"),
                result.Actor.DeviceInstallationId.ToString("N"),signedRequest.CredentialId.ToString("N"),
                predecessor.ToString(CultureInfo.InvariantCulture),request.CandidateKeyId.ToString("N"),
                request.SuccessorPublicVerifierSpki,request.SuccessorPublicKeyThumbprint,idempotencyKey.ToString("N"))+"\n");
            if (!Verify(spki,thumbprint,rotatePreimage,proof))
                return Failure(CaptureRuntimeErrorCodes.AccessDenied,403);
            selected=result.SelectedRequestFingerprint;
            var command=new CaptureRuntimeRotationCommand(rotationId,signedRequest.CredentialId,
                signedRequest.CredentialGeneration,request.CandidateKeyId,idempotencyKey,spki,thumbprint,proof,
                selected,(timeProvider??TimeProvider.System).GetUtcNow());
            return result.Branch==CaptureRuntimeRotationBranch.Predecessor
                ? await gateway.CompleteRotationAsync(command,cancellationToken).ConfigureAwait(false)
                : await gateway.ReplayCompletedRotationAsync(command,cancellationToken).ConfigureAwait(false);
        }
        catch(CryptographicException) { return Failure(CaptureRuntimeErrorCodes.AccessDenied,403); }
        finally
        {
            foreach(var bytes in new[] {spki,thumbprint,proof,selected,rotatePreimage,candidates.AsPredecessor,candidates.AsSuccessor})
                if(bytes is not null) CryptographicOperations.ZeroMemory(bytes);
        }
    }

    private static CaptureRuntimeRotationFingerprintCandidates CreateCandidates(
        Guid credential,long generation,Guid rotation,Guid idempotencyKey,ReadOnlyMemory<byte> body)
    {
        // Same R13 domain and route on both branches; Generation is a pair, not
        // merely the generation presenting the current CRT1 envelope.
        var route=$"/api/ekyc/capture-runtime/credential-rotations/{rotation:N}/complete";
        byte[] Compute(long predecessor,long successor)
        {
            Span<byte> partition=stackalloc byte[32];
            credential.TryWriteBytes(partition,bigEndian:true,out _);
            BinaryPrimitives.WriteInt64BigEndian(partition[16..24],predecessor);
            BinaryPrimitives.WriteInt64BigEndian(partition[24..32],successor);
            return CaptureRuntimeHttpFingerprint.Compute("R13",route,body,idempotencyKey,partition);
        }
        return new(generation<long.MaxValue?Compute(generation,generation+1):null,
            generation>1?Compute(generation-1,generation):null);
    }
    private static bool Verify(byte[] spki,byte[] thumb,byte[] preimage,byte[] proof)
    {
        if(!CryptographicOperations.FixedTimeEquals(SHA256.HashData(spki),thumb)) return false;
        using var verifier=ECDsa.Create();
        verifier.ImportSubjectPublicKeyInfo(spki,out var consumed);
        return consumed==91 && verifier.KeySize==256 &&
            verifier.ExportParameters(false).Curve.Oid.Value=="1.2.840.10045.3.1.7" &&
            verifier.VerifyData(preimage,proof,HashAlgorithmName.SHA256,DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }
    private static bool IsUuidV4(Guid value)
    {
        var text=value.ToString("N");
        return text[12]=='4' && text[16] is '8' or '9' or 'a' or 'b';
    }
    private static bool Hex(string? text,out byte[] bytes)
    {
        bytes=[];
        if(text is not {Length:64} || text.Any(c=>c is not (>= '0' and <= '9') and not (>= 'a' and <= 'f'))) return false;
        bytes=Convert.FromHexString(text);return true;
    }
    private static bool Decode(string? text,int length,out byte[] bytes)
    {
        bytes=[];
        if(text is null || text.Length!=(length*8+5)/6 ||
            text.Any(c=>c is not (>= 'A' and <= 'Z') and not (>= 'a' and <= 'z') and
                not (>= '0' and <= '9') and not '-' and not '_')) return false;
        try
        {
            bytes=Convert.FromBase64String(text.Replace('-','+').Replace('_','/')+new string('=',(4-text.Length%4)%4));
            if(bytes.Length==length && Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_')==text) return true;
        }
        catch(FormatException) { }
        CryptographicOperations.ZeroMemory(bytes);bytes=[];return false;
    }
    private static SessionOperationResult<CaptureRuntimeRotationCompletionResponse> Failure(string code,int status)=>
        SessionOperationResult<CaptureRuntimeRotationCompletionResponse>.Failure(code,
            status==503?"Capture Runtime is not ready.":"Rotation request was denied.",status);
}
