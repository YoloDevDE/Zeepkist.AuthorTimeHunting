using System;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>The game's medals, as characters a TMP label can write.</summary>
public class MedalSpriteAsset : IDisposable
{
	public const int Author = 0;

	public const int Gold = 1;

	public const int OhNo = 2;

	/// <summary>The side of one cell in the strip.</summary>
	private const int _cell = 128;

	private TMP_SpriteAsset _asset;

	private bool _built;

	public void Dispose()
	{
		if (_asset == null)
		{
			return;
		}

		Object.Destroy(_asset.spriteSheet);
		Object.Destroy(_asset.material);
		Object.Destroy(_asset);

		_asset = null;
	}

	/// <summary>Hands <paramref name="label" /> the sprites and says whether it got them.</summary>
	public bool Install(TMP_Text label)
	{
		if (label == null)
		{
			return false;
		}

		TMP_SpriteAsset asset = Asset();

		if (asset == null)
		{
			return false;
		}

		if (label.spriteAsset == asset)
		{
			return true;
		}

		label.spriteAsset = asset;

		return true;
	}

	/// <summary>The tag for one medal, tinted.</summary>
	public static string Tag(int medal, string hex)
	{
		return $"<sprite index={medal} tint=1 color={hex}>";
	}

	/// <summary>Whether <paramref name="medal" /> is a picture this asset actually has.</summary>
	public bool Has(int medal)
	{
		return Asset() != null && medal < _asset.spriteCharacterTable.Count;
	}

	private TMP_SpriteAsset Asset()
	{
		if (_built)
		{
			return _asset;
		}

		Sprite author = GameSprites.AuthorMedal;
		Sprite gold = GameSprites.GoldMedal;

		if (author == null || gold == null)
		{
			return null;
		}

		_built = true;
		_asset = Build(Sprites(author, gold));

		return _asset;
	}

	/// <summary>
	///     The sprites in the order their indices name them, cut short at the first one the game has not got.
	/// </summary>
	private static Sprite[] Sprites(Sprite author, Sprite gold)
	{
		Sprite ohNo = EmojiSprites.OhNo;

		if (ohNo == null)
		{
			return [author, gold];
		}

		return [author, gold, ohNo];
	}

	private static TMP_SpriteAsset Build(Sprite[] sprites)
	{
		try
		{
			return Assemble(sprites);
		}
		catch (Exception e)
		{
			Logger.LogWarning($"MedalSpriteAsset: The medals stay letters: {e.Message}");

			return null;
		}
	}

	/// <summary>The material goes on last, and that ordering is the whole feature.</summary>
	private static TMP_SpriteAsset Assemble(Sprite[] sprites)
	{
		Texture2D strip = Strip(sprites);

		TMP_SpriteAsset asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();

		asset.name = "ATH Medals";
		asset.hideFlags = HideFlags.HideAndDontSave;
		asset.spriteSheet = strip;

		for (int index = 0; index < sprites.Length; index++)
		{
			TMP_SpriteGlyph glyph = Glyph(index);

			asset.spriteGlyphTable.Add(glyph);
			asset.spriteCharacterTable.Add(Character(glyph, Name(index)));
		}

		asset.UpdateLookupTables();

		asset.material = Material(strip);

		return asset;
	}

	private static string Name(int medal)
	{
		return medal switch
		{
			Author => "author",
			Gold => "gold",
			_ => "ohno"
		};
	}

	/// <summary>One row of cells, the sprites left to right in the order their indices name them.</summary>
	private static Texture2D Strip(Sprite[] sprites)
	{
		Texture2D strip = new(_cell * sprites.Length, _cell, TextureFormat.RGBA32, false)
		{
			hideFlags = HideFlags.HideAndDontSave
		};

		for (int index = 0; index < sprites.Length; index++)
		{
			Paint(strip, sprites[index], index);
		}

		strip.Apply();

		return strip;
	}

	/// <summary>One medal into its cell.</summary>
	private static void Paint(Texture2D strip, Sprite sprite, int cell)
	{
		Rect area = sprite.textureRect;
		Texture source = sprite.texture;

		Vector2 scale = new(area.width / source.width, area.height / source.height);
		Vector2 offset = new(area.x / source.width, area.y / source.height);

		RenderTexture buffer = RenderTexture.GetTemporary(_cell, _cell, 0, RenderTextureFormat.ARGB32);
		RenderTexture previous = RenderTexture.active;

		try
		{
			Graphics.Blit(source, buffer, scale, offset);
			RenderTexture.active = buffer;
			strip.ReadPixels(new Rect(0f, 0f, _cell, _cell), cell * _cell, 0);
		}
		finally
		{
			RenderTexture.active = previous;
			RenderTexture.ReleaseTemporary(buffer);
		}
	}

	private static Material Material(Texture2D strip)
	{
		Material material = new(SpriteShader()) { hideFlags = HideFlags.HideAndDontSave };

		material.SetTexture(ShaderUtilities.ID_MainTex, strip);

		return material;
	}

	/// <summary>Taken off TMP's own sprite asset where there is one.</summary>
	private static Shader SpriteShader()
	{
		TMP_SpriteAsset defaults = TMP_Settings.defaultSpriteAsset;

		if (defaults != null && defaults.material != null)
		{
			return defaults.material.shader;
		}

		return Shader.Find("TextMeshPro/Sprite");
	}

	/// <summary>A square glyph that steps the cursor on by its own width, so a medal takes the room a wide letter would
	/// and the row after it starts where it started before.</summary>
	private static TMP_SpriteGlyph Glyph(int medal)
	{
		GlyphMetrics metrics = new(_cell, _cell, 0f, _cell, _cell);
		GlyphRect rect = new(medal * _cell, 0, _cell, _cell);

		return new TMP_SpriteGlyph((uint)medal, metrics, rect, 1f, 0);
	}

	/// <summary>0xFFFE is TMP's "this sprite has no character of its own": it is written by index or by name, never by
	/// typing something.</summary>
	private static TMP_SpriteCharacter Character(TMP_SpriteGlyph glyph, string name)
	{
		return new TMP_SpriteCharacter(0xFFFE, glyph) { name = name, scale = 1f };
	}
}
