using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public interface IBarriorUsable
{
    Barrior Barrior { get; }
}

public class Barrior
{
    private class BarriorPart
    {
        public float val;
        public float maintainTime;

        public BarriorPart(float val, float maintainTime)
        {
            this.val = val;
            this.maintainTime = maintainTime;
        }
    }

    private List<BarriorPart> barriors = new();

    #region Barrior
    public void AddBarrior(float val, float maintainTime)
    {
        barriors.Add(new BarriorPart(val, maintainTime));
    }

    public float GetBarriorValue() => barriors.Select((b) => b.val).Sum();

    public bool ExistBarrior() => barriors.Any();

    private BarriorPart GetDamagedBarriorPart() => barriors.OrderByDescending(n => n.maintainTime).FirstOrDefault();

    public void Update()
    {
        for (int i = barriors.Count - 1; i >= 0; i--)
        {
            barriors[i].maintainTime -= Time.deltaTime;
            if (barriors[i].maintainTime < 0)
                barriors.RemoveAt(i);
        }
    }

    public bool TakeDamage(float amount, out float remain) {

        remain = amount;

        if (barriors.Count <= 0) return false;

        do
        {
            var barrior = GetDamagedBarriorPart();
            barrior.val -= remain;
            if (barrior.val <= 0)
            {
                remain = -barrior.val;
                barriors.Remove(barrior);
            }
            else
            {
                remain = 0;
            }

        } while (remain > 0 && barriors.Count > 0);

        return true;
    }
    #endregion
}