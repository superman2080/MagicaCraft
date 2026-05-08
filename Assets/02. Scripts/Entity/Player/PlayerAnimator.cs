using PlayerNamespace;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Player owner;
    private Animator animator;

    private readonly int moveHash = Animator.StringToHash("1_Move");
    private readonly int attackHash = Animator.StringToHash("2_Attack");
    private readonly int damageHash = Animator.StringToHash("3_Damaged");

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = gameObject.GetComponentInChildren<Animator>();
        owner = gameObject.GetComponent<Player>();

        owner.OnTakeDamage += (s, c, a) => animator.SetTrigger(damageHash);
        owner.StateMachine.OnStateChanged += (from, to) => {
            if (owner.StateMachine.CompareState(from, PlayerState.IDLE) && owner.StateMachine.CompareState(to, PlayerState.MOVE)) {
                animator.SetBool(moveHash, true);
            }
            else if(owner.StateMachine.CompareState(from, PlayerState.MOVE) && owner.StateMachine.CompareState(to, PlayerState.IDLE))
            {
                animator.SetBool(moveHash, false);
            }
        };
        owner.OnAttack += (s, t, a) => animator.SetTrigger(attackHash);
    }
}
