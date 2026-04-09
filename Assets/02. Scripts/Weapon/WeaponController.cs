
using UnityEngine;

public class WeaponController
{
    public IAttackable Owner => owner;
    private IAttackable owner;

    public IWeapon Weapon => weapon;
    private IWeapon weapon;

    public void EquipWeapon(IWeapon weapon)
    {
        this.weapon?.DivestWeapon(this);

        this.weapon = weapon;
        weapon.EquipWeapon(this);
    }

    public void DivestWeapon()
    {
        weapon?.DivestWeapon(this);
    }

    public void UseWeapon()
    {
        weapon?.Using(owner);
    }
}
