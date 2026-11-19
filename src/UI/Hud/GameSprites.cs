using UnityEngine;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>The game's own medal sprites.</summary>
public static class GameSprites
{
	public static Sprite AuthorMedal => PlayerManager.Instance == null ? null : PlayerManager.Instance.authorMedal;

	public static Sprite GoldMedal => PlayerManager.Instance == null ? null : PlayerManager.Instance.goldMedal;

	public static Sprite SilverMedal => PlayerManager.Instance == null ? null : PlayerManager.Instance.silverMedal;

	public static Sprite BronzeMedal => PlayerManager.Instance == null ? null : PlayerManager.Instance.bronzeMedal;

	public static Sprite YouTriedMedal => PlayerManager.Instance == null ? null : PlayerManager.Instance.youTriedMedal;
}
