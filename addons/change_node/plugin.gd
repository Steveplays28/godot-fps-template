tool
extends EditorPlugin

var changed_node_paths:Array = []
var removed_nodes:Dictionary = {}

func _enter_tree():
	add_tool_menu_item("Change nodes", self, "show_file_dialog")

func _exit_tree():
	remove_tool_menu_item("Change nodes")

func show_file_dialog(args):
	var nodes = get_editor_interface().get_selection().get_selected_nodes()
	if nodes.size() <= 0:
		return

	var file_dialog:FileDialog = FileDialog.new()
	file_dialog.connect("file_selected", self, "file_dialog_file_selected")
	file_dialog.mode = FileDialog.MODE_OPEN_FILE
	file_dialog.access = FileDialog.ACCESS_RESOURCES
	file_dialog.filters = PoolStringArray(["*.tscn, *.scn ; Scene files","*.res ; Resource files"])

	add_child(file_dialog)
	file_dialog.popup_centered(OS.get_window_safe_area().size / 2)

func file_dialog_file_selected(path:String):
	changed_node_paths.append(path)

	var undo_redo:UndoRedo = get_undo_redo()
	undo_redo.create_action("Change nodes")
	undo_redo.add_do_method(self, "do_change_nodes")
	undo_redo.add_undo_method(self, "undo_change_nodes")
	undo_redo.commit_action()

func do_change_nodes():
	var selected_nodes = get_editor_interface().get_selection().get_selected_nodes()

	var new_node_path:String = changed_node_paths[changed_node_paths.size() - 1]
	var loaded_node = load(new_node_path)
	var instanced_node = loaded_node.instance()
	
	for selected_node in selected_nodes:
		if (selected_node is Spatial && instanced_node is Spatial):
			var selected_spatial:Spatial = selected_node as Spatial
			
			var copied_translation:Vector3 = selected_spatial.transform.origin
			var copied_rotation_degrees:Vector3 = selected_node.rotation_degrees
			var copied_scale:Vector3 = selected_node.scale
			
			selected_node.get_parent().add_child(instanced_node)
			instanced_node.owner = selected_node.get_parent()
			
			var new_spatial:Spatial = instanced_node as Spatial
			new_spatial.get_parent().move_child(new_spatial, selected_spatial.get_index())
			new_spatial.transform.origin = copied_translation
			new_spatial.rotation_degrees = copied_rotation_degrees
			new_spatial.scale = copied_scale
			
			removed_nodes[selected_node] = selected_node.get_parent()
			selected_node.get_parent().remove_child(selected_node)

func undo_change_nodes():
	if removed_nodes.size() > 0:
		var last_removed_node:Node = removed_nodes.keys()[removed_nodes.size() - 1]
		removed_nodes[last_removed_node].add_child(last_removed_node)
		last_removed_node.owner = last_removed_node.get_parent()
		removed_nodes.erase(last_removed_node)
