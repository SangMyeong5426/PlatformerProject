# 2026-09-02 · 미뤄 뒀던 검증 셋을 실행으로 닫았다

관련 커밋: `663e0f53`

관련 항목: [followups 2 · 3 · 4 · 14](../followups.md) 닫음

## 무엇을 했는가

`followups` 2·3·4 는 전부 **"게임을 조작할 수 있을 때"** 확인하려고 미뤄 둔 것이었다.
09-01 에 플레이모드 테스트 층이 생겼으니 조작 없이 확인할 수 있게 됐다.

| # | 무엇 | 어떤 통합의 뒤처리인가 |
| --- | --- | --- |
| 2 | 씬 전환 시 `Destroy` / `DestroyPL` | A단계 — `DestroyPL` 을 `Destroy` 상속으로 |
| 3 | 보스 클리어 후 포탈 개방 | B단계 — 포탈 4개 → `BossClearPortal` + `StageId` |
| 4 | 대화 트리거 발동 | B단계 — `NPCTri` 4개 → `TalkTrigger` + `TalkChannel` |

셋 다 **여러 클래스를 하나로 합친 자리**다. 그래서 "동작하는가"보다 **"갈래가 섞이지
않았는가"**를 봐야 한다. 합치기 전에는 클래스가 갈래를 나눠 줬는데, 합친 뒤에는 값 하나가
그 일을 한다. 그 값을 잘못 읽으면 컴파일도 되고 테스트도 없으면 아무도 모른다.

그래서 세 테스트 다 **"자기 것만 반응하는가"**를 본다.

- 포탈: 다른 스테이지를 깼을 때 **열리지 않는지**까지 확인한다
- 대화: `MainFirst` 만 한도에 닿게 해 두고 나머지 셋이 안 막히는지, 반대로 `EndSecond` 만
  막고 `MainFirst` 가 안 막히는지 — 양방향으로 본다
- 씬 전환: `EndingScene` 에서 `Destroy` 는 죽고 `DestroyPL` 은 사는 그 한 칸

## 설계에서 정한 것 둘

### 씬 전환 테스트는 진짜 씬을 띄우지 않는다

`Destroy` / `DestroyPL` 이 보는 것은 **활성 씬의 이름**뿐이다.

```csharp
if (System.Array.IndexOf(DestroyScenes, scene.name) >= 0) Destroy(gameObject);
else DontDestroyOnLoad(gameObject);
```

그래서 `SceneManager.CreateScene("EndingScene")` 으로 만든 **빈 씬**이면 충분하다. 진짜
`EndingScene` 을 띄우면 그 씬의 다른 스크립트가 매 프레임 예외를 던져 다음 테스트를
죽인다 — 어제 두 번 겪었다(`InventoryUI`, `Far_Monster`).

검증하려는 계약이 "이름"이라면 이름만 맞춘 최소한을 만드는 쪽이 맞다. 진짜 씬을 끌어오면
검증과 상관없는 것이 잔뜩 딸려 오고, 그중 하나가 깨지면 **엉뚱한 테스트가 실패한다.**

### 대화 트리거는 둘로 나눠 본다

| 무엇 | 어떻게 | 왜 |
| --- | --- | --- |
| 실제 접촉으로 열리는가 | 물리 충돌 1회 | 트리거가 진짜 발동하는 것은 한 번은 봐야 한다 |
| 채널마다 자기 진행도를 읽는가 | 게이트 로직 직접 호출 6가지 | 어긋날 수 있는 자리가 여기다 |

둘째를 물리로 하면 여덟 번을 부딪혀야 하고 그만큼 불안정해진다. **통합에서 어긋날 수 있는
자리는 채널 매핑이지 Unity 의 충돌 전달이 아니다.** 무엇을 검증하는지 정하면 어디까지
진짜로 할지가 따라 나온다.

## followups 14 도 함께 닫았다

콘솔에 늘 뜨던 무해한 `Boss_Spawn` 널 참조를 없앴다. **게임 코드는 건드리지 않았다.**

`LoadStage` 가 난이도를 둘 다 끈 `Mode_Select` 대역을 항상 세운다. 둘 다 꺼 두면 스포너가
아무것도 소환하지 않으므로, 보스를 직접 세우는 테스트가 보스를 두 마리 보는 일도 없다.
난이도가 필요한 테스트만 `Begin()` 이후에 플래그를 켠다.

덤으로 **불안하던 `[TearDown]` 이 없어졌다.** 대역을 매 테스트마다 지우고 다시 만드는
구조였는데, 어제 그 방식으로 플레이어 대역을 지웠다가 테스트 4개를 죽인 적이 있다.
공유해서 계속 두는 쪽이 실제 게임과도 닮았다.

## 한 번 실패했다 — 내가 만든 소음이었다

첫 실행에서 **새 테스트 하나와 기존 둘이 죽었다.**

```
PASS BossClearPortal_OpensOnlyForItsOwnStage              (0.041s)
PASS SceneSwitch_DestroyAndDestroyPLFollowTheirOwnSceneLists (5.989s)
FAIL TalkTrigger_FiresOnContactAndReadsItsOwnChannel      (0.019s)
FAIL WindBoss_BulletIsAnIndependentPattern                (0.011s)
FAIL WindBoss_BulletPatternFires60Bullets                 (0.010s)
     Unhandled log message: '[Exception] NullReferenceException'
     Boss_HpBar.Update () (at Assets/Scripts/Boss/Boss_HpBar.cs:30)
```

**세 번째로 같은 함정에 빠졌다.** 매 프레임 예외를 던지는 것이 남아 있으면 그 로그가 다음
테스트에 귀속된다. 실패 시간 0.01~0.02초가 늘 그 단서다 — 본문이 시작도 못 했다는 뜻이다.

원인은 이번에도 **스테이지 씬이 단독으로 서지 못한다**는 것이었다. `Boss_HpBar.Start` 가
이렇다.

```csharp
Boss = GameObject.FindWithTag("Monster");
stat = Boss.GetComponent<Monster_Stats>();   // 널 검사가 없다
```

보스가 없으면 `stat` 이 null 로 남고 `Update` 가 매 프레임 터진다. 실제 게임에서는
`Boss_Spawn.Awake` 가 `Start` 보다 먼저 보스를 만들어 주므로 안 걸린다.

**아이러니한 것은 내가 방금 만든 조건이라는 점이다.** followups 14 를 고치면서 난이도를 꺼
둔 `Mode_Select` 대역을 넣었더니 보스가 안 생기게 됐고, 그러자 지금까지 `Boss_Spawn` 의 널
참조에 가려 있던 `Boss_HpBar` 의 널 참조가 드러났다. 소음 하나를 없애니 그 뒤에 있던
소음이 나왔다.

`LoadStage` 가 체력바를 끄게 해서 해결했고, `SpawnWindBoss` 안에 있던 같은 코드도 그
도우미로 합쳤다. 두 번째 실행에서 **10개 전부 통과**했다.

## 검증

```
성공 10  실패 0  건너뜀 0
```

`compile-check` PASS, `boss-pattern-diff` PASS. 실행 후 `.prefab` / `.unity` 변경이 작업
트리에 없는 것도 확인했다.

## 검증하지 못한 것

- **`requireBossSeal` 갈래 (옛 `NPCTri3`).** `B_Test.Boss_seal` 이 봉인 진행과 얽혀 있어
  대역만으로는 실제 조건을 만들 수 없다. 그 갈래를 **끈 채로만** 확인했다
- **`Destroy` 의 `UI_Select` 항목.** 목록에는 `UI_Main` · `EndingScene` · `UI_Select` 셋이
  있는데 테스트는 앞의 둘만 짚는다. 셋째는 앞의 둘과 같은 코드 경로다
- **실제 씬에서의 포탈.** 테스트는 `BossClearPortal` 컴포넌트만 세워서 본다. 씬의 포탈
  오브젝트가 그 컴포넌트에 제대로 연결돼 있는지는 별개이고, 그건 씬 데이터의 문제다
