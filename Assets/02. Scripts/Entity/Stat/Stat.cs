using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AttackStatType
{
    AttackSpeed,
    Damage,
    AttackDistance,
}

public enum HealthStatType
{
    MaxHP,
    HPRegen,
}

public enum MoveStatType
{
    MoveSpeed,
    JumpForce,
}

public enum ManaStatType
{
    MaxMana,
    ManaRegen,
}

[Serializable]
public class Resource
{
    private ClampedFloat source;
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
        set => regenTick = Mathf.Max(0, value);
    }

    public float Ratio => source.Ratio;
    public bool IsEmpty => source.IsEmpty;
    public bool IsFull => source.IsFull;

    public event Action<float, float> OnValueChanged;
    public event Action<float, float> OnValueSubtract;
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
        source.OnValueSubtract += (current, max) => OnValueSubtract?.Invoke(current, max);
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

    public virtual void SetDefault(StatData<T> otherStat)
    {
        foreach (var type in defaultStat.Keys)
        {
            defaultStat[type] = otherStat.StatDict[type];
        }
        Update();
    }

    public virtual void Add(T type, float addition)
    {
        addValue[type] += addition;
    }

    public virtual void Add(StatData<T> otherStat)
    {
        foreach (var type in defaultStat.Keys)
        {
            addValue[type] += otherStat.StatDict[type];
        }
    }

    public virtual void Multiply(T type, float multiplier)
    {
        multipleValue[type] *= multiplier;
    }

    public virtual void Multiply(StatData<T> otherStat)
    {
        foreach (var type in defaultStat.Keys)
        {
            addValue[type] *= otherStat.StatDict[type];
        }
    }

    public float Get(T type)
    {
        return currentValue[type];
    }
}

public class HealthStat : Stat<HealthStatType>
{
    public Resource HP => hp;
    private Resource hp;

    public HealthStat(StatData<HealthStatType> statData) : base(statData)
    {
        var dict = statData.StatDict;
        hp = new Resource(dict[HealthStatType.MaxHP], dict[HealthStatType.HPRegen]);
    }

    public override void SetDefault(HealthStatType type, float defaultVal)
    {
        if (type == HealthStatType.MaxHP)
        {
            float oldMaxHP = defaultStat[HealthStatType.MaxHP];
            hp.OnMaxValueChanged(oldMaxHP, defaultVal, keepRatio: true);
        }
        base.SetDefault(type, defaultVal);
    }

    public override void Update()
    {
        base.Update();
        hp.Tick(Time.deltaTime);
    }
}

public class MoveStat : Stat<MoveStatType>
{
    public MoveStat(StatData<MoveStatType> statData) : base(statData)
    {
    }
}

public class AttackStat : Stat<AttackStatType>
{
    public AttackStat(StatData<AttackStatType> statType): base(statType)
    {
    }
}

public class ManaStat : Stat<ManaStatType>
{
    public Resource Mana => mana;
    private Resource mana;

    public ManaStat(StatData<ManaStatType> statData) : base(statData)
    {
        var dict = statData.StatDict;
        mana = new Resource(dict[ManaStatType.MaxMana], dict[ManaStatType.ManaRegen]);
    }

    public override void SetDefault(ManaStatType type, float defaultVal)
    {
        if (type == ManaStatType.MaxMana)
        {
            float oldMaxHP = defaultStat[ManaStatType.MaxMana];
            mana.OnMaxValueChanged(oldMaxHP, defaultVal);
        }
        base.SetDefault(type, defaultVal);
    }

    public override void Update()
    {
        base.Update();
        mana.Tick(Time.deltaTime);
    }
}