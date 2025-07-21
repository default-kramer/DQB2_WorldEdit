extends Container
class_name ButtonSelectorContainer

signal button_pressed(id: int)

@export var ConnectChildrenOnReady: bool = false

var _Buttons: Dictionary = {}

func _ready() -> void:
    if ConnectChildrenOnReady:
        for child in get_children():
            if child is BaseButton:
                connect_button(child)

func add_button(button: BaseButton, id: int = -1) -> void:
    var actual_id: int = id if id >= 0 else _get_first_unused_index()
    add_child(button)
    _Buttons[actual_id] = button
    button.pressed.connect(func(): _on_button_pressed(actual_id))
func connect_button(button: BaseButton, id: int = -1) -> void:
    var actual_id: int = id if id >= 0 else _get_first_unused_index()
    _Buttons[actual_id] = button
    button.pressed.connect(func(): _on_button_pressed(actual_id))

func get_button(id: int) -> BaseButton:
    return _Buttons[id]

func _on_button_pressed(id: int) -> void:
    button_pressed.emit(id)

func _get_first_unused_index() -> int:
    var i: int = 0
    while true:
        if not _Buttons.has(i):
            return i
        else:
            i += 1

    return -1