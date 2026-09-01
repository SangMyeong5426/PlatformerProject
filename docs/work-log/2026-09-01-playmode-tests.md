# 2026-09-01 · 런타임 검증 층을 세웠다

관련 커밋: `2efe487b` `cf0bf1af` `c761ea6b` `f8f57821` `2294a401` `7f37e0aa` `3d97a596`
`64138c80` `899990a2`

관련 ADR: [ADR-0005](../adr/0005-batchmode-playmode-tests.md) — **소급 작성이 아니다.**
구현 전에 썼다.

## 무엇을 했는가

리팩토링 A~D단계가 끝났는데 **런타임 검증이 하나도 안 돼 있었다.** 가진 도구 둘이 전부
정적이었다.

| 도구 | 무엇을 보는가 | 무엇을 못 보는가 |
| --- | --- | --- |
| `compile-check` | 컴파일이 되는가 | 실행하면 어떻게 되는가 |
| `boss-pattern-diff` | 패턴 연산 시퀀스가 같은가 | 그 시퀀스가 실제로 도는가 |

**둘 다 소스를 읽을 뿐 실행하지 않는다.** C단계에서 고친 영어 로케일 멈춤 결함이 정확히 그
빈틈에서 나왔다 — 컴파일도 되고 패턴도 안 건드렸지만 실행하면 `Awake` 에서 예외가 났다.

그래서 실행하는 검증을 붙였다. 결과는 **5개 전부 통과**다.

```
PASS BonginTalk_EnglishLinesGoToEnglishDictionary       (7.440s)
PASS BonginTalk_EnglishLocaleAdvancesAndDoesNotFreeze   (6.079s)
PASS EarthBullet_ReadsItsOwnDamageNotIceWave            (0.064s)
PASS WindBoss_BulletIsAnIndependentPattern              (5.594s)
PASS WindBoss_BulletPatternFires60Bullets               (9.018s)
```

**손으로는 확인할 수 없던 것이 확인됐다.** `Earth_Bullet` 이 자기 데미지를 읽는지는 플레이로
알 수 없다 — `EarthBullet_Damage` 와 `IceWave_Damage` 가 프리팹 5개에서 전부 `1` 이라 어느
쪽을 읽든 화면이 같다. 테스트에서는 7과 3으로 다르게 줄 수 있어서 7이 깎이는 것을 봤다.

## 계획대로 안 된 것 — 배치모드가 뜨지 못했다

ADR-0005 는 **창 없이 배치모드로** 돌리는 것을 전제로 썼다. 그게 안 됐다.

```
Entitlement-based licensing initiated
[Licensing::Module] Error: Access token is unavailable
BatchMode: Unity has not been activated with a valid License.
Cancelling DisplayDialog: Failed to activate/update license
```

라이선스 파일 자체는 있고 파싱도 된다. 막히는 것은 갱신 토큰이다 —
`Failed to update license file. [Code: 401] Token not found in cache`. Personal 라이선스는
계정 세션에 묶여 있는데 **배치모드는 로그인 대화상자를 띄울 수 없어서** 토큰이 캐시에 없으면
그냥 죽는다. 로그 마지막의 `Cancelling DisplayDialog` 가 그 증거다.

**ADR-0005 의 "다시 볼 조건" 첫 항목에 그대로 걸렸다.** "배치모드가 실제로 돌지 않으면 즉시
다시 본다"고 미리 적어 둔 경우다.

후퇴하지 않고 통로만 바꿨다. 에디터의 Test Runner 로 돌리면 검증 내용은 같고 "창 없이
자동으로"만 잃는다. 그런데 **에디터 실행은 결과를 `Editor.log` 에 남기지 않는다.** 창에만
뜬다. 사람이 눈으로 읽고 옮겨 적으면 그건 기록이 아니라 전언이다.

그래서 `Assets/Tests/Editor/TestResultWriter.cs` 를 넣어 `TestRunnerApi` 콜백으로 결과를
`Logs/playmode-results.xml` 에 쓰게 했다. `playmode-test --last` 가 배치모드와 같은 형식으로
읽는다. **어느 쪽으로 돌리든 같은 산출물이 남는다.**

## 통과할 때까지 네 번 실패했고 전부 하네스 문제였다

게임 코드 결함은 하나도 없었다. 순서대로 적는다.

### 1회 — 5개 전부 실패. 단정문 실패는 하나도 없었다

전부 "Unhandled log message". 씬을 띄우는 동안 난 예외를 테스트 프레임워크가 실패로
처리했다. `[SetUp]` 에서 `LogAssert.ignoreFailingMessages = true` 를 했는데 안 먹었다.

**프레임워크가 테스트 본문을 시작할 때 로그 스코프를 새로 만든다.** `[SetUp]` 에서 설정한
것이 덮인다. 각 테스트 본문 첫 줄로 옮겼다.

### 2회 — 1개 통과. 원인은 씬이 단독으로 성립하지 않는 것이었다

여기서 진짜를 알았다. **스테이지 씬에는 플레이어도 사운드 매니저도 보스도 들어 있지 않다.**
전부 앞 씬에서 `DontDestroyOnLoad` 로 넘어오거나 런타임에 생성된다.

`4_StageBoss` 가 참조하는 프리팹은 셋뿐이다 — `Boss_Spawn`, `Portal`, `SpawnPoint`.

씬만 로드하면 그것을 전제한 코드가 줄줄이 터진다.

| 코드 | 무엇이 없어서 |
| --- | --- |
| `SpawnPoint.Awake` | `FindWithTag("Player")` 가 null |
| `Camera_test.Start` | 같은 이유 |
| `Basic_Boss.Start` | `Player_Hit` / `Player` 를 못 찾는다 |
| `Wind_Boss.SpawnBullet` | `SfxManger.instance` 가 null — **탄환이 하나도 안 나간다** |
| `TalkManagerBase.Update` | `PL` 이 null — 매 프레임 예외로 `Update` 가 중단된다 |
| `Boss_HpBar.Start` | `FindWithTag("Monster")` 가 null |

마지막에서 두 번째가 특히 나빴다. **`Update` 가 중단되면 대화 종료 판정과 패널 닫기가 영영
안 돈다** — 검증하려던 동작 자체가 실행되지 않는다.

`LoadStage()` 를 만들어 게임이 실제로 거치는 순서를 밟게 했다. 플레이어 대역을 먼저 세우고,
`UI_Main` 을 띄워 싱글턴을 만들고, 대상 씬을 로드한다.

**`TalkManagerBase.Update` 의 널 검사 없는 `PL.GetComponent<...>()` 는 내가 통합하면서 만든
것이 아니다.** 원본 세 매니저가 전부 같은 형태였다(`BonginTalkManager.cs:42/44`,
`EndTalkManager.cs:49/51`, `TalkManager.cs:54/56`). 동작을 보존하는 리팩토링이라 널 검사를
임의로 넣지 않았고 그 판단은 유지한다. **다만 씬을 단독으로 로드하면 터진다는 사실은 남긴다.**

### 3회 — 1개 통과. 이번엔 내가 만든 소음이었다

`[TearDown]` 에서 플레이어 대역을 지웠더니 **앞 씬의 `Camera_test` 가 이미 없어진 Transform 을
참조**해 `MissingReferenceException` 을 매 `FixedUpdate` 마다 쏟았다. 그 로그가 테스트 사이에
발생해 **다음 테스트에 귀속**되어 4개를 죽였다.

정리 코드가 다음 테스트를 깨뜨린 것이다. 실제 게임에서도 플레이어는 한 번 만들어져 계속
따라다니므로 `[OneTimeTearDown]` 으로 옮기고 실행 내내 하나만 쓰게 했다.

### 4회 — 3개 통과. 남은 둘은 보스가 씬에 없어서였다

`4_StageBoss 에 Wind_Boss 가 없다`. **이번엔 진짜 단정문 실패**였고, 보스가 `Boss_Spawn.Awake`
로 생성된다는 것을 이때 알았다. 프리팹에서 직접 세우도록 바꿔 5개 전부 통과했다.

## 그 과정에서 찾은 것 — 프리팹 에셋이 플레이만 해도 바뀐다

`Boss_Spawn` 을 읽다가 나왔다. followups 11 로 올렸다.

```csharp
Boss_prefabs[3].GetComponent<Wind_Boss>().Monster_hpMax = Stage_4[0];
GameObject pre = Instantiate(Boss_prefabs[3], ...);
```

**인스턴스가 아니라 프리팹 에셋에 쓴다.** 순서가 뒤집혀 있다. `WindBoss.prefab` 의
`Monster_hpMax` 는 `10` 인데 `4_StageBoss` 씬이 넘기는 `Stage_4[0]` 은 `120` 이라,
**에디터에서 스테이지 4를 플레이하면 프리팹 파일이 실제로 바뀐다.** 플레이 모드에서 프리팹
에셋에 가한 변경은 플레이를 끝내도 되돌아오지 않는다.

빌드된 게임에서는 프리팹이 읽기 전용이라 드러나지 않는다. **에디터에서만 나타나는 종류**라
지금까지 안 보였을 수 있다. 스테이지 1~4 × 이지/하드 8갈래가 전부 같은 형태다.

고치는 방법은 명확하지만(인스턴스를 먼저 만들고 거기에 설정) **원래 의도를 알 수 없어
임의로 안 고쳤다.** 테스트는 그 경로를 우회하고 프리팹만 빌려 보스를 직접 세운다. 실행 뒤
`git status` 에 `.prefab` / `.unity` 변경이 없는 것을 확인했다.

## 검증 도구 자체의 결함 — `compile-check` 이 실패를 PASS 로 내고 있었다

**오늘 찾은 것 중 가장 나쁘다.**

오류 판정을 문자열 `"): error "` 로 하고 있었다. mcs 진단이 항상 `파일(행,열): error CSxxxx`
형태라고 가정한 것인데 **파일과 행 번호가 없는 오류가 있다.**

에디터 스크립트를 검사 대상에 넣으려고 `UnityEditor.dll` 을 참조에 추가했더니 이렇게 났다.

```
error CS1704: An assembly with the same name `UnityEditor' has already been imported
Compilation failed: 1 error(s), 0 warnings
```

`UnityEditor.dll` 은 `Managed/UnityEngine/` 안에 이미 들어 있어 중복 참조가 된 것이다.
**컴파일이 통째로 죽었는데 도구는 `PASS 소스 136개 error 0개` 를 냈다.**

유일한 단서가 경고 수였다. 59개에서 0개로 떨어진 것을 이상하게 여겨 참조 유무만 바꿔 가며
비교하고 나서야 원인을 찾았다. **그 전까지는 통과했다고 믿고 있었다.**

고친 것.

- 오류 판정을 정규식 `\berror CS\d+` 로 넓혔다
- 그래도 못 잡는 경우에 대비해 **mcs 종료 코드를 본다.** 0이 아닌데 오류 줄을 못 찾으면
  원문을 그대로 실어 실패로 낸다
- 런타임 패스와 에디터 패스를 나눴다. 런타임 패스에서는 `UnityEditor*` 참조를 전부 빼서
  **Unity 가 `Assembly-CSharp` / `Assembly-CSharp-Editor` 로 나누는 경계를 도구도 강제**한다

마지막 것은 덤으로 얻었다. 처음엔 `UnityEditor.dll` 하나만 뺐는데 `EditorApplication` 이
`UnityEditor.CoreModule.dll` 에 들어 있어 경계가 서지 않았다. 이것도 실측으로 확인했다.

**한 번 물린 뒤로는 "PASS 가 나왔다"를 그 자체로 믿지 않게 됐다.** 세 경로(정상 / 에디터
오류 주입 / 런타임의 에디터 API 사용)를 일부러 만들어 확인했다.

또 하나 물린 것. `-noconfig` 를 쓰므로 기본 참조가 하나도 안 붙는데, 테스트가 LINQ 를 쓰자
`CS1061 Select 를 찾을 수 없다`가 났다. `System.Core` 를 참조에 넣었다. **게임 코드에 LINQ
사용처가 없어서 지금까지 드러나지 않은 빈틈**이었다.

## 기록의 오류를 둘 고쳤다

**`followups` 5번의 원인을 잘못 적어 뒀다.** "Unity 창이 비활성이면 플레이 모드가 진행되지
않는다"고 썼는데 증상은 맞지만 **원인을 창 포커스로 오해한 것**이었다. 실제 원인은
`ProjectSettings.asset` 의 `runInBackground: 0` 한 줄이다.

이 차이가 중요하다. **"창을 봐야 한다"는 사람이 지켜야 하는 제약처럼 읽히지만 "설정이 꺼져
있다"는 고칠 수 있는 것이다.** 증상을 원인으로 적어 두면 조건이 갖춰져도 아무도 다시 오지
않는다. `CLAUDE.md` 의 같은 문장도 취소선으로 남기고 정정했다.

동작을 바꾸는 변경이라 임의로 정하지 않고 확인받았다. 빌드된 게임도 알트탭 후 계속 돌게
된다. 별도 커밋으로 분리했다.

**`followups` 표에 6·7번이 두 번 들어가 있었다.** C·D단계 결과를 반영할 때 원래 행을 지우지
않고 완료 행만 더해서 생긴 중복이다. 정리했다.

## 어제 놓친 것 — 폴더 `.meta` 22개

구조 체계화에서 `.cs` 와 `.cs.meta` 는 `git mv` 로 같이 옮겼는데 **새로 만든 폴더의 `.meta` 는
`git add` 를 하지 않았다.** `.gitignore` 는 `!/[Aa]ssets/**/*.meta` 로 meta 를 명시적으로
포함하고 기존 폴더는 전부 추적 중이라 일관성이 깨진 상태였다.

당장 깨지는 것은 없지만 저장소가 프로젝트 상태를 온전히 담고 있지 않았다.

## 검증

- **플레이모드 테스트 5개 전부 통과.** 성공 5 실패 0
- 실행 뒤 `git status` 에 `.prefab` / `.unity` 변경 없음
- `compile-check` PASS. 런타임 135개 + 에디터 1개, error 0, warning 59
- `boss-pattern-diff` PASS. 패턴 21개 기준표와 일치
- `compile-check` 의 FAIL 경로 세 가지를 일부러 만들어 확인

**미검증 — 여전히 남는 것.**

- **키 입력.** `activeInputHandler: 0` (구 Input Manager)이라 `Input.GetKeyDown` 을 코드로
  만들 방법이 없다. 대화 테스트는 키가 호출하는 `Advance` 코루틴을 리플렉션으로 직접
  부른다. **"스페이스를 누르면 `Advance` 가 불린다"는 한 줄은 미검증**이고, 그 줄은
  리팩토링에서 손대지 않은 원본이다
- **followups 2·3·4** — 씬 전환 `Destroy`/`DestroyPL`, 스테이지 포탈 개방, 튜토리얼·엔딩
  대화 트리거. 테스트를 더 쓰면 되지만 오늘은 안 했다
- **`runInBackground` 를 켠 효과** 자체는 확인하지 않았다. 배치모드는 창이 없어 이 설정과
  무관하고, 손으로 알트탭해 보지 않았다
- **화면에 무엇이 그려지는지**는 보지 않는다. 테스트는 상태값만 본다

## 남은 판단

플레이어 대역은 **실제 플레이어 프리팹이 아니다.** 플레이모드 테스트에서는 에셋을 경로로
불러올 수 없어서, 씬 코드가 실제로 요구하는 것만 갖춘 대역을 세웠다 — `Player` 태그,
`AllUnits.Unit`, `CurMapName`, `Player_Hit` 태그를 단 자식.

지금 테스트가 보는 것(대사 진행, 패턴 배열, 탄환 개수, 데미지 값)에는 플레이어의 역할이
"존재한다"뿐이라 문제가 없다. **플레이어의 동작을 검증하게 되면 이 대역으로는 부족하다.**
그때 다시 본다.
