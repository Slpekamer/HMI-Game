extends SceneTree
# Rasterize an original code-native solid UI background; never edit generated face pixels.
func _initialize():
    var image := Image.new()
    var svg := FileAccess.get_file_as_string("res://content/ui/launcher/adaptive-background.svg")
    var result := image.load_svg_from_string(svg)
    if result != OK:
        push_error("Launcher background SVG failed")
        quit(1)
        return
    image.convert(Image.FORMAT_RGBA8)
    if image.save_png("res://content/ui/launcher/adaptive-background.png") != OK:
        push_error("Launcher background PNG failed")
        quit(1)
        return
    print("LAUNCHER_BACKGROUND_RASTERIZED ", image.get_width(), "x", image.get_height())
    quit(0)
