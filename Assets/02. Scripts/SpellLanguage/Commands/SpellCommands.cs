using System;
using System.Collections.Generic;
using UnityEngine;

// 기본 마법 명령어 모음
// SpellContext에 등록해 사용한다.
//
// 등록 예시 (게임 초기화 시):
//   SpellCommands.RegisterAll(context);
//
// 티어별 해금 예시:
//   SpellCommands.RegisterTier0(context); // 시작 시
//   SpellCommands.RegisterTier1(context); // 티어 1 해금 시
public static class SpellCommands
{
    // ─── 마나 비용 상수 ───────────────────────────────────────
    // 각 명령어의 효과 단위당 마나 비용. 밸런스 조정 시 여기만 수정한다.

    // Bolt: 피해량 1당 소모 마나
    public const float ManaCostBoltPerDamage    = 1.0f;
    // Heal: 회복량 1당 소모 마나 (공격보다 저렴)
    public const float ManaCostHealPerAmount    = 0.8f;
    // Explode: 피해량 1당 기본 비용 + 반경 보정 (광역일수록 비쌈)
    public const float ManaCostExplodePerDamage = 1.0f;
    public const float ManaCostExplodeRadiusMul = 0.5f;
    // Shield: 보호량 1당 소모 마나
    public const float ManaCostShieldPerAmount  = 0.7f;
    // Fire: 투사체 피해량 1당 소모 마나 (Bolt보다 저렴 — 빗나갈 수 있으므로)
    public const float ManaCostFirePerDamage    = 0.5f;

    // ─── 일괄 등록 헬퍼 ───────────────────────────────────────

    public static void RegisterTier0(SpellContext ctx)
    {
        ctx.RegisterCommand("Bolt",        Bolt);
        ctx.RegisterCommand("Heal",        Heal);
        ctx.RegisterCommand("Fire",        Fire);
        ctx.RegisterCommand("SetVelocity", SetVelocity);
        ctx.RegisterCommand("SetSpeed",    SetSpeed);
    }

    public static void RegisterTier1(SpellContext ctx)
    {
        ctx.RegisterCommand("Explode", Explode);
        ctx.RegisterCommand("Shield",  Shield);
    }

    // ─── 티어 0 명령어 ────────────────────────────────────────

    // Bolt(target, element, damage)
    // 단일 대상에게 속성 데미지를 준다.
    // 마나 비용: damage * ManaCostBoltPerDamage
    //
    // 사용 예:
    //   Bolt(target, Fire, power * 1.5);
    //   Bolt(target, None, 30);
    private static object Bolt(List<object> args, SpellContext ctx, int line, int col)
    {
        const string sig = "Bolt(target, element, damage)";

        if (args.Count != 3)
            throw new RuntimeException($"{sig}: 인자 3개 필요", line, col);

        var target   = RequireEntity(args[0], sig);
        var element  = RequireElement(args[1], sig);
        float damage = (float)RequireDouble(args[2], sig);

        ctx.Mana.Spend(damage * ManaCostBoltPerDamage, line, col);
        target.TakeDamage(ctx.Caster as IAttackable, damage, element);
        return null;
    }

    // Heal(target, amount)
    // 대상의 HP를 회복한다.
    // 마나 비용: amount * ManaCostHealPerAmount
    //
    // 사용 예:
    //   Heal(self, power * 0.5);
    //   Heal(target, 20);
    private static object Heal(List<object> args, SpellContext ctx, int line, int col)
    {
        const string sig = "Heal(target, amount)";

        if (args.Count != 2)
            throw new RuntimeException($"{sig}: 인자 2개 필요", line, col);

        var target   = RequireEntity(args[0], sig);
        float amount = (float)RequireDouble(args[1], sig);

        ctx.Mana.Spend(amount * ManaCostHealPerAmount, line, col);
        target.Heal(ctx.Caster, amount);
        return null;
    }

    // ─── 투사체 명령어 (티어 0) ──────────────────────────────

    // Fire(startPos, direction, element, damage, speed [, onHit [, onUpdate]])
    // startPos  — 발사 위치 (Vec2 또는 Entity)
    // direction — 발사 방향 벡터 (자동 정규화). 예: target.position - self.position
    // element   — 기본 피격 속성 (onHit 없을 때 사용)
    // damage    — 기본 피격 데미지 (onHit 없을 때 사용)
    // speed     — 이동 속도
    // onHit     — (선택) def onHit(victim) { ... }  피격 시 실행
    // onUpdate  — (선택) def onUpdate(proj) { ... } 매 프레임 실행 (유도·경로 변경)
    //
    // 마나 비용: damage * ManaCostFirePerDamage
    //
    // 사용 예:
    //   Fire(self.position, target.position - self.position, Fire, 50, 10)
    //
    //   def onHit(victim) { Explode(victim.position, 2, Fire, 30) }
    //   Fire(self.position, target.position - self.position, Fire, 50, 10, onHit)
    //
    //   def onUpdate(proj) {
    //     let dir = target.position - proj.position
    //     SetVelocity(proj, dir * proj.speed)
    //   }
    //   Fire(self.position, target.position - self.position, Fire, 50, 10, onHit, onUpdate)
    private static object Fire(List<object> args, SpellContext ctx, int line, int col)
    {
        const string sig = "Fire(startPos, direction, element, damage, speed [, onHit [, onUpdate]])";

        if (args.Count < 5)
            throw new RuntimeException($"{sig}: 최소 5개 인자 필요", line, col);

        var   startPos  = RequireVector2(args[0], sig);
        var   direction = RequireVector2(args[1], sig);
        var   element   = RequireElement(args[2], sig);
        float damage    = (float)RequireDouble(args[3], sig);
        float speed     = (float)RequireDouble(args[4], sig);

        SpellFunction onHit    = args.Count > 5 ? RequireFunction(args[5], sig) : null;
        SpellFunction onUpdate = args.Count > 6 ? RequireFunction(args[6], sig) : null;

        ctx.Mana.Spend(damage * ManaCostFirePerDamage, line, col);

        if (SpellProjectilePool.Instance == null)
            throw new RuntimeException(
                "SpellProjectilePool이 씬에 없습니다. Pool 오브젝트를 씬에 추가하세요.", line, col);

        var proj = SpellProjectilePool.Instance.Get();
        proj.Init(ctx, ctx.Mana, startPos, direction, element, damage, speed, onHit, onUpdate);

        return null;
    }

    // SetVelocity(proj, velocity)
    // onUpdate 안에서 투사체 이동 방향·속도를 설정한다.
    // 예: SetVelocity(proj, Normalize(target.position - proj.position) * proj.speed)
    private static object SetVelocity(List<object> args, SpellContext ctx, int line, int col)
    {
        const string sig = "SetVelocity(proj, velocity)";

        if (args.Count != 2)
            throw new RuntimeException($"{sig}: 인자 2개 필요", line, col);

        if (args[0] is not SpellProjectileData proj)
            throw new RuntimeException($"{sig}: 첫 번째 인자는 투사체여야 합니다.", line, col);

        var vel = RequireVector2(args[1], sig);
        proj.SetVelocity(vel);
        return null;
    }

    // SetSpeed(proj, speed)
    // 이동 방향은 유지한 채 속도 크기만 변경한다.
    // 예: SetSpeed(proj, proj.speed * 1.5)
    private static object SetSpeed(List<object> args, SpellContext ctx, int line, int col)
    {
        const string sig = "SetSpeed(proj, speed)";

        if (args.Count != 2)
            throw new RuntimeException($"{sig}: 인자 2개 필요", line, col);

        if (args[0] is not SpellProjectileData proj)
            throw new RuntimeException($"{sig}: 첫 번째 인자는 투사체여야 합니다.", line, col);

        float speed = (float)RequireDouble(args[1], sig);
        proj.SetSpeed(speed);
        return null;
    }

    // ─── 티어 1 명령어 ────────────────────────────────────────

    // Explode(center, radius, element, damage)
    // 지정 위치 주변 radius 범위 내 모든 적에게 속성 데미지를 준다.
    // 마나 비용: damage * (ManaCostExplodePerDamage + radius * ManaCostExplodeRadiusMul)
    // → 반경이 넓을수록 비쌈
    //
    // 사용 예:
    //   Explode(target.position, 3, Fire, power);
    private static object Explode(List<object> args, SpellContext ctx, int line, int col)
    {
        const string sig = "Explode(center, radius, element, damage)";

        if (args.Count != 4)
            throw new RuntimeException($"{sig}: 인자 4개 필요", line, col);

        var center   = RequireVector2(args[0], sig);
        float radius = (float)RequireDouble(args[1], sig);
        var element  = RequireElement(args[2], sig);
        float damage = (float)RequireDouble(args[3], sig);

        float manaCost = damage * (ManaCostExplodePerDamage + radius * ManaCostExplodeRadiusMul);
        ctx.Mana.Spend(manaCost, line, col);

        // 반경 내 IDamagable 탐색 후 데미지 적용
        // IAttackable을 구현한 경우 해당 레이어만, 아니면 전체 탐색
        var attacker  = ctx.Caster as IAttackable;
        var layerMask = attacker != null ? (int)attacker.AttackLayer : Physics2D.AllLayers;
        var hits      = Physics2D.OverlapCircleAll(center, radius, layerMask);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamagable>(out var damagable))
                damagable.TakeDamage(attacker, damage, element);
        }

        return null;
    }

    // Shield(target, amount)
    // 대상에게 임시 HP 보호막을 부여한다. (구현은 Entity 확장 시 교체)
    // 마나 비용: amount * ManaCostShieldPerAmount
    //
    // 사용 예:
    //   Shield(self, power * 2);
    private static object Shield(List<object> args, SpellContext ctx, int line, int col)
    {
        const string sig = "Shield(target, amount)";

        if (args.Count != 2)
            throw new RuntimeException($"{sig}: 인자 2개 필요", line, col);

        var target   = RequireEntity(args[0], sig);
        float amount = (float)RequireDouble(args[1], sig);

        ctx.Mana.Spend(amount * ManaCostShieldPerAmount, line, col);
        // TODO: 보호막 시스템 구현 후 교체. 임시로 즉시 회복으로 처리
        target.Heal(ctx.Caster, amount);
        return null;
    }

    // ─── 인자 검증 헬퍼 ───────────────────────────────────────

    private static Entity RequireEntity(object val, string sig)
    {
        if (val is Entity e) return e;
        throw new RuntimeException($"{sig}: Entity 타입 인자가 필요합니다. (self 또는 target 사용)");
    }

    private static ElementType RequireElement(object val, string sig)
    {
        if (val is ElementType el) return el;
        throw new RuntimeException($"{sig}: 속성(Fire, Water 등) 인자가 필요합니다.");
    }

    private static double RequireDouble(object val, string sig)
    {
        if (val is double d) return d;
        throw new RuntimeException($"{sig}: 숫자 인자가 필요합니다.");
    }

    private static Vector2 RequireVector2(object val, string sig)
    {
        if (val is Vector2 v) return v;
        if (val is Entity  e) return e.Rb2d != null ? e.Rb2d.position : Vector2.zero;
        throw new RuntimeException($"{sig}: Vec2 또는 Entity 타입 인자가 필요합니다.");
    }

    private static SpellFunction RequireFunction(object val, string sig)
    {
        if (val is SpellFunction fn) return fn;
        throw new RuntimeException($"{sig}: def로 정의한 함수를 인자로 전달해야 합니다.");
    }
}
