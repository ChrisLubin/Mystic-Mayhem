using Unity.Netcode;
using UnityEngine;

public class ResetAnimatorParameterEnter : StateMachineBehaviour
{
    [SerializeField] private string _targetParameter;
    [SerializeField] private ParameterType _type;
    [SerializeField] private float _resetFloatValue;
    [SerializeField] private int _resetIntValue;
    [SerializeField] private bool _resetBoolValue;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        NetworkObject networkObject = animator.GetComponent<NetworkObject>();
        if (networkObject == null || !networkObject.IsOwner) { return; }

        if (this._type == ParameterType.Float)
            animator.SetFloat(this._targetParameter, this._resetFloatValue);
        else if (this._type == ParameterType.Int)
            animator.SetInteger(this._targetParameter, this._resetIntValue);
        else if (this._type == ParameterType.Bool)
            animator.SetBool(this._targetParameter, this._resetBoolValue);
    }
}

public enum ParameterType
{
    Float,
    Int,
    Bool
}
