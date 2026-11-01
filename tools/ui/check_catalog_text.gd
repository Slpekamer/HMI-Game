extends SceneTree
# Actual engine font layout regression for every currently shipped card and item.
# This measures text geometry, not user visual acceptance or interaction.
var failures=[]
func _initialize(): call_deferred("run")
func run():
 var data=JSON.parse_string(FileAccess.get_file_as_string("res://content/gameplay.json"))
 var font=FontVariation.new();font.base_font=load("res://content/fonts/CandySans.otf");font.variation_embolden=.75
 var count=0
 for group in ["cards","items","temporaryContracts"]:
  for item in data.get(group,[]):
   var label=Label.new();label.autowrap_mode=TextServer.AUTOWRAP_WORD_SMART
   label.add_theme_font_override("font",font)
   label.add_theme_font_size_override("font_size",17 if group=="items" else 20)
   label.add_theme_constant_override("line_spacing",-3 if group=="items" else 0)
   root.add_child(label)
   var box=Vector2(242,135) if group=="items" else Vector2(278,190 if group=="temporaryContracts" else 150)
   label.size=box
   label.text=item.description if group=="temporaryContracts" else (item.effect if group=="cards" else item.effect+"\n"+item.downside)
   await process_frame
   if label.get_minimum_size().y>box.y or label.size.y>box.y:failures.append(item.id)
   print("TEXT_LAYOUT ",item.id," minimum=",label.get_minimum_size().y," available=",box.y)
   label.queue_free();count+=1
 print("CATALOG_TEXT_LAYOUT count=",count," failures=",failures)
 quit(0 if failures.size()==0 else 1)
