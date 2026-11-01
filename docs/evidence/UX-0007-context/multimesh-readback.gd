extends SceneTree
func _initialize():
 var mm=MultiMesh.new()
 mm.transform_format=MultiMesh.TRANSFORM_3D
 mm.instance_count=3
 var expected=Transform3D(Basis.IDENTITY.scaled(Vector3(.15,.105,.18)),Vector3(0,.13,2))
 mm.set_instance_transform(2,expected)
 print("INDEPENDENT_MM expected=",expected," observed=",mm.get_instance_transform(2)," display=",DisplayServer.get_name())
 quit()
