extends SceneTree
# Import-level articulation gate, not a replacement for native motion/video review.
var failures := 0

func player_in(node: Node) -> AnimationPlayer:
    if node is AnimationPlayer:
        return node
    for child in node.get_children():
        var result = player_in(child)
        if result != null:
            return result
    return null

func poses(node: Node, output: Dictionary) -> void:
    if node is Node3D:
        output[str(node.get_path())] = node.transform
    if node is Skeleton3D:
        for bone in range(node.get_bone_count()):
            output[str(node.get_path()) + "/bone/" + str(node.get_bone_name(bone))] = node.get_bone_pose(bone)
    for child in node.get_children():
        poses(child, output)

func fail(message: String) -> void:
    push_error(message)
    failures += 1

func _initialize() -> void:
    call_deferred("verify")

func verify() -> void:
    for id in ["hero_orbit", "hero_crit", "hero_honey", "hero_summon", "hero_bounce", "enemy_chaser", "companion_shooter", "companion_support"]:
        var path = "res://content/models/" + id + ".glb"
        if not ResourceLoader.exists(path):
            fail("Missing animated asset " + id)
            continue
        var model = load(path).instantiate()
        root.add_child(model)
        var player = player_in(model)
        if player == null:
            fail("No AnimationPlayer: " + id)
            model.free()
            continue
        for expected in ["Idle", "Run", "Attack"]:
            var clip := ""
            for candidate in player.get_animation_list():
                if candidate.to_lower() == expected.to_lower() or candidate.to_lower().ends_with("/" + expected.to_lower()):
                    clip = candidate
            if clip.is_empty():
                fail("Missing " + expected + ": " + id)
                continue
            var animation = player.get_animation(clip)
            # glTF has no loop flag. Match documented host clip policy after import.
            animation.loop_mode = Animation.LOOP_NONE if expected == "Attack" else Animation.LOOP_LINEAR
            if animation.length <= 0.1:
                fail("Clip too short: " + id + "/" + clip)
            if expected != "Attack" and animation.loop_mode == Animation.LOOP_NONE:
                fail("Locomotion/idle must loop: " + id + "/" + clip)
            if expected == "Attack" and animation.loop_mode != Animation.LOOP_NONE:
                fail("Attack must not loop: " + id)
            player.play(clip)
            player.seek(animation.length * 0.13, true)
            player.advance(0)
            var before := {}
            poses(model, before)
            var root_before = model.transform
            player.seek(animation.length * 0.57, true)
            player.advance(0)
            var after := {}
            poses(model, after)
            var changed := 0
            for key in before:
                if key != str(model.get_path()) and not before[key].is_equal_approx(after[key]):
                    changed += 1
            if changed < 2:
                fail("Needs at least two articulated nodes: " + id + "/" + clip)
            if not root_before.is_equal_approx(model.transform):
                fail("Model root motion forbidden: " + id + "/" + clip)
            print("ANIMATION_SAMPLE ", id, " ", clip, " articulated_nodes=", changed)
        model.free()
    print("ANIMATION_PROBE failures=", failures)
    quit(0 if failures == 0 else 1)
