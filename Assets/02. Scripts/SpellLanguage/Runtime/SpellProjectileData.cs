using UnityEngine;

// 투사체의 DSL 접근 래퍼
// Interpreter에서 proj.position, proj.velocity 등으로 읽을 수 있다.
// 이동 변경은 SetVelocity / SetSpeed 명령어를 통해 SpellProjectile에 직접 위임한다.
public class SpellProjectileData
{
    internal readonly SpellProjectile Projectile;

    public SpellProjectileData(SpellProjectile proj) { Projectile = proj; }

    // ─── 읽기 전용 멤버 (DSL에서 proj.xxx 로 접근) ─────────────

    public Vector2    Position => Projectile.Rb2d.position;
    public Vector2    Velocity => Projectile.Rb2d.linearVelocity;
    public float      Speed    => Projectile.Speed;
    public float      Damage   => Projectile.Damage;
    public ElementType Element => Projectile.Element;

    // ─── 쓰기 (SetVelocity / SetSpeed 명령어에서 호출) ─────────

    public void SetVelocity(Vector2 vel)
    {
        Projectile.Rb2d.linearVelocity = vel;
    }

    public void SetSpeed(float speed)
    {
        Vector2 dir = Projectile.Rb2d.linearVelocity.normalized;
        Projectile.Rb2d.linearVelocity = dir * speed;
        Projectile.Speed = speed;
    }
}
