using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

[Tool]
public partial class VirtualScrollList : Control
{
	[Export]
	SubViewport TemplateViewport;
	[Export]
	GridAlignment GridAlignment;
	Control Template;

	[Signal]
	public delegate void OnItemSelectedEventHandler(int idx);

	public IList<object> Items = new List<object>();

	static bool DebugDraw = true;

	public float Scroll = 0;
	[Export]
	public float ScrollTickAmount = 10;
	[Export]
	public int RowWidth = -1;

	int selectedIdx = -1;

	public override void _Ready()
	{
		Template = GetNode<Control>("Template");
		if (!Engine.IsEditorHint())
		{
			if (Template == null)
			{
				GD.PushError("Virtual Scroll List template '" + GetPath() + "Template' not found");
				return;
			}
			RemoveChild(Template);

			TemplateViewport.AddChild(Template);
		}
	}

	public void AddItem(Variant o)
	{
		Items.Add(o);

		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		Template.Position = Vector2.Zero;
		Template.SetSize(GetItemSize());

		if (!Engine.IsEditorHint())
		{
			if (Template == null || Items == null) return;
			if (Scroll < 0)
			{
				Scroll = (float)Mathf.Lerp(Scroll, 0, delta * 10);
				QueueRedraw();
			}
			else
			{
				if (GetEndPositon() > GetRect().Size.Y)
				{
					if (Scroll + GetRect().Size.Y > GetEndPositon())
					{
						Scroll = (float)Mathf.Lerp(Scroll, GetEndPositon() - GetRect().Size.Y, delta * 10);
						QueueRedraw();
					}
				}
				else
				{
					if (Scroll > 0)
					{
						Scroll = (float)Mathf.Lerp(Scroll, 0, delta * 10);
						QueueRedraw();
					}
				}
			}
		}
		else
		{
			Template.Position += new Vector2(GetGridMargin(), 0);
		}
	}

	bool pressed = false;
	public override void _GuiInput(InputEvent @event)
	{
		if (Template == null || Items == null) return;

		if (@event is InputEventMouseButton mb)
		{
			if (mb.ButtonIndex == MouseButton.Left)
			{
				pressed = mb.Pressed;
				if (mb.Pressed)
					selectedIdx = GetIndexAtPosition(mb.Position);
				QueueRedraw();
			}
			else if (mb.ButtonIndex == MouseButton.WheelDown)
			{
				Scroll += ScrollTickAmount;
				QueueRedraw();
			}
			else if (mb.ButtonIndex == MouseButton.WheelUp)
			{
				Scroll -= ScrollTickAmount;
				QueueRedraw();
			}
		}
		else if (@event is InputEventMouseMotion mm)
		{
			if (pressed)
			{
				selectedIdx = GetIndexAtPosition(mm.Position);
				QueueRedraw();
			}
		}
	}

	public Vector2 GetItemSize()
	{
		if (RowWidth <= 0)
			return new Vector2(GetRect().Size.X, Template.GetRect().Size.Y);
		else
			return new Vector2(RowWidth, Template.GetRect().Size.Y);
	}

	public int GetColumnCount()
	{
		return Mathf.FloorToInt(GetRect().Size.X / GetItemSize().X);
	}

	public int GetIndexAtPosition(Vector2 pos)
	{
		pos += new Vector2(-GetGridMargin(), Scroll);

		var isize = GetItemSize();
		int cols = GetColumnCount();
		float width = isize.X * cols;
		float height = GetEndPositon();
		
		if(!new Rect2(Vector2.Zero, width, height).HasPoint(pos))
			return -1;

		int col = Mathf.FloorToInt(pos.X/isize.X);
		int row = Mathf.FloorToInt(pos.Y/isize.Y);

		GD.Print(row + ":" + col);

		return (row * cols) + col;
	}

	public void SelctItem(int idx)
	{
		selectedIdx = idx;
		EmitSignal(nameof(OnItemSelected), idx);
	}

	public float GetEndPositon()
		=> (Items.Count * Template.Size.Y) / GetColumnCount();
	
	float GetGridMargin()
	{
		switch(GridAlignment)
		{
			case GridAlignment.Left:
			default:
				return 0;

			case GridAlignment.Right:
				return GetRect().Size.X - (GetItemSize().X * GetColumnCount());
			
			case GridAlignment.Center:
				return (GetRect().Size.X - (GetItemSize().X * GetColumnCount()))/2;
		}
	}

	public override void _Draw()
	{
		if (Template == null || Items == null) return;

		Rect2 TemplateBox = Template.GetRect();

		var cols = GetColumnCount();
		int startIndex = Math.Max(0, Mathf.FloorToInt(Scroll / TemplateBox.Size.Y) * cols);
		int endIndex = Math.Min(Items.Count, startIndex + ((Mathf.CeilToInt(GetRect().Size.Y / TemplateBox.Size.Y) * cols)));

		// Nothing to draw
		if (startIndex > endIndex)
			return;

		var margin = GetGridMargin();
		for (int i = startIndex; i < endIndex; i++)
		{
			int col = i % cols;
			int row = i / cols;

			Rect2 ItemBBox = TemplateBox;
			Vector2 newPos = ItemBBox.Position;
			newPos.Y -= Scroll;
			newPos += new Vector2(col * TemplateBox.Size.X, row * TemplateBox.Size.Y);
			newPos.X += margin;
			ItemBBox.Position = newPos;

			if (i == -1)
				continue;

			if (DebugDraw)
			{
				DrawRect(ItemBBox, selectedIdx == i ? Colors.Red : Colors.Transparent, false, 8);
			}

			DrawItem(Template, ItemBBox, Items[i]);
		}
	}

	void DrawItem(Control template, Rect2 box, object item)
	{
		Rect2 ItemBox = template.GetGlobalRect();
		ItemBox.Position += box.Position;

		if (template is Label l)
		{
			string text = null;
			if (template.Name.ToString()[0] == '-')
				text = string.Format(l.Text, GetProperty(item, template.Name.ToString()[1..]));
			else
				text = l.Text;

			int fontSize = l.GetThemeFontSize("font_size");
			if (fontSize == 0) fontSize = l.GetThemeDefaultFontSize();

			DrawString(
				l.GetThemeFont("font"),
				ItemBox.Position + new Vector2(0, fontSize),
				text,
				l.HorizontalAlignment,
				fontSize: fontSize,
				width: ItemBox.Size.X
				);
		}
		else if (template is TextureRect rect)
		{
			DrawTextureRect(rect.Texture, ItemBox, false, modulate: rect.Modulate);
		}
		else if (template is ColorRect cr)
		{
			DrawRect(ItemBox, cr.Color, true);
		}

		if (DebugDraw)
			DrawRect(ItemBox, Colors.White, false, 1);

		foreach (Control ch in template.GetChildren())
			DrawItem(ch, box, item);
	}

	object GetProperty(object obj, string name)
	{
		if (obj is Variant v)
		{
			if (v.VariantType == Variant.Type.Dictionary)
				return ((Godot.Collections.Dictionary)v).GetValueOrDefault(name).Obj;

			if (v.VariantType == Variant.Type.Object)
				return ((GodotObject)(object)v).Get(name).Obj;
		}

		var prop = obj.GetType().GetProperty(name, BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		if (prop != null)
			return prop.GetValue(obj);

		var field = obj.GetType().GetField(name, BindingFlags.GetField | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		if (field != null)
			return field.GetValue(obj);



		return null;
	}
}

enum GridAlignment
{
	Left,
	Right,
	Center,
}