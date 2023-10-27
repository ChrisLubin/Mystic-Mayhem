using UnityEngine;
using static StarterAssets.ThirdPersonController;

public class PlayerServerDummyController : MonoBehaviour
{
    private Animator _animator;
    private int _speedHash;

    private void Awake()
    {
        this._animator = GetComponent<Animator>();
        _speedHash = Animator.StringToHash("Speed");
    }

    private void Start() => this._animator.speed = 0f;

    public void SetState(Vector3 position, float rotation, PlayerAnimationController.AnimatorState animatorState, MoveState moveState)
    {
        this._animator.speed = 1f;
        transform.position = position;
        transform.eulerAngles = new(0f, rotation, 0f);
        this._animator.SetFloat(this._speedHash, moveState.AnimationBlend);

        if (animatorState.IsLayerIndexIsZero)
            this._animator.Play("Empty", 1, 0f);

        this._animator.Play(animatorState.AnimationHash, -1, animatorState.AnimationNormalizedTime);
        this._animator.Update(0f);
        this._animator.speed = 0f;
    }

    // Called from animation events
    private void EnableCanDealMeleeDamage() { }
    private void DisableCanDealMeleeDamage() { }
    private void EnableCanCombo() { }
    private void DisableCanCombo() { }
    private void EnableCanBeParried() { }
    private void DisableCanBeParried() { }
}
