using System;
using AuthorTimeHunting.Util;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>A hunt on the game's own Play Game screen, beside Zeepkist Online.</summary>
public class PlayMenuButton
{
	private const string _menuScene = "3D_MainMenu";

	/// <summary>What the game calls the click we are looking for, on the button we want to copy.</summary>
	private const string _onlineMethod = "StartOnline";

	private const string _label = "Author Time Hunting";

	private const string _copyName = "ATH Button";

	private const string _rowName = "ATH Row";

	/// <summary>How much of the button's height the label keeps for itself, under the picture.</summary>
	private const float _labelBand = 0.28f;

	/// <summary>What stays empty between the two halves of the row, in the panel's own units.</summary>
	private const float _gap = 16f;

	public void Listen()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	public void Dispose()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (scene.name != _menuScene)
		{
			return;
		}

		try
		{
			Add();
		}
		catch (Exception e)
		{
			Logger.LogError($"PlayMenuButton: Could not add the button: {e.Message}\n{e.StackTrace}");
		}
	}

	/// <summary>
	///     The play menu is a panel the main menu keeps switched off until somebody picks Play Game, so the screen
	///     is looked for among the inactive objects as well.
	/// </summary>
	private static void Add()
	{
		StartGameUI screen = Object.FindObjectOfType<StartGameUI>(true);

		if (screen == null)
		{
			Logger.LogWarning("PlayMenuButton: No play menu in this scene, the button stays away.");

			return;
		}

		GenericButton online = Online(screen);

		if (online == null)
		{
			Logger.LogWarning("PlayMenuButton: No Online button to copy, the play menu stays as it was.");

			return;
		}

		Transform slot = Slot(online.transform);

		if (slot.parent == null || slot.parent.Find(_rowName) != null)
		{
			return;
		}

		Clone(screen, online, slot);
	}

	/// <summary>The button that takes you online, found by what it does rather than by what it is called.</summary>
	private static GenericButton Online(StartGameUI screen)
	{
		foreach (GenericButton button in screen.buttonsToDisableWhenGoingIntoAthing)
		{
			if (button == null || !StartsOnline(button))
			{
				continue;
			}

			return button;
		}

		return null;
	}

	private static bool StartsOnline(GenericButton button)
	{
		for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
		{
			if (button.onClick.GetPersistentMethodName(i) == _onlineMethod)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	///     The game does not hang its play menu buttons in the panel directly: each one sits by itself in a
	///     numbered holder, and the panel arranges the holders.
	/// </summary>
	private static Transform Slot(Transform button)
	{
		Transform slot = button;

		while (slot.parent != null && slot.parent.childCount == 1)
		{
			slot = slot.parent;
		}

		return slot;
	}

	private static void Clone(StartGameUI screen, GenericButton online, Transform slot)
	{
		RectTransform row = Row(slot as RectTransform);

		GameObject copy = Object.Instantiate(slot.gameObject, row);

		copy.name = _copyName;

		Share(copy);

		GenericButton button = copy.GetComponentInChildren<GenericButton>(true);

		Rename(button);
		Paint(button);
		Picture(button);

		button.onClick = new UnityEvent();
		button.onClick.AddListener(OpenMenu);

		Navigate(online, button);

		screen.buttonsToDisableWhenGoingIntoAthing.Add(button);

		Report(row, copy);
	}

	/// <summary>Where the two halves of the row live.</summary>
	private static RectTransform Row(RectTransform slot)
	{
		GameObject row = new(_rowName, typeof(RectTransform), typeof(HorizontalLayoutGroup));

		RectTransform rect = row.GetComponent<RectTransform>();

		rect.SetParent(slot.parent, false);
		rect.SetSiblingIndex(slot.GetSiblingIndex());

		Fit(rect, slot);
		Arrange(row.GetComponent<HorizontalLayoutGroup>());

		slot.SetParent(rect, false);

		Share(slot.gameObject);

		return rect;
	}

	/// <summary>
	///     The row stands exactly where Online's holder stood, in whatever terms the panel places its rows: anchors
	///     if the panel places them itself, and a preferred height if a <c>LayoutGroup</c> does.
	/// </summary>
	private static void Fit(RectTransform row, RectTransform slot)
	{
		row.anchorMin = slot.anchorMin;
		row.anchorMax = slot.anchorMax;
		row.pivot = slot.pivot;
		row.anchoredPosition = slot.anchoredPosition;
		row.sizeDelta = slot.sizeDelta;

		LayoutElement mine = Element(row.gameObject);
		LayoutElement theirs = slot.GetComponent<LayoutElement>();

		if (theirs == null)
		{
			mine.preferredHeight = slot.rect.height;

			return;
		}

		mine.minHeight = theirs.minHeight;
		mine.preferredHeight = theirs.preferredHeight;
		mine.flexibleHeight = theirs.flexibleHeight;
		mine.flexibleWidth = theirs.flexibleWidth;
	}

	/// <summary>
	///     Two halves, side by side, each as tall as the row and each given the same share of its width once the
	///     gap between them is taken out.
	/// </summary>
	private static void Arrange(HorizontalLayoutGroup layout)
	{
		layout.spacing = _gap;
		layout.childControlWidth = true;
		layout.childControlHeight = true;
		layout.childForceExpandWidth = true;
		layout.childForceExpandHeight = true;
	}

	/// <summary>One half of the row.</summary>
	private static void Share(GameObject half)
	{
		LayoutElement element = Element(half);

		element.minWidth = 0f;
		element.preferredWidth = 0f;
		element.flexibleWidth = 1f;
	}

	private static LayoutElement Element(GameObject target)
	{
		LayoutElement element = target.GetComponent<LayoutElement>();

		if (element == null)
		{
			return target.AddComponent<LayoutElement>();
		}

		return element;
	}

	/// <summary>Which of the panel's own arrangers ends up placing the new row.</summary>
	private static void Report(RectTransform row, GameObject copy)
	{
		Transform panel = row.parent;

		LayoutGroup layout = panel.GetComponent<LayoutGroup>();

		Logger.LogInfo($"PlayMenuButton: '{copy.name}' is half {copy.transform.GetSiblingIndex()} "
		               + $"of {row.childCount} in '{row.name}', which is row {row.GetSiblingIndex()} "
		               + $"of {panel.childCount} in '{panel.name}', "
		               + $"arranged by {(layout == null ? "nothing but its anchors" : layout.GetType().Name)}, "
		               + $"anchored {row.anchorMin.x} to {row.anchorMax.x} at {row.anchoredPosition}.");

		Parts(copy);
	}

	/// <summary>What the copy is made of, by name.</summary>
	private static void Parts(GameObject copy)
	{
		foreach (Transform part in copy.GetComponentsInChildren<Transform>(true))
		{
			Logger.LogInfo($"PlayMenuButton: part '{part.name}'"
			               + $"{(part.GetComponent<Image>() == null ? "" : ", picture")}"
			               + $"{(part.GetComponent<TMP_Text>() == null ? "" : ", label")}");
		}
	}

	/// <summary>
	///     A cloned button carries the original's label and, with it, whatever localiser was keeping that label up
	///     to date.
	/// </summary>
	private static void Rename(GenericButton button)
	{
		foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
		{
			Write(label);
		}
	}

	private static void Write(TMP_Text label)
	{
		if (label == null)
		{
			return;
		}

		Localize localize = label.GetComponent<Localize>();

		if (localize != null)
		{
			Object.Destroy(localize);
		}

		label.text = _label;
	}

	/// <summary>The hunt's own color, in every state the game paints a button in.</summary>
	private static void Paint(GenericButton button)
	{
		Color hunt = Color.Zeepkist.Medal.Author;
		Color lit = Color.Lerp(hunt, Color.white, 0.35f);
		Color pressed = Color.Lerp(hunt, Color.white, 0.6f);
		Color deep = Color.Lerp(hunt, Color.black, 0.4f);
		Color deepLit = Color.Lerp(deep, Color.white, 0.3f);

		button.normalColor = hunt;
		button.hoverColor = lit;
		button.clickColor = pressed;
		button.selectedColor = deep;
		button.selectedHoverColor = deepLit;

		button.normalColor_disabled = Faded(hunt);
		button.hoverColor_disabled = Faded(lit);
		button.clickColor_disabled = Faded(pressed);
		button.selectedColor_disabled = Faded(deep);
		button.selectedHoverColor_disabled = Faded(deepLit);

		button.buttonImage.color = hunt;
	}

	/// <summary>Half there, which is what the game means by a disabled button.</summary>
	private static Color Faded(Color color)
	{
		return new Color(color.r, color.g, color.b, 0.5f);
	}

	/// <summary>
	///     The mod's own picture where the Online one was, across the whole button rather than in the little square
	///     the game uses for a pictogram.
	/// </summary>
	private static void Picture(GenericButton button)
	{
		Sprite thumbnail = Plugin.Instance.Services.Thumbnail.Sprite;

		if (thumbnail == null)
		{
			Logger.LogWarning("PlayMenuButton: No thumbnail, the button keeps the Online picture.");

			return;
		}

		Image icon = null;

		foreach (Image image in button.GetComponentsInChildren<Image>(true))
		{
			if (image == button.buttonImage)
			{
				continue;
			}

			Replace(image, icon == null ? thumbnail : null);

			icon = icon ?? image;
		}

		if (icon == null)
		{
			Logger.LogWarning("PlayMenuButton: The Online button has no picture to replace.");

			return;
		}

		Spread(icon.rectTransform);
		Behind(icon, button.buttonImage);
	}

	/// <summary>
	///     The Online picture is not one picture: it is a stack of them, coins and a hand and a sparkle, each its
	///     own image.
	/// </summary>
	private static void Replace(Image image, Sprite thumbnail)
	{
		if (thumbnail == null)
		{
			image.enabled = false;

			return;
		}

		image.sprite = thumbnail;
		image.color = Color.white;
		image.type = Image.Type.Simple;
		image.preserveAspect = true;
	}

	private static void Spread(RectTransform rect)
	{
		rect.anchorMin = new Vector2(0f, _labelBand);
		rect.anchorMax = new Vector2(1f, 1f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}

	/// <summary>In front of the button's own back and behind everything else on it.</summary>
	private static void Behind(Image icon, Image background)
	{
		if (background == null || background.transform.parent != icon.transform.parent)
		{
			icon.transform.SetSiblingIndex(0);

			return;
		}

		icon.transform.SetSiblingIndex(background.transform.GetSiblingIndex() + 1);
	}

	/// <summary>A button nobody points at can be reached with the mouse and by no other means.</summary>
	private static void Navigate(GenericButton online, GenericButton hunt)
	{
		hunt.up = online.up;
		hunt.down = online.down;
		hunt.left = online;

		online.right = hunt;
	}

	/// <summary>The mod's own menu, over the main menu.</summary>
	private static void OpenMenu()
	{
		Plugin.Instance.Services.Menu.Visible = true;
	}
}
