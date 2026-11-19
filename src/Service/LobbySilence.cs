using FMOD;
using FMOD.Studio;
using FMODUnity;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.Service;

/// <summary>
///     Keeps the game quiet while the mod builds the lobby the hunt runs in.
///     Mutes FMOD's master bus instead of the volume, so the player's settings stay untouched.
/// </summary>
public class LobbySilence
{
	private const string _masterBus = "bus:/";

	private bool _muted;

	public void Silence()
	{
		Mute(true);
	}

	public void Restore()
	{
		if (!_muted)
		{
			return;
		}

		Mute(false);
	}

	private void Mute(bool mute)
	{
		if (!RuntimeManager.IsInitialized)
		{
			return;
		}

		RESULT found = RuntimeManager.StudioSystem.getBus(_masterBus, out Bus bus);

		if (found != RESULT.OK)
		{
			Logger.LogWarning($"LobbySilence: No master bus to mute ({found}).");

			return;
		}

		bus.setMute(mute);
		_muted = mute;
	}
}
