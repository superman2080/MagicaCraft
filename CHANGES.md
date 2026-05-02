# 변경 내역


>>> 투사체를 포함한 나중에 확장 가능한 형태로 개발하고 싶어. 소환수, 자가 버프, 범위 공격 등

## 1. 투사체 시스템 추가

즉시 효과 방식(`Bolt`로 바로 데미지)에서 **투사체 기반 시스템**으로 확장.
기존 명령어(`Bolt`, `Heal`, `Explode`, `Shield`)는 그대로 유지.

### 새 파일

#### `Assets/02. Scripts/Projectile/SpellProjectile.cs`
- Unity MonoBehaviour 투사체 (`Poolable` 상속)
- `Init(ctx, mana, startPos, direction, element, damage, speed, onHit, onUpdate)` 로 초기화
- `Update()` — `onUpdate(proj)` DSL 콜백 실행 (매 프레임)
- `OnTriggerEnter2D()` — `onHit(victim)` DSL 콜백 실행, 기본 동작은 직접 데미지
- 수명 초과(`maxLifetime`, 기본 5초) 시 자동 풀 반환
- `CircleCollider2D`(isTrigger), `Rigidbody2D`(gravityScale=0) 자동 설정

#### `Assets/02. Scripts/Projectile/SpellProjectilePool.cs`
- 씬에 배치하는 싱글톤 오브젝트 풀
- `prefab` 미지정 시 `CircleCollider2D` + `Rigidbody2D`로 자동 생성
- `Get()` / `Return(proj)` API

#### `Assets/02. Scripts/SpellLanguage/Runtime/SpellProjectileData.cs`
- DSL에서 `proj.xxx`로 접근하는 래퍼 클래스
- 읽기: `position`, `velocity`, `speed`, `damage`, `element`
- 쓰기: `SetVelocity(vel)`, `SetSpeed(speed)` (명령어 경유)

### 수정 파일

#### `SpellCommands.cs`
| 추가 항목 | 설명 |
|---|---|
| `ManaCostFirePerDamage = 0.5f` | 마나 비용 상수 |
| `Fire(startPos, dir, element, damage, speed [, onHit [, onUpdate]])` | 티어0 투사체 발사 |
| `SetVelocity(proj, vel)` | 투사체 속도 벡터 설정 |
| `SetSpeed(proj, speed)` | 방향 유지하며 속도 크기만 변경 |
| `RequireFunction(val, sig)` | 인자 검증 헬퍼 |
| `RegisterTier0`에 Fire, SetVelocity, SetSpeed 등록 | |

#### `Interpreter.cs`
| 추가 항목 | 설명 |
|---|---|
| `Run(List<Stmt>)` | SpellFunction 본문 직접 실행 (투사체 콜백용) |
| `EvalProjectileMember()` | `proj.position` 등 멤버 접근 지원 |
| `SubValues()` | Vector2 뺄셈 지원 (`target.position - self.position`) |
| `Normalize(vec)` 내장 함수 | 단위 벡터 반환 |

---

## 2. 디버그 UI 추가

### 새 파일

#### `Assets/02. Scripts/Debug/SpellDebugUI.cs`
- 빈 오브젝트에 컴포넌트로 추가하면 런타임에 UI 자동 생성 (프리팹 불필요)
- One Dark 테마, TMPro 기반

**조작:**

| 입력 | 동작 |
|---|---|
| `` ` `` (백쿼트) | 패널 토글 (Inspector에서 키 변경 가능) |
| `Ctrl+Enter` | 코드 실행 |
| 헤더 드래그 | 패널 이동 |
| `[▶ Run]` 버튼 | 코드 실행 |
| `[Clear Log]` 버튼 | 로그 지우기 |
| `[Reset Mana]` 버튼 | 마나 수동 초기화 |

**Inspector 설정:**

| 필드 | 설명 |
|---|---|
| `Caster` | 시전자 Entity (필수, 없으면 Run 시 오류) |
| `Target` | 대상 Entity (선택) |
| `Max Mana` | 실행마다 초기화되는 마나 총량 (기본 500) |
| `Toggle Key` | 패널 토글 키 (기본 BackQuote) |
| `Panel Size` | 패널 크기 (기본 680×520) |

**디버그 전용 DSL 명령어:**
- `Print(value)` — 콘솔 로그에 값 출력

**로그 표시:**
- 실행 성공: 경과 시간, 마나 소모량/잔여량
- 런타임 오류: `[오류 줄:열]` + 해당 소스 줄 표시
- 마나 부족: 필요량/잔여량 표시

---
## 사용 예시 (DSL)

```
// 1. 기본 직선 투사체
Fire(self.position, target.position - self.position, Fire, 50, 10)

// 2. 피격 시 커스텀 처리
def onHit(victim) {
    Explode(victim.position, 2, Fire, 30)
}
Fire(self.position, target.position - self.position, Fire, 50, 10, onHit)

// 3. 유도 투사체 (onUpdate로 매 프레임 방향 수정)
def onUpdate(proj) {
    let dir = Normalize(target.position - proj.position)
    SetVelocity(proj, dir * proj.speed)
}
Fire(self.position, target.position - self.position, Fire, 50, 10, onHit, onUpdate)

// 4. 디버그 출력
Print(self.hp)
Print(target.position)
```

---

## 씬 설정 체크리스트

- [ ] 빈 오브젝트에 `SpellProjectilePool` 추가 (투사체 사용 시)
- [ ] `SpellProjectilePool.prefab`에 SpellProjectile 프리팹 연결 (없으면 자동 생성)
- [ ] 빈 오브젝트에 `SpellDebugUI` 추가
- [ ] `SpellDebugUI.Caster`에 플레이어 Entity 연결
