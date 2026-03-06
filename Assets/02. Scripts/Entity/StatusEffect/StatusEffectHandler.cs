using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public delegate void OnEffectStartHandler(StatusEffectHandler sender, StatusEffect effect);

public class StatusEffectHandler
{
    public Entity Caster { get; private set; }
    private List<StatusEffect> statusEffects = new List<StatusEffect>();

    public StatusEffectHandler(Entity caster)
    {
        Caster = caster;
    }

    public void AddEffect(StatusEffect effect)
    {
        if (HasEffect<Unstoppable>() &&
            ((effect.Type == StatusEffectType.HARD_CC) || (effect.Type == StatusEffectType.NORMAL_CC)))
            return;
        statusEffects.Add(effect);
        effect.OnStart(Caster);
    }

    public bool HasEffect<T>() where T : StatusEffect
    {
        return !(statusEffects.Find(n => n is T) is null);
    }

    public bool HasEffect(StatusEffectType type)
    {
        return !(statusEffects.Find(n => n.Type == type) is null);
    }

    public void UpdateEffect()
    {
        if (statusEffects.Count <= 0)
            return;

        statusEffects = statusEffects.OrderBy(eff => eff.Type).
            ThenBy(effect => effect.Duration).ToList();
        List<StatusEffect> delEffect = new List<StatusEffect>();
        foreach (var eff in statusEffects)
        {
            if (!HasEffect<Unstoppable>()
                || (HasEffect<Unstoppable>() && eff.Type == StatusEffectType.BUFF))
                eff.OnUpdate(Caster);
            eff.Duration -= Time.deltaTime;
            if (eff.Duration <= 0)
            {
                eff.OnExit(Caster);
                delEffect.Add(eff);
            }
        }

        statusEffects.RemoveAll(eff => delEffect.Contains(eff));
    }

    public void ClearEffects()
    {
        foreach (var eff in statusEffects)
            eff.OnExit(Caster);
        statusEffects.Clear();
    }
}
