using System;
using System.Collections.Generic;
using AuthorTimeHunting.UI.Views;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using Imui.Style;
using UnityEngine;

namespace AuthorTimeHunting.UI.Toolkit;

/// <summary>
///     The pieces every ATH panel is built from: a medal counter, the run timeline, a label/value row, a button strip.
/// </summary>
public static class UiWidgets
{
	public static void MedalCount(ImGui gui, ImRect rect, Texture2D medal, int count, Color32 colour)
	{
		float iconSize = Mathf.Min(rect.H, rect.W * 0.5f);
		ImRect icon = rect.TakeLeft(iconSize, gui.Style.Layout.InnerSpacing, out ImRect countRect);

		Color32 tint = count == 0 ? Color.Style.Text.Muted : colour;

		Medal(gui, icon, medal, tint);

		UiText.Draw(gui, UiNumbers.Text(count), tint, countRect, gui.Style.Layout.TextSize * 1.5f, 0f);
	}

	/// <summary>The game's own medal, or a dot in its color where the mod has not got a copy of the art yet.</summary>
	public static void Medal(ImGui gui, ImRect rect, Texture2D medal, Color32 tint)
	{
		if (medal == null)
		{
			gui.Canvas.Circle(rect.Center, Mathf.Min(rect.W, rect.H) * 0.3f, tint);

			return;
		}

		gui.Image(medal, Square(rect), true);
	}

	/// <summary>The hour, left to right, one block per stretch already spent, the rest of it empty.</summary>
	public static void Timeline(ImGui gui, ImRect rect, IReadOnlyList<TimelineSegment> segments)
	{
		float radius = rect.H * 0.5f;

		gui.Canvas.Rect(rect, Color.Style.Surface.Track, radius);
		gui.Canvas.PushRectMask(rect, radius);

		try
		{
			Segments(gui, rect, segments);
		}
		finally
		{
			gui.Canvas.PopRectMask();
		}
	}

	/// <summary>
	///     A button that is its symbol over a word: the shape carries it at a glance and the word settles which one
	///     it was.
	/// </summary>
	public static bool IconTile(ImGui gui, ImRect rect, UiIcon icon, string label, Color32 accent, bool enabled)
	{
		if (!enabled)
		{
			DrawTile(gui, rect, icon, label, Color.Style.Surface.Track, Color.Style.Text.Muted);

			return false;
		}

		uint id = gui.GetNextControlId();
		bool clicked = gui.InvisibleButton(id, rect);
		float hover = UiMotion.Hover(id, gui.IsControlHovered(id));
		float press = UiMotion.Press(id, gui.IsControlActive(id));

		ImRect drawn = Lift(rect, hover, press);

		Outline(gui, drawn, drawn.H * 0.16f);
		DrawTile(gui, drawn, icon, label, Shade(accent, hover, press), Color.Style.Surface.White);

		return clicked;
	}

	public static void Row(ImGui gui, ImRect rect, string label, string value, Color32 valueColour)
	{
		ImRect labelRect = rect.TakeLeft(UiMetrics.LabelWidth(rect.W), out ImRect valueRect);

		UiText.Left(gui, label, Color.Style.Text.Muted, labelRect);
		UiText.Left(gui, value, valueColour, valueRect);
	}

	/// <summary>
	///     One choice on a fullscreen menu: what it does, and a line saying what that means, on a tile the whole of
	///     which is the button.
	/// </summary>
	public static bool Card(ImGui gui, ImRect rect, string title, string caption, Color32 accent, bool enabled)
	{
		if (!enabled)
		{
			DrawCard(gui, rect, title, caption, Color.Style.Text.Muted, Color.Style.Surface.Track,
				Color.Style.Text.Muted);

			return false;
		}

		uint id = gui.GetNextControlId();
		bool clicked = gui.InvisibleButton(id, rect);
		float hover = UiMotion.Hover(id, gui.IsControlHovered(id));
		float press = UiMotion.Press(id, gui.IsControlActive(id));

		ImRect drawn = Lift(rect, hover, press);

		Outline(gui, drawn, drawn.H * 0.1f);
		DrawCard(gui, drawn, title, caption, accent, Face(hover, press), Color.Style.Surface.White);

		return clicked;
	}

	public static bool Clickable(ImGui gui, ImRect row)
	{
		uint id = gui.GetNextControlId();
		bool clicked = gui.InvisibleButton(id, row);

		if (gui.IsControlHovered(id))
		{
			gui.Canvas.Rect(row, Color.Style.Surface.Track, row.H * 0.2f);
		}

		return clicked;
	}

	public static ImRect Cell(ImRect row, ReadOnlySpan<float> weights, int column)
	{
		float offset = 0f;

		for (int i = 0; i < column; i++)
		{
			offset += weights[i];
		}

		return new ImRect(row.X + row.W * offset, row.Y, row.W * weights[column], row.H);
	}

	public static ImRect Column(ImGui gui, ImRect row, int index, int count)
	{
		float gap = gui.Style.Layout.InnerSpacing;
		float width = (row.W - gap * (count - 1)) / count;

		return new ImRect(row.X + index * (width + gap), row.Y, width, row.H);
	}

	public static void Heading(ImGui gui, ImRect rect, string text)
	{
		UiText.Draw(gui, text, Color.Style.Text.Section, rect, gui.Style.Layout.TextSize * 0.85f, 0f);
	}

	public static bool Button(ImGui gui, ImRect rect, string label)
	{
		return gui.Button(label.AsSpan(), rect);
	}

	public static bool IconButton(ImGui gui, ImRect rect, UiIcon icon, string label, Color32 accent)
	{
		return IconButton(gui, rect, icon, label, accent, true);
	}

	public static bool IconButton(ImGui gui, ImRect rect, UiIcon icon, string label, Color32 accent, bool enabled)
	{
		if (!enabled)
		{
			DrawDisabled(gui, rect, icon, label);
			return false;
		}

		ImStyleButton previous = gui.Style.Button;

		gui.Style.Button.Normal.BackColor = accent;
		gui.Style.Button.Hovered.BackColor = Lighten(accent, 1.35f);
		gui.Style.Button.Pressed.BackColor = Lighten(accent, 0.75f);
		gui.Style.Button.Normal.FrontColor = Color.Style.Surface.White;
		gui.Style.Button.Hovered.FrontColor = Color.Style.Surface.White;
		gui.Style.Button.Pressed.FrontColor = Color.Style.Surface.White;

		bool pressed;

		try
		{
			pressed = gui.Button(label.AsSpan(), rect);
		}
		finally
		{
			gui.Style.Button = previous;
		}

		float inset = rect.H * 0.25f;
		UiIcons.Draw(gui, new ImRect(rect.X + inset, rect.Y + inset, rect.H - inset * 2f, rect.H - inset * 2f), icon,
			Color.Style.Surface.White);

		return pressed;
	}

	private static void Segments(ImGui gui, ImRect rect, IReadOnlyList<TimelineSegment> segments)
	{
		float hairline = Mathf.Max(1f, rect.H * 0.12f);
		float x = rect.X;

		foreach (TimelineSegment segment in segments)
		{
			float width = rect.W * Mathf.Clamp01(segment.Fraction);
			x += width;

			if (width <= 0f)
			{
				continue;
			}

			gui.Canvas.Rect(new ImRect(x - width, rect.Y, Mathf.Max(1f, width - hairline), rect.H), segment.Colour);
		}
	}

	private static void DrawTile(ImGui gui, ImRect rect, UiIcon icon, string label, Color32 back, Color32 front)
	{
		float pad = rect.H * 0.12f;

		// The caption is a share of the tile as well as of the theme, so that a smaller tile keeps
		// its symbol. Sized off the theme alone, a tile half the height gave the fixed line of text
		// most of the room and left the icon a sliver.
		float labelSize = Mathf.Min(gui.Style.Layout.TextSize * 0.75f, rect.H * 0.26f);

		gui.Canvas.Rect(rect, back, rect.H * 0.16f);

		ImRect inner = new(rect.X + pad, rect.Y + pad, rect.W - pad * 2f, rect.H - pad * 2f);
		ImRect glyph = inner.TakeTop(inner.H - UiText.LineHeight(gui, labelSize), 0f, out ImRect caption);

		UiIcons.Draw(gui, glyph, icon, front);
		UiText.Centre(gui, label, front, caption, labelSize);
	}

	/// <summary>The button under the pointer stands up, and the one being held sinks past where it started.</summary>
	private static ImRect Lift(ImRect rect, float hover, float press)
	{
		float rise = rect.H * (0.03f * hover - 0.02f * press);

		return new ImRect(rect.X, rect.Y + rise, rect.W, rect.H);
	}

	/// <summary>A hairline behind the fill, drawn as a rect one pixel bigger on every side.</summary>
	private static void Outline(ImGui gui, ImRect rect, float radius)
	{
		gui.Canvas.Rect(new ImRect(rect.X - 1f, rect.Y - 1f, rect.W + 2f, rect.H + 2f), Color.Style.Surface.Outline,
			radius);
	}

	/// <summary>The three states of a plain button, as one color: resting, lit, pushed in.</summary>
	private static Color32 Face(float hover, float press)
	{
		Color32 lit = Color32.Lerp(Color.Style.Surface.Button, Color.Style.Surface.ButtonHovered, hover);

		return Color32.Lerp(lit, Color.Style.Surface.ButtonPressed, press);
	}

	/// <summary>The same three states for a button whose fill is its accent.</summary>
	private static Color32 Shade(Color32 accent, float hover, float press)
	{
		Color32 lit = Color32.Lerp(accent, Lighten(accent, 1.35f), hover);

		return Color32.Lerp(lit, Lighten(accent, 0.75f), press);
	}

	/// <summary>Top down: title, rule, caption.</summary>
	private static void DrawCard(ImGui gui, ImRect rect, string title, string caption, Color32 accent,
		Color32 back, Color32 titleColour)
	{
		float text = gui.Style.Layout.TextSize;
		float titleSize = text * 1.2f;
		float pad = rect.H * 0.1f;
		float bar = Mathf.Max(2f, rect.H * 0.03f);
		float gap = gui.Style.Layout.InnerSpacing;

		gui.Canvas.Rect(rect, back, rect.H * 0.1f);

		ImRect inner = new(rect.X + pad, rect.Y + pad, rect.W - pad * 2f, rect.H - pad * 2f);
		ImRect head = inner.TakeTop(UiText.LineHeight(gui, titleSize), gap, out ImRect body);

		UiText.Draw(gui, title, titleColour, head, titleSize, 0f);

		gui.Canvas.Rect(new ImRect(body.X, body.Top - bar, body.W * 0.25f, bar), accent);
		UiText.Wrapped(gui, caption, Color.Style.Text.Muted,
			new ImRect(body.X, body.Y, body.W, Mathf.Max(0f, body.H - bar - gap)), text * 0.9f);
	}

	private static void DrawDisabled(ImGui gui, ImRect rect, UiIcon icon, string label)
	{
		float inset = rect.H * 0.25f;

		gui.Canvas.Rect(rect, Color.Style.Surface.Track, rect.H * 0.2f);
		UiIcons.Draw(gui, new ImRect(rect.X + inset, rect.Y + inset, rect.H - inset * 2f, rect.H - inset * 2f), icon,
			Color.Style.Text.Muted);
		UiText.Centre(gui, label, Color.Style.Text.Muted, rect, gui.Style.Layout.TextSize);
	}

	private static Color32 Lighten(Color32 colour, float factor)
	{
		return new Color32((byte)Mathf.Clamp(colour.r * factor, 0f, 255f),
			(byte)Mathf.Clamp(colour.g * factor, 0f, 255f),
			(byte)Mathf.Clamp(colour.b * factor, 0f, 255f),
			colour.a);
	}

	private static ImRect Square(ImRect rect)
	{
		float size = Mathf.Min(rect.W, rect.H);

		return new ImRect(rect.X + (rect.W - size) * 0.5f, rect.Y + (rect.H - size) * 0.5f, size, size);
	}
}
