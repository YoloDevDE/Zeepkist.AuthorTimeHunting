using Imui.Core;

namespace AuthorTimeHunting.UI.Toolkit;

/// <summary>A rect that hands out rows from the top down, the way Imui's own layout does inside a window.</summary>
public class UiColumn
{
	private readonly float _line;
	private readonly ImRect _rect;

	private float _top;

	public UiColumn(ImRect rect, float line)
	{
		_rect = rect;
		_line = line;
		_top = rect.Top;
	}

	public ImRect Row(float scale)
	{
		float height = _line * scale;

		_top -= height;

		return new ImRect(_rect.X, _top, _rect.W, height);
	}

	public void Space(float scale)
	{
		_top -= _line * scale;
	}

	/// <summary>Everything the cursor has not handed out yet.</summary>
	public ImRect Rest()
	{
		return new ImRect(_rect.X, _rect.Y, _rect.W, _top - _rect.Y);
	}
}
