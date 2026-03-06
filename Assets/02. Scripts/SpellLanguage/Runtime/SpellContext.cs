using System;
using System.Collections.Generic;

// 마법 실행 컨텍스트 - 인터프리터가 게임 세계와 상호작용하는 창구
// 시전자/타겟 Entity 참조와 게임 명령어 레지스트리를 보관한다.
public class SpellContext
{
    public Entity Caster { get; }

    // 마법의 대상 (광역기나 자기 대상은 null 허용)
    public Entity Target { get; }

    // 마나 추적기 - Interpreter 생성 시 주입된다.
    // 명령어에서 ctx.Mana.Spend(cost, line, col)으로 마나를 소모한다.
    public ManaTracker Mana { get; internal set; }

    // 게임 명령어 레지스트리
    // key  : 명령어 이름 (Bolt, Heal, Explode …)
    // value: (평가된 인자 목록, 컨텍스트, 줄번호, 열번호) → 반환값
    private readonly Dictionary<string, Func<List<object>, SpellContext, int, int, object>> _commands = new();

    public SpellContext(Entity caster, Entity target)
    {
        Caster = caster;
        Target = target;
    }

    #region 명령어 등록

    // 게임 명령어를 등록한다 (게임 초기화 시 또는 해금 시 호출)
    public void RegisterCommand(string name, Func<List<object>, SpellContext, int, int, object> impl)
    {
        _commands[name] = impl;
    }

    public bool HasCommand(string name) => _commands.ContainsKey(name);

    // 등록된 명령어를 실행한다. 미등록 명령어는 RuntimeException
    public object ExecuteCommand(string name, List<object> args, int line, int col)
    {
        if (!_commands.TryGetValue(name, out var cmd))
            throw new RuntimeException(
                $"'{name}'은 해금되지 않았거나 존재하지 않는 명령어입니다.", line, col);

        return cmd(args, this, line, col);
    }

    #endregion

    #region 내장 변수 주입

    // 마법 실행 시작 및 함수 호출 시 항상 사용 가능한 변수를 환경에 주입한다.
    // self, target, power 등 플레이어가 언제나 참조할 수 있는 값들
    public void InjectBuiltins(SpellEnvironment env)
    {
        // 엔티티 참조
        env.Define("self",   Caster);
        env.Define("target", (object)Target);

        // TODO: 전용 power(공격력) 스탯 추가 시 이 줄을 교체
        env.Define("power", (double)Caster.DefaultStat.Get(DefaultStatType.MAX_HP) * 0.25);

        // 속성 상수 - 식별자로 직접 사용 (예: Bolt(target, Fire, 30))
        env.Define("None",      ElementType.None);
        env.Define("Fire",      ElementType.Fire);
        env.Define("Water",     ElementType.Water);
        env.Define("Ice",       ElementType.Ice);
        env.Define("Lightning", ElementType.Lightning);
        env.Define("Earth",     ElementType.Earth);
        env.Define("Wind",      ElementType.Wind);
        env.Define("Light",     ElementType.Light);
        env.Define("Dark",      ElementType.Dark);
    }

    #endregion
}
