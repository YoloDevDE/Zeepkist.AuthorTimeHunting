using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>The game's chat emoji, as sprites the mod can draw.</summary>
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
