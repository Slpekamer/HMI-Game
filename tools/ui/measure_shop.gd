extends SceneTree
func _initialize(): call_deferred("run")
func run():
 var data=JSON.parse_string(FileAccess.get_file_as_string("res://content/gameplay.json"))
 var f=FontVariation.new();f.base_font=load("res://content/fonts/CandySans.otf");f.variation_embolden=.75
 for item in data.items:
  var l=Label.new();l.autowrap_mode=TextServer.AUTOWRAP_WORD_SMART;l.add_theme_font_override("font",f);l.add_theme_font_size_override("font_size",17);l.add_theme_constant_override("line_spacing",-3);root.add_child(l);l.size=Vector2(242,135);l.text=item.effect+"\n"+item.downside
  await process_frame
  print("SHOP_TEXT ",item.id," lines=",l.get_line_count()," lineheight=",l.get_line_height()," minimum=",l.get_minimum_size()," size=",l.size)
 quit()
