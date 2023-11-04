using System;
using System.Collections.Generic;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;

public class PlayerPredictionController : NetworkBehaviourWithLogger<PlayerPredictionController>
{
    private ThirdPersonController _movementController;
    private PlayerAnimationController _animationController;
    private PlayerAttackController _attackController;

    private List<TickInputs> _inputs = new();
    private List<TickStates> _states = new();

    // Testing
    private bool _isTesting = false;
    private bool _isRecording = false;
    private bool _isSimulating = false;
    private int _currentSimulatedTick = -1;
    [SerializeField] private Transform _serverDummyPrefab;
    private PlayerServerDummyController _serverDummyController;

    // Shared
    private const int _BUFFER_SIZE = 1024; // ~17 seconds
    private const float _MAX_POSITION_THRESHOLD = 0.03f;
    private const float _MAX_ROTATION_THRESHOLD = 25f;
    public static event Action<int> OnTickDiffBetweenLocalClientAndServer;
    private static bool _SHOULD_SHOW_LOGS = true;

    // Client
    private TickInputs[] _clientInputBuffer;
    private TickStates[] _clientStateBuffer;
    private TickStates _clientLatestServerState;
    private TickStates _clientLastProcessedState;

    // Non-owner extrapolation properties
    private TickInputs _clientLastServerInput;
    private int _nonLocalPlayerTicksAhead = 0;
    private int _nonOwnerCurrentTick = 0;
    private bool _shouldForceReconcile = false;
    private bool _didReceiveStateFromServer = false;
    private const int _TICK_EXTRAPOLATION_DIFF_THRESHOLD = 5;

    // Server
    private Queue<TickInputs> _serverInputQueue;
    private TickStates[] _serverStateBuffer;

    protected override void Awake()
    {
        base.Awake();
        this._movementController = GetComponent<ThirdPersonController>();
        this._animationController = GetComponent<PlayerAnimationController>();
        this._attackController = GetComponent<PlayerAttackController>();
        this._serverDummyController = Instantiate(this._serverDummyPrefab, transform.position, Quaternion.identity, null).GetComponent<PlayerServerDummyController>();
    }

    private void Start()
    {
        this._clientInputBuffer = new TickInputs[_BUFFER_SIZE];
        this._clientStateBuffer = new TickStates[_BUFFER_SIZE];
        this._serverInputQueue = new Queue<TickInputs>(_BUFFER_SIZE);
        this._serverStateBuffer = new TickStates[_BUFFER_SIZE];
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        TickSystem.OnTick += this.OnTick;

        if (!this.IsOwner)
            OnTickDiffBetweenLocalClientAndServer += this.OnTicksAheadUpdate;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        TickSystem.OnTick -= this.OnTick;

        if (!this.IsOwner)
            OnTickDiffBetweenLocalClientAndServer -= this.OnTicksAheadUpdate;
    }

    private void OnTick(int currentTick)
    {
        if (this._isTesting)
            this.DoTestingLogic(currentTick);
        else
            this.DoRealLogic(currentTick);
    }

    private void OnTicksAheadUpdate(int ticksAhead)
    {
        // Used by non-owner players to determine how far to extrapolate
        if (Math.Abs(this._nonLocalPlayerTicksAhead - ticksAhead) > _TICK_EXTRAPOLATION_DIFF_THRESHOLD)
        {
            this._nonLocalPlayerTicksAhead = ticksAhead;
            this._nonOwnerCurrentTick = this._clientLatestServerState.Tick + ticksAhead;
            this._shouldForceReconcile = true;

            if (_SHOULD_SHOW_LOGS)
                this._logger.Log($"Changed extrapolated ticks to {ticksAhead}");
        }
    }

    private bool ShouldReconcile()
    {
        if (this.IsHost) { return false; }
        if (this._shouldForceReconcile)
        {
            this._shouldForceReconcile = false;
            return true;
        }

        int latestServerStateBufferIndex = this._clientLatestServerState.Tick % _BUFFER_SIZE;
        TickStates clientTickStatesForLatestServerTick = this._clientStateBuffer[latestServerStateBufferIndex];
        float positionDistance = Vector3.Distance(this._clientLatestServerState.MoveState.TransformPosition, clientTickStatesForLatestServerTick.MoveState.TransformPosition);
        float rotationDistance = Quaternion.Angle(Quaternion.Euler(0f, this._clientLatestServerState.MoveState.TransformEurlerAngles.y, 0f), Quaternion.Euler(0f, clientTickStatesForLatestServerTick.MoveState.TransformEurlerAngles.y, 0f));
        bool isAnimationDifferent = clientTickStatesForLatestServerTick.AnimatorState.AnimationHash != this._clientLatestServerState.AnimatorState.AnimationHash;

        if (positionDistance > _MAX_POSITION_THRESHOLD)
        {
            if (_SHOULD_SHOW_LOGS)
                this._logger.Log($"Reconciled due to POS on tick {this._clientLatestServerState.Tick}");
            return true;
        }
        else if (rotationDistance > _MAX_ROTATION_THRESHOLD)
        {
            if (_SHOULD_SHOW_LOGS)
                this._logger.Log($"Reconciled due to ROTATION on tick {this._clientLatestServerState.Tick}");
            return true;
        }
        else if (isAnimationDifferent)
        {
            if (_SHOULD_SHOW_LOGS)
                this._logger.Log($"Reconciled due to ANIMATION on tick {this._clientLatestServerState.Tick}");
            return true;
        }

        return false;
    }

    private void DoRealLogic(int currentTick)
    {
        if (this.IsOwner)
        {
            // Owner processing
            if (this.ShouldReconcile())
            {
                int latestServerStateBufferIndex = this._clientLatestServerState.Tick % _BUFFER_SIZE;

                this._movementController.SetJumpAndGravityState(this._clientLatestServerState.JumpAndGravityState);
                this._movementController.SetGroundedState(this._clientLatestServerState.GroundedState);
                this._movementController.SetMoveState(this._clientLatestServerState.MoveState);
                this._movementController.SetCameraState(this._clientLatestServerState.CameraState);
                this._animationController.SetAnimatorState(this._clientLatestServerState.AnimatorState);
                this._attackController.SetAttackState(this._clientLatestServerState.AttackState);

                this._clientStateBuffer[latestServerStateBufferIndex] = this._clientLatestServerState;

                int tickToProcess = this._clientLatestServerState.Tick + 1;

                while (tickToProcess < currentTick)
                {
                    int tickToProcessBufferIndex = tickToProcess % _BUFFER_SIZE;
                    TickInputs tickToProcessInputs = this._clientInputBuffer[tickToProcessBufferIndex];

                    var tickToProcessMovementStates = this._movementController.OnTick(tickToProcessInputs.JumpAndGravityInput, tickToProcessInputs.MoveInput, tickToProcessInputs.MoveVelocityInput, tickToProcessInputs.CameraInput);
                    PlayerAnimationController.AnimatorState tickToProcessAnimatorState = this._animationController.OnTick();
                    int tickToProcessAttackState = this._attackController.OnTick(tickToProcessInputs.AttackInput);
                    TickStates tickToProcessTickStates = new(tickToProcess, tickToProcessMovementStates.Item1, tickToProcessMovementStates.Item2, tickToProcessMovementStates.Item3, tickToProcessMovementStates.Item4, tickToProcessAnimatorState, tickToProcessAttackState);

                    this._clientStateBuffer[tickToProcessBufferIndex] = tickToProcessTickStates;
                    tickToProcess++;
                }
            }

            int bufferIndex = currentTick % _BUFFER_SIZE;

            bool jumpAndGravityInput = this._movementController.GetJumpAndGravityInput();
            ThirdPersonController.MoveInput moveInput = this._movementController.GetMoveInput();
            Vector3 moveVelocityInput = this._movementController.GetMoveVelocityInput();
            Vector2 cameraInput = this._movementController.GetCameraInput();
            var movementStates = this._movementController.OnTick(jumpAndGravityInput, moveInput, moveVelocityInput, cameraInput);

            PlayerAnimationController.AnimatorState animatorState = this._animationController.OnTick();

            PlayerAttackController.AttackInput attackInput = this._attackController.GetAttackInput();
            int attackState = this._attackController.OnTick(attackInput);

            TickInputs tickInputs = new(currentTick, jumpAndGravityInput, moveInput, moveVelocityInput, cameraInput, attackInput);
            TickStates tickStates = new(currentTick, movementStates.Item1, movementStates.Item2, movementStates.Item3, movementStates.Item4, animatorState, attackState);
            this._clientInputBuffer[bufferIndex] = tickInputs;
            this._clientStateBuffer[bufferIndex] = tickStates;
            this._clientLastProcessedState = tickStates;
            this.SendInputToServerRpc(tickInputs);

            if (this.IsHost)
                this.SendStateToClientRpc(tickStates);
        }
        else if (!this.IsOwner && this.IsHost)
        {
            // Host processing
            int bufferIndex = -1;

            while (this._serverInputQueue.Count > 0)
            {
                TickInputs tickInputs = this._serverInputQueue.Dequeue();
                bufferIndex = tickInputs.Tick % _BUFFER_SIZE;

                var movementStates = this._movementController.OnTick(tickInputs.JumpAndGravityInput, tickInputs.MoveInput, tickInputs.MoveVelocityInput, tickInputs.CameraInput);
                PlayerAnimationController.AnimatorState animatorState = this._animationController.OnTick();
                int attackState = this._attackController.OnTick(tickInputs.AttackInput);

                this._serverStateBuffer[bufferIndex] = new(tickInputs.Tick, movementStates.Item1, movementStates.Item2, movementStates.Item3, movementStates.Item4, animatorState, attackState);
            }

            if (bufferIndex != -1)
                this.SendStateToClientRpc(this._serverStateBuffer[bufferIndex]);
        }
        else if (!this.IsOwner && !this.IsHost)
        {
            if (this._nonOwnerCurrentTick == 0 || !this._didReceiveStateFromServer) { return; } // Do not predict until we get first state from server

            // Non-host client extrapolation
            if (this.ShouldReconcile())
            {
                CanvasDebugController.Instance.IncrementCounter();
                int latestServerStateBufferIndex = this._clientLatestServerState.Tick % _BUFFER_SIZE;

                this._movementController.SetJumpAndGravityState(this._clientLatestServerState.JumpAndGravityState);
                this._movementController.SetGroundedState(this._clientLatestServerState.GroundedState);
                this._movementController.SetMoveState(this._clientLatestServerState.MoveState);
                this._movementController.SetCameraState(this._clientLatestServerState.CameraState);
                this._animationController.SetAnimatorState(this._clientLatestServerState.AnimatorState);
                this._attackController.SetAttackState(this._clientLatestServerState.AttackState);

                this._clientStateBuffer[latestServerStateBufferIndex] = this._clientLatestServerState;

                int tickToProcess = this._clientLatestServerState.Tick + 1;

                while (tickToProcess < this._nonOwnerCurrentTick)
                {
                    int tickToProcessBufferIndex = tickToProcess % _BUFFER_SIZE;

                    var tickToProcessMovementStates = this._movementController.OnTick(this._clientLastServerInput.JumpAndGravityInput, this._clientLastServerInput.MoveInput, this._clientLastServerInput.MoveVelocityInput, this._clientLastServerInput.CameraInput);
                    PlayerAnimationController.AnimatorState tickToProcessAnimatorState = this._animationController.OnTick();
                    int tickToProcessAttackState = this._attackController.OnTick(this._clientLastServerInput.AttackInput);
                    TickStates tickToProcessTickStates = new(tickToProcess, tickToProcessMovementStates.Item1, tickToProcessMovementStates.Item2, tickToProcessMovementStates.Item3, tickToProcessMovementStates.Item4, tickToProcessAnimatorState, tickToProcessAttackState);

                    this._clientStateBuffer[tickToProcessBufferIndex] = tickToProcessTickStates;
                    tickToProcess++;
                }
            }

            int bufferIndex = this._nonOwnerCurrentTick % _BUFFER_SIZE;

            var movementStates = this._movementController.OnTick(this._clientLastServerInput.JumpAndGravityInput, this._clientLastServerInput.MoveInput, this._clientLastServerInput.MoveVelocityInput, this._clientLastServerInput.CameraInput);
            PlayerAnimationController.AnimatorState animatorState = this._animationController.OnTick();
            int attackState = this._attackController.OnTick(this._clientLastServerInput.AttackInput);

            TickStates tickStates = new(this._nonOwnerCurrentTick, movementStates.Item1, movementStates.Item2, movementStates.Item3, movementStates.Item4, animatorState, attackState);
            this._clientStateBuffer[bufferIndex] = tickStates;
            this._nonOwnerCurrentTick++;
        }
    }

    [ServerRpc]
    private void SendInputToServerRpc(TickInputs tickInputs)
    {
        this._serverInputQueue.Enqueue(tickInputs);
        this.SendInputToClientRpc(tickInputs);
    }
    [ClientRpc]
    private void SendInputToClientRpc(TickInputs tickInputs) => this._clientLastServerInput = tickInputs;
    [ClientRpc]
    private void SendStateToClientRpc(TickStates tickStates)
    {
        this._clientLatestServerState = tickStates;
        this._didReceiveStateFromServer = true;
        this._serverDummyController.SetState(tickStates.MoveState.TransformPosition, tickStates.MoveState.TransformEurlerAngles.y, tickStates.AnimatorState, tickStates.MoveState);

        if (this.IsOwner)
        {
            int tickDiff = this._clientLastProcessedState.Tick - tickStates.Tick;
            OnTickDiffBetweenLocalClientAndServer?.Invoke(tickDiff);
        }
    }

    // Delete when done with CCP
    private void DoTestingLogic(int currentTick)
    {
        if (!this._isSimulating)
        {
            bool jumpAndGravityInput = this._movementController.GetJumpAndGravityInput();
            ThirdPersonController.MoveInput moveInput = this._movementController.GetMoveInput();
            Vector3 moveVelocityInput = this._movementController.GetMoveVelocityInput();
            Vector2 cameraInput = this._movementController.GetCameraInput();
            var movementStates = this._movementController.OnTick(jumpAndGravityInput, moveInput, moveVelocityInput, cameraInput);

            PlayerAnimationController.AnimatorState animatorState = this._animationController.OnTick();

            PlayerAttackController.AttackInput attackInput = this._attackController.GetAttackInput();
            int attackState = this._attackController.OnTick(attackInput);

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

    [Serializable]
    private struct TickInputs : INetworkSerializable
    {
        public int Tick;
        public bool JumpAndGravityInput;
        public ThirdPersonController.MoveInput MoveInput;
        public Vector3 MoveVelocityInput;
        public Vector2 CameraInput;
        public PlayerAttackController.AttackInput AttackInput;

        public TickInputs(int tick, bool jumpAndGravityInput, ThirdPersonController.MoveInput moveInput, Vector3 moveVelocityInput, Vector2 cameraInput, PlayerAttackController.AttackInput attackInput)
        {
            this.Tick = tick;
            this.JumpAndGravityInput = jumpAndGravityInput;
            this.MoveInput = moveInput;
            this.MoveVelocityInput = moveVelocityInput;
            this.CameraInput = cameraInput;
            this.AttackInput = attackInput;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out Tick);
                reader.ReadValueSafe(out JumpAndGravityInput);
                reader.ReadValueSafe(out MoveInput);
                reader.ReadValueSafe(out MoveVelocityInput);
                reader.ReadValueSafe(out CameraInput);
                reader.ReadValueSafe(out AttackInput);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Tick);
                writer.WriteValueSafe(JumpAndGravityInput);
                writer.WriteValueSafe(MoveInput);
                writer.WriteValueSafe(MoveVelocityInput);
                writer.WriteValueSafe(CameraInput);
                writer.WriteValueSafe(AttackInput);
            }
        }
    }

    [Serializable]
    private struct TickStates : INetworkSerializable
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

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out Tick);
                reader.ReadValueSafe(out JumpAndGravityState);
                reader.ReadValueSafe(out GroundedState);
                reader.ReadValueSafe(out MoveState);
                reader.ReadValueSafe(out CameraState);
                reader.ReadValueSafe(out AnimatorState);
                reader.ReadValueSafe(out AttackState);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(Tick);
                writer.WriteValueSafe(JumpAndGravityState);
                writer.WriteValueSafe(GroundedState);
                writer.WriteValueSafe(MoveState);
                writer.WriteValueSafe(CameraState);
                writer.WriteValueSafe(AnimatorState);
                writer.WriteValueSafe(AttackState);
            }
        }
    }
}
