using UnityEngine;

public class PlayerAnimationInterrupted : StateMachineBehaviour
{
    private PlayerAnimationController _playerAnimationController;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo _, int __)
    {
        if (animator.name == "PlayerServerDummy(Clone)") { return; }

        if (this._playerAnimationController == null)
            this._playerAnimationController = animator.gameObject.GetComponent<PlayerAnimationController>();
        if (this._playerAnimationController == null)
            Debug.Log("PlayerAnimationController is still null!");

        this._playerAnimationController.AnimationInterrupted();
    }
}
