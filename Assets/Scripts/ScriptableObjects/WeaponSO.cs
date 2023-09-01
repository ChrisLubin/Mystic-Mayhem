using UnityEngine;

[CreateAssetMenu(menuName = "Weapon")]
public class WeaponSO : ScriptableObject
{
    public GameObject ModelPrefab;
    public WeaponName Name;

    // Make sure IDs increase sequentially, don't skip any
    [Header("One-Handed Attack Animations (ID Corresponds with Animator Parameter)")]
    public int LightAttackOneId;
    public int HeavyAttackOneId;

    // Make sure IDs increase sequentially, don't skip any
    [Header("Take Damage Animations (ID Corresponds with Animator Parameter)")]
    public int TakeDamageFrontId;

    [Header("Damage Values")]
    public int LightAttackDamage;
    public int HeavyAttackDamage;
}

public enum WeaponName
{
    None,
    Sword,
    Staff,
}
