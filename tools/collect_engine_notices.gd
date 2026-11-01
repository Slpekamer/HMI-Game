extends SceneTree
func _initialize():
    var args=OS.get_cmdline_user_args()
    var output=args[0]
    var f=FileAccess.open(output+"/Godot-LICENSE.txt",FileAccess.WRITE)
    f.store_string(Engine.get_license_text())
    f.close()
    f=FileAccess.open(output+"/Godot-THIRD-PARTY.json",FileAccess.WRITE)
    f.store_string(JSON.stringify({"engine":Engine.get_version_info(),"copyright":Engine.get_copyright_info(),"licenses":Engine.get_license_info()},"  "))
    f.close()
    print("LICENSE_EXPORT_OK")
    quit()
