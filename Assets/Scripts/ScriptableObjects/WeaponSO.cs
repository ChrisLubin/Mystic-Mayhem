using UnityEngine;

[CreateAssetMenu(menuName = "Weapon")]
public class WeaponSO : ScriptableObject
{
    public GameObject ModelPrefab;
    public WeaponName Name;

    [Header("Attack Animations (ID Corresponds with Animator Parameter)")]
    public int LightAttackOneId;
    public int LightAttackTwoId;
    public int LightAttackThreeId;
    public int HeavyAttackOneId;

    [Header("Take Damage Animations (ID Corresponds with Animator Parameter)")]
    public int TakeDamageFrontId;

    [Header("Parry Animations (ID Corresponds with Animator Parameter)")]
    public int DoParryId;
    public int GetParriedId;

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
