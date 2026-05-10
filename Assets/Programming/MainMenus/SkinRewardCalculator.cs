using UnityEngine;

public static class SkinRewardCalculator
{
    public static int FireballCoins(int score)
    {
        return Mathf.Clamp(Mathf.RoundToInt(score / 100f), 2, 10);
    }

    public static int HelixCoins(int score)
    {
        return Mathf.Clamp(Mathf.RoundToInt(score / 25f), 4, 10);
    }
}
