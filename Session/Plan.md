# Plan — UIManager 구현

## 개요

`SubUIManager`가 `Start()`에서 UIManager에 자동 등록한다.
UIManager는 특정 레이어 타입을 코드에서 직접 선언하지 않으며,
`Dictionary<Type, SubUIManager>` + `Get<T>()` / `TryGet<T>()` 로 타입 안전하게 접근한다.

새 레이어 추가 시 UIManager 수정 불필요 — 클래스 작성 후 씬에 배치만 하면 된다.

---

## 파일 구조

```
Assets/02. Scripts/UI/
├── UIManager.cs
├── SubUIManager.cs
└── Layers/
    ├── BackgroundLayer.cs
    ├── HUDLayer.cs
    ├── MenuLayer.cs
    ├── PopupLayer.cs
    └── OverlayLayer.cs
```

---

## 씬 오브젝트 구조

레이어는 UIManager의 자식 GameObject로 씬에 배치한다.
각 레이어 GameObject는 Canvas + CanvasScaler + GraphicRaycaster + 해당 레이어 컴포넌트를 가진다.

```
[UIManager]
├── BackgroundLayer  ← Canvas(sortingOrder=0)   + BackgroundLayer 컴포넌트
├── HUDLayer         ← Canvas(sortingOrder=100) + HUDLayer 컴포넌트
├── MenuLayer        ← Canvas(sortingOrder=200) + MenuLayer 컴포넌트
├── PopupLayer       ← Canvas(sortingOrder=300) + PopupLayer 컴포넌트
└── OverlayLayer     ← Canvas(sortingOrder=400) + OverlayLayer 컴포넌트
```

---

## 단계별 구현 계획

### ✅ [완료] 단계 1 — `SubUIManager` 추상 기반 작성

**파일**: `Assets/02. Scripts/UI/SubUIManager.cs` (신규)

- 각 레이어가 `SortingOrder`를 직접 정의 (추상 프로퍼티)
- `Awake()`에서 Canvas를 가져와 `sortingOrder` 자동 적용
- `Start()`가 `private`이고 `UIManager.Instance.Register(this)`를 호출 → 서브클래스가 실수로 override해 등록을 누락하는 상황 방지

```csharp
public abstract class SubUIManager : MonoBehaviour
{
    public abstract int SortingOrder { get; }

    public Canvas Canvas { get; private set; }
    public bool IsVisible => gameObject.activeSelf;

    protected virtual void Awake()
    {
        Canvas = GetComponent<Canvas>();
        Canvas.sortingOrder = SortingOrder;
    }

    private void Start()
    {
        UIManager.Instance.Register(this);
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}
```

---

### ✅ [완료] 단계 2 — 구체 레이어 클래스 5개 작성

**파일**: `Assets/02. Scripts/UI/Layers/` (신규 5개)

각 클래스는 `SortingOrder`만 정의한다. 레이어별 고유 UI 요소는 추후 각 클래스 안에 확장한다.

```csharp
// BackgroundLayer.cs
public class BackgroundLayer : SubUIManager
{
    public override int SortingOrder => 0;
}

// HUDLayer.cs
public class HUDLayer : SubUIManager
{
    public override int SortingOrder => 100;
}

// MenuLayer.cs
public class MenuLayer : SubUIManager
{
    public override int SortingOrder => 200;
}

// PopupLayer.cs
public class PopupLayer : SubUIManager
{
    public override int SortingOrder => 300;
}

// OverlayLayer.cs
public class OverlayLayer : SubUIManager
{
    public override int SortingOrder => 400;
}
```

---

### ✅ [완료] 단계 3 — `UIManager` 작성

**파일**: `Assets/02. Scripts/UI/UIManager.cs` (신규)

UIManager는 레이어 타입을 전혀 모른다.
`Register()`를 통해 들어온 레이어를 타입 기반 딕셔너리로 보관한다.
새 레이어가 추가돼도 이 파일은 수정할 필요가 없다.

접근자는 두 가지를 제공한다:
- `Get<T>()` — 미등록 시 null 반환
- `TryGet<T>(out T)` — 등록 여부를 bool로 반환, out 파라미터로 레이어 전달

```csharp
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
```

---

## 사용 예시

```csharp
// 팝업 레이어 표시 (Get)
UIManager.Instance.Get<PopupLayer>()?.Show();

// 등록 여부 확인 후 접근 (TryGet)
if (UIManager.Instance.TryGet<HUDLayer>(out var hud))
    hud.Show();

// 새 레이어(TutorialLayer) 추가 — UIManager.cs 수정 없음
// 1. public class TutorialLayer : SubUIManager { public override int SortingOrder => 150; }
// 2. 씬에 GameObject 배치 (Canvas + TutorialLayer 컴포넌트)
// 완료
```

---

## OCP 달성 포인트

| 시나리오 | UIManager 수정 | SubUIManager 수정 |
|---|---|---|
| 새 레이어 추가 | ✅ 불필요 | ✅ 불필요 |
| 레이어 SortingOrder 변경 | ✅ 불필요 | ✅ 불필요 (해당 레이어만) |
| 레이어 UI 요소 추가 | ✅ 불필요 | ✅ 불필요 (해당 레이어만) |

---

## 완료 기준

- `Get<T>()` / `TryGet<T>()` 로 각 레이어 타입 안전하게 접근 가능
- 각 레이어가 SortingOrder 차순으로 올바르게 렌더링됨
- `Show()` / `Hide()`로 레이어 단위 토글 가능
- UIManager에 특정 레이어 타입 참조 없음
- 컴파일 에러 없음
