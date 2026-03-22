using System;
using System.Collections.Generic;
using UnityEngine;

// 기본 마법 명령어 모음
// SpellContext에 등록해 사용한다.
//
// 등록 예시 (게임 초기화 시):
//   SpellCommands.RegisterTier0(context);
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
    //   Bolt(target, Fire, power * 1.5)
    //   Bolt(victim, None, 30)
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
    //   Heal(self, power * 0.5)
    //   Heal(target, 20)
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

    // ─── 아이템 명령어 (티어 0) ──────────────────────────────

    // Fire(spellObj, startPos, direction, speed)
    // SpellEffectItem(SpellObject)을 월드에 생성·발사한다.
    // spellObj  — SpellEffectItem (인벤토리에서 등록된 SpellObject 아이템)
    // startPos  — 발사 위치 (Vec2 또는 Entity)
    // direction — 발사 방향 벡터 (자동 정규화)
    // speed     — 이동 속도
    //
    // 마나 비용: projItem.ManaCost
    //
    // 사용 예:
    //   Fire(fireBolt, self.position, target.position - self.position, 10)
    private static object Fire(List<object> args, SpellContext ctx, int line, int col)
    {
        const string sig = "Fire(projItem, startPos, direction, speed)";

        if (args.Count != 4)
            throw new RuntimeException($"{sig}: 인자 4개 필요", line, col);

        var item      = RequireEffectItem(args[0], sig, SpellEffectType.SpellObject);
        var startPos  = RequireVector2(args[1], sig);
        var direction = RequireVector2(args[2], sig);
        float speed   = (float)RequireDouble(args[3], sig);

        ctx.Mana.Spend(item.ManaCost, line, col);

        if (SpellProjectilePool.Instance == null)
            throw new RuntimeException(
                "SpellProjectilePool이 씬에 없습니다. Pool 오브젝트를 씬에 추가하세요.", line, col);

        var proj = SpellProjectilePool.Instance.Get();
        proj.Init(ctx, ctx.Mana, startPos, direction, speed, item);

        return null;
    }

    // SetVelocity(proj, velocity)
    // OnUpdate 안에서 투사체 이동 방향·속도를 설정한다.
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
    //
    // 사용 예 (아이템 OnEnd 코드 안에서):
    //   Explode(victim.position, 3, Fire, 50)
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
    // 사용 예 (아이템 OnStart 코드 안에서):
    //   Shield(self, 100)
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

    // expectedType이 None이 아니면 타입 일치 여부를 검증한다.
    private static SpellEffectItem RequireEffectItem(object val, string sig,
                                                     SpellEffectType expectedType)
    {
        if (val is not SpellEffectItem item)
            throw new RuntimeException(
                $"{sig}: SpellEffectItem이 필요합니다. ctx.RegisterItem()으로 등록 후 사용하세요.");

        if (item.EffectType != expectedType)
            throw new RuntimeException(
                $"{sig}: {expectedType} 타입 아이템이 필요하지만 {item.EffectType} 타입이 전달되었습니다.");

        return item;
    }
}
