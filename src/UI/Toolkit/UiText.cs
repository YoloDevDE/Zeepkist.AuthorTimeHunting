using System;
using Imui.Controls;
using Imui.Core;
using Imui.Rendering;
using UnityEngine;

namespace AuthorTimeHunting.UI.Toolkit;

/// <summary>
///     Text drawn into a rect at an explicit size and alignment.
///     Imui's plain gui.Text draws at the theme's body size, left aligned. The HUD needs
///     neither: a clock is only a clock if it is large, and anything centred in a slice of
///     the screen has to be centred in the rect too, or it starts at an offset that moves
///     with the resolution.
///     Every rect here goes through <see cref="Fits" /> first, and that is not a nicety.
///     Anything but ImTextOverflow.Overflow makes Imui fit whole lines into the rect -
///     <c>(int)((boundsHeight + 0.001f) / LineHeight)</c> of them - and a rect one pixel short
///     of a single line gets zero lines and is drawn as nothing at all. No exception, no
///     warning, no text. A line is the font's line height rather than the size asked for, which
///     is the trap: a rect sized generously against the font size can still be under it. Sizing
///     the rect from the size is the mistake every caller makes once, so it is corrected here
///     instead of being written down for them.
/// </summary>
public static class UiText
{
	/// <summary>The point below which shrinking has stopped being a way of showing the text.</summary>
	private const float _smallestSize = 9f;

	/// <summary>One step back for the rounding, where the exact guess lands a fraction over.</summary>
	private const float _nudge = 0.97f;

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
	///     The largest size at or under <paramref name="size" /> that fits <paramref name="text" />
	///     into <paramref name="width" />.
	///     Text that does not fit used to lose its tail to an ellipsis, and a level called
	///     "Skyline Sprint Revisit…" is a level you cannot look up, ask about or remember. Nothing on
	///     this HUD is long enough for shrinking to cost more than cutting does - these are names and
	///     clocks, not paragraphs - so the rule is that whatever is written is written in full.
	///     A glyph's advance is proportional to the font size, so the first guess is exact to within
	///     rounding, and the second measure is what catches the rounding. Below
	///     <see cref="_smallestSize" /> the text would be there without being readable, and an
	///     ellipsis is the more honest answer.
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

	/// <summary>
	///     How wide a line of text comes out. Only worth asking when several runs of text in
	///     different colours have to sit on one line: Imui draws one colour per call, so the line
	///     is laid out by measuring the pieces and handing each one the rect it ends in.
	/// </summary>
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
