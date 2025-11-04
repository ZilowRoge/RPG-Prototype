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

    public interface IHealthProvider
    {
        float CurrentHealth { get; }
        float maxHealth { get; }
    }

    public interface IDamageable
    {
        void ReceiveDamage(float amount, Transform source = null);
    }

    public interface IKnockbackable
    {
        void ApplyKnockback(Vector3 direction, float force);
    }
}
