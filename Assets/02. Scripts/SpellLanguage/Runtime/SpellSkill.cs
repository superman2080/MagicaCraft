using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 플레이어가 버튼에 배치하여 발동하는 마법 스킬.
// 각 슬롯(OnStart / OnUpdate / OnEnd)은 고정 코드 + 편집 가능 매개변수 + 플레이어 추가 코드로 구성된다.
//
// 스킬 타입:
//   OneShot    — 버튼 1회 입력 시 OnStart만 실행
//   Continuous — 버튼 다운→OnStart, 홀드→OnUpdate(매프레임), 업→OnEnd
//
// 마나 소진 시 동작:
//   ManaTracker.OnDepleted 이벤트를 구독하여 Cancel()을 호출한다.
//   IsCancelled == true이면 RunOnUpdate / RunOnEnd는 실행되지 않는다.

public enum SkillType
{
    OneShot,    // 단발성
    Continuous, // 유지성
}

// ─── SkillParameter ──────────────────────────────────────────
// 고정 코드(LockedCode)에서 사용하는 편집 가능 변수.
// Value는 DSL 표현식 문자열로 저장된다.
// 예: Name="speed", Value="10"  /  Name="direction", Value="target.position - self.position"
public class SkillParameter
{
    public string Name         { get; set; }
    public string DefaultValue { get; set; }
    public string Value        { get; set; }

    public SkillParameter(string name, string defaultValue)
    {
        Name         = name;
        DefaultValue = defaultValue;
        Value        = defaultValue;
    }

    // 값을 기본값으로 초기화한다.
    public void Reset() => Value = DefaultValue;
}

// ─── SkillCodeSlot ───────────────────────────────────────────
// 라이프사이클 슬롯 하나 (OnStart / OnUpdate / OnEnd).
// 실행 시 BuildCode()로 세 부분을 하나의 DSL 코드로 조합한다.
public class SkillCodeSlot
{
    // 수정 불가 고정 코드 — 스킬의 핵심 동작
    public string               LockedCode  { get; set; } = "";
    // 고정 코드에서 사용하는 편집 가능 매개변수
    public List<SkillParameter> Parameters  { get; set; } = new List<SkillParameter>();
    // 플레이어가 자유롭게 작성하는 추가 코드
    public string               UserCode    { get; set; } = "";

    // 실행할 최종 DSL 코드를 조합한다.
    // 결과: "let param1 = val1;\n...\nLockedCode\nUserCode"
    public string BuildCode()
    {
        var sb = new StringBuilder();

        // 1. 매개변수를 let 선언으로 주입
        foreach (var param in Parameters)
        {
            string val = string.IsNullOrWhiteSpace(param.Value) ? param.DefaultValue : param.Value;
            sb.AppendLine($"let {param.Name} = {val};");
        }

        // 2. 고정 코드
        if (!string.IsNullOrWhiteSpace(LockedCode))
            sb.AppendLine(LockedCode);

        // 3. 플레이어 추가 코드
        if (!string.IsNullOrWhiteSpace(UserCode))
            sb.AppendLine(UserCode);

        return sb.ToString();
    }
}

// ─── SpellSkill ──────────────────────────────────────────────
public class SpellSkill
{
    public string    Name        { get; set; } = "새 스킬";
    public string    Description { get; set; } = "";
    public SkillType Type        { get; set; }
    public float     ManaCost    { get; set; } = 0f;

    // OneShot / Continuous 모두 사용
    public SkillCodeSlot OnStart  { get; set; } = new SkillCodeSlot();
    // Continuous 전용 (OneShot일 때는 무시)
    public SkillCodeSlot OnUpdate { get; set; } = new SkillCodeSlot();
    public SkillCodeSlot OnEnd    { get; set; } = new SkillCodeSlot();

    // 마나 소진 등으로 강제 중단 시 true.
    // RunOnUpdate / RunOnEnd 진입 시 즉시 반환한다.
    public bool IsCancelled { get; private set; }

    private ManaTracker _subscribedMana;

    // ─── 라이프사이클 실행 ────────────────────────────────────

    // OneShot: 버튼 입력 시 호출
    // Continuous: 버튼 다운 시 호출
    public void RunOnStart(SpellContext ctx, ManaTracker mana)
    {
        IsCancelled = false;

        // 마나 소진 구독 (Continuous는 Cancel, OneShot도 중단 신호 용도)
        _subscribedMana = mana;
        mana.OnDepleted += Cancel;

        ctx.Mana.Spend(ManaCost, 0, 0);
        RunSlot(OnStart, ctx, mana);
    }

    // Continuous 전용: 버튼 홀드 중 매 프레임 호출
    public void RunOnUpdate(SpellContext ctx, ManaTracker mana)
    {
        if (IsCancelled) return;
        RunSlot(OnUpdate, ctx, mana);
    }

    // Continuous 전용: 버튼 업 시 호출
    // 마나가 소진된 경우 실행하지 않는다.
    public void RunOnEnd(SpellContext ctx, ManaTracker mana)
    {
        if (IsCancelled) return;
        RunSlot(OnEnd, ctx, mana);
        Unsubscribe();
    }

    // 강제 중단 — ManaTracker.OnDepleted에 연결되거나 외부에서 직접 호출한다.
    public void Cancel()
    {
        IsCancelled = true;
        Unsubscribe();
    }

    // ─── 내부 헬퍼 ────────────────────────────────────────────

    private void Unsubscribe()
    {
        if (_subscribedMana != null)
        {
            _subscribedMana.OnDepleted -= Cancel;
            _subscribedMana = null;
        }
    }

    private void RunSlot(SkillCodeSlot slot, SpellContext ctx, ManaTracker mana)
    {
        string code = slot.BuildCode();
        if (string.IsNullOrWhiteSpace(code)) return;

        try
        {
            var tokens = new Lexer(code).Tokenize();
            var ast    = new Parser(tokens).Parse();
            var interp = new Interpreter(ctx, mana);
            interp.Run(ast);
        }
        catch (OutOfManaException)
        {
            // 마나 소진 — ManaTracker.OnDepleted가 Cancel()을 호출하므로 별도 처리 불필요
        }
        catch (RuntimeException ex)
        {
            Debug.LogWarning($"[SpellSkill:{Name}] {ex.Message}");
        }
        catch (LexerException ex)
        {
            Debug.LogWarning($"[SpellSkill:{Name}] 코드 오류: {ex.Message}");
        }
        catch (ParserException ex)
        {
            Debug.LogWarning($"[SpellSkill:{Name}] 코드 오류: {ex.Message}");
        }
    }
}
