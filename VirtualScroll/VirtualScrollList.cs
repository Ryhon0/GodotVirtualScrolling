using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

public partial class VirtualScrollList : Control
{
	[Export]
	VBoxContainer LayoutBox;
	Control Template;

	[Signal]
	public delegate void OnItemSelectedEventHandler(int idx);

	public IList<object> Items = new List<object>();

	static bool DebugDraw = false;

	[Export]
	public float Scroll = 0;
	[Export]
	public float ScrollTickAmount = 10;

	int selectedIdx = -1;

	public override void _Ready()
	{
		Template = GetNode<Control>("Template");
		if (Template == null)
		{
			GD.PushError("Virtual Scroll List template '" + GetPath() + "Template' not found");
			return;
		}
		RemoveChild(Template);

		LayoutBox.AddChild(Template);
	}

	public void AddItem(Variant o)
	{
		Items.Add(o);

		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		LayoutBox.SetSize(GetRect().Size);

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

	public int GetIndexAtPosition(Vector2 pos)
	{
		pos += new Vector2(0, Scroll);

		if (pos.Y >= GetEndPositon())
			return -1;

		return Mathf.FloorToInt(pos.Y / Template.GetRect().Size.Y);
	}

	public void SelctItem(int idx)
	{
		selectedIdx = idx;
		EmitSignal(nameof(OnItemSelected), idx);
	}

	public float GetEndPositon()
		=> Items.Count * Template.Size.Y;

	public override void _Draw()
	{
		if (Template == null || Items == null) return;

		Rect2 TemplateBox = Template.GetRect();

		int startIndex = Math.Max(0, Mathf.FloorToInt(Scroll / TemplateBox.Size.Y));
		int endIndex = Math.Min(Items.Count, startIndex + Mathf.CeilToInt(GetRect().Size.Y / TemplateBox.Size.Y) + 1);

		// Nothing to draw
		if (startIndex > endIndex)
			return;

		for (int i = startIndex; i < endIndex; i++)
		{
			Rect2 ItemBBox = TemplateBox;
			Vector2 newPos = ItemBBox.Position;
			newPos.Y -= Scroll;
			newPos.Y += i * TemplateBox.Size.Y;
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
			if(v.VariantType == Variant.Type.Dictionary)
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
