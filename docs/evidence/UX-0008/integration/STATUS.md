# Combined mobile R4 + UI motion R2 — ongoing

Root: /workspace/scratch/80f5bc988ef7/HMI-Game-mobile-ui-motion-integration-r1
Branch: feat/mobile-ui-motion-integration-20261101; base ae9ac5fe56b5926ac23ccf50d7686a4c4a89a93c.
No remote write / no APK / no mobile hardware claim. No OBS/VFX/audio mixed.

GameUi three-way merge preserved both input/navigation cancellation and cosmetic scopes. Instructions contain full generic gamepad guidance plus reduced-motion preference. New adapter tests are now in verify.sh (strict behavior unchanged).

Merged build passes 0 warnings/errors. A wrong-working-directory invocation was retained as setup failure. 42 authored Core/Persistence/content/model files match UX4 exactly (authority-model-hashes.json).

Targeted runners are being run once each. Their known editor shutdown ERROR is NOT ignored: each strict exit is preserved. Separate execution of the already built exact test project is labeled behavior-only, never a strict pass. Responsive lifecycle gets one merged-source attempt, not repeated green-seeking runs. Mobile worker owns only a new cross-feature test template/runner and independent copy, not production.

Prior accepted scoped evidence remains separately versioned: mobile Library libfile_1bc5235ecf8081918a8dc2718bc88a3f v3; UI motion libfile_e6472682fee8819189c4db22e13dc5d0 v3. These do not certify this merge. Existing intermittent ResponsiveUiProbe native teardown fatal remains unresolved.
