using UnityEngine;

public interface IEquipable<T> where T : MonoBehaviour
{
    T Owner { get; }
    void Equip(T owner);
    void Divest(T owner);

    void Execute(T owner);

    void FixedExecute(T owner);
}
