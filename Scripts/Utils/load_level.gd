extends Area3D

@export_file("*.tscn") var Scene

func _on_body_entered(body: PhysicsBody3D)->void:
	get_tree().change_scene_to_file(Scene)
