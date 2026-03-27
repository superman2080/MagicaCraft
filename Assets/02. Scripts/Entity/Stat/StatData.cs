using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;
using SerializedDictionary = AYellowpaper.SerializedCollections;

public abstract class StatData<T> : ScriptableObject where T : Enum
{
    public SerializedDictionary<T, float> StatDict => statDict;
    [SerializedDictionary("StatType", "Value")] [SerializeField] protected SerializedDictionary<T, float> statDict = new();
}
