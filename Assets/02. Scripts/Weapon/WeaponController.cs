
using UnityEngine;

public class WeaponController
{
    public IAttackable Owner => owner;
    private IAttackable owner;

    public IWeapon Weapon => weapon;
    private IWeapon weapon;

    public void InitializeWeapon(IWeapon weapon)
    {
        
    }
}
