// 투사체 타입 SpellObject.
// SpellProjectile(MonoBehaviour)이 인스턴스별 데이터(proj, victim)를
// instanceVars로 RunStart/Update/Finish에 전달한다.
//
// DSL 슬롯별 주입 변수:
//   OnStart / OnUpdate : proj (SpellProjectileData)
//   OnFinish           : proj (SpellProjectileData), victim (Entity)
public class ProjectileObject : SpellObject
{
    public ElementType DefaultElement { get; set; } = ElementType.None;
    public float       DefaultDamage  { get; set; } = 10f;
    public float       DefaultSpeed   { get; set; } = 10f;
}
