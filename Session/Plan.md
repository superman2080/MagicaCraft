# Plan — 오브젝트 풀 팩토리 메서드 패턴 리팩터링

## 개요

단일 `Pool : Singleton<Pool>`이 `SerializedDictionary<Enum, Poolable>`로
모든 IPoolable 객체를 한 곳에서 관리한다.
데이터 주입은 `Get<T>(key, Action<T> initializer)` 오버로드로 처리한다:
caller가 람다로 초기화 코드를 전달하면 Pool이 `OnSpawn()` 전에 실행한다.
서브클래스·오버라이드 없이 Inspector 프리팹 주입만으로 새 타입 추가 가능.

---

## 단계별 구현 계획

### [완료] 단계 1 — `IPoolable` 인터페이스 신설

**파일**: `Assets/02. Scripts/Pool/IPoolable.cs` (신규)

```csharp
public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}
```

---

### [완료] 단계 2 — `Poolable` → `IPoolable` 구현으로 변환

**파일**: `Assets/02. Scripts/Pool/Poolable.cs`

```csharp
public abstract class Poolable : MonoBehaviour, IPoolable
{
    public virtual void OnSpawn()   { gameObject.SetActive(true); }
    public virtual void OnDespawn() { gameObject.SetActive(false); }
}
```

---

### [완료] 단계 3 — `Pool : Singleton<Pool>` 재작성

**파일**: `Assets/02. Scripts/Pool/Pool.cs`

- `Singleton<Pool>` 상속으로 싱글톤 구현 (Awake 수동 작성 불필요)
- `SerializedDictionary<Enum, Poolable>` — Inspector에서 키·프리팹 주입
- `Get(Enum key)` — 기본 꺼내기
- `Get<T>(Enum key, Action<T> initializer)` — 초기 데이터 주입 오버로드
  - `initializer` 실행 → `OnSpawn()` 순서 보장 (활성화 전 데이터 설정)
- `Return(Enum key, IPoolable obj)` — 반환
- `CreateNew(Enum key)` — private, prefabs 딕셔너리에서 Instantiate

```csharp
using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

public class Pool : Singleton<Pool>
{
    [SerializeField] private SerializedDictionary<Enum, Poolable> prefabs = new();
    [SerializeField] private int initialSize = 5;

    private readonly Dictionary<Enum, Queue<IPoolable>> _pools = new();

    private void Start()
    {
        foreach (var kvp in prefabs)
        {
            var queue = new Queue<IPoolable>();
            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateNew(kvp.Key);
                obj.OnDespawn();
                queue.Enqueue(obj);
            }
            _pools[kvp.Key] = queue;
        }
    }

    private IPoolable CreateNew(Enum key)
    {
        if (!prefabs.TryGetValue(key, out var prefab) || prefab == null)
            throw new InvalidOperationException(
                $"[Pool] 키 '{key}'에 대한 프리팹이 등록되지 않았습니다.");
        return Instantiate(prefab);
    }

    private IPoolable GetInternal(Enum key)
    {
        if (!_pools.TryGetValue(key, out var queue))
            _pools[key] = queue = new Queue<IPoolable>();
        return queue.Count > 0 ? queue.Dequeue() : CreateNew(key);
    }

    public IPoolable Get(Enum key)
    {
        var obj = GetInternal(key);
        obj.OnSpawn();
        return obj;
    }

    // 데이터 주입 오버로드 — initializer 실행 후 OnSpawn() 호출
    public T Get<T>(Enum key, Action<T> initializer) where T : class, IPoolable
    {
        var obj = GetInternal(key) as T;
        initializer?.Invoke(obj);
        obj.OnSpawn();
        return obj;
    }

    public void Return(Enum key, IPoolable obj)
    {
        obj.OnDespawn();
        if (!_pools.TryGetValue(key, out var queue))
            _pools[key] = queue = new Queue<IPoolable>();
        queue.Enqueue(obj);
    }
}
```

---

### [완료] 단계 4 — `SpellProjectilePool` 제거 및 `PoolKey` enum 신설

**파일**: `Assets/02. Scripts/Projectile/SpellProjectilePool.cs` → **삭제**  
**파일**: `Assets/02. Scripts/Pool/PoolKey.cs` (신규)

```csharp
public enum PoolKey
{
    SpellProjectile,
}
```

Inspector에서 `Pool` 컴포넌트의 `prefabs`에 `PoolKey.SpellProjectile` → SpellProjectile 프리팹 할당.

---

### [완료] 단계 5 — `SpellProjectile.Init()` 콜백 주입으로 결합 제거

**파일**: `Assets/02. Scripts/Projectile/SpellProjectile.cs`

`SpellProjectilePool.Instance` 직접 참조를 제거한다.
`Init()` 호출 시 반환 콜백을 주입받아 `ReturnToPool()`에서 사용한다.

```csharp
private System.Action _returnCallback;

public void Init(
    SpellContext     ctx,
    ManaTracker      mana,
    Vector2          startPos,
    Vector2          direction,
    float            speed,
    System.Action    returnCallback,
    ProjectileObject projObj = null,
    ElementType      element = ElementType.None,
    float            damage  = 0f)
{
    // ... 기존 초기화 코드 유지 ...
    _returnCallback = returnCallback;
}

private void ReturnToPool()
{
    _isAlive = false;
    _returnCallback?.Invoke();
}

public override void OnDespawn()
{
    _returnCallback = null;
    // ... 기존 OnDespawn 코드 유지 ...
}
```

---

### [완료] 단계 6 — `FireAction` 수정

**파일**: `Assets/02. Scripts/SpellLanguage/Items/Actions/FireAction.cs`

`Get<T>(key, initializer)` 오버로드로 꺼내기와 데이터 주입을 한 번에 처리한다.

```csharp
var proj = Pool.Instance.Get<SpellProjectile>(PoolKey.SpellProjectile, p =>
    p.Init(ctx, ctx.Mana, startPos, direction, speed,
           returnCallback: () => Pool.Instance.Return(PoolKey.SpellProjectile, p),
           projObj: projObj));
```

---

## 완료 기준

- `IPoolable` 인터페이스로 풀링 계약 분리
- `Pool : Singleton<Pool>` 단일 싱글톤으로 모든 타입 관리
- `Get<T>(key, initializer)` 오버로드로 타입 안전한 데이터 주입
- 서브클래스·오버라이드 없이 Inspector 프리팹 주입만으로 새 타입 추가
- `SpellProjectile.cs`에 `SpellProjectilePool` 참조 없음
- `SpellProjectilePool.cs` 삭제
- Unity 컴파일 에러 없음, 투사체 발사 동작 정상
