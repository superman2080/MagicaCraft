using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace PlayerNamespace
{

    #region PlayerStates
    public enum PlayerState
    {
        Idle,
        Move,
        Death,
    }

    public enum PlayerMoveState
    {
        Walk,
        Dash,
        Jump
    }

    public class PlayerIdle : StateBase<Entity>
    {
        private Player player;
        public PlayerIdle(Player caster) : base(caster)
        {
            player = caster;
        }

        public override void Enter()
        {
            base.Enter();
        }

        public override void FixedExecute()
        {
            base.FixedExecute();
            Vector2 vel = caster.Rb2d.linearVelocity;
            vel.x = 0f;
            caster.Rb2d.linearVelocity = vel;
        }
    }

    public class PlayerCompositeMove : CompositeStateBase<Entity>
    {
        private Player player;
        private PlayerDash dashState;

        public PlayerCompositeMove(Player caster) : base(caster)
        {
            player = caster;
            SubStateMachine.RegisterState(PlayerMoveState.Walk, new PlayerWalk(caster));
            SubStateMachine.RegisterState(PlayerMoveState.Dash, new PlayerDash(caster, caster.DashConfig));
            SubStateMachine.RegisterState(PlayerMoveState.Jump, new PlayerJump(caster));

            dashState = SubStateMachine.GetState(PlayerMoveState.Dash) as PlayerDash;

            SubStateMachine.RegisterCondition(PlayerMoveState.Walk, PlayerMoveState.Dash, 
                () => player.InputHandler.DashPressed && dashState.dashCor == null);
            SubStateMachine.RegisterCondition(PlayerMoveState.Dash, PlayerMoveState.Walk,
                () => dashState.dashCor == null);

            SubStateMachine.RegisterCondition(PlayerMoveState.Walk, PlayerMoveState.Jump, () => player.InputHandler.JumpPressed && player.IsGrounded);
            SubStateMachine.RegisterCondition(PlayerMoveState.Jump, PlayerMoveState.Walk, () => player.IsGrounded && caster.Rb2d.linearVelocity.y <= 0f);
        }

        public override void Enter()
        {
            base.Enter();
            if (player.InputHandler.JumpPressed && player.IsGrounded)
                SubStateMachine.ChangeState(PlayerMoveState.Jump);
            else
                SubStateMachine.ChangeState(PlayerMoveState.Walk);
        }

        public class PlayerWalk : StateBase<Entity>
        {
            private Player player;
            public PlayerWalk(Player caster) : base(caster)
            {
                player = caster;
            }

            public override void FixedExecute()
            {
                base.FixedExecute();
                float moveSpeed = player.MoveStat.Get(MoveStatType.MOVE_SPEED);

                Vector2 vel = caster.Rb2d.linearVelocity;
                vel.x = player.InputHandler.MoveInput.x * moveSpeed;
                caster.Rb2d.linearVelocity = vel;

                if (player.InputHandler.MoveInput.x != 0f)
                    player.FacingDirection = Mathf.Sign(player.InputHandler.MoveInput.x);
            }
        }

        [Serializable]
        public class DashConfig
        {
            [Tooltip("대시 속도 커브 (X=정규화 시간 0~1, Y=배율)")]
            public AnimationCurve dashSpeed = AnimationCurve.Linear(0, 1, 1, 0);
            public float dashTime;
            public float baseSpeed;
        }

        public class PlayerDash : StateBase<Entity>
        {
            public Coroutine dashCor;
            private Player player;
            private DashConfig dashConfig;
            private float dashDir;
            private float originalGravityScale;

            public PlayerDash(Player caster, DashConfig config) : base(caster)
            {
                player = caster;
                dashConfig = config;
            }

            public override void Enter()
            {
                base.Enter();
                dashDir = player.InputHandler.MoveInput.x != 0f
                    ? Mathf.Sign(player.InputHandler.MoveInput.x)
                    : player.FacingDirection;

                originalGravityScale = caster.Rb2d.gravityScale;
                caster.Rb2d.gravityScale = 0f;
                var vel = caster.Rb2d.linearVelocity;
                vel.y = 0f;
                caster.Rb2d.linearVelocity = vel;

                dashCor = player.StartCoroutine(DashCoroutine());
            }

            public override void Exit()
            {
                base.Exit();
                caster.Rb2d.gravityScale = originalGravityScale;
                if (dashCor != null)
                {
                    player.StopCoroutine(dashCor);
                    dashCor = null;
                }
            }

            private IEnumerator DashCoroutine()
            {
                for (float elapsed = 0; elapsed < dashConfig.dashTime; elapsed += Time.fixedDeltaTime)
                {
                    float t = elapsed / dashConfig.dashTime;
                    float speed = dashConfig.dashSpeed.Evaluate(t) * dashConfig.baseSpeed;

                    Vector2 vel = caster.Rb2d.linearVelocity;
                    vel.x = dashDir * speed;
                    vel.y = 0f;
                    caster.Rb2d.linearVelocity = vel;

                    yield return null;
                }

                dashCor = null;
            }
        }

        public class PlayerJump : StateBase<Entity>
        {
            private Player player;

            public PlayerJump(Player caster) : base(caster)
            {
                player = caster;
            }

            public override void Enter()
            {
                base.Enter();
                caster.Rb2d.AddForce(Vector2.up * player.MoveStat.Get(MoveStatType.JUMP_FORCE), ForceMode2D.Impulse);
            }
        }
    }

    public class PlayerDeath : StateBase<Entity>
    {
        private Player player;
        public PlayerDeath(Player caster) : base(caster)
        {
            player = caster;
        }
    }
    #endregion

    [DefaultExecutionOrder(0)]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class Player : Entity, IAttackable, IMovable, IBarriorUsable
    {
        public override Enum EntryStateKey => entryStateKey;
        [SerializeField] private PlayerState entryStateKey;

        public PlayerInputHandler InputHandler { get; private set; }

        public event OnAttackHandler OnAttack;

        #region Stat & StatDatas
        public AttackStat AttackStat => attackStat;
        [SerializeField] private AttackStat attackStat;

        public AttackStatDataSO AttackStatData => attackStatData;
        [SerializeField] private AttackStatDataSO attackStatData;

        public MoveStat MoveStat => moveStat;
        private MoveStat moveStat;

        public MoveStatDataSO MoveStatData => moveStatData;
        [SerializeField] private MoveStatDataSO moveStatData;

        [SerializeField] private PlayerCompositeMove.DashConfig dashConfig;
        public PlayerCompositeMove.DashConfig DashConfig => dashConfig;

        public float FacingDirection { get; set; } = -1f;

        #endregion

        #region Barrior
        public Barrior Barrior => barrior;
        private Barrior barrior = new Barrior();

        #endregion


        public LayerMask AttackLayer => 1 << LayerMask.NameToLayer("Enemy") | LayerMask.NameToLayer("Entity");

        private int groundContactCount = 0;
        public bool IsGrounded => groundContactCount > 0;


        private void OnCollisionEnter2D(Collision2D col)
        {
            if (col.gameObject.layer == LayerMask.NameToLayer("Ground"))
                groundContactCount++;
        }

        private void OnCollisionExit2D(Collision2D col)
        {
            if (col.gameObject.layer == LayerMask.NameToLayer("Ground"))
                groundContactCount--;
        }

        protected override void Start()
        {
            InputHandler = GetComponent<PlayerInputHandler>();
            base.Start();
            moveStat = new MoveStat(moveStatData);
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            moveStat.Update();
            barrior.Update();
        }

        protected override void InitializeStateMachine()
        {
            base.InitializeStateMachine();
            stateMachine.RegisterState(PlayerState.Idle, new PlayerIdle(this));
            stateMachine.RegisterState(PlayerState.Move, new PlayerCompositeMove(this));
            stateMachine.RegisterState(PlayerState.Death, new PlayerDeath(this));

            stateMachine.RegisterCondition(PlayerState.Idle, PlayerState.Move, () => InputHandler.MoveInput != Vector2.zero || InputHandler.JumpPressed == true);
            stateMachine.RegisterCondition(PlayerState.Move, PlayerState.Idle, () => InputHandler.MoveInput == Vector2.zero && InputHandler.JumpPressed == false && IsGrounded && Rb2d.linearVelocity.y <= 0f);
            stateMachine.RegisterAnyStateTransition(PlayerState.Death, () => HP <= 0f);
        }

        public override void TakeDamage(IAttackable caster, float amount, ElementType element = ElementType.None)
        {


            if(barrior.ExistBarrior())
            {
                float multiplier = GetDamageMultiplier(element);
                float finalAmount = Mathf.Abs(amount) * multiplier;
                barrior.TakeDamage(finalAmount, out float remain);
                base.TakeDamage(caster, remain, element);
            }
            else
                base.TakeDamage(caster, amount, element);
        }

        public void Attack(IDamagable target, float amount)
        {
            OnAttack?.Invoke(this, target, amount);
            throw new NotImplementedException();
        }


    }
}
