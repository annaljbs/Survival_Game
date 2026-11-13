using System;
using UnityEngine;

/// <summary>
/// Health, hunger, thirst and energy.
/// A day lasts 120s (see DayNightCycle): thirst empties in ~3 days, hunger in ~6.
/// Sprinting burns energy and a little health. Missions failed cost energy.
/// </summary>
public class SurvivalStats : MonoBehaviour
{
    [Header("Maximums")]
    public float maxHealth = 100f;
    public float maxHunger = 100f;
    public float maxThirst = 100f;
    public float maxEnergy = 100f;

    [Header("Decay per second")]
    public float hungerDecay = 0.15f;   // ~6 days from full
    public float thirstDecay = 0.30f;   // ~3 days from full
    public float sprintDecayMultiplier = 1.5f;

    [Header("Energy")]
    public float sprintEnergyCost = 12f;
    public float sprintHealthCost = 0.6f;
    public float energyRegen = 2.5f;

    [Header("Health")]
    public float starvationDamagePerSecond = 1.5f;
    public float regenPerSecond = 0.8f;
    [Range(0f, 1f)] public float regenThreshold = 0.5f;

    public float Health { get; private set; }
    public float Hunger { get; private set; }
    public float Thirst { get; private set; }
    public float Energy { get; private set; }
    public bool IsDead { get; private set; }
    public string DeathCause { get; private set; } = "";
    public bool IsSprinting { get; set; }
    public bool IsExhausted => Energy <= 0.1f;

    public event Action Died;

    void Awake()
    {
        Health = maxHealth;
        Hunger = maxHunger;
        Thirst = maxThirst;
        Energy = maxEnergy;
    }

    void Update()
    {
        if (IsDead) return;

        bool sprinting = IsSprinting && !IsExhausted;
        float mult = sprinting ? sprintDecayMultiplier : 1f;
        Hunger = Mathf.Max(0f, Hunger - hungerDecay * mult * Time.deltaTime);
        Thirst = Mathf.Max(0f, Thirst - thirstDecay * mult * Time.deltaTime);

        if (sprinting)
        {
            Energy = Mathf.Max(0f, Energy - sprintEnergyCost * Time.deltaTime);
            Health -= sprintHealthCost * Time.deltaTime;
        }
        else if (Hunger > 0f && Thirst > 0f)
        {
            Energy = Mathf.Min(maxEnergy, Energy + energyRegen * Time.deltaTime);
        }

        if (Hunger <= 0f || Thirst <= 0f)
        {
            Health -= starvationDamagePerSecond * Time.deltaTime;
        }
        else if (Hunger / maxHunger >= regenThreshold && Thirst / maxThirst >= regenThreshold && !sprinting)
        {
            Health = Mathf.Min(maxHealth, Health + regenPerSecond * Time.deltaTime);
        }

        if (Health <= 0f) Die();
    }

    public void Eat(float amount) => Hunger = Mathf.Min(maxHunger, Hunger + amount);
    public void Drink(float amount) => Thirst = Mathf.Min(maxThirst, Thirst + amount);
    public void RestoreEnergy(float amount) => Energy = Mathf.Min(maxEnergy, Energy + amount);

    /// <summary>Lose energy; whatever can't be paid from energy hurts health instead.</summary>
    public void LoseEnergy(float amount)
    {
        if (IsDead) return;
        Energy -= amount;
        if (Energy < 0f)
        {
            Health += Energy * 0.5f; // overflow, half as harsh
            Energy = 0f;
            if (Health <= 0f) Die();
        }
    }

    void Die()
    {
        if (IsDead) return;
        Health = 0f;
        IsDead = true;
        if (Thirst <= 0f) DeathCause = "You died of thirst.";
        else if (Hunger <= 0f) DeathCause = "You starved.";
        else DeathCause = "You collapsed from exhaustion.";
        Died?.Invoke();
    }
}
