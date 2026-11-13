using System.Collections.Generic;
using UnityEngine;

/// <summary>Hands out timed missions, tracks progress, pays rewards and charges penalties.</summary>
public class MissionManager : MonoBehaviour
{
    public PlayerController player;
    public SurvivalStats stats;
    public Inventory inventory;

    public int maxActive = 2;
    public float firstMissionDelay = 5f;
    public float newMissionInterval = 40f;

    public readonly List<Mission> Active = new List<Mission>();
    public int Completed { get; private set; }
    public int Failed { get; private set; }
    public string LastMessage { get; private set; } = "";
    public float MessageTime { get; private set; } = -100f;

    float spawnTimer;

    void OnEnable() => ResourceNode.Harvested += OnHarvested;
    void OnDisable() => ResourceNode.Harvested -= OnHarvested;

    void Start() => spawnTimer = newMissionInterval - firstMissionDelay;

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;
        if (stats == null || stats.IsDead) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= newMissionInterval && Active.Count < maxActive)
        {
            spawnTimer = 0f;
            CreateMission();
        }

        for (int i = Active.Count - 1; i >= 0; i--)
        {
            var m = Active[i];
            m.timeLeft -= Time.deltaTime;

            if (m.type == MissionType.Reach &&
                Vector2.Distance(player.transform.position, m.targetPos) < 1.5f)
            {
                Complete(m);
            }
            else if (m.timeLeft <= 0f)
            {
                Fail(m);
            }
        }
    }

    void OnHarvested(ItemType item, int amount)
    {
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            var m = Active[i];
            if (m.type != MissionType.Gather || m.item != item) continue;
            m.progress += amount;
            if (m.progress >= m.target) Complete(m);
        }
    }

    void Complete(Mission m)
    {
        if (m.hasRewardItem) inventory.Add(m.rewardItem, m.rewardItemCount);
        if (m.rewardEnergy > 0f) stats.RestoreEnergy(m.rewardEnergy);
        Completed++;
        Remove(m);
        Say("Mission complete!  " + m.RewardText());
    }

    void Fail(Mission m)
    {
        stats.LoseEnergy(m.penaltyEnergy);
        Failed++;
        Remove(m);
        Say($"Mission failed!  -{m.penaltyEnergy:F0} energy");
    }

    void Remove(Mission m)
    {
        if (m.beacon != null) Destroy(m.beacon);
        Active.Remove(m);
    }

    void Say(string msg)
    {
        LastMessage = msg;
        MessageTime = Time.unscaledTime;
    }

    void CreateMission()
    {
        int day = DayNightCycle.Instance != null ? DayNightCycle.Instance.DayCount : 1;
        int scale = (day - 1) / 3; // missions get a bit bigger as days pass

        for (int attempt = 0; attempt < 6; attempt++)
        {
            Mission m = null;
            switch (Random.Range(0, 4))
            {
                case 0:
                    m = new Mission { type = MissionType.Gather, item = ItemType.Wood, target = 4 + scale * 2,
                        timeLeft = 80f, rewardEnergy = 30f };
                    break;
                case 1:
                    m = new Mission { type = MissionType.Gather, item = ItemType.Stone, target = 3 + scale,
                        timeLeft = 80f, rewardEnergy = 15f, hasRewardItem = true, rewardItem = ItemType.Berry, rewardItemCount = 1 };
                    break;
                case 2:
                    m = new Mission { type = MissionType.Gather, item = ItemType.Berry, target = 2,
                        timeLeft = 100f, rewardEnergy = 20f, hasRewardItem = true, rewardItem = ItemType.Water, rewardItemCount = 1 };
                    break;
                case 3:
                    float dist = Random.Range(30f, 45f);
                    Vector2 pos = (Vector2)player.transform.position + Random.insideUnitCircle.normalized * dist;
                    // Time only just allows it if you sprint part of the way.
                    float time = dist / (player.moveSpeed * 1.25f);
                    m = new Mission { type = MissionType.Reach, targetPos = pos, timeLeft = time,
                        rewardEnergy = 20f, hasRewardItem = true, rewardItem = ItemType.Water, rewardItemCount = 1 };
                    m.beacon = Bootstrap.CreateSprite("Beacon", pos, new Color(0.2f, 1f, 1f), 1.6f);
                    break;
            }

            if (m != null && !IsDuplicate(m))
            {
                Active.Add(m);
                Say("New mission: " + m.Describe(player.transform.position));
                return;
            }
            if (m != null && m.beacon != null) Destroy(m.beacon);
        }
    }

    bool IsDuplicate(Mission m)
    {
        foreach (var a in Active)
            if (a.type == m.type && (m.type == MissionType.Reach || a.item == m.item)) return true;
        return false;
    }
}
