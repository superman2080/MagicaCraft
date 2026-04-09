using UnityEngine;

public abstract class Weapon : IWeapon
{
    [SerializeField] private AttackStatDataSO attackStatData;

    public void DivestWeapon(WeaponController controller)
    {
    }

    public void EquipWeapon(WeaponController controller)
    {
        
    }

    public abstract void Using(IAttackable attackable);
}
