using Imui.Controls;
using Imui.Core;
using UnityEngine;

namespace AuthorTimeHunting.UI.Toolkit;

/// <summary>Screen-relative sizing for ATH's windows and overlays.</summary>
public static class UiMetrics
{
	public static float Margin(ImGui gui)
	{
		return gui.Style.Layout.Spacing * 2f;
	}

	public static float Width(ImGui gui, float fraction, float min, float max)
	{
		float available = gui.Canvas.SafeScreenRect.W - Margin(gui) * 2f;
		float low = Mathf.Min(min, available);
		float high = Mathf.Min(max, available);

		if (low > high)
		{
			low = high;
		}

		return Mathf.Clamp(gui.Canvas.SafeScreenRect.W * fraction, low, high);
	}

	public static float LabelWidth(float rowWidth)
	{
		return Mathf.Clamp(rowWidth * 0.45f, 80f, 240f);
	}

	public static float Slack(ImGui gui)
	{
		return gui.Style.Layout.Spacing;
	}

	public static float ContentHeight(ImGui gui)
	{
		return gui.Layout.GetFrame().Size.y;
	}

	public static float ButtonHeight(ImGui gui)
	{
		return gui.GetRowHeight() * 1.2f;
	}

	public static float ContentPadding(ImGui gui)
	{
		return gui.Style.Window.ContentPadding.Vertical;
	}

	public static float WindowChrome(ImGui gui)
	{
		return ContentPadding(gui) + ImWindow.GetTitleBarHeight(gui);
	}

	/// <summary>A full-width row that many row heights tall, with the layout spacing under it.</summary>
	public static ImRect Row(ImGui gui, float scale)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), gui.GetRowHeight() * scale);
	}

	/// <summary>The same, tall enough for a button.</summary>
	public static ImRect ButtonRow(ImGui gui)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), ButtonHeight(gui));
	}

	/// <summary>What a window has to be to hold what was drawn in it last frame.</summary>
	public static float WindowHeight(ImGui gui, float contentHeight, float fallbackRows)
	{
		float content = contentHeight > 0f ? contentHeight : gui.GetRowHeight() * fallbackRows;

		return content + WindowChrome(gui) + Slack(gui);
	}
}
