# SMID50 V0.4 post-commit binding

Reviewed pre-commit physical/product manifest SHA-256: 750CEBDAC3CACA4D8C65E47295F8C27AAA61A58A9C0497E440D2F62E7E529DFB.

The reviewed manifest is preserved unchanged and explicitly uses WORKING_TREE_BYTES_PRE_COMMIT. It is not regenerated from post-commit Git status. This successor maps the exact reviewed checkout bytes to the two accepted commits and records both checkout SHA-256 and Git blob identity so LF/CRLF normalization is visible rather than misclassified as product drift.

| Repository | Commit | Parent | Paths |
|---|---|---|---:|
| Agent | 5eb4d900d8c6f9e4321c499b9c187fdc0fb33d30 | cbabf18584cfec82adce296398078d1901e77e97 | 29 |
| Server | a5b5e956ce59ec6f760532a1aa414da633071c52 | 6759b624c7e30d2e1fd9db47de26cb89f647513c | 5 |

- Exact committed path set: 34/34.
- Checkout bytes equal reviewed pre-commit manifest: 34/34.
- Files whose checkout SHA differs from normalized Git blob SHA because of EOL normalization: 21.
- Binding TSV SHA-256: 493357BA14FF81C9791EF5FC5A3BD38DF2302221A3945EBA7D89390DECFA0521.
- Agent commit closure: clean/staged/conflicted/untracked = 0/0/0/0 at independent review.
- Server commit closure: clean/staged/conflicted/untracked = 0/0/0/0 at independent review.
- Server durable local ref: codex/smid50-bac-evidence -> a5b5e956ce59ec6f760532a1aa414da633071c52.

No product file, physical manifest, physical result, test result, or reviewed evidence artifact is rewritten by this binding.