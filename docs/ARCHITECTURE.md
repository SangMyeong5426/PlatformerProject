# 스크립트 구조

이 문서는 `Assets/Scripts/` 아래 코드 배치를 기술한다. **코드와 항상 일치해야 한다** —
어느 한쪽만 바꾸지 않는다. 갱신 규칙은 맨 아래에 있다.

배치를 이렇게 정한 근거는 [ADR-0004](adr/0004-script-layout.md) 에 있다. 여기서는 반복하지
않는다.

## 범위

사용자가 작성한 스크립트 **134개**가 `Assets/Scripts/` 아래에 있다. 여기에 더해
플레이모드 테스트가 `Assets/Tests/` 에 있다(아래 참조).

`Assets/` 의 나머지 폴더(`1. Monster`, `2. Player`, `3. Level_design`, `4. UI`, 에셋 스토어
패키지들)에는 **에셋만 있다** — 스프라이트, 애니메이션, 프리팹, 씬, 사운드. 코드는 없다.

## 배치

```
Assets/Scripts/
├── Boss/       28    보스 5종
├── UI/         13    HUD·텍스트·패널
├── Player/     13    플레이어와 무기 3종
├── Scene/      12    씬 전환·게임 흐름
├── Monster/    10    일반 몬스터
├── Core/        9    전역 상태와 객체 수명
├── Item/        9    인벤토리·드롭
├── Legacy/      9    옛 구현. 아직 씬이 참조한다
├── Talk/        8    대화 시스템
├── Select/      7    캐릭터·모드 선택
├── Sound/       7    BGM·SFX
├── Level/       6    카메라·배경·플랫폼
└── Portal/      3    스테이지 이동

Assets/Tests/
└── PlayMode/    1    리팩토링 결과를 실행해서 확인하는 테스트
```

`asmdef` 는 두지 않는다. 지금 의존 관계가 얽혀 있어 어셈블리를 나누면 순환 참조가 난다.

**테스트도 `asmdef` 없이 둔다.** `asmdef` 어셈블리는 `Assembly-CSharp` 을 참조할 수 없어서
테스트가 게임 코드를 못 본다. 대신 `playModeTestRunnerEnabled` 를 켜서 게임 코드와 같은
어셈블리에 두고, 파일 전체를 `#if UNITY_INCLUDE_TESTS` 로 감싸 빌드에서 뺀다.
근거는 [ADR-0005](adr/0005-batchmode-playmode-tests.md).

## `Boss/` — 보스 5종

`Basic_Boss` 가 패턴 스케줄러와 공통 코루틴(돌진·텔레포트·사망 처리)을 갖고, 보스별 차이는
`protected virtual` 훅으로 재정의한다. 근거는 [ADR-0002](adr/0002-boss-template-method-hooks.md).

| 보스 | 본체 클래스 | 폴더 |
| --- | --- | --- |
| 불 | `Fire_Boss` | `Boss/Fire/` |
| 얼음 | `Ice_Boss` | `Boss/Ice/` |
| 대지 | `Earth_Boss` | `Boss/Earth/` |
| 바람 | `Wind_Boss` | `Boss/Wind/` |
| 모드 | `Boss_mode` | `Boss/Mode/` |

얼음과 대지는 원래 `Stage_2_monster` / `One_Stage_Boss` 였다. 2023년 작업 당시 스테이지
번호로 이름을 붙인 것이라 어느 보스인지 이름이 말해 주지 않았다. **2026-09-02 에 바꿨다.**
배치 이동과 섞지 않으려고 미뤄 뒀던 것이고, 회귀를 잡을 플레이모드 테스트가 생긴 뒤에
했다.

각 보스 폴더에는 본체와 함께 **그 보스의 투사체·이펙트 스크립트**가 들어 있다.
`Boss/Mode/` 에는 모드 보스 본체와 `FinBoss_*` 프리팹에 붙은 투사체 5개가 있다.

`Basic_Boss` 와 `Boss_HpBar` 는 `Boss/` 바로 아래에 둔다. 특정 보스에 속하지 않는다.

## `Core/` — 전역 상태와 객체 수명

```
BoolManager  BoolReset  StageId  EnemyCountManager
Destroy  DestroyPL  DontDestroy  DontDestroyObj
Unit
```

두 종류가 있다.

**전역 진행 상태.** `BoolManager` 가 보스 클리어 플래그를, `EnemyCountManager` 가 스테이지별
몬스터 수를 갖는다. 둘 다 `StageId` 를 인덱스로 쓴다([ADR-0003](adr/0003-stage-id-enum.md)).
`BoolReset` 이 초기화한다.

**씬을 넘는 객체의 수명.** `Destroy` / `DestroyPL` 은 씬이 바뀔 때 자신을 파괴할지
`DontDestroyOnLoad` 로 남길지 판정한다. `DestroyPL` 은 `Destroy` 를 상속해 씬 목록만
재정의한다.

`Unit`(`AllUnits.Unit`)은 체력을 가진 모든 것의 기반 클래스다. 플레이어와 몬스터가 함께
쓴다.

## `Legacy/` — 아직 지울 수 없는 옛 구현

```
Boss  Final_Stage_Boss  Boss_Pattern  Boss_Pattern_Expo  pattern
Monster_Bullet  Monster_chase  Monster_chase_far  Monster_chase_Test2
```

원래 `X/` 와 `test/` 라는 폴더에 있던 것들이다. **현재 설계에 속하지 않지만 아직 씬·프리팹이
참조하고 있어 지우면 씬이 깨진다.**

`Final_Stage_Boss` 와 그 부모 `Boss` 는 `FinalBoss.prefab` 에 붙어 있고, 그 프리팹은
**빌드 설정에 없는 `Monster_Scenes` 에서만** 쓰인다. 현재 게임에서 도달할 수 없는 경로다.

**여기 있는 것을 새로 참조하지 않는다.** 씬이 참조를 끊는 만큼 줄어들고, 비면 폴더를 없앤다.

같은 폴더에 있던 9개는 어디서도 참조하지 않아 제거했다(`d430270d`).

## 나머지 폴더

| 폴더 | 무엇이 있나 |
| --- | --- |
| `Player/` | 이동·공격·대시. `Sword/` `Spear/` `Shield/` 에 무기별 애니메이션과 스킬 |
| `Monster/` | 일반 몬스터의 상태·추적·원거리 공격 |
| `Scene/` | 씬 전환, `GameManager`, 일시정지, 로딩, 사망 패널 |
| `UI/` | 체력·마나 바, 쿨타임, 남은 몬스터 수, 맵 이름 |
| `Talk/` | 공통 골격 `TalkManagerBase` 와 매니저 3종, `TalkTrigger`, 봉인(`B_Test`)과 Bongin NPC |
| `Item/` | 인벤토리, 슬롯, 드롭, 보석 |
| `Select/` | 캐릭터 선택과 모드 선택 |
| `Sound/` | BGM·SFX 매니저와 컨트롤 |
| `Level/` | 카메라(흔들림·해상도·추적), 배경 반복, 내려가는 플랫폼 |
| `Portal/` | 스테이지 포탈, 보스 클리어 포탈 |

## 알려진 문제

배치와 별개로 **아직 정리되지 않은 것**들이다.

- **대사가 코드에 하드코딩돼 있다.** `TalkManagerBase.Channel` 의 `Ko`/`En` 딕셔너리를
  `GenerateData()`/`GenerateDataENG()` 가 채운다. 프로젝트는 이미 Unity Localization 을
  쓰고 있어 문자열 테이블로 옮길 수 있다. 매니저 3종의 공통 골격은 정리했지만 대사의
  자리는 그대로다 — [`followups`](followups.md) 8번
- **클래스 이름이 내용을 말하지 않는 것이 남아 있다.** `Postion`(오타), `Camera_test`
  (실제로는 플레이어 추적 카메라). `Stage_2_monster` · `One_Stage_Boss` · `Mosnter_Repeat`
  · `SfxManger` 는 2026-09-02 에 정리했다
- **의존 관계가 얽혀 있다.** `Basic_Boss` 가 `BoolManager` 를, `Item/Gemstone` 이
  `BoolManager` 를 읽는 식이라 `asmdef` 로 나누면 순환 참조가 난다

미결 사항은 [`followups.md`](followups.md) 에 있다.

## 갱신 규칙

다음을 하거나 발견하면 **이 문서도 함께 고친다.**

- 폴더를 추가·삭제·이동·이름 변경
- 스크립트를 다른 폴더로 옮기거나 새로 만들거나 지움
- 보스를 추가하거나 본체 클래스 이름을 바꿈
- `Legacy/` 의 파일이 줄거나 늘어남
- `asmdef` 를 도입

**한쪽만 바꾸고 끝내지 않는다.** 코드를 바꿨으면 이 문서를, 이 문서를 바꿨으면 코드를
확인한다. 어긋난 것을 발견했는데 즉시 해소할 수 없으면 [`followups.md`](followups.md) 에
적고 넘어간다. 조용히 지나가지 않는다.

**개수를 손으로 세어 적은 값이 이 문서에 여럿 있다** — 범위의 134, 각 폴더 개수, `Talk/` 의
줄 수. 검사하는 장치가 없으므로 폴더를 건드릴 때 함께 고친다.
