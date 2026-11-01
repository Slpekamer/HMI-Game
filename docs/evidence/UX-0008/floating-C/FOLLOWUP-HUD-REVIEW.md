# Deferred HUD readability review — 2026-11-01

Independent reviewer and parent accepted only C joystick drawing subtraction. Input capture, hit region, deadzone, provider routing and movement remain unchanged.

Observed in actual C frame 211 (controlled desktop, not Android):
- Lower companion panel occupies substantial horizontal combat space.
- Timer and upper HUD can still overlap the character's travel path.
- Light joystick outline is less strong on cream floor; physical finger occlusion and bright outdoor phone viewing have not been tested.

Do not silently merge HUD layout or gameplay changes into the joystick commit. Next authorized review should compare the same scene, hero positions and viewport sizes, preserve all partner commands/accessible labels and safe-area rules, and require net reduction in occlusion rather than moving the obstruction to another edge. No claim that the current HUD is fully unobstructed.

Current video semantics:
- A→C: same 18 injected touch events, fixed versus floating behavior; different MoveIntent/world paths are expected, so not authority-equal.
- B→C: same floating behavior, draw-only difference; actual event and authority traces are byte-identical.
- Both use actual desktop rendering in a controlled fixed test viewport. Neither establishes Android full-screen or physical controller compatibility.
