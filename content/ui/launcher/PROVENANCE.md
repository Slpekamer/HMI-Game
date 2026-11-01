# Kaotuan launcher icon

The owner authorized a minimal project-icon correction while retaining the existing new food-character identity. This is not a model, roster, portrait, or overall art redesign. Generated through imagegen from the actual approved Kaotuan reference/selection image. First generation failed safe-area composition; the second was reviewed at64/48px on cream, white and dark backgrounds.

`kaotuan-face.png` is the **unaltered1254×1254 RGBA** second output, SHA256213cc297853de5d4874bb94bd8e26bc8d9dede1dd9241291548484b5181e01c4. No code alpha cleanup, crop, resize or repaint. Insignificant isolated alpha1 pixels outside the safe circle remain documented; not called pixel-perfect. Opaque head fits the central Android safe circle. The official exporter—not a modified source image—produces platform dpi icons.

Original PNG Library: libfile_732cae93b37c81919f45d1fe85515451 v0. Source/prompts/rejected r1/three-background QA Library: libfile_3a00a34bddcc8191ad19b31d4b313387 v0.

`adaptive-background.svg` is an original code-native full-canvas #f4d8ab rectangle using the existing game cream palette. `tools/ui/build_launcher_background.gd` rasterizes it with Godot to432×432 RGBA PNG. This creates the background only and never edits the generated character. Android main/project/foreground share the same original PNG; adaptive background is explicitly supplied to avoid Godot's default branding. Monochrome/themed-icon design is not included.

This source gate does not establish all-device launcher rendering. Packaged dpi images/adaptive XML and representative mask rendering are separate export evidence. No signing key is part of this directory or public preset.
