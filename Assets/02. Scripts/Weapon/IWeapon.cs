using UnityEngine;

public interface IWeapon
{
    void InitialWeapon(WeaponController controller);

    void Using(IAttackable attackable);
}
