using System.Collections;
using UnityEngine;

public delegate void OnToggleSkillActivated(ToggleSkillBase skill);
public delegate void OnToggleSkillInactivated(ToggleSkillBase skill);

public class ToggleSkillBase : ActiveSkillBase
{
    #region ConfigDatas
    public override ActiveSkillType Type => ActiveSkillType.Toggle;
    public bool IsActive => isActive;
    private bool isActive;
    public float Tick { get; protected set; } = 0.25f;
    #endregion

    #region Events
    public event OnToggleSkillActivated OnActivated;
    public event OnToggleSkillInactivated OnInactivated;
    #endregion


    private Coroutine SkillCor;

    public ToggleSkillBase(Entity caster, float tick = 0.25f) : base(caster)
    {
        Tick = tick;
    }


    public sealed override void Perform()
    {
        if(isActive == false)
        {
            OnActivated?.Invoke(this);
            caster.StartCoroutine(SkillCoroutine());
        }
        else
        {
            OnInactivated?.Invoke(this);
            caster.StopCoroutine(SkillCoroutine());
        }

        isActive = !isActive;
    }

    private IEnumerator SkillCoroutine()
    {
        yield break;
    }
}
