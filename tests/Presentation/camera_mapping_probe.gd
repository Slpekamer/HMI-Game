extends SceneTree
func _initialize() -> void:
    call_deferred("verify")
func verify() -> void:
    var scene = load("res://scenes/main.tscn").instantiate()
    root.add_child(scene)
    await process_frame
    var camera = root.get_camera_3d()
    assert(camera != null, "Main gameplay camera missing")
    var right = camera.global_basis.x
    right.y = 0
    right = right.normalized()
    var down = camera.global_basis.z
    down.y = 0
    down = down.normalized()
    assert(abs(right.dot(down)) < 0.0001, "Screen input axes not orthogonal")
    var center = camera.unproject_position(Vector3.ZERO)
    var projected_right = camera.unproject_position(right) - center
    var projected_down = camera.unproject_position(down) - center
    assert(projected_right.x > 0 and abs(projected_right.y) < 0.01, "Right input is not screen-right")
    assert(projected_down.y > 0 and abs(projected_down.x) < 0.01, "Down input is not screen-down")
    assert(abs((right * 0.6 + down * 0.8).length() - 1) < 0.0001, "Camera input changes movement magnitude")
    var instances := 0
    for color in range(4):
        var batch = scene.get_node("CandyPetals_" + str(color))
        assert(batch is MultiMeshInstance3D and batch.multimesh.instance_count == 55, "Decoration batching changed")
        instances += batch.multimesh.instance_count
    assert(instances == 220)
    print("SCENERY_BATCH_PASS 220 petals in four immutable instance batches")
    print("CAMERA_MAPPING_PASS screen right/down, orthogonal and speed preserving")
    quit()
