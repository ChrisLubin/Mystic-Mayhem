using UnityEngine;

public class ResetAnimatorParameterExit : StateMachineBehaviour
{
    [SerializeField] private string _targetParameter;
    private int _targetParameterHash;
    [SerializeField] private ParameterType _type;
    [SerializeField] private float _resetFloatValue;
    [SerializeField] private int _resetIntValue;
    [SerializeField] private bool _resetBoolValue;

    private void Awake() => this._targetParameterHash = Animator.StringToHash(this._targetParameter);

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (this._type == ParameterType.Float)
            animator.SetFloat(this._targetParameterHash, this._resetFloatValue);
        else if (this._type == ParameterType.Int)
            animator.SetInteger(this._targetParameterHash, this._resetIntValue);
        else if (this._type == ParameterType.Bool)
            animator.SetBool(this._targetParameterHash, this._resetBoolValue);
    }
}
