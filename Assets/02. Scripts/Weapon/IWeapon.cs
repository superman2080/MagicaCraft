using UnityEngine;

public interface IWeapon
{
    void EquipWeapon(WeaponController controller);
    void DivestWeapon(WeaponController controller);

    void Using(IAttackable attackable);
}
