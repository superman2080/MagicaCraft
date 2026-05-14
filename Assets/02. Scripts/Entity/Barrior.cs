using System.Collections.Generic;
using UnityEngine;

public class BarriorController
{
    class Barrior: Resource
    {
        private float maintainTime;
        public Barrior(float maxValue) : base(maxValue, 0, 0)
        {
            MaxValue = maxValue;
            CurrentValue = maxValue;
            RegenPerSecond = 0;
            RegenTick = 0;
        }
    }

    private List<Barrior> barriors;




}
