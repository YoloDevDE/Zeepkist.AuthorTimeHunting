using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>
///     A picture baked into the plugin, loaded once on the first ask and dropped with the session: the mod's crest,
///     the menu's backdrop.
/// </summary>
public class AthImage : IDisposable
{
	private readonly string _resource;

	private bool _sliced;

	private Sprite _sprite;

	private Texture2D _texture;

	private bool _tried;

	public AthImage(string resource)
	{
		_resource = resource;
	}

	public Texture2D Texture
	{
		get
		{
			if (_tried)
			{
				return _texture;
			}

			_tried = true;
			_texture = Load();

			return _texture;
		}
	}

	/// <summary>
	///     The same image as a Sprite, for the parts of the mod that hang it on one of the game's own UI controls
	///     rather than drawing it themselves.
	/// </summary>
	public Sprite Sprite
	{
		get
		{
			if (_sliced)
			{
				return _sprite;
			}

			_sliced = true;
			_sprite = Slice(Texture);

			return _sprite;
		}
	}

	public void Dispose()
	{
		if (_sprite != null)
		{
			Object.Destroy(_sprite);
			_sprite = null;
		}

		if (_texture == null)
		{
			return;
		}

		Object.Destroy(_texture);
		_texture = null;
	}

	private static Sprite Slice(Texture2D texture)
	{
		if (texture == null)
		{
			return null;
		}

		Sprite sprite = Sprite.Create(texture,
			new Rect(0f, 0f, texture.width, texture.height),
			new Vector2(0.5f, 0.5f));

		sprite.hideFlags = HideFlags.HideAndDontSave;

		return sprite;
	}

	private Texture2D Load()
	{
		try
		{
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(_resource);

			if (stream == null)
			{
				Logger.LogWarning($"AthImage: '{_resource}' is not in the plugin.");

				return null;
			}

			byte[] png = new byte[stream.Length];
			int read = stream.Read(png, 0, png.Length);

			Texture2D texture = new(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
			texture.LoadImage(png);

			return texture;
		}
		catch (Exception e)
		{
			Logger.LogWarning($"AthImage: Could not load the thumbnail: {e.Message}");

			return null;
		}
	}
}
