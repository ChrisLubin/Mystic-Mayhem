using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM 
        private UnityEngine.InputSystem.PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }

        private PlayerAnimationController _animationController;

        private void Awake()
        {
            this._animationController = GetComponent<PlayerAnimationController>();
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private bool _isSimulating = false;
        private bool _isRecording = false;
        private int _currentTick = -1;

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            if (this._isSimulating)
            {
                bool isFirstTickInSimulationWindow = this._currentTick == -1;
                if (isFirstTickInSimulationWindow)
                    this._currentTick = 1;

                bool jumpAndGravityPayload = this._jumpAndGravityPayloads[this._currentTick];
                MovePayload movePayload = this._movePayloads[this._currentTick];
                MoveState prevMoveState = this._moveStates[this._currentTick - 1];
                CameraPayload cameraPayload = this._cameraPayloads[this._currentTick];

                if (isFirstTickInSimulationWindow)
                {
                    transform.position = this._moveStates[0].TransformPosition;
                    SetJumpAndGravityState(this._jumpAndGravityStates[0]);
                    SetGroundedState(this._groundedStates[0].Grounded);
                    SetMoveState(prevMoveState);
                    SetCameraState(this._cameraStates[0].CinemachineTargetYaw, this._cameraStates[0].CinemachineTargetPitch);
                }

                JumpAndGravity(this._isRecording, jumpAndGravityPayload);
                GroundedCheck(this._isRecording);
                Move(this._isRecording, movePayload.Sprint, movePayload.Move, isFirstTickInSimulationWindow ? prevMoveState.Velocity : _controller.velocity, prevMoveState.IsAttacking, prevMoveState.IsTakingDamage, prevMoveState.CanCombo);
                CameraRotation(this._isRecording, cameraPayload.Look);

                this._currentTick++;

                if (this._currentTick == this._cameraPayloads.Count)
                {
                    // Stop simulating
                    _input.look = Vector2.zero;
                    this._currentTick = -1;
                    this._isSimulating = false;
                    this._isRecording = false;
                    this._jumpAndGravityPayloads.Clear();
                    this._jumpAndGravityStates.Clear();
                    this._groundedStates.Clear();
                    this._movePayloads.Clear();
                    this._moveStates.Clear();
                    this._cameraPayloads.Clear();
                    this._cameraStates.Clear();
                    return;
                }
            }
            else
            {
                JumpAndGravity(this._isRecording, _input.jump);
                GroundedCheck(this._isRecording);
                Move(this._isRecording, _input.sprint, _input.move, _controller.velocity, this._animationController.IsAttacking, this._animationController.IsTakingDamage, this._animationController.CanCombo);
                CameraRotation(this._isRecording, _input.look);
            }

            if (Input.GetKeyDown(KeyCode.Q))
                this._isRecording = true;
            if (Input.GetKeyDown(KeyCode.T))
            {
                this._isSimulating = true;
                this._isRecording = false;
            }
        }

        private void SetCameraState(float cinemachineTargetYaw, float cinemachineTargetPitch)
        {
            _cinemachineTargetYaw = cinemachineTargetYaw;
            _cinemachineTargetPitch = cinemachineTargetPitch;
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        public struct GroundedState
        {
            public bool Grounded;

            public GroundedState(bool grounded)
            {
                this.Grounded = grounded;
            }
        }

        private void SetGroundedState(bool grounded)
        {
            Grounded = grounded;
        }

        private List<GroundedState> _groundedStates = new();

        // Set grounded on first tick of re-simulation window
        // input props - 
        // state props - transform.position, grounded

        private void GroundedCheck(bool isRecording)
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }

            if (isRecording)
                this._groundedStates.Add(new(Grounded));
        }

        public struct CameraPayload
        {
            public Vector2 Look;

            public CameraPayload(Vector2 look)
            {
                this.Look = look;
            }
        }

        public struct CameraState
        {
            public float CinemachineTargetYaw;
            public float CinemachineTargetPitch;

            public CameraState(float cinemachineTargetYaw, float cinemachineTargetPitch)
            {
                this.CinemachineTargetYaw = cinemachineTargetYaw;
                this.CinemachineTargetPitch = cinemachineTargetPitch;
            }
        }

        private List<CameraPayload> _cameraPayloads = new();
        private List<CameraState> _cameraStates = new();

        // Don't forget to do _input.look = Vector2.zero; after done simulating
        // input props - _input.look, 
        // state props - _cinemachineTargetYaw, _cinemachineTargetPitch,

        private void CameraRotation(bool isRecording, Vector2 look)
        {
            if (isRecording)
                this._cameraPayloads.Add(new(look));

            // // if there is an input and camera position is not fixed
            if (look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);

            if (isRecording)
                this._cameraStates.Add(new(_cinemachineTargetYaw, _cinemachineTargetPitch));
        }

        public struct MovePayload
        {
            public bool Sprint;
            public Vector2 Move;

            public MovePayload(bool sprint, Vector2 move)
            {
                this.Sprint = sprint;
                this.Move = move;
            }
        }

        public struct MoveState
        {
            public bool IsAttacking; // Remove when done testing
            public bool IsTakingDamage; // Remove when done testing
            public bool CanCombo; // Remove when done testing
            public Vector3 Velocity;
            public float AnimationBlend;
            public Vector3 MainCameraEulerAngles;
            public Vector3 TransformEurlerAngles;
            public float RotationVelocity;
            public float VerticalVelocty;
            public Vector3 TransformPosition;

            public MoveState(bool isAttacking, bool isTakingDamage, bool canCombo, Vector3 velocty, float animationBlend, Vector3 mainCamEulerAngles, Vector3 transformEurlerAngles, float rotationVelocity, float verticalVelocity, Vector3 transformPosition)
            {
                this.IsAttacking = isAttacking;
                this.IsTakingDamage = isTakingDamage;
                this.CanCombo = canCombo;
                this.Velocity = velocty;
                this.AnimationBlend = animationBlend;
                this.MainCameraEulerAngles = mainCamEulerAngles;
                this.TransformEurlerAngles = transformEurlerAngles;
                this.RotationVelocity = rotationVelocity;
                this.VerticalVelocty = verticalVelocity;
                this.TransformPosition = transformPosition;
            }
        }

        private List<MovePayload> _movePayloads = new();
        private List<MoveState> _moveStates = new();

        private void SetMoveState(MoveState moveState)
        {
            _animationBlend = moveState.AnimationBlend;
            _mainCamera.transform.eulerAngles = moveState.MainCameraEulerAngles;
            transform.eulerAngles = moveState.TransformEurlerAngles;
            _rotationVelocity = moveState.RotationVelocity;
            _verticalVelocity = moveState.VerticalVelocty;
        }

        // Set transform.position to previous tick's state for the first tick during simulation window.
        // If doing simulation for first tick during resimulation window use velocity = isSimulating ? (previous tick state velocity) : _controller.velocity
        // Make sure to set animationController state before simulating this to be accurate. (Set this tick's animation state to the previous tick's animation state)
        // input props - _input.sprint, _input.move (2 value are either -1f, 0f, or 1f), 
        // state props - IsAttacking, IsTakingDamage, CanCombo, _controller.velocity (only use x & y even tho it's Vector3), _animationBlend, _mainCamera.transform.eulerAngles.y, transform.eulerAngles.y, _rotationVelocity, _verticalVelocity

        private void Move(bool isRecording, bool sprint, Vector2 move, Vector3 velocity, bool isAttacking, bool isTakingDamage, bool CanCombo)
        {
            if (isRecording)
                this._movePayloads.Add(new(sprint, move));

            // Uncomment when done with tests
            // bool shouldLockPlayerMovement = this._animationController.IsAttacking || this._animationController.IsTakingDamage;
            // bool shouldLockPlayerRotation = this._animationController.IsAttacking && !this._animationController.CanCombo;
            bool shouldLockPlayerMovement = isAttacking || isTakingDamage;
            bool shouldLockPlayerRotation = isAttacking && !CanCombo;

            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (move == Vector2.zero || shouldLockPlayerMovement) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(velocity.x, 0.0f, velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(move.x, 0.0f, move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;

                if (shouldLockPlayerRotation)
                    _targetRotation = transform.eulerAngles.y;

                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }

            if (isRecording)
                this._moveStates.Add(new(this._animationController.IsAttacking, this._animationController.IsTakingDamage, this._animationController.CanCombo, _controller.velocity, _animationBlend, _mainCamera.transform.eulerAngles, transform.eulerAngles, _rotationVelocity, _verticalVelocity, transform.position));
        }

        public struct JumpAndGravityState
        {
            public float VerticalVelocity;
            public float JumpTimeoutDelta;
            public float FallTimeoutDelta;
            public bool Jump;

            public JumpAndGravityState(float verticalVelocity, float jumpTimeoutDelta, float fallTimeoutDelta, bool jump)
            {
                this.VerticalVelocity = verticalVelocity;
                this.JumpTimeoutDelta = jumpTimeoutDelta;
                this.FallTimeoutDelta = fallTimeoutDelta;
                this.Jump = jump;
            }
        }

        private List<bool> _jumpAndGravityPayloads = new();
        private List<JumpAndGravityState> _jumpAndGravityStates = new();

        private void SetJumpAndGravityState(JumpAndGravityState state)
        {
            _verticalVelocity = state.VerticalVelocity;
            _jumpTimeoutDelta = state.JumpTimeoutDelta;
            _fallTimeoutDelta = state.FallTimeoutDelta;
            _input.jump = state.Jump;
        }

        // Use isSimulating ? payload.jump : _input.jump;
        // input props - _input.jump, 
        // state props - _verticalVelocity, _jumpTimeoutDelta, _fallTimeoutDelta, _input.jump

        private void JumpAndGravity(bool isRecording, bool jump)
        {
            if (isRecording)
                this._jumpAndGravityPayloads.Add(jump);

            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }

            if (isRecording)
                this._jumpAndGravityStates.Add(new(_verticalVelocity, _jumpTimeoutDelta, _fallTimeoutDelta, _input.jump));
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }
}
