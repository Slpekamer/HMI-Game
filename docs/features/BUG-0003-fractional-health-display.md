# Fractional health display rounding

A normal Buding run with risk.glass and the shield item produced authoritative81.25 maximum/current HP. UI used ceiling for current and integer truncation for maximum, incorrectly showing82/81. This was a presentation mismatch, not evidence that Core exceeded its health bound.

The isolated follow-up rounds both displayed values upward; positive sub-one HP remains visibly alive. The progress bar still receives raw fractional values; Core stats, damage, content, saves and camera are unchanged. Tested cases include full81.25, damaged80.1, positive0.1, zero and integer100 health. The actual-engine regression fails the precise text assertion before the one-expression fix and passes afterward. The whole-source aggregate completed exit0 and the controlled native81.25HP screenshot shows82/82 with an intact full bar. The screenshot is explicitly a fixture, not another natural-play result. Evidence is in `docs/evidence/BUG-0003`.

Decision: consistent integer ceiling, rather than adding decimal noise or flooring positive health to0. Reversible formatting only; owner morning review pending. Frozen E milestone remains unchanged.
