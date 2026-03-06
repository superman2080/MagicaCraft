using System;
using UnityEngine;

[Serializable]
public class ClampedFloat
{
    public static implicit operator float(ClampedFloat self) => self.CurrentValue;

    [Tooltip("ÃÖ´ë °ª")][SerializeField] private float maxValue;
    [SerializeField] private float currentValue;

    public float MaxValue
    {
        get => maxValue;
        set
        {
            maxValue = Mathf.Max(0f, value);
            CurrentValue = currentValue;
        }
    }

    public float CurrentValue
    {
        get => currentValue;
        set
        {
            float oldValue = currentValue;
            currentValue = Mathf.Clamp(value, 0f, maxValue);

            if (!Mathf.Approximately(oldValue, currentValue))
            {
                OnValueChanged?.Invoke(currentValue, maxValue);
            }
        }
    }

    public float Ratio => maxValue > 0f ? currentValue / maxValue : 0f;
    public bool IsEmpty => Mathf.Approximately(currentValue, 0);
    public bool IsFull => Mathf.Approximately(currentValue, maxValue);

    public event Action<float, float> OnValueChanged; // current, max
    public event Action OnDepleted;
    public event Action OnFilled;

    public ClampedFloat(float maxValue, float? initialValue = null)
    {
        this.maxValue = Mathf.Max(0f, maxValue);
        currentValue = initialValue ?? this.maxValue;
    }

    public void Add(float amount)
    {
        float oldValue = currentValue;
        CurrentValue += amount;

        if (oldValue > 0f && Mathf.Approximately(currentValue, 0))
            OnDepleted?.Invoke();
        else if (oldValue < maxValue && IsFull)
            OnFilled?.Invoke();
    }

    public void Subtract(float amount) => Add(-amount);

    public void SetToMax() => CurrentValue = maxValue;

    public void SetToZero() => CurrentValue = 0f;

    public bool TryConsume(float amount)
    {
        if (currentValue >= amount)
        {
            Subtract(amount);
            return true;
        }
        return false;
    }
}