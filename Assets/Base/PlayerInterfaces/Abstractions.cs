using System;
using UnityEngine;

namespace Player.Interfaces
{
    public interface IProgressReadOnly
    {
        bool HasJob(string jobId);
        bool KnowsSymbol(int symbolId);
        int KnownSymbolCount { get; }
        bool GetFlag(string key);
    }

    public interface IQuestProgressContext : IProgressReadOnly
    {
        void GrantExperience(int amount);
    }

    public interface IProgressFlagWriter : IProgressReadOnly
    {
        void SetFlag(string key, bool value);
    }

    public interface IFlagChangeSource
    {
        event Action<string, bool> FlagChanged;
    }

    public interface IDialogueProgressContext : IProgressFlagWriter
    {
        bool KnowsSymbol(string symbolKey);
        bool IsQuestActive(string questId);
        bool IsQuestCompleted(string questId);
        bool IsQuestStarted(string questId);
        void StartQuest(string questId);
        void AddJob(string jobId);
        void LearnSymbol(string symbolKey);
    }

    public interface IResourceStatsReadOnly
    {
        float maxHealth { get; }
        float maxMana { get; }
        float maxStamina { get; }
    }

    public interface IMovementStatsReadOnly
    {
        float walkSpeed { get; }
        float runSpeed { get; }
    }

    public interface IStatsReadOnly : IResourceStatsReadOnly, IMovementStatsReadOnly
    {
    }

    public interface ISpellCasterStats
    {
        Transform Transform { get; }
        float CurrentMana { get; }
        bool UseMana(float amount);
        void RefillMana();
        void RegisterDamageShield(IDamageShield shield);
        void UnregisterDamageShield(IDamageShield shield);
    }

    public interface ISpellPowerProvider
    {
        float SpellPowerMultiplier { get; }
    }

    public interface IHealthProvider
    {
        float CurrentHealth { get; }
        float maxHealth { get; }
    }

    public interface IDeathState
    {
        bool IsDead { get; }
    }

    public interface IShieldState
    {
        bool IsShieldActive();
    }

    public interface IDamageable
    {
        void ReceiveDamage(float amount, Transform source = null);
    }

    public interface IDamageShield
    {
        float AbsorbDamage(float amount);
        void Deactivate();
    }

    public interface IKnockbackable
    {
        void ApplyKnockback(Vector3 direction, float force);
    }
}
