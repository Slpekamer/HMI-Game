# Local wave-start checkpoints

The E integration uses one local game-specific directory: Godot `user://checkpoints/v1` (the actual OS path is platform-dependent). No account/cloud synchronization is provided.

## Player-facing behavior

- A new run first durably records a new run identity, then saves its prepared first wave before combat starts.
- After a wave's free choice and purchases, **Next Wave** saves the full prepared build, ledger and deterministic random state before enemies spawn.
- **Exit to selection** preserves that safe point. Continuing restores its wave start, discarding later progress in the interrupted wave, including HP, coins and kills.
- **Restart / new run**, official death, and **Finish this run** terminate the prior run. A backup is not a free revival.
- If a save cannot be confirmed, combat does not start. Retry uses the same transaction, not a second reward or purchase.
- Only a confirmed new identity with a failed first snapshot may explicitly continue **the whole run without saving**. Later waves in that run also do not save, and exiting cannot restore it. Later-wave failures in normally saved runs offer Retry only.
- Damaged/unknown authority is preserved and reported. There is currently no destructive reset button. Automatic deletion, arbitrary old-snapshot scanning and restoration of closed runs are deliberately unsupported.

Checksums detect accidental corruption; they are not an anti-cheat/security boundary against a client owner editing files. The catalog-byte hash and rules version must match. A later incompatible game version may reject these checkpoints; migration is not implemented.

## Reproduction and safety

All automated and native QA uses a fresh explicit `XDG_DATA_HOME` outside the normal game profile. Source the toolchain environment **before** assigning the isolated directory; invoking a wrapper that re-sources it can override isolation. Do not test destructive scenarios on a user's actual store.

```
source /path/to/godot-toolchain-verification/env.sh
export XDG_DATA_HOME="$(mktemp -d /tmp/hmi-checkpoint-play.XXXXXX)"
"$GODOT_NET" --path /path/to/project --audio-driver Dummy
```

Keep the same directory to test process restart/Continue. `Dummy` deliberately does not test audio. For automated checks use `scripts/verify.sh`; it isolates its data/config and uses invocation-scoped single-node MSBuild (`-m:1`) for this cloud environment.

The physical writer flushes and reads back a same-directory temporary file before atomic rename. Tests cover injected failures and killed child processes, not arbitrary hardware power loss, all mobile filesystems or malicious concurrent tools. No Android/phone or Unity checkpoint certification is implied.

Implementation/evidence: `docs/architecture/ADR-0007-prepared-wave-checkpoints.md`, `docs/checkpoint-transaction-proposal.md`, `docs/checkpoint-session-adapter.md`, `docs/evidence/E-*`. Final integration status belongs in the milestone report, not inferred from this behavioral specification.

### Failed terminal write limitation

If storage cannot accept a terminal Close, the current process blocks normal navigation and asks for Retry. If that process is forcibly terminated before any successful terminal write, no software-only mechanism can guarantee the old committed Active record was invalidated. The UI warns that an old safe point may remain. Do not describe this failure combination as guaranteed no-revival; successful Close and injected lost-ack recovery are separately tested.
