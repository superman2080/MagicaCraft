# Research: UIManager 설계

## 목표

인게임 UI 전체를 총괄하는 싱글톤 `UIManager`와,
각 레이어별 UI를 담당하는 `SubUIManager` 계층 구조 설계.

---

## 기존 코드 분석

### Singleton<T> — `Utils/Singleton.cs`

```csharp
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance { get; }   // 없으면 자동 생성
    protected virtual void Awake()      // dontDestroy 처리
}
```
- `UIManager : Singleton<UIManager>` 패턴으로 바로 적용 가능.
- `dontDestroy`는 `readonly bool` — 서브클래스에서 생성자 override 불가. 필요 시 `Awake`에서 직접 처리.

### TimerManager — `Utils/TimerManager.cs`
- `Singleton<TimerManager>` 상속의 실제 구현 예시.
- 자체 `private List` 보유, 의존 객체들은 외부에서 주입.

### `Assets/02. Scripts/UI/` 폴더
- 현재 비어 있음 — 신규 구현.

---

## Unity UI 레이어링 방식

Unity에서 UI 레이어 우선순위는 **Canvas의 `sortingOrder`** 로 제어한다.
- `ScreenSpaceOverlay` Canvas는 `sortingOrder` 높을수록 앞에 렌더링.
- 각 레이어마다 Canvas GameObject를 분리하면 레이어 단위 Show/Hide 가능.

---

## 제안 레이어 구조

| 레이어 클래스 | sortingOrder | 용도 |
|---|---|---|
| `BackgroundLayer` | 0 | 배경 패널, 월드 UI |
| `HUDLayer` | 100 | HP바, 마나바, 스킬 버튼 |
| `MenuLayer` | 200 | 인벤토리, 스킬트리, 설정 |
| `PopupLayer` | 300 | 확인 다이얼로그, 툴팁 |
| `OverlayLayer` | 400 | 로딩화면, 페이드, 알림 |

---

## 의존성 및 영향 범위

- 신규 파일만 생성. 기존 코드 변경 없음.
- `SpellDebugUI.cs`는 별도 Canvas를 직접 생성하므로 UIManager와 무관하게 유지.
- `Singleton<T>` 그대로 활용.

---

## 핵심 설계 결정

### SubUIManager — abstract MonoBehaviour
- 각 레이어 Canvas를 소유
- `Show()` / `Hide()` / `IsVisible` 공통 인터페이스 제공
- 구체 타입에서 레이어별 UI 요소를 추가로 정의

### UIManager
- 각 레이어를 **명명된 프로퍼티**로 노출 (`UIManager.Instance.HUD`, `.Popup` 등)
- `Awake`에서 레이어 GameObject를 자식으로 생성하고 SubUIManager 컴포넌트를 추가
- 정렬 순서는 `UILayerOrder` 상수 클래스로 중앙 관리

### 레이어 생성 헬퍼
```csharp
private T CreateLayer<T>(int sortingOrder) where T : SubUIManager
```
Canvas + CanvasScaler + GraphicRaycaster를 자동으로 추가해 보일러플레이트 제거.
