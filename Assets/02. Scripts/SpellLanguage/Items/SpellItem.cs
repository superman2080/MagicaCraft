// 마법 아이템의 추상 기반 클래스.
// SpellObject (라이프사이클 객체)와 SpellAction (단발성 효과) 모두 이를 상속한다.
public abstract class SpellItem
{
    public string Name        { get; set; } = "";
    public string Description { get; set; } = "";
    // 0 = 처음부터 제공, 1+ = 해금 필요
    public int    Tier        { get; set; } = 0;
}
