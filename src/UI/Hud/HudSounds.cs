using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using FMOD;
using FMODUnity;
using UnityEngine;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>
///     Everything the run ticker says out loud: the start lights counting down, and the three things that can
///     happen to the medal being chased.
/// </summary>
public class HudSounds : IDisposable
{
	private const int _sampleRate = 44100;

	private const int _channels = 1;

	/// <summary>How long the room takes to fall 60 dB.</summary>
	private const float _reverbSeconds = 0.45f;

	/// <summary>How much of the room is heard against the clip itself.</summary>
	private const float _wetMix = 0.45f;

	private const float _minus60Db = 0.001f;

	/// <summary>Room at the end of every clip for the reverb to die away in.</summary>
	private const float _tailSeconds = 0.25f;

	/// <summary>What every clip is scaled to once it is mixed.</summary>
	private const float _headroom = 0.95f;

	private const float _attackSeconds = 0.006f;
	private const float _decayRate = 5f;

	private const float _baseVolume = 0.9f;

	private const string _beepResource = "AuthorTimeHunting.CountdownBeep.wav";

	private const float _recordedPitch = 1f;

	/// <summary>The release, an octave above the lamps. Trackmania's countdown, near enough.</summary>
	private const float _goPitch = 2f;

	/// <summary>The partials every clip is built from, as a share of the fundamental.</summary>
	private static readonly float[] _partials = [1f, 0.34f, 0.16f, 0.07f];

	/// <summary>The reverb, in samples of delay: 25, 38 and 59 milliseconds.</summary>
	private static readonly int[] _echoes = [1103, 1697, 2593];

	/// <summary>The medal getting tight.</summary>
	private static readonly Tone[] _warningNotes = [new(587f, 0f, 0.18f)];

	/// <summary>The medal about to be gone.</summary>
	private static readonly Tone[] _alertNotes = [new(880f, 0f, 0.1f), new(880f, 0.15f, 0.1f)];

	/// <summary>The author time going past.</summary>
	private static readonly Tone[] _missedNotes = [new(415f, 0f, 0.16f), new(311f, 0.18f, 0.32f)];

	private Sound _alert;

	/// <summary>One lamp of the countdown, and at <see cref="_goPitch" /> the release.</summary>
	private Sound _beep;

	/// <summary>The tones are made on the first beep rather than in the constructor.</summary>
	private bool _made;

	private Sound _missed;

	private Sound _warning;

	public void Dispose()
	{
		Release(_beep);
		Release(_warning);
		Release(_alert);
		Release(_missed);

		_beep = default;
		_warning = default;
		_alert = default;
		_missed = default;
	}

	public void Lamp()
	{
		Make();
		Play(_beep, _recordedPitch);
	}

	public void Go()
	{
		Make();
		Play(_beep, _goPitch);
	}

	public void Warning()
	{
		Make();
		Play(_warning, _recordedPitch);
	}

	public void Alert()
	{
		Make();
		Play(_alert, _recordedPitch);
	}

	public void Missed()
	{
		Make();
		Play(_missed, _recordedPitch);
	}

	private void Make()
	{
		if (_made)
		{
			return;
		}

		_made = true;
		_beep = Beep();
		_warning = Clip(_warningNotes);
		_alert = Clip(_alertNotes);
		_missed = Clip(_missedNotes);
	}

	/// <summary>
	///     Started paused so the volume is on it before its first sample is heard, which is the difference between
	///     a beep and a beep with a click in front of it.
	/// </summary>
	private static void Play(Sound sound, float pitch)
	{
		if (!sound.hasHandle())
		{
			return;
		}

		RESULT result =
			RuntimeManager.CoreSystem.playSound(sound, default, true, out Channel channel);

		if (result != RESULT.OK)
		{
			return;
		}

		channel.setVolume(Volume());
		channel.setPitch(pitch);
		channel.setPaused(false);
	}

	private static void Release(Sound sound)
	{
		if (!sound.hasHandle())
		{
			return;
		}

		sound.release();
	}

	private static float Volume()
	{
		if (PlayerManager.Instance == null || PlayerManager.Instance.instellingen == null)
		{
			return _baseVolume;
		}

		GameSettingsScriptableObject settings = GetSettings.Get();

		return _baseVolume * Mathf.Clamp01(settings.audio_master / 100f)
		                   * Mathf.Clamp01(settings.audio_gameplay / 100f);
	}

	/// <summary>The wav as it sits in the plugin. FMOD reads the header itself, so only the length is said.</summary>
	private static Sound Beep()
	{
		using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(_beepResource);

		if (stream == null)
		{
			Logger.LogWarning($"HudSounds: '{_beepResource}' is not in the plugin, the countdown stays silent.");

			return default;
		}

		byte[] wav = new byte[stream.Length];
		stream.Read(wav, 0, wav.Length);

		CREATESOUNDEXINFO info = new() { cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)), length = (uint)wav.Length };

		MODE mode = MODE.OPENMEMORY | MODE.CREATESAMPLE | MODE.LOOP_OFF;

		RESULT result = RuntimeManager.CoreSystem.createSound(wav, mode, ref info, out Sound sound);

		if (result == RESULT.OK)
		{
			return sound;
		}

		Logger.LogWarning($"HudSounds: FMOD would not take the beep ({result}), the countdown stays silent.");

		return default;
	}

	/// <summary>
	///     Raw PCM handed to FMOD as if it were a file in memory: no header, no decoding, and
	///     <see
	///         cref="FMOD.CREATESOUNDEXINFO" />
	///     saying what the bytes are instead.
	/// </summary>
	private static Sound Clip(Tone[] notes)
	{
		byte[] pcm = Pcm(notes);

		CREATESOUNDEXINFO info = new()
		{
			cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)),
			length = (uint)pcm.Length,
			numchannels = _channels,
			defaultfrequency = _sampleRate,
			format = SOUND_FORMAT.PCMFLOAT
		};

		MODE mode = MODE.OPENMEMORY | MODE.OPENRAW | MODE.CREATESAMPLE | MODE.LOOP_OFF;

		RESULT result = RuntimeManager.CoreSystem.createSound(pcm, mode, ref info, out Sound sound);

		if (result == RESULT.OK)
		{
			return sound;
		}

		Logger.LogWarning($"HudSounds: FMOD would not take the tone ({result}), the ticker stays silent.");

		return default;
	}

	private static byte[] Pcm(Tone[] notes)
	{
		float[] wave = new float[Samples(notes)];

		Mix(wave, notes);
		Reverb(wave);
		Normalize(wave);

		byte[] pcm = new byte[wave.Length * sizeof(float)];

		Buffer.BlockCopy(wave, 0, pcm, 0, pcm.Length);

		return pcm;
	}

	/// <summary>As long as its last note plays, plus the room the reverb needs to fall silent in.</summary>
	private static int Samples(Tone[] notes)
	{
		float end = 0f;

		foreach (Tone note in notes)
		{
			end = Mathf.Max(end, note.Start + note.Seconds);
		}

		return Mathf.CeilToInt(_sampleRate * (end + _tailSeconds));
	}

	private static void Mix(float[] wave, Tone[] notes)
	{
		foreach (Tone note in notes)
		{
			Add(wave, note);
		}
	}

	private static void Add(float[] wave, Tone note)
	{
		int start = Mathf.RoundToInt(note.Start * _sampleRate);
		int samples = Mathf.CeilToInt(note.Seconds * _sampleRate);

		for (int i = 0; i < samples; i++)
		{
			float t = (float)i / _sampleRate;

			wave[start + i] += Wave(note.Frequency, t) * Envelope(t, note.Seconds);
		}
	}

	/// <summary>
	///     Left unnormalized on purpose - <see cref="Normalize" /> scales the finished clip, so dividing by the
	///     weights here would only be undone a moment later.
	/// </summary>
	private static float Wave(float frequency, float t)
	{
		float phase = 2f * Mathf.PI * frequency * t;
		float sum = 0f;

		for (int partial = 0; partial < _partials.Length; partial++)
		{
			sum += _partials[partial] * Mathf.Sin(phase * (partial + 1));
		}

		return sum;
	}

	private static float Envelope(float t, float seconds)
	{
		if (t < _attackSeconds)
		{
			return t / _attackSeconds;
		}

		return Mathf.Exp(-_decayRate * (t - _attackSeconds) / seconds);
	}

	private static void Reverb(float[] wave)
	{
		float[] wet = new float[wave.Length];

		foreach (int delay in _echoes)
		{
			Comb(wave, wet, delay);
		}

		Blend(wave, wet);
	}

	/// <summary>
	///     A feedback comb: every sample picks up an attenuated copy of one a delay earlier, which then feeds the
	///     one after it.
	/// </summary>
	private static void Comb(float[] dry, float[] wet, int delay)
	{
		float[] line = (float[])dry.Clone();
		float feedback = Feedback(delay);

		for (int i = delay; i < line.Length; i++)
		{
			line[i] += line[i - delay] * feedback;
		}

		Accumulate(wet, line);
	}

	/// <summary>What one echo has to be worth for the comb to lose 60 dB in <see cref="_reverbSeconds" />.</summary>
	private static float Feedback(int delay)
	{
		return Mathf.Pow(_minus60Db, delay / (float)_sampleRate / _reverbSeconds);
	}

	private static void Accumulate(float[] wet, float[] line)
	{
		for (int i = 0; i < wet.Length; i++)
		{
			wet[i] += line[i];
		}
	}

	private static void Blend(float[] wave, float[] wet)
	{
		for (int i = 0; i < wave.Length; i++)
		{
			wave[i] += wet[i] * _wetMix;
		}
	}

	private static void Normalize(float[] wave)
	{
		float peak = Peak(wave);

		if (peak <= 0f)
		{
			return;
		}

		Scale(wave, _headroom / peak);
	}

	private static float Peak(float[] wave)
	{
		float peak = 0f;

		foreach (float sample in wave)
		{
			peak = Mathf.Max(peak, Mathf.Abs(sample));
		}

		return peak;
	}

	private static void Scale(float[] wave, float factor)
	{
		for (int i = 0; i < wave.Length; i++)
		{
			wave[i] *= factor;
		}
	}
}
