extends Control

@export var list : Control


func AddItem():
	list.AddItem(gen_random_item())

func gen_random_item():
	return {"Name":"ABCD", "Price":5}
