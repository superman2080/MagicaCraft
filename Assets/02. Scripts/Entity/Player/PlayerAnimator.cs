using PlayerNamespace;
using UnityEngine;

[DefaultExecutionOrder(10)]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Transform modelTr;

    [SerializeField] private Player owner;
    [SerializeField] private Animator animator;

    private readonly int moveHash = Animator.StringToHash("1_Move");
    private readonly int attackHash = Animator.StringToHash("2_Attack");
    private readonly int damageHash = Animator.StringToHash("3_Damaged");

    private Vector3 flipX = new Vector3(-2, 2, 2);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        modelTr = transform.GetChild(0);

        owner ??= gameObject.GetComponent<Player>();
        animator ??= gameObject.GetComponentInChildren<Animator>();

        owner.OnTakeDamage += (s, c, a, t) => animator.SetTrigger(damageHash);
        owner.StateMachine.OnStateChanged += (from, to) => {
            if (owner.StateMachine.CompareState(from, PlayerState.Idle) && owner.StateMachine.CompareState(to, PlayerState.Move)) {
                
                animator?.SetBool(moveHash, true);
            }
            else if(owner.StateMachine.CompareState(from, PlayerState.Move) && owner.StateMachine.CompareState(to, PlayerState.Idle))
            {
                animator?.SetBool(moveHash, false);
            }
        };
        owner.OnAttack += (s, t, a) => animator.SetTrigger(attackHash);
    }

    void Update()
    {
        FlipX(owner.FacingDirection > 0f);
    }

    public void FlipX(bool b)
    {
        modelTr.localScale = b ? flipX : Vector3.one * 2f;
    }
}
