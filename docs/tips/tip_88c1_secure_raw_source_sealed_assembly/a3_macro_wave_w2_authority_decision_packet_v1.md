# A3 macro-wave W2 — authority decision packet v1

**Date:** 2026-09-20  
**Scope:** exact 14 rows formerly assigned to `W2_AUTHORITY_DECISION`  
**Decision:** `WIRE_PRODUCT_REQUIRED_BY_AUTHORITY` for all four families  
**Row closure:** none; official census remains **51 = 50 normative + 1 seam**  
**A3:** HOLD

## 1. One decision, four implementation families

| Family | Rows | Authority conclusion | Group disposition |
| --- | ---: | --- | --- |
| Provider-unknown semantics | BP17 | The broker contract already fixes the cause precedence: complete durable inspection → O21; incomplete/unknown → O18; never invent O19. | Implement/prove the existing rule; do not seek a new semantic choice. |
| Missing ingress producers | P03–P05 | Planning Brief §10.0 and composition dispatch v0.6 define exact phases, residue and retry rules. Constants/mappers without producers are incomplete implementation, not surplus ledger rows. | Wire the three production origins and prove them as one pre-body admission family. |
| Phase reachability | P12–P13 | Planning Brief §10.0 assigns both outcomes to P3. Earlier denial/lock ordering that makes them unreachable is a product reachability gap. | Correct ordering/reachability and prove both through one claim-state family. |
| Assembly shipping path | P29–P36 | Planning Brief §10.2 priorities 3, 6–10, 13 and 17 plus composition dispatch O29–O36 explicitly require these outcomes. Current fixture-only topology cannot satisfy the ratified contract. | Add a production-qualified assembly command/caller and exact outcome adapters; reuse existing component evidence. |

No row is reclassified away, and none remains blocked merely because current product code lacks the path. The detailed 14-row audit is `a3_macro_wave_w2_authority_audit.tsv`.

## 2. Why this does not overrule safety boundaries

- Production activation remains forbidden. Wiring must stay fail-closed until real owner/provider/role configuration is supplied.
- Existing synthetic providers may remain test prerequisites only; they cannot become production defaults.
- The aggregate NO-EGRESS proof for O29–O36 remains valid but does not close their internal producer/residue rows.
- O03–O05 are implemented at their ratified ingress phases, not by injecting synthetic `Final` values.
- P12/P13 must become reachable through the production claim-state graph; direct SQL calls are only component controls.
- Provider unknown is evidence of uncertainty, never positive absence and never fabricated content mismatch.

## 3. Execution handoff

The 14 rows move together to `W2_AUTHORIZED_IMPLEMENTATION`, retaining four scenario families. Execution is horizontal:

1. finish all product/test design for the four families;
2. build one candidate;
3. run positive controls by family;
4. mutate shared production guards, including affected ratified sentinels;
5. restore exact bytes and rebuild once;
6. run one joined restored gate;
7. freeze one manifest, failed-run census and review packet.

No per-row review packet or per-row ratification request is allowed. A row may close only on its own exact phase/residue proof, but the handoff remains one W2 implementation packet.
