extends Camera3D

@export var Speed: float = 1

func _process(delta: float) -> void:
    if Input.is_action_pressed("camera_left"):
        position += Vector3(delta * Speed, 0, 0)
    if Input.is_action_pressed("camera_right"):
        position -= Vector3(delta * Speed, 0, 0)
    if Input.is_action_pressed("camera_forward"):
        position += Vector3(0, 0, delta * Speed)
    if Input.is_action_pressed("camera_back"):
        position -= Vector3(0, 0, delta * Speed)
    if Input.is_action_pressed("camera_up"):
        position += Vector3(0, delta * Speed, 0)
    if Input.is_action_pressed("camera_down"):
        position -= Vector3(0, delta * Speed, 0)