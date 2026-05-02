using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class ChargingSkillConfig
{
    public float ChargingTime => chargingTime;
    [SerializeField] private float chargingTime;
}

public class ChargingSkillBase : ActiveSkillBase
{
    public override ActiveSkillType Type => ActiveSkillType.Charging;

    public ChargingSkillBase(Entity caster) : base(caster)
    {
    }

    protected override void Enter()
    {
        base.Enter();
    }

    private IEnumerator ChargingSkillCoroutine()
    {
        for (float i = 0; i < 5; i+= Time.deltaTime)
        {
            yield return null;
        }
    }
}
