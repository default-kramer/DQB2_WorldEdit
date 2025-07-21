extends PopupMenu

@export var SubmenuIndex: int = -1

func _ready() -> void:
    if SubmenuIndex < 0:
        return
    
    var parent := get_parent()
    if parent is PopupMenu:
        parent.set_item_submenu_node(SubmenuIndex, self)
    if parent is MenuButton:
        parent.get_popup().set_item_submenu_node(SubmenuIndex, self)