using System;
using UnityEngine;

namespace AuthorTimeHunting.Util;

/// <summary>
///     Gives a plain class a frame tick. Unity only calls Update on MonoBehaviours, so this
///     exists purely to call back in.
/// </summary>
public class TickBehaviour : MonoBehaviour
{
	private Action _tick;

	private void Update()
	{
		_tick();
	}

	/// <summary>A hidden host that survives scene loads and calls <paramref name="tick" /> every frame.</summary>
	public static TickBehaviour Create(string name, Action tick)
	{
		GameObject host = new(name) { hideFlags = HideFlags.HideAndDontSave };

		DontDestroyOnLoad(host);
		TickBehaviour behaviour = host.AddComponent<TickBehaviour>();
		behaviour.Bind(tick);

		return behaviour;
	}

	private void Bind(Action tick)
	{
		_tick = tick;
	}
}
