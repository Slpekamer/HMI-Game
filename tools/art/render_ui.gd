extends SceneTree
func _init():
 var args = OS.get_cmdline_user_args()
 assert(args.size() == 1, 'Pass UI directory after --')
 var path = args[0].trim_suffix('/') + '/'
 DirAccess.make_dir_recursive_absolute(path + 'png')
 for file in DirAccess.get_files_at(path):
  if file.ends_with('.svg'):
   var img = Image.new()
   assert(img.load_svg_from_string(FileAccess.get_file_as_string(path + file)) == OK)
   assert(img.save_png(path + 'png/' + file.replace('.svg', '.png')) == OK)
 quit()
