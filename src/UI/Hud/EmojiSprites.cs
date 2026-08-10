using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>
///     The game's chat emoji, as sprites the mod can draw.
///     Zeepkist writes them as <c>&lt;sprite="Zeepkist2" name="OhNo"&gt;</c>, which only works
///     inside a label TMP has already handed the right sprite asset to. The mod draws them in
///     places TMP does not reach - the bar is Imui, and the run ticker carries a sprite asset of
///     the mod's own - so what is wanted here is the picture, not the markup.
///     A TMP sprite asset is one sheet with a rect per glyph, which is exactly what a Sprite is,
///     so the answer is a Sprite over the sheet rather than a copy of the pixels. Everything that
///     already knows how to cut one of the game's sprites out of its atlas - <see cref="MedalArt" />,
///     <see cref="MedalSpriteAsset" /> - then takes this one without knowing where it came from.
///     The assets are not loaded by the mod and are not loaded on a fixed path either: they arrive
///     with the game and are found by name among the ones Unity already has. Before any label has
///     drawn one, the answer is null, and every caller here is expected to have something else to
///     draw.
/// </summary>
public static class EmojiSprites
{
	private const string _ohNoAsset = "Zeepkist2";

	private const string _ohNoGlyph = "OhNo";

	private static Sprite _ohNo;

	/// <summary>The face this mode makes when a run misses the author time.</summary>
	public static Sprite OhNo
	{
		get
		{
			if (_ohNo != null)
			{
				return _ohNo;
			}

			_ohNo = Cut(_ohNoAsset, _ohNoGlyph);

			return _ohNo;
		}
	}

	private static Sprite Cut(string assetName, string glyphName)
	{
		TMP_SpriteAsset asset = Resources.FindObjectsOfTypeAll<TMP_SpriteAsset>()
			.FirstOrDefault(candidate => candidate.name == assetName);

		if (asset == null || asset.spriteSheet == null)
		{
			return null;
		}

		TMP_SpriteCharacter character =
			asset.spriteCharacterTable.FirstOrDefault(candidate => candidate.name == glyphName);

		if (character?.glyph == null)
		{
			Logger.LogWarning($"EmojiSprites: '{assetName}' has no glyph called '{glyphName}'.");

			return null;
		}

		GlyphRect area = character.glyph.glyphRect;
		Rect rect = new(area.x, area.y, area.width, area.height);

		Sprite sprite = Sprite.Create((Texture2D)asset.spriteSheet, rect, new Vector2(0.5f, 0.5f));

		sprite.hideFlags = HideFlags.HideAndDontSave;

		return sprite;
	}
}
