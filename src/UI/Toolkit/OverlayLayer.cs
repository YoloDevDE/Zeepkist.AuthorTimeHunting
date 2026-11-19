using Imui.IO.UGUI;
using UnityEngine;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.UI.Toolkit;

/// <summary>Puts the mod's drawing above everything the game has, for as long as it needs to be there.</summary>
public class OverlayLayer
{
	/// <summary>As high as a canvas sorts. Nothing in the game asks for this, which is the point.</summary>
	private const int _front = short.MaxValue;

	private Canvas _canvas;

	private bool _raised;

	private bool _searched;

	private int _wasOrder;

	public void Raise()
	{
		if (_raised)
		{
			return;
		}

		Canvas canvas = Find();

		if (canvas == null)
		{
			return;
		}

		_raised = true;
		_wasOrder = canvas.sortingOrder;
		canvas.sortingOrder = _front;
	}

	public void Drop()
	{
		if (!_raised || _canvas == null)
		{
			_raised = false;

			return;
		}

		_canvas.sortingOrder = _wasOrder;
		_raised = false;
	}

	/// <summary>The canvas Imui renders into, found through the backend component that draws it.</summary>
	private Canvas Find()
	{
		if (_searched && _canvas != null)
		{
			return _canvas;
		}

		_searched = true;

		ImuiUnityGUIBackend backend = Object.FindObjectOfType<ImuiUnityGUIBackend>();

		if (backend == null)
		{
			Logger.LogWarning("OverlayLayer: No Imui backend to lift, the game may draw over the setup screen.");

			return null;
		}

		Canvas canvas = backend.GetComponentInParent<Canvas>();

		_canvas = canvas == null ? null : canvas.rootCanvas;

		return _canvas;
	}
}
