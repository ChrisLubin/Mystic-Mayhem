using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class PlayerWeaponManager : NetworkBehaviorAutoDisable<PlayerWeaponManager>
{
    private PlayerNetworkController _networkController;
    private PlayerAnimationController _animationController;

    [SerializeField] private WeaponName _weaponOnSpawnName;

    [SerializeField] private List<WeaponDetails> _weaponDetailsList = new();
    Dictionary<WeaponName, WeaponDetails> _weaponDetailsDict = new();

    private void Awake()
    {
        this._networkController = GetComponent<PlayerNetworkController>();
        this._animationController = GetComponent<PlayerAnimationController>();
        this._weaponDetailsDict = this._weaponDetailsList.ToDictionary(w => w.Name, w => w);
        this._networkController.CurrentWeaponName.OnValueChanged += this.OnCurrentWeaponChange;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        this._networkController.CurrentWeaponName.OnValueChanged -= this.OnCurrentWeaponChange;
    }

    protected override void OnOwnerNetworkSpawn()
    {
        this.EquipWeapon(this._weaponOnSpawnName);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!this.IsOwner)
            this.OnCurrentWeaponChange(WeaponName.None, this._networkController.CurrentWeaponName.Value);
    }

    private void Update()
    {
        if (!this.IsOwner || this._animationController.IsAttacking) { return; }

        if (Input.GetKeyDown(KeyCode.Alpha1))
            this.EquipWeapon(WeaponName.Sword);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            this.EquipWeapon(WeaponName.Staff);
        else if (Input.GetKeyDown(KeyCode.Alpha0))
            this.EquipWeapon(WeaponName.None);
    }

    private void EquipWeapon(WeaponName newWeaponName)
    {
        if (!this.IsOwner || newWeaponName == this._networkController.CurrentWeaponName.Value) { return; }
        this._networkController.CurrentWeaponName.Value = newWeaponName;
    }

    private void OnCurrentWeaponChange(WeaponName prevWeaponName, WeaponName newWeaponName)
    {
        if (prevWeaponName == newWeaponName) { return; }
        if (newWeaponName == WeaponName.None)
        {
            this._weaponDetailsDict[prevWeaponName].Object.SetActive(false);
            return;
        }

        if (prevWeaponName != WeaponName.None)
            this._weaponDetailsDict[prevWeaponName].Object.SetActive(false);

        this._weaponDetailsDict[newWeaponName].Object.SetActive(true);
    }
}

[Serializable]
public class WeaponDetails
{
    public WeaponName Name;
    public GameObject Object;
}
