using System;
using Imui.Controls;
using Imui.Core;
using Imui.Rendering;
using UnityEngine;

namespace AuthorTimeHunting.UI.Toolkit;

/// <summary>Text drawn into a rect at an explicit size and alignment.</summary>
public static class UiText
{
	/// <summary>The point below which shrinking has stopped being a way of showing the text.</summary>
	private const float _smallestSize = 9f;

	/// <summary>One step back for the rounding, where the exact guess lands a fraction over.</summary>
	private const float _nudge = 0.97f;

	/// <summary>Faded share of the theme's text color for asides.</summary>
	private const byte _mutedAlpha = 150;

	/// <summary>The active theme's text color, readable on a light window as on a dark one.</summary>
	public static Color32 Default(ImGui gui)
	{
		return gui.Style.Text.Color;
	}

	/// <summary>The active theme's text color, faded for asides.</summary>
	public static Color32 Muted(ImGui gui)
	{
		Color32 text = gui.Style.Text.Color;

		return new Color32(text.r, text.g, text.b, _mutedAlpha);
	}

	public static void Left(ImGui gui, string text, Color32 colour, ImRect rect)
	{
		Draw(gui, text, colour, rect, gui.Style.Layout.TextSize, 0f);
	}

	public static void Centre(ImGui gui, string text, Color32 colour, ImRect rect, float size)
	{
		Draw(gui, text, colour, rect, size, 0.5f);
	}

	public static void Right(ImGui gui, string text, Color32 colour, ImRect rect, float size)
	{
		Draw(gui, text, colour, rect, size, 1f);
	}

	public static void Paragraph(ImGui gui, string text, Color32 colour)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		gui.Text(text.AsSpan(), colour, true);
	}

	/// <summary>A line of text that breaks into the rect rather than being cut off at its edge.</summary>
	public static void Wrapped(ImGui gui, string text, Color32 colour, ImRect rect, float size)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		ImTextSettings settings = new(size, 0f, 0f, true, ImTextOverflow.Ellipsis);
		gui.Text(text.AsSpan(), in settings, colour, Fits(gui, rect, size));
	}

	public static void Draw(ImGui gui, string text, Color32 colour, ImRect rect, float size, float alignX)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		float fitted = Fit(gui, text, size, rect.W);

		ImTextSettings settings = new(fitted, alignX, 0.5f, false, ImTextOverflow.Ellipsis);
		gui.Text(text.AsSpan(), in settings, colour, Fits(gui, rect, fitted));
	}

	/// <summary>
	///     The largest size at or under <paramref name="size" /> that fits <paramref name="text" /> into
	///     <paramref
	///         name="width" />
	///     .
	/// </summary>
	public static float Fit(ImGui gui, string text, float size, float width)
	{
		if (width <= 0f || string.IsNullOrEmpty(text))
		{
			return size;
		}

		float natural = Width(gui, text, size);

		if (natural <= width)
		{
			return size;
		}

		float scaled = Mathf.Max(_smallestSize, size * width / natural);

		return Width(gui, text, scaled) <= width ? scaled : Mathf.Max(_smallestSize, scaled * _nudge);
	}

	/// <summary>How tall a rect has to be before Imui will put a line of this size in it at all.</summary>
	public static float LineHeight(ImGui gui, float size)
	{
		return gui.TextDrawer.GetLineHeightFromFontSize(size);
	}

	/// <summary>How wide a line of text comes out.</summary>
	public static float Width(ImGui gui, string text, float size)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0f;
		}

		ImTextSettings settings = new(size, 0f, 0.5f);

		return gui.MeasureTextSize(text.AsSpan(), in settings).x;
	}

	/// <summary>A rect too short for one line, grown around its own middle until it is not.</summary>
	private static ImRect Fits(ImGui gui, ImRect rect, float size)
	{
		float line = LineHeight(gui, size);

		if (rect.H >= line)
		{
			return rect;
		}

		return new ImRect(rect.X, rect.Y - (line - rect.H) * 0.5f, rect.W, line);
	}
}
