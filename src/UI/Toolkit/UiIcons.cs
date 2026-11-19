using Imui.Core;
using UnityEngine;

namespace AuthorTimeHunting.UI.Toolkit;

/// <summary>One icon, drawn out of <see cref="UiIconAtlas" /> and tinted.</summary>
public static class UiIcons
{
	public static void Draw(ImGui gui, ImRect rect, UiIcon icon, Color32 colour)
	{
		if (icon == UiIcon.None)
		{
			return;
		}

		Texture2D atlas = UiIconAtlas.Texture;

		if (atlas == null)
		{
			return;
		}

		Vector4 previous = gui.Canvas.GetTexScaleOffset();

		gui.Canvas.PushTexture(atlas);
		gui.Canvas.SetTexScaleOffset(UiIconAtlas.Cell(icon));

		try
		{
			gui.Canvas.Rect(Square(rect), colour);
		}
		finally
		{
			gui.Canvas.SetTexScaleOffset(previous);
			gui.Canvas.PopTexture();
		}
	}

	private static ImRect Square(ImRect rect)
	{
		float size = Mathf.Min(rect.W, rect.H);

		return new ImRect(rect.X + (rect.W - size) * 0.5f, rect.Y + (rect.H - size) * 0.5f, size, size);
	}
}
