using System;
using System.Collections.Generic;
using UnityEngine;

public enum AttackStatType
{
    ATTACK_SPEED,
    DAMAGE,
    ATTACK_DISTANCE,
}
public enum DefaultStatType
{
    MAX_HP,
    HP_REGEN,
    MOVE_SPEED,
    JUMP_FORCE,
}

[Serializable]
public class Resource
{
    [SerializeField] private ClampedFloat source = new ClampedFloat(100f);
    [SerializeField] private float regenPerSecond;
    [SerializeField, Min(0.01f)] private float regenTick = 0.5f;
    public bool disableRegen = false;

    private float tickDelta;
    private bool isInitialized;

    public float MaxValue
    {
        get => source.MaxValue;
        set => source.MaxValue = value;
    }

    public float CurrentValue
    {
        get => source.CurrentValue;
        set => source.CurrentValue = value;
    }

    public float RegenPerSecond
    {
        get => regenPerSecond;
        set => regenPerSecond = value;
    }

    public float RegenTick
    {
        get => regenTick;
        set => regenTick = Mathf.Max(0.01f, value);
    }

    public float Ratio => source.Ratio;
    public bool IsEmpty => source.IsEmpty;
    public bool IsFull => source.IsFull;

    public event Action<float, float> OnValueChanged;
    public event Action OnDepleted;
    public event Action OnFilled;

    public Resource() : this(100f, 0f) { }

    public Resource(float maxValue, float regenPerSecond, float regenTick = 0.5f, float? initialValue = null)
    {
        source = new ClampedFloat(maxValue, initialValue);
        this.regenPerSecond = regenPerSecond;
        this.regenTick = Mathf.Max(0.01f, regenTick);
        tickDelta = 0f;

        BindEvents();
    }

    public void Initialize()
    {
        if (isInitialized) return;

        BindEvents();
        isInitialized = true;
    }

    private void BindEvents()
    {
        source.OnValueChanged += (current, max) => OnValueChanged?.Invoke(current, max);
        source.OnDepleted += () => OnDepleted?.Invoke();
        source.OnFilled += () => OnFilled?.Invoke();
    }

    public void Add(float amount) => source.Add(amount);

    public void Subtract(float amount) => source.Subtract(amount);

    public bool TryConsume(float amount) => source.TryConsume(amount);

    public void SetToMax() => source.SetToMax();

    public void SetToZero() => source.SetToZero();

    public void Tick(float deltaTime)
    {
        if (IsFull || regenPerSecond <= 0f || disableRegen == true) return;

        tickDelta += deltaTime;

        if (tickDelta < regenTick) return;

        float regenAmount = regenPerSecond * regenTick;
        source.Add(regenAmount);
        tickDelta -= regenTick;
    }

    public void OnMaxValueChanged(float oldMax, float newMax, bool keepRatio = false)
    {
        if (keepRatio && oldMax > 0f)
        {
            float ratio = source.CurrentValue / oldMax;
            source.MaxValue = newMax;
            source.CurrentValue = newMax * ratio;
        }
        else
        {
            float diff = newMax - oldMax;
            source.MaxValue = newMax;

            if (diff > 0f)
            {
                source.CurrentValue += diff;
            }
        }
    }

    public void ResetTickDelta() => tickDelta = 0f;

    public static implicit operator float(Resource self) => self.CurrentValue;
}

public abstract class Stat<T> where T : Enum
{
    public abstract StatData<T> StatData { get;  }
    public Dictionary<T, float> defaultStat = new Dictionary<T, float>();
    public Dictionary<T, float> addValue = new Dictionary<T, float>();
    public Dictionary<T, float> multipleValue = new Dictionary<T, float>();
    public Dictionary<T, float> currentValue = new Dictionary<T, float>();

    public Stat(StatData<T> statData)
    {
        InitStatData(statData);

        InitStat();
        UpdateStat();
    }

    private void InitStatData(StatData<T> statData)
    {
        var dict = statData.StatDict;
        foreach (var key in dict.Keys)
        {
            if(dict.TryGetValue(key, out float val))
            {
                defaultStat[key] = val;
            }
            else
            {
                Debug.LogAssertion($"This key does not exist! key: {key.ToString()}");
                defaultStat[key] = 1;
            }
        }
    }

    public virtual void Update()
    {
        UpdateStat();
        InitStat();
    }
    public void InitStat()
    {
        T[] statTypes = (T[])Enum.GetValues(typeof(T));
        foreach (var stat in statTypes)
        {
            addValue[stat] = 0;
            multipleValue[stat] = 1;
        }
    }
    public void UpdateStat()
    {
        T[] statTypes = (T[])Enum.GetValues(typeof(T));
        foreach (var type in statTypes)
        {
            currentValue[type] = (defaultStat[type] + addValue[type]) * multipleValue[type];
        }
    }
    public virtual void SetDefault(T type, float defaultVal)
    {
        defaultStat[type] = defaultVal;
        Update();
    }
    public virtual void Add(T type, float addition)
    {
        addValue[type] += addition;
    }
    public virtual void Multiply(T type, float multiplier)
    {
        multipleValue[type] *= multiplier;
    }
    public float Get(T type)
    {
        return currentValue[type];
    }
}


[Serializable]
public class DefaultStat : Stat<DefaultStatType>
{
    public Resource HP;

    public DefaultStat(StatData<DefaultStatType> statData): base(statData)
    {
        var dict = statData.StatDict;
        HP = new Resource(dict[DefaultStatType.MAX_HP], dict[DefaultStatType.HP_REGEN]);
    }


    public override StatData<DefaultStatType> StatData => DefaultStatData;
    [SerializeField] private DefaultStatDataSO DefaultStatData;

    public override void SetDefault(DefaultStatType type, float defaultVal)
    {
        if(type == DefaultStatType.MAX_HP)
        {
            float oldMaxHP = defaultStat[DefaultStatType.MAX_HP];
            HP.OnMaxValueChanged(oldMaxHP, defaultVal, keepRatio: true);
        }
        base.SetDefault(type, defaultVal);
    }

    public override void Update()
    {
        base.Update();
        HP.Tick(Time.deltaTime);
    }
}


[Serializable]
public class AttackStat : Stat<AttackStatType>
{
    public override StatData<AttackStatType> StatData => throw new NotImplementedException();

    public AttackStat(StatData<AttackStatType> statType): base(statType)
    {
    }
}