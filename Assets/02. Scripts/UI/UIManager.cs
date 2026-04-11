using System;
using System.Collections.Generic;

public class UIManager : Singleton<UIManager>
{
    private readonly Dictionary<Type, SubUIManager> layers = new();

    // SubUIManager.Start()에서 자동 호출
    public void Register(SubUIManager layer)
    {
        layers[layer.GetType()] = layer;
    }

    // 미등록 레이어는 null 반환
    public T Get<T>() where T : SubUIManager
    {
        return layers.TryGetValue(typeof(T), out var layer) ? layer as T : null;
    }

    // 등록 여부 확인 + 레이어 동시 획득
    public bool TryGet<T>(out T layer) where T : SubUIManager
    {
        if (layers.TryGetValue(typeof(T), out var found) && found is T typed)
        {
            layer = typed;
            return true;
        }

        layer = null;
        return false;
    }
}
