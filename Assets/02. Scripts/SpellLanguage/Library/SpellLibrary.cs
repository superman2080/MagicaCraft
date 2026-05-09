using System.Collections.Generic;

// 마법 아이템 레지스트리 + 잠금/해금 시스템.
//
// 사용 흐름:
//   1. Register(item)   — 게임 시작 시 모든 아이템을 DB에 등록
//   2. UnlockTier(0)    — 기본 아이템 해금
//   3. Unlock("name")   — 개별 아이템 해금 (획득 시)
//   4. ApplyToContext(ctx) — 해금된 아이템을 SpellContext에 반영
public class SpellLibrary
{
    private readonly Dictionary<string, SpellItem> registry = new();
    private readonly HashSet<string>               unlocked  = new();

    // SpellItem을 레지스트리에 등록한다 (해금 없이 DB에만 추가).
    public void Register(SpellItem item)
    {
        if (item != null) registry[item.Name] = item;
    }

    // 특정 이름의 아이템을 해금한다.
    public void Unlock(string name) => unlocked.Add(name);

    // 해당 티어의 모든 아이템을 해금한다.
    public void UnlockTier(int tier)
    {
        foreach (var (name, item) in registry)
            if (item.Tier == tier)
                unlocked.Add(name);
    }

    public bool IsUnlocked(string name) => unlocked.Contains(name);

    // 해금된 아이템을 SpellContext에 적용한다.
    //   SpellAction  → 명령어로 등록
    //   SpellObject  → SpellObject 변수로 등록
    public void ApplyToContext(SpellContext ctx)
    {
        foreach (var name in unlocked)
        {
            if (!registry.TryGetValue(name, out var item)) continue;

            switch (item)
            {
                case SpellAction action: action.RegisterTo(ctx);              break;
                case SpellObject obj:    ctx.RegisterSpellObject(name, obj);  break;
            }
        }
    }

    // ─── 기본 라이브러리 팩토리 ───────────────────────────────

    // 티어 0이 해금된 기본 SpellLibrary를 반환한다.
    // 디버그 UI 등 빠른 초기화에 사용한다.
    public static SpellLibrary CreateDefault()
    {
        var lib = new SpellLibrary();

        // 티어 0 액션
        lib.Register(new BoltAction        { Tier = 0 });
        lib.Register(new HealAction        { Tier = 0 });
        lib.Register(new FireAction        { Tier = 0 });
        lib.Register(new SetVelocityAction { Tier = 0 });
        lib.Register(new SetSpeedAction    { Tier = 0 });

        // 티어 1 액션
        lib.Register(new ExplodeAction { Tier = 1 });
        lib.Register(new BarriorAction  { Tier = 1 });

        // 기본 투사체
        lib.Register(new ProjectileObject
        {
            Name           = "basicBolt",
            Description    = "기본 투사체",
            Tier           = 0,
            DefaultDamage  = 10f,
            DefaultSpeed   = 10f,
            DefaultElement = ElementType.None,
        });

        // 티어 0 전체 해금
        lib.UnlockTier(0);

        return lib;
    }
}
