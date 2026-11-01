extends SceneTree
# Actual imported runtime asset gate; not a visual/device-performance certification.
var failures := 0
func fail(message: String) -> void:
    push_error(message)
    failures += 1
func collect(node: Node, kind: String, result: Array) -> void:
    if node.is_class(kind): result.append(node)
    for child in node.get_children(): collect(child, kind, result)
func suffix_clip(player: AnimationPlayer, expected: String) -> String:
    var found := ""
    for name in player.get_animation_list():
        if str(name) == expected or str(name).ends_with("/" + expected):
            if not found.is_empty(): fail("Ambiguous animation " + expected)
            found = name
    return found
func poses(skeletons: Array) -> Dictionary:
    var result := {}
    for skeleton in skeletons:
        for bone in range(skeleton.get_bone_count()):
            result[str(skeleton.get_path()) + "/" + skeleton.get_bone_name(bone)] = skeleton.get_bone_pose(bone)
    return result
func _initialize() -> void:
    call_deferred("verify")
func verify() -> void:
    var heroes := ["summon", "orbit", "honey", "crit", "bounce"]
    for arg in OS.get_cmdline_user_args():
        if arg.begins_with("--hero="): heroes = [arg.trim_prefix("--hero=")]
    for hero in heroes:
        var path = "res://content/models/hero_" + hero + ".glb"
        if not ResourceLoader.exists(path):
            fail("Missing food hero " + path)
            continue
        var scene = load(path)
        var model = scene.instantiate()
        root.add_child(model)
        await process_frame
        var players: Array = []; var skeletons: Array = []; var meshes: Array = []
        collect(model, "AnimationPlayer", players); collect(model, "Skeleton3D", skeletons); collect(model, "MeshInstance3D", meshes)
        if players.size() != 1 or skeletons.is_empty():
            fail("Expected one player and skinned rig " + hero)
            model.free(); continue
        var player: AnimationPlayer = players[0]
        player.callback_mode_process = AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL
        var imported_root = model.transform
        if not imported_root.is_equal_approx(Transform3D.IDENTITY): fail("Non-identity root " + hero)
        var found_shapes := {}; var bounds: AABB; var first := true
        for mesh in meshes:
            if mesh.mesh == null: continue
            var box: AABB = mesh.global_transform * mesh.get_aabb()
            bounds = box if first else bounds.merge(box); first = false
            for name in ["Blink", "Smile", "Surprise"]:
                var index = mesh.find_blend_shape_by_name(name)
                if index >= 0:
                    found_shapes[name] = true
                    if abs(mesh.get_blend_shape_value(index)) > 0.00001: fail("Non-neutral default morph " + hero + name)
                    mesh.set_blend_shape_value(index, 1.0)
                    if abs(mesh.get_blend_shape_value(index) - 1.0) > 0.00001: fail("Unwritable imported morph " + hero + name)
                    mesh.set_blend_shape_value(index, 0.0)
        if found_shapes.size() != 3: fail("Missing imported morphs " + hero)
        # Imported PackedScene resources may be shared; expression values must remain per instance.
        var twin = scene.instantiate()
        root.add_child(twin)
        await process_frame
        var twin_meshes: Array = []
        collect(twin, "MeshInstance3D", twin_meshes)
        for mesh in meshes:
            var smile = mesh.find_blend_shape_by_name("Smile")
            if smile >= 0: mesh.set_blend_shape_value(smile, 0.6)
        for mesh in twin_meshes:
            for name in ["Blink", "Smile", "Surprise"]:
                var index = mesh.find_blend_shape_by_name(name)
                if index >= 0 and abs(mesh.get_blend_shape_value(index)) > 0.00001:
                    fail("Imported facial state leaked between instances " + hero + name)
        for mesh in meshes:
            var smile = mesh.find_blend_shape_by_name("Smile")
            if smile >= 0: mesh.set_blend_shape_value(smile, 0.0)
        twin.queue_free()
        await process_frame
        await process_frame
        var replacement = scene.instantiate()
        root.add_child(replacement)
        var replacement_meshes: Array = []
        collect(replacement, "MeshInstance3D", replacement_meshes)
        for mesh in replacement_meshes:
            for name in ["Blink", "Smile", "Surprise"]:
                var index = mesh.find_blend_shape_by_name(name)
                if index >= 0 and abs(mesh.get_blend_shape_value(index)) > 0.00001:
                    fail("Recreated imported instance retained facial state " + hero + name)
        replacement.free()
        print("FOOD_IMPORTED_INSTANCE_ISOLATION ", hero, " passed")

        if first or abs(bounds.position.y) > 0.025 or abs(bounds.end.y - 1.2) > 0.06:
            fail("Rest ground/height contract " + hero + " " + str(bounds))
        for expected in ["Idle", "Run", "Attack"]:
            var clip = suffix_clip(player, expected)
            if clip.is_empty(): fail("Missing " + hero + "/" + expected); continue
            var animation = player.get_animation(clip)
            animation.loop_mode = Animation.LOOP_NONE if expected == "Attack" else Animation.LOOP_LINEAR
            var first_poses := {}; var previous_poses := {}; var changed := {}
            var root_bones := {}
            for skeleton in skeletons:
                var index = skeleton.find_bone("Root")
                if index < 0: fail("Missing named Root bone " + hero)
                else: root_bones[str(skeleton.get_path())] = skeleton.get_bone_pose(index)
            for ratio in [0.0, 0.13, 0.37, 0.57, 0.83, 0.999]:
                player.play(clip); player.seek(animation.length * ratio, true); player.advance(0)
                var current = poses(skeletons)
                if first_poses.is_empty(): first_poses = current
                for key in current:
                    if not first_poses[key].is_equal_approx(current[key]): changed[key] = true
                if not model.transform.is_equal_approx(imported_root): fail("Model root drift " + hero + expected)
                for skeleton in skeletons:
                    var index = skeleton.find_bone("Root")
                    if index >= 0 and not skeleton.get_bone_pose(index).is_equal_approx(root_bones[str(skeleton.get_path())]):
                        fail("Inner Root bone drift " + hero + expected)
            if changed.size() < 2: fail("Insufficient actual bone articulation " + hero + expected)
            print("FOOD_IMPORTED_CLIP ", hero, " ", expected, " changed_bones=", changed.size())
        print("FOOD_IMPORTED_ASSET ", hero, " bounds=", bounds, " meshes=", meshes.size())
        model.free()
    print("FOOD_HERO_IMPORT_PROBE failures=", failures, " scope=", heroes)
    quit(0 if failures == 0 else 1)
