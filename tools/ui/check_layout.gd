extends SceneTree
var issues = []
func _initialize():
 call_deferred("run")
func walk(node):
 if node is BaseButton and node.is_visible_in_tree():
  var r=node.get_global_rect()
  if r.position.x < -1 or r.position.y < -1 or r.end.x>1281 or r.end.y>721:
   issues.append(str(node.text)+" "+str(r))
 for child in node.get_children(): walk(child)
func run():
 var scene=load("res://scenes/main.tscn").instantiate()
 root.add_child(scene)
 await process_frame
 await process_frame
 walk(scene)
 print("UI_LAYOUT_CHECK visible_buttons_outside_base=",issues)
 quit(0 if issues.size()==0 else 1)
