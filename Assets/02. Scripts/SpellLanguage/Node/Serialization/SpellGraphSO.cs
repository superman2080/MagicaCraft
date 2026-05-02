using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// SpellGraph 전체를 직렬화하는 ScriptableObject.
// Build()로 런타임 SpellGraph 인스턴스를 복원한다.
[CreateAssetMenu(menuName = "MagicaCraft/SpellGraph", fileName = "NewSpellGraph")]
public class SpellGraphSO : ScriptableObject
{
    public string graphName = "새 그래프";
    public string castEntryNodeId;
    public string updateEntryNodeId;
    public string endEntryNodeId;

    public List<SpellNodeData> nodes = new();

    // SO → 런타임 SpellGraph 복원.
    // 실패 시 null 반환이 아닌 예외 throw — 잘못된 그래프 데이터를 런타임에 숨기지 않는다.
    public SpellGraph Build()
    {
        var graph     = new SpellGraph { Name = graphName };
        var nodeMap   = new Dictionary<string, SpellNode>();

        // 1단계: 노드 인스턴스 생성 + 파라미터 바인딩
        foreach (var data in nodes)
        {
            var node = CreateNode(data);
            BindParameters(node, data.parameters);
            nodeMap[data.nodeId] = node;
            graph.AddNode(node);
        }

        // 2단계: 디스패처 연결
        foreach (var data in nodes)
        {
            if (!nodeMap.TryGetValue(data.nodeId, out var node))
                continue;

            ConnectDispatcher(node.OnEnterDispatcher,   data.onEnterConnections,   nodeMap, data.nodeId);
            ConnectDispatcher(node.OnExecuteDispatcher, data.onExecuteConnections, nodeMap, data.nodeId);
            ConnectDispatcher(node.OnExitDispatcher,    data.onExitConnections,    nodeMap, data.nodeId);

            if (node is ProjectileNode projNode)
            {
                ConnectDispatcher(projNode.OnProjectileHitDispatcher,     data.hitConnections,    nodeMap, data.nodeId);
                ConnectDispatcher(projNode.OnProjectileExpiredDispatcher,  data.expireConnections, nodeMap, data.nodeId);
            }
        }

        // 3단계: 진입 노드 할당
        graph.CastEntryNode   = Resolve(castEntryNodeId,   nodeMap);
        graph.UpdateEntryNode = Resolve(updateEntryNodeId, nodeMap);
        graph.EndEntryNode    = Resolve(endEntryNodeId,    nodeMap);

        return graph;
    }

    // ─── 내부 헬퍼 ────────────────────────────────────────────

    private static SpellNode CreateNode(SpellNodeData data)
    {
        var type = Type.GetType(data.nodeType)
                   ?? throw new InvalidOperationException(
                       $"[SpellGraphSO] 노드 타입을 찾을 수 없음: '{data.nodeType}'");
        if (!typeof(SpellNode).IsAssignableFrom(type))
            throw new InvalidOperationException(
                $"[SpellGraphSO] '{data.nodeType}'은 SpellNode를 상속하지 않음");

        var node = (SpellNode)Activator.CreateInstance(type);
        node.EditorPosition = data.editorPosition;

        // NodeId는 직렬화된 값으로 교체 (리플렉션으로 private setter 우회)
        typeof(SpellNode)
            .GetField("<NodeId>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(node, data.nodeId);

        return node;
    }

    private static void BindParameters(SpellNode node, List<SpellParamData> parameters)
    {
        var type = node.GetType();
        foreach (var param in parameters)
        {
            var prop = type.GetProperty(param.name, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null || !prop.CanWrite) continue;

            try
            {
                object value = ParseValue(param.value, param.type);
                prop.SetValue(node, value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"[SpellGraphSO] 파라미터 바인딩 실패 — {type.Name}.{param.name}: {ex.Message}");
            }
        }
    }

    private static object ParseValue(string raw, string typeName)
    {
        return typeName switch
        {
            "float"       => float.Parse(raw),
            "int"         => int.Parse(raw),
            "bool"        => bool.Parse(raw),
            "string"      => raw,
            "ElementType" => Enum.Parse<ElementType>(raw),
            "Vector2"     => ParseVector2(raw),
            _             => throw new NotSupportedException($"지원하지 않는 파라미터 타입: '{typeName}'"),
        };
    }

    private static Vector2 ParseVector2(string raw)
    {
        // 형식: "x,y"
        var parts = raw.Split(',');
        if (parts.Length != 2)
            throw new FormatException($"Vector2 파싱 실패: '{raw}' (형식: \"x,y\")");
        return new Vector2(float.Parse(parts[0].Trim()), float.Parse(parts[1].Trim()));
    }

    private static void ConnectDispatcher(
        SpellEventDispatcher dispatcher,
        List<string>         connections,
        Dictionary<string, SpellNode> nodeMap,
        string sourceNodeId)
    {
        foreach (var targetId in connections)
        {
            if (!nodeMap.TryGetValue(targetId, out var target))
                throw new InvalidOperationException(
                    $"[SpellGraphSO] 연결 대상 노드를 찾을 수 없음: '{sourceNodeId}' → '{targetId}'");
            dispatcher.Connect(target);
        }
    }

    private static SpellNode Resolve(string nodeId, Dictionary<string, SpellNode> nodeMap)
    {
        if (string.IsNullOrEmpty(nodeId)) return null;
        if (!nodeMap.TryGetValue(nodeId, out var node))
            throw new InvalidOperationException(
                $"[SpellGraphSO] 진입 노드를 찾을 수 없음: '{nodeId}'");
        return node;
    }
}
