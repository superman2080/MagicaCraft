using System;
using System.Collections.Generic;
using UnityEngine;

// 노드 하나의 직렬화 데이터.
// nodeType은 풀 타입명(예: "BoltEffectNode") — SpellGraphSO.Build()에서 리플렉션으로 인스턴스 생성.
[Serializable]
public class SpellNodeData
{
    public string   nodeId;
    public string   nodeType;
    public Vector2  editorPosition;

    public List<SpellParamData> parameters = new();

    // 디스패처 연결 (nodeId 참조)
    public List<string> onEnterConnections   = new();
    public List<string> onExecuteConnections = new();
    public List<string> onExitConnections    = new();

    // ProjectileNode 전용 추가 디스패처
    public List<string> hitConnections    = new();
    public List<string> expireConnections = new();
}
