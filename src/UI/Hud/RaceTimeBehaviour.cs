using UnityEngine;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>The frame hook for <see cref="RaceTimeDisplay" />.</summary>
public class RaceTimeBehaviour : MonoBehaviour
{
	private RaceTimeDisplay _owner;

	private void LateUpdate()
	{
		_owner.LateUpdate();
	}

	public void Bind(RaceTimeDisplay owner)
	{
		_owner = owner;
	}
}
