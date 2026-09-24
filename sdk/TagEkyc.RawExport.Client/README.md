# TagEkyc.RawExport.Client

Standalone SDK consumer for the TagEkyc encrypted raw-export package. This project has no dependency on SignFlow or on the TagEkyc server runtime.

```csharp
var options = new TagEkycRawExportClientOptions(
    new Uri("https://tagekyc.example.vn"),
    "X-API-Key",
    apiKey,
    policyId,
    policyVersion,
    recipientClientApplicationId,
    recipientKeyId,
    recipientKeyVersion,
    TimeSpan.FromMilliseconds(250),
    120);

var client = new TagEkycRawExportClient(httpClient, options, privateKeySource);
using var raw = await client.AcquireAsync(
    new TagEkycRawExportAcquisitionRequest(verificationSessionId, operationId),
    cancellationToken);

// The consuming application decides how to call its TSP.
await tsp.SendAsync(raw.ChipDg2Portrait, raw.LiveSelfieImage, cancellationToken);
```

`ITagEkycRecipientPrivateKeySource` is deliberately caller-owned so a deployment can use its existing protected configuration, HSM, KMS, or secret manager without adding those choices to this client.

Dispose both the private-key lease and returned biometric lease. Their owned byte arrays are zeroed on disposal.
