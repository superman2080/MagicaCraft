using System.Collections.Generic;
using UnityEngine;

public enum EquipmentPart
{
    Helmet,
    Top,
    Bottoms,
    Armor,
    Cape,
    Weapon,
}

public abstract class EquipmentController<T> where T: MonoBehaviour
{
    protected Dictionary<EquipmentPart, IEquipable<T>> equipmentDict = new();

    public T Owner => owner;
    private T owner;

    public EquipmentController(T owner)
    {
        this.owner = owner;
    }

    public void Equip(EquipmentPart part, IEquipable<T> equipment)
    {
        Divest(part, equipment);

        equipmentDict[part] = equipment;
        equipmentDict[part].Equip(owner);
    }

    public void Divest(EquipmentPart part, IEquipable<T> equipment)
    {
        if (equipmentDict.TryGetValue(part, out var val))
        {
            val.Divest(owner);
        }
        equipmentDict.Remove(part);
    }

    public IEquipable<T> GetEquipPart(EquipmentPart part) => equipmentDict[part];

    public Weapon<T> GetWeapon() => equipmentDict[EquipmentPart.Weapon] as Weapon<T>;

    public void Execute()
    {
        foreach (var equip in equipmentDict.Values)
        {
            equip.Execute(owner);
        }
    }

    public void FixedExecute()
    {
        foreach (var equip in equipmentDict.Values)
        {
            equip.FixedExecute(owner);
        }
    }
}
