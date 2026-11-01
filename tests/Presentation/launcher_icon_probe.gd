extends SceneTree
func _initialize():
    var path = ProjectSettings.get_setting("application/config/icon", "")
    if path != "res://content/ui/launcher/kaotuan-face.png":
        push_error("Project launcher icon path mismatch")
        quit(1)
        return
    var texture = load(path)
    if not texture is Texture2D or texture.get_width() != 1254 or texture.get_height() != 1254:
        push_error("Project launcher texture did not import at original dimensions")
        quit(1)
        return
    var presets := ConfigFile.new()
    if presets.load("res://export_presets.cfg") != OK:
        push_error("Public export configuration missing")
        quit(1)
        return
    for field in ["main_192x192", "adaptive_foreground_432x432", "adaptive_background_432x432"]:
        var image_path = presets.get_value("preset.1.options", "launcher_icons/" + field, "")
        var image = load(image_path) as Texture2D
        var minimum := 192 if field == "main_192x192" else 432
        if image == null or image.get_width() < minimum or image.get_width() != image.get_height():
            push_error("Launcher source image invalid: " + field)
            quit(1)
            return
        print("LAUNCHER_SOURCE ", field, " ", image.get_width(), "x", image.get_height())
    print("LAUNCHER_ENGINE_PROBE PASS actual project icon import and configured source images; exported dpi/device separate")
    quit(0)
