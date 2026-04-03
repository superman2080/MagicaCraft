using System;
using System.Collections;
using UnityEngine;

namespace PlayerNamespace
{
    public enum PlayerState
    {
        Idle,
        Move,
        Dash,
    }

    public enum PlayerMoveState
    {
        Walk,
        Dash,
        Jump
    }

    public class PlayerIdle : StateBase
    {
        public PlayerIdle(Player caster) : base(caster)
        {
        }

        public override void FixedExecute()
        {
            base.FixedExecute();
            Vector2 vel = caster.Rb2d.linearVelocity;
            vel.x = 0f;
            caster.Rb2d.linearVelocity = vel;
        }
    }

    public class PlayerCompositeMove : CompositeStateBase
    {
        private Player player;
        private PlayerDash dashState;
        private PlayerJump jumpState;

        public PlayerCompositeMove(Player caster) : base(caster)
        {
            player = caster;
            SubStateMachine.RegisterState(PlayerMoveState.Walk, new PlayerWalk(caster));
            SubStateMachine.RegisterState(PlayerMoveState.Dash, new PlayerDash(caster));
            SubStateMachine.RegisterState(PlayerMoveState.Jump, new PlayerJump(caster));

            dashState = SubStateMachine.GetState(PlayerMoveState.Dash) as PlayerDash;
            jumpState = SubStateMachine.GetState(PlayerMoveState.Jump) as PlayerJump;

            SubStateMachine.RegisterCondition(PlayerMoveState.Walk, PlayerMoveState.Dash, () => player.InputHandler.DashPressed && dashState.dashCor == null);
            SubStateMachine.RegisterCondition(PlayerMoveState.Dash, PlayerMoveState.Walk, () => player.InputHandler.DashPressed == false
            && dashState.dashCor != null);
            SubStateMachine.RegisterAnyStateTransition(PlayerMoveState.Jump, () => player.InputHandler.JumpPressed && jumpState.isGrounded == true);
        }

        public override void Enter()
        {
            base.Enter();

            SubStateMachine.ChangeState(PlayerMoveState.Walk);
        }

        public class PlayerWalk : StateBase
        {
            private Player player;
            public PlayerWalk(Player caster) : base(caster)
            {
                player = caster;
            }

            public override void FixedExecute()
            {
                base.FixedExecute();
                float moveSpeed = caster.DefaultStat.Get(DefaultStatType.MOVE_SPEED);

                Vector2 vel = caster.Rb2d.linearVelocity;
                vel.x = player.InputHandler.MoveInput.x * moveSpeed;
                caster.Rb2d.linearVelocity = vel;
            }
        }

        [Serializable]
        public class DashConfig
        {
            [Tooltip("대시")] public AnimationCurve dashSpeed = AnimationCurve.Linear(0, 1, 1, 0);
            public float dashTime;
        }

        public class PlayerDash : StateBase
        {
            public Coroutine dashCor;
            private Player player;
            private DashConfig dashConfig;

            public PlayerDash(Player caster) : base(caster)
            {
                player = caster;
            }

            public override void Enter()
            {
                base.Enter();
                //dashCor = player.StartCoroutine(DashCoroutine());
            }
        }

        public class PlayerJump : StateBase
        {
            public bool isGrounded = true;

            public PlayerJump(Player caster) : base(caster)
            {
            }

            public override void Enter()
            {
                base.Enter();
                Vector2 vel = caster.Rb2d.linearVelocity;
                vel.y = caster.DefaultStat.Get(DefaultStatType.JUMP_FORCE);
                caster.Rb2d.AddForce(vel, ForceMode2D.Impulse);
            }

            public override void FixedExecute()
            {
                base.FixedExecute();
                var col = caster.Col as BoxCollider2D;
                Vector2 feetPos = (Vector2)caster.transform.position
                                  + col.offset
                                  + Vector2.down * (col.size.y / 2f + 0.05f);
                isGrounded = Physics2D.OverlapBox(
                    feetPos,
                    new Vector2(col.size.x - 0.1f, 0.1f),
                    0f,
                    LayerMask.GetMask("Ground")
                );
            }
        }
    }

    [RequireComponent(typeof(PlayerInputHandler))]
    public class Player : Entity, IAttackable
    {
        [HideInInspector] public Animator animator;
        public override Enum EntryStateKey => entryStateKey;
        [SerializeField] private PlayerState entryStateKey;

        public PlayerInputHandler InputHandler { get; private set; }

        public AttackStat AttackStat => attackStat;
        [SerializeField] private AttackStat attackStat;

        public AttackStatDataSO AttackStatData => attackStatData;
        [SerializeField] private AttackStatDataSO attackStatData;

        public LayerMask AttackLayer => 1 << LayerMask.NameToLayer("Enemy") | LayerMask.NameToLayer("Entity");

        protected override void Start()
        {
            InputHandler = GetComponent<PlayerInputHandler>();
            animator = gameObject.GetComponent<Animator>();
            base.Start();
        }

        protected override void InitializeStateMachine()
        {
            base.InitializeStateMachine();
            stateMachine.RegisterState(PlayerState.Idle, new PlayerIdle(this));
            stateMachine.RegisterState(PlayerState.Move, new PlayerCompositeMove(this));

            stateMachine.RegisterCondition(PlayerState.Idle, PlayerState.Move, () => InputHandler.MoveInput != Vector2.zero || InputHandler.JumpPressed == true);
            stateMachine.RegisterCondition(PlayerState.Move, PlayerState.Idle, () => InputHandler.MoveInput == Vector2.zero && InputHandler.JumpPressed == false);
        }

        protected override void Update()
        {
            base.Update();
        }

        public void Attack(IDamagable target, float amount)
        {
            throw new NotImplementedException();
        }
    }
}
