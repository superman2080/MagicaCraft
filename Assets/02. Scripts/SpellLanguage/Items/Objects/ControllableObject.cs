// 플레이어가 DSL Update 코드로 직접 이동을 제어하는 SpellObject.
// 대응 MonoBehaviour(ControllableSpellBehaviour)가 매 프레임 instanceVars를 주입한다:
//   "vel"       — 현재 velocity (Vector2)
//   "moveSpeed" — 이동 속도 (double)
public class ControllableObject : SpellObject
{
    public float DefaultMoveSpeed { get; set; } = 5f;

    // 플레이어에게 제공하는 예시 기본값 (편집 가능)
    public static readonly string DefaultStartCode  = "# 객체 생성 시 실행";
    public static readonly string DefaultUpdateCode = "# 매 프레임 실행\n# SetVelocity(self, vel)";
    public static readonly string DefaultFinishCode = "# 객체 소멸 시 실행";
}
