using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// One repository for all scriptable objects. Create your query methods here to keep your business logic clean.
/// I make this a MonoBehaviour as sometimes I add some debug/development references in the editor.
/// If you don't feel free to make this a standard class
/// </summary>
public static class ResourceSystem
{
    private static List<WeaponSO> _WEAPON_SOS;
    private static Dictionary<WeaponName, WeaponSO> _WEAPON_SOS_DICT;

    private static void AssembleResources()
    {
        _WEAPON_SOS = Resources.LoadAll<WeaponSO>("Weapons").ToList();
        _WEAPON_SOS_DICT = _WEAPON_SOS.ToDictionary(w => w.Name, r => r);
    }

    public static WeaponSO GetWeapon(WeaponName weaponName)
    {
        if (_WEAPON_SOS == null)
            AssembleResources();

        return _WEAPON_SOS_DICT[weaponName];
    }
}
