using System.Collections.Generic;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;

public class PlayerPredictionController : NetworkBehaviour
{
    private ThirdPersonController _movementController;
    private PlayerAnimationController _animationController;
    private PlayerAttackController _attackController;

    private List<TickInputs> _inputs = new();
    private List<TickStates> _states = new();

    // Testing
    private bool _isRecording = false;
    private bool _isSimulating = false;
    private int _currentSimulatedTick = -1;

    private void Awake()
    {
        this._movementController = GetComponent<ThirdPersonController>();
        this._animationController = GetComponent<PlayerAnimationController>();
        this._attackController = GetComponent<PlayerAttackController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        TickSystem.OnTick += this.OnTick;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        TickSystem.OnTick -= this.OnTick;
    }

    private void OnTick(int currentTick)
    {
        if (!this._isSimulating)
        {
            bool jumpAndGravityInput = this._movementController.GetJumpAndGravityInput();
            ThirdPersonController.MoveInput moveInput = this._movementController.GetMoveInput();
            Vector3 moveVelocityInput = this._movementController.GetMoveVelocityInput();
            ThirdPersonController.CameraInput cameraInput = this._movementController.GetCameraInput();
            var movementStates = this._movementController.OnTick(jumpAndGravityInput, moveInput, moveVelocityInput, cameraInput);

            PlayerAnimationController.AnimatorState animatorState = this._animationController.OnTick();

            PlayerAttackController.MouseClick attackInput = this._attackController.GetAttackInput();
            int attackState = this._attackController.OnTick(attackInput); // WILL HAVE TO REFACTOR LAST ATTACK INPUT FRAME WHEN IMPLEMENTING TICKETS BETWEEN FRAMES

            if (this._isRecording)
            {
                this._inputs.Add(new(currentTick, jumpAndGravityInput, moveInput, moveVelocityInput, cameraInput, attackInput));
                this._states.Add(new(currentTick, movementStates.Item1, movementStates.Item2, movementStates.Item3, movementStates.Item4, animatorState, attackState));
            }

            if (Input.GetKeyDown(KeyCode.Q))
                this._isRecording = true;
            if (Input.GetKeyDown(KeyCode.T))
            {
                this._isSimulating = true;
                this._isRecording = false;
            }
        }
        else
        {
            bool isFirstTickBeingSimulated = this._currentSimulatedTick == -1;
            if (isFirstTickBeingSimulated)
            {
                this._currentSimulatedTick = 1;
                TickStates statesBeforeFirstSimulatedTick = this._states[this._currentSimulatedTick - 1];
                this._movementController.SetJumpAndGravityState(statesBeforeFirstSimulatedTick.JumpAndGravityState);
                this._movementController.SetGroundedState(statesBeforeFirstSimulatedTick.GroundedState);
                this._movementController.SetMoveState(statesBeforeFirstSimulatedTick.MoveState);
                this._movementController.SetCameraState(statesBeforeFirstSimulatedTick.CameraState);
                this._animationController.SetAnimatorState(statesBeforeFirstSimulatedTick.AnimatorState);
                this._attackController.SetAttackState(statesBeforeFirstSimulatedTick.AttackState);
            }

            TickInputs tickInputs = this._inputs[this._currentSimulatedTick];
            this._movementController.OnTick(tickInputs.JumpAndGravityInput, tickInputs.MoveInput, isFirstTickBeingSimulated ? this._inputs[0].MoveVelocityInput : this._movementController.GetMoveVelocityInput(), tickInputs.CameraInput);
            this._animationController.OnTick();
            this._attackController.OnTick(tickInputs.AttackInput);

            this._currentSimulatedTick++;

            if (this._currentSimulatedTick == this._states.Count)
            {
                // Stop simulating
                this._currentSimulatedTick = -1;
                this._isSimulating = false;
                this._isRecording = false;
                this._inputs.Clear();
                this._states.Clear();
            }
        }
    }

    private struct TickInputs
    {
        public int Tick;
        public bool JumpAndGravityInput;
        public ThirdPersonController.MoveInput MoveInput;
        public Vector3 MoveVelocityInput;
        public ThirdPersonController.CameraInput CameraInput;
        public PlayerAttackController.MouseClick AttackInput;

        public TickInputs(int tick, bool jumpAndGravityInput, ThirdPersonController.MoveInput moveInput, Vector3 moveVelocityInput, ThirdPersonController.CameraInput cameraInput, PlayerAttackController.MouseClick attackInput)
        {
            this.Tick = tick;
            this.JumpAndGravityInput = jumpAndGravityInput;
            this.MoveInput = moveInput;
            this.MoveVelocityInput = moveVelocityInput;
            this.CameraInput = cameraInput;
            this.AttackInput = attackInput;
        }
    }

    private struct TickStates
    {
        public int Tick;
        public ThirdPersonController.JumpAndGravityState JumpAndGravityState;
        public ThirdPersonController.GroundedState GroundedState;
        public ThirdPersonController.MoveState MoveState;
        public ThirdPersonController.CameraState CameraState;
        public PlayerAnimationController.AnimatorState AnimatorState;
        public int AttackState;

        public TickStates(int tick, ThirdPersonController.JumpAndGravityState jumpAndGravityState, ThirdPersonController.GroundedState groundedState, ThirdPersonController.MoveState moveState, ThirdPersonController.CameraState cameraState, PlayerAnimationController.AnimatorState animatorState, int attackState)
        {
            this.Tick = tick;
            this.JumpAndGravityState = jumpAndGravityState;
            this.GroundedState = groundedState;
            this.MoveState = moveState;
            this.CameraState = cameraState;
            this.AnimatorState = animatorState;
            this.AttackState = attackState;
        }
    }
}
