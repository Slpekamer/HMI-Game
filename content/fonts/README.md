# Candy Sans subset

Renamed subset of installed Noto Sans CJK SC Regular, distributed under the included SIL Open Font License. Source: https://github.com/notofonts/noto-cjk. Rebuild with `python3 tools/art/subset_font.py` (fontTools and the system Noto TTC required). This is a build-time dependency, not a game dependency. Regenerate when UI/catalog text changes to retain glyph coverage.

C2 UI reconstruction preserves the prior1321-codepoint cmap and adds all missing current adapter/catalog characters. `glyph-coverage.json` records exact source/output hashes and coverage lists. Run `python3 tools/ui/validate_runtime_glyphs.py` to reject missing glyphs without relying on desktop system fallback. The original OFL notice remains verbatim. FontTools is an existing build-only dependency; regeneration may update font-table timestamps, so exact released bytes are identified by the manifest rather than a byte-reproducibility claim.
