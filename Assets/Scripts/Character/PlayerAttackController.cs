using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackController : NetworkBehaviourWithLogger<PlayerAttackController>
{
    private PlayerAnimationController _animationController;
    private PlayerNetworkController _networkController;
    private PlayerParryController _parryController;

    private int _lastAttackId;
    private int _lastAttackInputFrame; // Used so only 1 attack can be sent to animator per Update frame

    protected override void Awake()
    {
        base.Awake();
        this._animationController = GetComponent<PlayerAnimationController>();
        this._networkController = GetComponent<PlayerNetworkController>();
        this._parryController = GetComponent<PlayerParryController>();
    }

    public void SetAttackState()
    {
        this._lastAttackId = this._attackStates[0];
    }

    public struct AttackPayload
    {
        public bool Sprint;
        public Vector2 Move;

        public AttackPayload(bool sprint, Vector2 move)
        {
            this.Sprint = sprint;
            this.Move = move;
        }
    }

    public enum MouseClick
    {
        None,
        Left,
        Right
    }

    private List<MouseClick> _attackPaylods = new();
    private List<int> _attackStates = new();

    public void OnUpdate(bool isRecording, bool isSimulating, bool doLog, int frameCount, int currentTick = 0)
    {
        if (!isSimulating)
        {
            MouseClick mouseClick;
            if (Input.GetKeyDown(KeyCode.Mouse0))
                mouseClick = MouseClick.Left;
            else if (Input.GetKeyDown(KeyCode.Mouse1))
                mouseClick = MouseClick.Right;
            else
                mouseClick = MouseClick.None;

            if (doLog)
                Debug.Log(frameCount);
            this.Run(isRecording, mouseClick);
            if (doLog)
                this.LogState(mouseClick);
        }
        else
        {
            if (doLog)
                Debug.Log(frameCount);
            this.Run(isRecording, this._attackPaylods[currentTick]);
            if (doLog)
                this.LogState(this._attackPaylods[currentTick]);

            if (currentTick == this._attackPaylods.Count - 1)
            {
                // Stop simulating
                this._attackPaylods.Clear();
                this._attackStates.Clear();
                return;
            }
        }
    }

    private void LogState(MouseClick mouseClick)
    {
        if (mouseClick == MouseClick.Left)
            Debug.Log("Left Click");
        else if (mouseClick == MouseClick.Right)
            Debug.Log("Right Click");
        else
            Debug.Log("NO Click");
    }

    private void Run(bool isRecording, MouseClick mouseClick)
    {
        if (!this.IsOwner) { return; }
        if (isRecording)
            this._attackPaylods.Add(mouseClick);
        // else
        //     Debug.Log(mouseClick);

        if (this._animationController.IsTakingDamage || this._lastAttackInputFrame == Time.frameCount || (this._animationController.IsAttacking && !this._animationController.CanCombo))
        {
            if (isRecording)
                this._attackStates.Add(this._lastAttackId);
            return;
        }
        if (mouseClick == MouseClick.None)
        {
            if (isRecording)
                this._attackStates.Add(this._lastAttackId);
            return;
        }
        this._lastAttackInputFrame = Time.frameCount;

        if (mouseClick == MouseClick.Left)
            this.HandleLightAttack(this._networkController.CurrentWeaponName.Value);
        else if (mouseClick == MouseClick.Right)
            this.HandleHeavyAttack(this._networkController.CurrentWeaponName.Value);

        if (isRecording)
            this._attackStates.Add(this._lastAttackId);
    }

    public void HandleLightAttack(WeaponName weaponName)
    {
        WeaponSO weaponSO = ResourceSystem.GetWeapon(weaponName);
        if (weaponSO == null) { return; }

        int attackId = 0;

        if (!this._animationController.CanCombo)
            attackId = weaponSO.LightAttackOneId;
        else if (this._lastAttackId == weaponSO.LightAttackOneId)
            attackId = weaponSO.LightAttackTwoId;
        else if (this._lastAttackId == weaponSO.LightAttackTwoId)
            attackId = weaponSO.LightAttackThreeId;
        else
            return;

        this._lastAttackId = attackId;
        this._animationController.PlayAttackAnimation(attackId, true);

        if (this._animationController.CanCombo)
            this._logger.Log(attackId + " - Light Combo");
        else
            this._logger.Log(attackId);
    }

    public void HandleHeavyAttack(WeaponName weaponName)
    {
        if (!this._animationController.IsAttacking && !this._animationController.IsParrying && this._parryController.CanParry())
        {
            this._parryController.DoParry();
            return;
        }

        WeaponSO weaponSO = ResourceSystem.GetWeapon(weaponName);
        if (weaponSO == null) { return; }

        int attackId = 0;

        if (!this._animationController.CanCombo)
            attackId = weaponSO.HeavyAttackOneId;
        else if (this._lastAttackId == weaponSO.LightAttackTwoId)
            attackId = weaponSO.HeavyAttackOneId;
        else
            return;

        this._lastAttackId = attackId;
        this._animationController.PlayAttackAnimation(attackId, false);

        if (this._animationController.CanCombo)
            this._logger.Log(attackId + " - Heavy Combo");
        else
            this._logger.Log(attackId);
    }
}
