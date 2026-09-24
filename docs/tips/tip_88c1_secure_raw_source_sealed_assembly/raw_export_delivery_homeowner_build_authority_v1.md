# Raw-export delivery — Homeowner build authority

**Status:** `APPROVED_FOR_IMPLEMENTATION_AND_PROOF`

**Date:** 2026-09-24

**Decision source:** the Homeowner's direct instruction to build the product, followed by the correction that governance must not block product work already requested.

## Authorized product surface

This record authorizes implementation and proof for the bounded raw-export delivery bridge required by the independent client:

- `POST /api/ekyc/raw-export/authorizations`;
- `POST /api/ekyc/raw-export/jobs`;
- `GET /api/ekyc/raw-export/jobs/{jobId}`;
- the application/contracts/projection/composition required only by those routes;
- the W2 assembly work-source, candidate-selection and durable actor-provenance path already covered by `WIRE_PRODUCT`.

The routes must reuse the existing B3 authorization repository, B4 job repository and C2/C3 package projection. This authority does not permit a second authorization engine, second job engine, SignFlow runtime coupling, TSP behavior, a new secret-store engine, raw-byte exposure, assembly topology activation, hospital deployment, stage, commit, push or production activation.

## Proof and governance boundary

Implementation may be tested with focused mutation and sentinel evidence. This record is not row ratification, does not change the activation census, does not re-mint the activation seal and does not mark P29-P36 implemented. Any backlog wording correction remains a separate governance transaction after technical review.
