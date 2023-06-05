using Godot;
using System;
using System.Collections.Generic;

public partial class Main : Control
{
	[Export]
	VirtualScrollList ScrollList;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ScrollList.Items = new List<object>()
		{
			CreateRandomItem(),
			CreateRandomItem(),
			CreateRandomItem(),
			CreateRandomItem(),
			CreateRandomItem(),
			CreateRandomItem(),
			CreateRandomItem(),
			CreateRandomItem(),
			CreateRandomItem(),
			CreateRandomItem(),
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	ListItem CreateRandomItem()
	{
		string[] names = {"Apple", "Milk", "Beans", "Water"};
		Random r = new ();

		ListItem i = new()
		{
			Name = names[r.NextInt64(names.Length-1)],
			Price = r.NextInt64(10,45)/10f
		};

		return i;
	}
}

class ListItem
{
	static string StaticText = "Some satic value";
	public string Name = "abcddawdawd";
	public float Price = 1.0f;
}
