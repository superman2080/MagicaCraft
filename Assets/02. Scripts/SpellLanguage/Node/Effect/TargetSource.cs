// Effect 노드 대상 선택 방식
public enum TargetSource
{
    ContextTarget,    // SpellNodeContext.Target 사용
    BlackboardVictim, // Blackboard["victim"] Entity 사용
}
