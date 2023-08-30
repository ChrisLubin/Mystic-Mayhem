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
    private static Logger _LOGGER;
    private static List<WeaponSO> _WEAPON_SOS;
    private static Dictionary<WeaponName, WeaponSO> _WEAPON_SOS_DICT;

    private static void Init()
    {
        _LOGGER = new("ResourceSystem");
        _WEAPON_SOS = Resources.LoadAll<WeaponSO>("Weapons").ToList();
        _WEAPON_SOS_DICT = _WEAPON_SOS.ToDictionary(w => w.Name, r => r);
    }

    public static WeaponSO GetWeapon(WeaponName weaponName)
    {
        if (_WEAPON_SOS == null)
            Init();
        if (weaponName == WeaponName.None)
            return null;

        WeaponSO weaponSO = _WEAPON_SOS_DICT[weaponName];
        if (weaponSO == null)
            _LOGGER.Log("Couldn't find weapon SO", Logger.LogLevel.Warning);

        return weaponSO;
    }
}
