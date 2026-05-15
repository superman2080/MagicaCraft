# MagicaCraft DSL 문법 레퍼런스

플레이어가 마법을 작성할 때 사용하는 인게임 스크립팅 언어의 완전한 문법 사양입니다.

---

## 목차

1. [기본 규칙](#1-기본-규칙)
2. [리터럴과 타입](#2-리터럴과-타입)
3. [변수](#3-변수)
4. [연산자와 우선순위](#4-연산자와-우선순위)
5. [멤버 접근](#5-멤버-접근)
6. [제어 흐름](#6-제어-흐름)
7. [함수 정의와 호출](#7-함수-정의와-호출)
8. [마법 명령어](#8-마법-명령어)
9. [내장 함수](#9-내장-함수)
10. [사전 정의 변수](#10-사전-정의-변수)
11. [속성 상수](#11-속성-상수)
12. [티어 해금 구조](#12-티어-해금-구조)
13. [안전장치와 제한사항](#13-안전장치와-제한사항)
14. [오류 종류](#14-오류-종류)

---

## 1. 기본 규칙

### 구분자

모든 **문장**은 세미콜론(`;`)으로 끝납니다.

```
Bolt(target, Fire, 30);
let x = 10;
```

### 주석

`#`으로 시작하는 한 줄 주석을 지원합니다. 블록 주석(`/* */`)은 없습니다.

```
# 이것은 주석입니다
let damage = 30;  # 줄 끝 주석도 가능
```

### 공백

스페이스, 탭, 줄바꿈은 모두 무시됩니다. 코드를 자유롭게 포맷할 수 있습니다.

### 대소문자

- **키워드**: 소문자 (`let`, `if`, `while`, `def`, ...)
- **마법 명령어 / 내장 함수**: PascalCase (`Bolt`, `Vec2`, `Normalize`)
- **SpellObject 변수**: camelCase (`basicBolt`)
- **멤버 이름**: camelCase (`hp`, `maxHp`, `position`)
- **속성 상수**: PascalCase (`Fire`, `Water`, `None`)

---

## 2. 리터럴과 타입

모든 숫자는 내부적으로 `double`로 처리됩니다. 정수·실수 구분 없이 연산 가능합니다.

| 리터럴 | 예시 | 내부 타입 |
|---|---|---|
| 정수 | `30`, `100`, `0` | `double` |
| 실수 | `1.5`, `0.3`, `10.0` | `double` |
| 불리언 | `true`, `false` | `bool` |
| 문자열 | `"hello"` | `string` |

### 런타임 타입 목록

DSL에서 값으로 전달될 수 있는 모든 타입입니다.

| 타입 | 설명 | 생성 방법 |
|---|---|---|
| `double` | 모든 숫자 | 리터럴, 연산, 내장 함수 |
| `bool` | 논리값 | `true`/`false`, 비교 연산 |
| `string` | 문자열 | 문자열 리터럴 |
| `Vector2` | 2D 벡터 | `Vec2(x, y)` |
| `Entity` | 게임 엔티티 | `self`, `target`, `victim` |
| `ElementType` | 속성 | `Fire`, `Water` 등 상수 |
| `SpellProjectileData` | 투사체 데이터 | `proj` (콜백 내부) |
| `SpellObject` | 마법 오브젝트 | `basicBolt` 등 등록된 변수 |
| `SpellFunction` | 사용자 정의 함수 | `def`로 정의 |
| `null` | 값 없음 | `return;` (반환값 없음) |

---

## 3. 변수

### 선언 — `let`

```
let name = expression;
```

변수는 선언과 동시에 반드시 초기화해야 합니다. 타입 선언 없이 우변으로 추론됩니다.

```
let damage = 30;
let dir = target.position - self.position;
let ratio = self.hpRatio;
```

### 대입

선언된 변수의 값을 바꿉니다. `let` 없이 이름 뒤에 `=`를 씁니다.

```
let x = 1;
x = x + 1;
```

### 스코프

`{...}` 블록마다 새 스코프가 생깁니다. 안쪽에서 선언한 변수는 블록 밖에서 접근할 수 없습니다.

```
let outer = 10;
if (outer > 5) {
    let inner = 20;   # inner는 이 블록 안에서만 유효
}
# inner에 접근하면 오류
```

---

## 4. 연산자와 우선순위

우선순위 낮음 → 높음 순서입니다.

| 우선순위 | 연산자 | 설명 |
|---|---|---|
| 1 (낮음) | `\|\|` | 논리 OR (단락 평가) |
| 2 | `&&` | 논리 AND (단락 평가) |
| 3 | `==` `!=` | 동등 비교 |
| 4 | `<` `>` `<=` `>=` | 크기 비교 |
| 5 | `+` `-` | 덧셈 / 뺄셈 |
| 6 | `*` `/` `%` | 곱셈 / 나눗셈 / 나머지 |
| 7 | `!` `-` (단항) | 논리 부정 / 부호 반전 |
| 8 (높음) | `.` `()` | 멤버 접근 / 함수 호출 |

### 산술 연산자

```
let a = 10 + 5;     # 15
let b = 3 * 4 - 1;  # 11
let c = 10 / 3;     # 3.333...
let d = 10 % 3;     # 1
```

`+`는 문자열 연결도 지원합니다.

```
let msg = "damage: " + 30;   # "damage: 30"
```

`+`, `-`, `*`는 `Vector2`와 `double`의 혼합 연산을 지원합니다.

```
let dir = target.position - self.position;       # Vector2 - Vector2
let scaled = Normalize(dir) * proj.speed;        # Vector2 * double
```

### 비교 연산자

숫자 타입만 비교 가능합니다. 결과는 `bool`입니다.

```
let isLow = self.hpRatio < 0.3;
let isMax = self.mana == self.maxMana;
```

### 논리 연산자

**단락 평가(short-circuit)**: `&&`는 좌변이 거짓이면 우변을 평가하지 않고, `||`는 좌변이 참이면 우변을 평가하지 않습니다.

```
if (self.hpRatio < 0.5 && self.manaRatio > 0.2) {
    Heal(self, 30);
}
```

### 진리값 규칙

| 값 | 진리값 |
|---|---|
| `null` | `false` |
| `false` | `false` |
| `0` (double) | `false` |
| `""` (빈 문자열) | `false` |
| 그 외 모든 값 | `true` |

### 지원하지 않는 연산자

`&` (비트 AND), `|` (비트 OR) 단독 사용은 오류입니다. 반드시 `&&`, `||`를 사용하세요.

---

## 5. 멤버 접근

`.`으로 객체의 속성에 접근합니다. 체이닝이 허용됩니다.

```
target.position          # Entity의 위치 (Vector2)
target.position.x        # Vector2의 x 좌표 (double)
proj.velocity.magnitude  # Vector2 크기 (double)
```

### Entity 멤버

`self`, `target`, `victim` 등 모든 `Entity`에 접근 가능한 멤버입니다.

| 멤버 | 타입 | 설명 |
|---|---|---|
| `hp` | `double` | 현재 HP |
| `maxHp` | `double` | 최대 HP |
| `hpRatio` | `double` | HP 비율 (0 ~ 1) |
| `mana` | `double` | 현재 마나 (마나가 없는 엔티티는 0) |
| `maxMana` | `double` | 최대 마나 (마나가 없는 엔티티는 0) |
| `manaRatio` | `double` | 마나 비율 0~1 (마나가 없는 엔티티는 0) |
| `moveSpeed` | `double` | 이동 속도 (이동 불가 엔티티는 0) |
| `position` | `Vector2` | 월드 위치 |
| `barriorAmount` | `double` | 현재 보호막 수치 (보호막 없는 엔티티는 0) |

### SpellProjectileData 멤버 (`proj`)

투사체 콜백(`OnUpdateCode`, `OnFinishCode`) 안에서만 사용 가능합니다.

| 멤버 | 타입 | 설명 |
|---|---|---|
| `position` | `Vector2` | 투사체 현재 위치 |
| `velocity` | `Vector2` | 투사체 속도 벡터 |
| `speed` | `double` | 속도 크기 |
| `damage` | `double` | 기본 데미지 |
| `element` | `ElementType` | 속성 |

### Vector2 멤버

| 멤버 | 타입 | 설명 |
|---|---|---|
| `x` | `double` | X 좌표 |
| `y` | `double` | Y 좌표 |
| `magnitude` | `double` | 벡터 크기 |

---

## 6. 제어 흐름

### 조건문 — `if` / `else`

```
if (condition) {
    # 참일 때 실행
}

if (condition) {
    # 참일 때
} else {
    # 거짓일 때
}
```

`else if`는 지원하지 않습니다. 중첩 `if`로 구현하세요.

```
if (self.hpRatio < 0.3) {
    Heal(self, 50);
} else {
    if (self.hpRatio < 0.6) {
        Heal(self, 20);
    }
}
```

### 고정 반복 — `loop`

지정한 횟수만큼 반복합니다. 횟수는 정수(`double`)여야 합니다.

```
loop (3) {
    Bolt(target, Fire, 10);
}
```

```
let n = 5;
loop (n) {
    Bolt(target, Lightning, 8);
}
```

최대 반복 횟수: **1000회**. 초과 시 `RuntimeException`.

### 조건 반복 — `while` *(티어 1 해금)*

조건이 참인 동안 반복합니다.

```
while (self.manaRatio > 0.1) {
    Bolt(target, Water, 15);
}
```

최대 반복 횟수: **1000회**. 초과 시 `RuntimeException`.

---

## 7. 함수 정의와 호출

### 함수 정의 — `def` *(티어 2 해금)*

```
def name(param1, param2) {
    # 함수 본문
    return value;
}
```

```
def multiHit(t, n, dmg) {
    loop (n) {
        Bolt(t, None, dmg);
    }
}

multiHit(target, 5, 10);
```

### `return`

값을 반환하거나 함수를 중도 종료합니다.

```
def clampDmg(dmg, maxDmg) {
    if (dmg > maxDmg) {
        return maxDmg;
    }
    return dmg;
}
```

`return;` — 반환값 없이 종료 (반환값은 `null`).

### 재귀

지원하지만 반복 한도가 없으므로 무한 재귀에 주의하세요.

### 제한사항

- 마법 명령어(`Bolt`, `Heal`, ...) 또는 내장 함수(`Vec2`, `Abs`, ...)와 동일한 이름으로 `def` 정의 불가
- 함수 인자 수가 다르면 `RuntimeException`

---

## 8. 마법 명령어

마나를 소비하는 게임 효과 명령어입니다. 마나 부족 시 `OutOfManaException`이 발생하고 실행이 중단됩니다.

### 티어 0 — 기본 제공

#### `Bolt(target, element, damage)`

단일 대상에게 속성 데미지를 줍니다.

| 인자 | 타입 | 설명 |
|---|---|---|
| `target` | `Entity` | 피격 대상 |
| `element` | `ElementType` | 공격 속성 |
| `damage` | `double` | 데미지량 |

**마나 비용**: `damage × 1.0`

```
Bolt(target, Fire, 30);
Bolt(self, None, 5);     # 자신에게도 가능
```

---

#### `Heal(target, amount)`

대상의 HP를 회복합니다.

| 인자 | 타입 | 설명 |
|---|---|---|
| `target` | `Entity` | 회복 대상 |
| `amount` | `double` | 회복량 |

**마나 비용**: `amount × 0.8`

```
Heal(self, 50);
Heal(target, self.maxHp * 0.1);
```

---

#### `Fire(projObj, startPos, direction, speed)`

`ProjectileObject`를 월드에 생성하고 발사합니다.

| 인자 | 타입 | 설명 |
|---|---|---|
| `projObj` | `SpellObject` | 발사할 투사체 오브젝트 |
| `startPos` | `Vector2` | 발사 시작 위치 |
| `direction` | `Vector2` | 발사 방향 (정규화 불필요) |
| `speed` | `double` | 투사체 속도 |

**마나 비용**: `projObj.DefaultDamage × 0.5`

```
Fire(basicBolt, self.position, target.position - self.position, 10);
Fire(basicBolt, self.position, Vec2(1, 0), 15);
```

---

#### `SetVelocity(proj, velocity)`

투사체의 속도 벡터를 직접 설정합니다. `OnUpdateCode`에서 유도 탄 구현에 사용합니다.

| 인자 | 타입 | 설명 |
|---|---|---|
| `proj` | `SpellProjectileData` | 제어할 투사체 |
| `velocity` | `Vector2` | 새 속도 벡터 |

**마나 비용**: 없음

```
# OnUpdateCode에서 유도 탄
let dir = target.position - proj.position;
SetVelocity(proj, Normalize(dir) * proj.speed);
```

---

#### `SetSpeed(proj, speed)`

이동 방향은 유지한 채 속도 크기만 변경합니다.

| 인자 | 타입 | 설명 |
|---|---|---|
| `proj` | `SpellProjectileData` | 제어할 투사체 |
| `speed` | `double` | 새 속도 크기 |

**마나 비용**: 없음

```
# OnUpdateCode에서 가속 탄
SetSpeed(proj, proj.speed * 1.02);
```

---

### 티어 1 — 해금 필요

#### `Explode(center, radius, element, damage)`

지정 위치 주변의 모든 적에게 범위 데미지를 줍니다.

| 인자 | 타입 | 설명 |
|---|---|---|
| `center` | `Vector2` | 폭발 중심 위치 |
| `radius` | `double` | 폭발 반경 |
| `element` | `ElementType` | 공격 속성 |
| `damage` | `double` | 데미지량 |

**마나 비용**: `damage × (1.0 + radius × 0.5)`

```
Explode(target.position, 3, Fire, 20);

# OnFinishCode에서 착탄 폭발
Bolt(victim, Fire, proj.damage);
Explode(victim.position, 2, Fire, proj.damage * 0.5);
```

---

#### `Barrior(target, amount, maintainTime)`

대상에게 보호막을 부여합니다.

| 인자 | 타입 | 설명 |
|---|---|---|
| `target` | `Entity` | 보호막 대상 |
| `amount` | `double` | 보호막 수치 |
| `maintainTime` | `double` | 유지 시간 (초) |

**마나 비용**: `amount × 0.7`

```
Barrior(self, 50, 5);
```

---

## 9. 내장 함수

마나를 소비하지 않습니다. `def`로 재정의할 수 없습니다.

#### `Vec2(x, y)` → `Vector2`

2D 벡터를 생성합니다.

```
let dir = Vec2(1, 0);
Fire(basicBolt, self.position, Vec2(-1, 0.5), 10);
```

---

#### `Distance(a, b)` → `double`

두 위치 사이의 거리를 반환합니다. `Vector2` 또는 `Entity`를 인자로 받습니다.

```
let dist = Distance(self.position, target.position);
if (dist < 3) {
    Explode(target.position, 2, None, 15);
}
```

---

#### `Abs(x)` → `double`

절댓값을 반환합니다.

```
let absDiff = Abs(self.hp - target.hp);
```

---

#### `Min(a, b)` / `Max(a, b)` → `double`

두 값 중 최솟값/최댓값을 반환합니다.

```
let safeDamage = Min(damage, target.hp);
let boosted = Max(base, 10);
```

---

#### `Floor(x)` / `Ceil(x)` → `double`

내림/올림합니다.

```
let hits = Floor(self.manaRatio * 10);
```

---

#### `Sqrt(x)` → `double`

제곱근을 반환합니다. 음수 전달 시 `RuntimeException`.

```
let r = Sqrt(100);  # 10
```

---

#### `Normalize(vec)` → `Vector2`

`Vector2`를 단위 벡터로 변환합니다.

```
let dir = Normalize(target.position - self.position);
SetVelocity(proj, dir * proj.speed);
```

---

#### `GetMousePosition()` → `Vector2`

마우스의 월드 좌표를 반환합니다.

```
let mouseDir = GetMousePosition() - self.position;
Fire(basicBolt, self.position, mouseDir, 12);
```

---

## 10. 사전 정의 변수

코드 슬롯·콜백 실행 시 자동으로 주입되는 변수입니다.

| 변수 | 타입 | 사용 가능 위치 | 설명 |
|---|---|---|---|
| `self` | `Entity` | 모든 슬롯 | 마법을 시전한 엔티티 |
| `target` | `Entity` | 모든 슬롯 | 마법의 대상 엔티티 |
| `proj` | `SpellProjectileData` | `OnUpdateCode`, `OnFinishCode` | 현재 투사체 데이터 |
| `victim` | `Entity` | `OnFinishCode` | 투사체에 맞은 엔티티 |

`target`이 없을 수 있으므로(타겟 없는 스킬), `target`을 사용하기 전 `null` 여부를 고려하세요.

---

## 11. 속성 상수

`ElementType` 열거형 값들입니다. 대소문자를 정확히 지켜야 합니다.

| 상수 | 설명 |
|---|---|
| `None` | 무속성 (배율 1.0배) |
| `Fire` | 불 |
| `Water` | 물 |
| `Ice` | 얼음 |
| `Lightning` | 번개 |
| `Earth` | 땅 |
| `Wind` | 바람 |
| `Light` | 빛 |
| `Dark` | 어둠 |

```
Bolt(target, Lightning, 25);
Explode(target.position, 2, Ice, 15);
```

---

## 12. 티어 해금 구조

기능과 명령어는 티어로 잠겨 있습니다. 게임 진행에 따라 해금됩니다.

| 티어 | 기본 해금 여부 | 포함 내용 |
|---|---|---|
| **0** | 처음부터 제공 | `Bolt`, `Heal`, `Fire`, `SetVelocity`, `SetSpeed`, `basicBolt` |
| **1** | 해금 필요 | `Explode`, `Barrior`, `while` 키워드 |
| **2** | 해금 필요 | `def` (함수 정의), `return` |

---

## 13. 안전장치와 제한사항

### 루프 한도

`loop`와 `while` 모두 단일 루프당 **최대 1000회** 반복 가능합니다. 초과 시 즉시 중단됩니다.

### 마나 소진

명령어 실행 중 마나가 부족하면 `OutOfManaException`이 발생하고 나머지 코드는 실행되지 않습니다.

### SpellObject 직접 호출 불가

`basicBolt(...)`처럼 SpellObject를 직접 호출하면 오류입니다. 반드시 `Fire()`에 전달해야 합니다.

```
# 잘못된 사용
basicBolt(self.position, Vec2(1, 0), 10);  # RuntimeException

# 올바른 사용
Fire(basicBolt, self.position, Vec2(1, 0), 10);
```

### 명령어·내장 함수 재정의 불가

```
def Bolt(t, e, d) { ... }   # RuntimeException — Bolt는 마법 명령어
def Vec2(x, y)   { ... }   # RuntimeException — Vec2는 내장 함수
```

### 문자열은 한 줄

문자열 리터럴(`"..."`) 안에서 줄바꿈을 사용할 수 없습니다.

### `&` / `|` 단독 사용 불가

비트 연산자는 지원하지 않습니다. 반드시 `&&`, `||`를 사용하세요.

### 0으로 나누기 불가

`/` 연산에서 우변이 `0`이면 `RuntimeException`이 발생합니다.

---

## 14. 오류 종류

| 예외 | 발생 시점 | 설명 |
|---|---|---|
| `LexerException` | 토큰화 단계 | 알 수 없는 문자, 닫히지 않은 문자열 |
| `ParserException` | 파싱 단계 | 문법 오류 (세미콜론 누락, 괄호 불일치 등) |
| `RuntimeException` | 실행 단계 | 미등록 명령어, 타입 불일치, 0 나눗셈 등 |
| `OutOfManaException` | 실행 단계 | 명령어 실행 중 마나 부족 (`RuntimeException`의 하위) |

모든 오류 메시지에는 `[줄:열]` 위치 정보가 포함됩니다.

---

## 빠른 예시 모음

```
# 기본 투사체 발사
Fire(basicBolt, self.position, target.position - self.position, 10);

# HP 비례 데미지
let bonus = (1 - self.hpRatio) * 50;
Bolt(target, Dark, 20 + bonus);

# 마나 비율에 따른 추가 공격
if (self.manaRatio > 0.5) {
    Bolt(target, Lightning, 15);
}

# 연속 타격
loop (3) {
    Bolt(target, Fire, 10);
}

# 유도 투사체 (OnUpdateCode)
let dir = target.position - proj.position;
SetVelocity(proj, Normalize(dir) * proj.speed);

# 착탄 폭발 (OnFinishCode)
Bolt(victim, Fire, proj.damage);
Explode(victim.position, 2, Fire, proj.damage * 0.5);

# 조건부 회복
if (self.hpRatio < 0.3) {
    Heal(self, 50);
}

# 함수 정의 (티어 2)
def burst(t, count, dmg) {
    loop (count) {
        Bolt(t, Lightning, dmg);
    }
}
burst(target, 5, 8);
```
