using UnityEngine;

public enum MissionType { Gather, Reach }

/// <summary>One timed job. Reward on success, energy penalty on failure.</summary>
public class Mission
{
    public MissionType type;
    public ItemType item;          // Gather
    public int target;             // Gather: amount
    public int progress;
    public Vector2 targetPos;      // Reach
    public GameObject beacon;      // Reach
    public float timeLeft;

    public float rewardEnergy;
    public bool hasRewardItem;
    public ItemType rewardItem;
    public int rewardItemCount;
    public float penaltyEnergy = 25f;

    public string Describe(Vector2 playerPos)
    {
        if (type == MissionType.Gather)
            return $"Gather {item}: {progress}/{target}";

        Vector2 d = targetPos - playerPos;
        return $"Reach the beacon: {d.magnitude:F0}m {Compass(d)}";
    }

    public string RewardText()
    {
        string s = "";
        if (hasRewardItem) s += $"+{rewardItemCount} {rewardItem}  ";
        if (rewardEnergy > 0f) s += $"+{rewardEnergy:F0} energy";
        return s.Trim();
    }

    static string Compass(Vector2 d)
    {
        string[] names = { "E", "NE", "N", "NW", "W", "SW", "S", "SE" };
        float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        int i = Mathf.RoundToInt(((a + 360f) % 360f) / 45f) % 8;
        return names[i];
    }
}
