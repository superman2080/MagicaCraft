using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public delegate void OnEffectEnterHandler(Entity sender, Entity target, StatusEffect effect);
public delegate void OnEffectExecuteHandler(Entity sender, Entity target, StatusEffect effect);
public delegate void OnEffectExitHandler(Entity sender, Entity target, StatusEffect effect);

public class StatusEffectHandler
{
    public Entity Caster { get; private set; }
    private List<StatusEffect> statusEffects = new List<StatusEffect>();
    public event OnEffectEnterHandler OnEffectEnter;
    public event OnEffectExecuteHandler OnEffectExecute;
    public event OnEffectExitHandler OnEffectExit;

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

    public StatusEffect GetEffect<T>() where T : StatusEffect
    {
        return statusEffects.Find(n => n is T);
    }

    public bool HasEffect<T>() where T : StatusEffect
    {
        return !(statusEffects.Find(n => n is T) is null);
    }

    public bool HasEffect(StatusEffectType type)
    {
        return !(statusEffects.Find(n => n.Type == type) is null);
    }

    public IEnumerable<StatusEffect> GetEffects(StatusEffectType type)
    {
        return statusEffects.Where(e => e.Type == type);
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

    public void ClearEffects(StatusEffectType type)
    {
        var delEffects = statusEffects.Where(eff => eff.Type == type).ToList();
        foreach (var eff in delEffects)
            eff.OnExit(Caster);
        statusEffects.RemoveAll(eff => delEffects.Contains(eff));
    }
}
