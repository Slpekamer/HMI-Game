# Fresh import comparison: no proven mitigation

Eight sequential fresh project/cache/XDG_CONFIG_HOME imports, alternating strict and delay120; same isolated fixture; neither global nor live settings altered. Strict errors retained.

- strict: 4 runs; nonzero exits 0; logs with errors 0; median elapsed 10.960s; imported files [150, 150, 150, 150].
- delay120: 4 runs; nonzero exits 0; logs with errors 0; median elapsed 11.619s; imported files [150, 150, 150, 150].

No Android EditorSettings error reproduced in this sample. This does not erase previous failures or prove reliability. --quit-after120 counts total process iterations, not120frames after import; cold import may already exceed the threshold. Exact source guarantees import wait only when --import/--quit enables wait_for_import. No causal fix evidence, therefore keep formal runner strict --import unchanged.

Per-run commands and logs: compare.py, comparison.log, comparison-results.json, trial-*/import.log. No native display used.
