# Explicit save upgrade and preserve-old new game

Rule 3 is a gameplay change, not a cosmetic replay-compatible patch. Existing rule 2 data is not silently interpreted as rule 3.

A selected valid rule 2 Active/RolledBack run can be upgraded only after explicit confirmation. The new rule resumes at the wave start, preserving the defined wallet/build/seed/RNG state rather than promising continuation at the old combat instant. A distinct run identity and target slot are used; original source bytes and a complete backup remain intact.

The separately confirmed preserve-old/start-new operation starts from the beginning. It is not recovery of an old Closed/PendingStart run. Rule 1 is not automatically chained through multiple upgrades; its supported option here is preserving the old files and starting a distinct new game.

Transactions use a fixed registry/source/target writer-lock order, validated staging, bounded storage and atomic selector publication. Lost acknowledgments inspect the durable current selection rather than replaying Begin. Cold retries revalidate the operation and return to character selection when a new game still requires fresh input.

Unknown temporary files, unproven orphans, invalid checksums, changed sources and ambiguous selectors fail closed. Only demonstrably owned uncommitted transaction files may be cleaned up. Selected authority, original source data and backups are not cleanup targets. Closed/PendingStart cleanup residues may be retained for a new-game operation only when complete ownership is established; unreferenced snapshots are never treated as authoritative recovery payload.

Validation has used synthetic saves and isolated test directories, including fault injection and real desktop confirmation/cancel flows. It is not certification against every historical user installation. Keep independent backups before evaluating this WIP.
