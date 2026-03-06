// 속성 상성 테이블
// chart[공격속성, 방어속성] = 데미지 배율
//
// 배율 종류
//   2.0 : 효과가 뛰어남  (Super Effective)
//   1.0 : 보통           (Normal)
//   0.5 : 효과가 별로    (Not Very Effective)
//   0.0 : 효과 없음      (Immune)
//
// 상성 요약
//   Fire      : Ice에 강함 / Water·Fire에 약함
//   Water     : Fire·Earth에 강함 / Water·Ice·Lightning에 약함
//   Ice       : Earth·Wind에 강함 / Fire·Water·Ice에 약함
//   Lightning : Water·Ice·Wind에 강함 / Earth에 무효 / Lightning에 약함
//   Earth     : Lightning에 강함 / Wind에 무효 / Earth에 약함
//   Wind      : Earth에 강함 / Lightning·Wind에 약함
//   Light     : Dark에 강함 / Light에 약함
//   Dark      : Light에 강함 / Dark에 약함
//   None      : 항상 1.0
public static class ElementChart
{
    // 행(Row)    = 공격 속성 (ElementType enum 순서)
    // 열(Column) = 방어 속성 (ElementType enum 순서)
    // None=0, Fire=1, Water=2, Ice=3, Lightning=4, Earth=5, Wind=6, Light=7, Dark=8
    private static readonly float[,] _chart =
    {
        //              None   Fire   Water  Ice    Lightning  Earth  Wind   Light  Dark
        /* None      */ { 1.0f, 1.0f,  1.0f,  1.0f,  1.0f,     1.0f,  1.0f,  1.0f,  1.0f },
        /* Fire      */ { 1.0f, 0.5f,  0.5f,  2.0f,  1.0f,     1.0f,  1.0f,  1.0f,  1.0f },
        /* Water     */ { 1.0f, 2.0f,  0.5f,  0.5f,  1.0f,     2.0f,  1.0f,  1.0f,  1.0f },
        /* Ice       */ { 1.0f, 0.5f,  0.5f,  0.5f,  1.0f,     2.0f,  2.0f,  1.0f,  1.0f },
        /* Lightning */ { 1.0f, 1.0f,  2.0f,  2.0f,  0.5f,     0.0f,  2.0f,  1.0f,  1.0f },
        /* Earth     */ { 1.0f, 1.0f,  1.0f,  1.0f,  2.0f,     0.5f,  0.0f,  1.0f,  1.0f },
        /* Wind      */ { 1.0f, 1.0f,  1.0f,  1.0f,  0.5f,     2.0f,  0.5f,  1.0f,  1.0f },
        /* Light     */ { 1.0f, 1.0f,  1.0f,  1.0f,  1.0f,     1.0f,  1.0f,  0.5f,  2.0f },
        /* Dark      */ { 1.0f, 1.0f,  1.0f,  1.0f,  1.0f,     1.0f,  1.0f,  2.0f,  0.5f },
    };

    // 공격 속성 → 방어 속성 엔티티에 대한 데미지 배율을 반환한다.
    public static float GetMultiplier(ElementType attacker, ElementType defender)
    {
        return _chart[(int)attacker, (int)defender];
    }

    // 복합 속성(이중 속성)을 가진 엔티티에 대한 최종 배율을 계산한다.
    // 포켓몬과 동일하게 각 속성 배율을 모두 곱한다.
    // 예: Water 공격 vs Fire/Earth 엔티티 = 2.0 × 2.0 = 4.0
    public static float GetMultiplier(ElementType attacker, ElementType[] defenderAffinities)
    {
        if (defenderAffinities == null || defenderAffinities.Length == 0)
            return GetMultiplier(attacker, ElementType.None);

        float multiplier = 1.0f;
        foreach (var affinity in defenderAffinities)
            multiplier *= GetMultiplier(attacker, affinity);
        return multiplier;
    }
}
