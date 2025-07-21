class_name Util

static func get_deepest_path(path: String) -> String:
	var split_path: PackedStringArray = path.simplify_path().split("/")
	var deepest_path: String = split_path[0] + "/"
	split_path = split_path.slice(1)
	
	if not DirAccess.dir_exists_absolute(deepest_path) and not FileAccess.file_exists(deepest_path):
		return ""
	
	for part in split_path:
		var combined_path: String = deepest_path.path_join(part)
		if (DirAccess.dir_exists_absolute(combined_path) or FileAccess.file_exists(combined_path)):
			deepest_path = combined_path
		else:
			break
	
	return deepest_path

static func get_item_icon(id: int) -> AtlasTexture:
	var atlas: Texture2D = load("res://Graphics/Items.png")
	var atlas_texture = AtlasTexture.new()
	atlas_texture.atlas = atlas
	
	if (id < 0):
		atlas_texture.region = Rect2(0,0,0,0)
		return atlas_texture
	
	var icon_x: int = id % floor(atlas.get_width()) / 112
	@warning_ignore("integer_division")
	var icon_y: int = floor(id / floor(atlas.get_width() / 112))
	
	atlas_texture.region = Rect2(112 * icon_x, 112 * icon_y, 112, 112)
	return atlas_texture

static func dict_to_object(dict: Dictionary, obj: Object) -> Object:
	for prop in obj.get_property_list():
		if dict.has(prop.name):
			obj.set(prop.name, dict[prop.name])
	
	return obj
static func load_json_object_array(json_path: String, object_path: String, out_array: Array) -> void:
	var json_array: Array = JSON.parse_string(FileAccess.get_file_as_string(json_path))
	var object_script: GDScript = load(object_path)

	for i in json_array:
		var obj = object_script.new()
		for prop in obj.get_property_list():
			if i.has(prop.name):
				obj.set(prop.name, i[prop.name])
		out_array.append(obj)