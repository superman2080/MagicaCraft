using System;
using System.Collections;
using UnityEngine;

namespace Player
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
    }

    public class PlayerCompositeMove : CompositeStateBase
    {
        private Player player;
        private PlayerDash dashState;

        public PlayerCompositeMove(Player caster) : base(caster)
        {
            player = caster;
            SubStateMachine.RegisterState(PlayerMoveState.Walk, new PlayerWalk(caster));
            SubStateMachine.RegisterState(PlayerMoveState.Dash, new PlayerDash(caster));


            dashState = SubStateMachine.GetState(PlayerMoveState.Dash) as PlayerDash;

            SubStateMachine.RegisterCondition(PlayerMoveState.Walk, PlayerMoveState.Dash, () => player.InputHandler.DashPressed && dashState.dashCor == null);
            SubStateMachine.RegisterCondition(PlayerMoveState.Dash, PlayerMoveState.Walk, () => player.InputHandler.DashPressed == false 
            && dashState.dashCor != null);
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
                float moveDelta = caster.DefaultStat.Get(DefaultStatType.MOVE_SPEED) * Time.fixedDeltaTime;
                caster.Rb2d.MovePosition(caster.Rb2d.position + player.InputHandler.MoveInput * moveDelta);
            }
        }

        [Serializable]
        public class DashConfig
        {
            [Tooltip("대쉬")] public AnimationCurve dashSpeed = AnimationCurve.Linear(0, 1, 1, 0);
            public float dashTime;
        }

        public class PlayerDash: StateBase
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
            // 초기 상태 설정
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