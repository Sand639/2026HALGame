using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private int maxHp = 100;
    [SerializeField] private int currentHp = 100;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public float Normalized => maxHp <= 0 ? 0f : (float)currentHp / maxHp;
    public bool IsDead => currentHp <= 0;

    public event Action<Health> OnHealthChanged;
    public event Action<Health> OnDead;

    private void Awake()
    {
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        if (currentHp == 0 && maxHp > 0)
        {
            currentHp = maxHp;
        }

        NotifyChanged();
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;
        if (damage <= 0) return;

        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        NotifyChanged();

        if (currentHp <= 0)
        {
            OnDead?.Invoke(this);
        }
    }

    public void Heal(int value)
    {
        if (IsDead) return;
        if (value <= 0) return;

        currentHp += value;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        NotifyChanged();
    }

    public void SetMaxHp(int value, bool fullHeal = true)
    {
        maxHp = Mathf.Max(1, value);
        currentHp = fullHeal ? maxHp : Mathf.Clamp(currentHp, 0, maxHp);
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        OnHealthChanged?.Invoke(this);
    }
}