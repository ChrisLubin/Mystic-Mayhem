using UnityEngine;

[CreateAssetMenu(menuName = "Weapon")]
public class WeaponSO : ScriptableObject
{
    public GameObject ModelPrefab;
    public WeaponName Name;
}

public enum WeaponName
{
    None,
    Sword,
    Staff,
}
