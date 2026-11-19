using System;
using System.Collections.Generic;
using AuthorTimeHunting.UI.Toolkit;
using UnityEngine;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>The game's medals, cut out once into textures of the mod's own.</summary>
public class MedalArt : IDisposable
{
	/// <summary>The side of one copy.</summary>
	private const int _size = 128;

	private readonly Dictionary<UiMedal, Texture2D> _copies = new();

	/// <summary>Set once a copy has been attempted and thrown.</summary>
	private bool _failed;

	public Texture2D Author => Copy(UiMedal.Author, GameSprites.AuthorMedal);

	public Texture2D Gold => Copy(UiMedal.Gold, GameSprites.GoldMedal);

	/// <summary>What a penalty is counted in.</summary>
	public Texture2D OhNo => Copy(UiMedal.OhNo, EmojiSprites.OhNo ?? GameSprites.YouTriedMedal);

	public void Dispose()
	{
		foreach (Texture2D copy in _copies.Values)
		{
			Object.Destroy(copy);
		}

		_copies.Clear();
	}

	private Texture2D Copy(UiMedal medal, Sprite sprite)
	{
		if (_copies.TryGetValue(medal, out Texture2D done) && done != null)
		{
			return done;
		}

		if (_failed || sprite == null)
		{
			return null;
		}

		Texture2D copy = Cut(sprite);

		if (copy == null)
		{
			return null;
		}

		_copies[medal] = copy;

		return copy;
	}

	private Texture2D Cut(Sprite sprite)
	{
		try
		{
			return Blit(sprite);
		}
		catch (Exception e)
		{
			_failed = true;
			Logger.LogWarning($"MedalArt: The medals stay dots: {e.Message}");

			return null;
		}
	}

	/// <summary>The blit's scale and offset cut the sprite out of the atlas it shares, and ReadPixels takes the result
	/// back off the GPU into a texture the mod owns.</summary>
	private static Texture2D Blit(Sprite sprite)
	{
		Rect area = sprite.textureRect;
		Texture source = sprite.texture;

		Vector2 scale = new(area.width / source.width, area.height / source.height);
		Vector2 offset = new(area.x / source.width, area.y / source.height);

		Texture2D copy = new(_size, _size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };

		RenderTexture buffer = RenderTexture.GetTemporary(_size, _size, 0, RenderTextureFormat.ARGB32);
		RenderTexture previous = RenderTexture.active;

		try
		{
			Graphics.Blit(source, buffer, scale, offset);
			RenderTexture.active = buffer;
			copy.ReadPixels(new Rect(0f, 0f, _size, _size), 0, 0);
			copy.Apply();
		}
		finally
		{
			RenderTexture.active = previous;
			RenderTexture.ReleaseTemporary(buffer);
		}

		return copy;
	}
}
