# Research — 오브젝트 풀 팩토리 메서드 패턴 리팩터링

## 현재 구조 분석

### Pool.cs (abstract MonoBehaviour)
```csharp
public abstract class Pool : MonoBehaviour
{
    public int initialSize = 10;
    public Poolable prefab;           // Inspector 할당

    // ⚠️ 버그: static instance가 추상 클래스에 선언 → 모든 서브클래스가 단 하나의 필드 공유
    private static Pool instance;
    public static Pool Instance { get { FindAnyObjectByType(typeof(Pool)) ... } }

    private Queue<Poolable> pool;

    protected virtual Poolable CreateNew() => Instantiate(prefab, transform);  // 가상 팩토리 메서드
    public virtual Poolable Get()           // 반환형: Poolable → 호출마다 캐스팅 필요
    public virtual Poolable Get(Vector3)
    public virtual Poolable Get(Vector3, Quaternion)
    public virtual Poolable Get(Vector3, Quaternion, Vector3)
    public virtual void Return(Poolable)
}
```

### Poolable.cs (abstract MonoBehaviour)
```csharp
public abstract class Poolable : MonoBehaviour
{
    public virtual void OnSpawn()   { gameObject.SetActive(true); }
    public virtual void OnDespawn() { gameObject.SetActive(false); }
}
```

### SpellProjectilePool.cs
```csharp
public class SpellProjectilePool : Pool
{
    // 자체 싱글톤 없음 → Pool.Instance(버그)를 통해 접근
    protected override Poolable CreateNew()  // prefab 없으면 코드로 GameObject 생성
}
```

### SpellProjectile.cs
```csharp
private void ReturnToPool()
{
    SpellProjectilePool.Instance.Return(this);  // ⚠️ 구체 풀 클래스 직접 참조
}
```

### FireAction.cs
```csharp
var proj = SpellProjectilePool.Instance.Get() as SpellProjectile;  // ⚠️ 구체 클래스 + 캐스팅
proj.Init(ctx, ctx.Mana, startPos, direction, speed, projObj);
```

---

## 문제점 정리

| # | 문제 | 위치 |
|---|------|------|
| 1 | **싱글톤 버그**: `static instance`가 추상 클래스에 선언 → 서브클래스 여러 개 공존 불가 | Pool.cs:9 |
| 2 | **비제네릭**: `Get()`이 `Poolable` 반환 → 모든 호출에서 캐스팅 필요 | Pool.cs:41-67 |
| 3 | **강한 결합**: `SpellProjectile`이 `SpellProjectilePool.Instance` 직접 참조 | SpellProjectile.cs:149 |
| 4 | **강한 결합**: `FireAction`이 `SpellProjectilePool.Instance` 직접 참조 | FireAction.cs:35,39 |
| 5 | `CreateNew()`가 virtual → 서브클래스가 오버라이드 안 해도 컴파일 통과 | Pool.cs:39 |

---

## 팩토리 메서드 패턴 GoF 매핑

| GoF 역할 | 현재 | 목표 |
|---|---|---|
| **Creator** (추상) | `Pool` (비제네릭, virtual factory) | `Pool<T> where T : Poolable` (abstract factory) |
| **Factory Method** | `CreateNew()` virtual | `CreateNew()` **abstract** |
| **ConcreteCreator** | `SpellProjectilePool : Pool` | `SpellProjectilePool : Pool<SpellProjectile>` |
| **Product** | `Poolable` (캐스팅 필요) | `T` (타입 안전) |

---

## 결합 제거 전략

`SpellProjectile.ReturnToPool()`과 `FireAction`의 `SpellProjectilePool` 직접 참조를 끊는 방법:

- `SpellProjectile.Init()`에 `Action returnCallback` 매개변수 추가
- `ReturnToPool()` → `_returnCallback?.Invoke()`만 호출 (풀 타입 모름)
- `FireAction`에서 `pool.Get()` 후 `() => pool.Return(proj)` 람다를 `Init()`에 전달

---

## 영향 범위

| 파일 | 변경 유형 |
|---|---|
| `Pool.cs` | 전면 재작성 (제네릭화, abstract factory method) |
| `SpellProjectilePool.cs` | 상속 타입 변경 (`Pool<SpellProjectile>`), 자체 싱글톤 추가 |
| `SpellProjectile.cs` | `Init()` 시그니처 변경 (returnCallback 추가), `ReturnToPool()` 수정 |
| `FireAction.cs` | 타입 안전한 Get() 사용, 콜백 전달 |
| `Poolable.cs` | 변경 없음 |
