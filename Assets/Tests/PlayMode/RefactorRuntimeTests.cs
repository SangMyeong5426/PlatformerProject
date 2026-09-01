#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Settings;

// 리팩토링(2026-08-29 ~ 09-01)이 실제로 돌 때도 맞는지 확인하는 플레이모드 테스트.
//
// 왜 이게 필요한가
//   이 저장소의 다른 검증 도구 둘(compile-check, boss-pattern-diff)은 소스를 읽을 뿐
//   실행하지 않는다. 영어 로케일 멈춤 결함이 정확히 그 빈틈에서 나왔다 - 컴파일도 되고
//   패턴 시퀀스도 같았지만 실행하면 Awake 에서 예외가 났다.
//
// 어떻게 도는가
//   Unity 를 닫고 scripts/playmode-test 를 실행한다. 창 없이 배치모드로 돈다.
//   근거와 대안 검토는 docs/adr/0005-batchmode-playmode-tests.md 에 있다.
//
// 검증하지 못하는 것
//   키 입력. activeInputHandler 가 구 Input Manager 라 Input.GetKeyDown 을 코드로
//   만들어낼 방법이 없다. 대화 진행 테스트는 키가 호출하는 코루틴을 리플렉션으로 직접
//   부른다. 따라서 "스페이스를 누르면 Advance 가 불린다"는 한 줄은 미검증으로 남는다.
//   그 한 줄은 리팩토링에서 손대지 않은 원본 코드다.
public class RefactorRuntimeTests
{
    // 각 테스트 본문의 첫 줄에서 부른다.
    //
    // **[SetUp] 에 두면 안 된다.** 테스트 프레임워크가 테스트 본문을 시작할 때 로그
    // 스코프를 새로 만들어서 [SetUp] 에서 설정한 ignoreFailingMessages 가 덮인다.
    // 처음에 [SetUp] 에 뒀다가 5개가 전부 "Unhandled log message" 로 실패했다.
    static void Begin()
    {
        // 대화 트리거가 timeScale 을 0 으로 두고 끝나면 다음 테스트의 WaitForSeconds 가
        // 영원히 안 끝난다. 테스트끼리 오염되지 않도록 매번 되돌린다.
        Time.timeScale = 1f;

        // 실제 씬을 씬 전환 없이 단독으로 로드하므로, 앞선 씬에서 넘어와야 할 것이
        // 없어서 배선이 덜 된 오브젝트가 예외를 뱉는다. 우리가 보는 것은 로그가 아니라
        // 상태값이다. 어떤 예외가 나오는지는 work log 에 적었다.
        LogAssert.ignoreFailingMessages = true;

        // 난이도 대역은 실행 내내 하나를 공유한다. 앞 테스트가 켜 둔 난이도가 남으면
        // 다음 테스트가 씬을 띄울 때 보스나 몬스터가 예상 밖으로 소환된다.
        if (modeStub != null)
        {
            Mode_Select mode = modeStub.GetComponent<Mode_Select>();
            mode.Easy = false;
            mode.Hard = false;
        }
    }

    // ── followups 대응: 영어 로케일 봉인 대화 (C단계에서 고친 멈춤 결함) ──────────

    [UnityTest]
    public IEnumerator BonginTalk_EnglishLinesGoToEnglishDictionary()
    {
        Begin();

        yield return LoadStage("Maze_Stage");

        var mgr = Object.FindObjectOfType<BonginTalkManager>();
        Assert.IsNotNull(mgr, "Maze_Stage 에 BonginTalkManager 가 없다");

        object[] channels = Channels(mgr);
        Assert.AreEqual(1, channels.Length, "봉인 대화는 채널 1개다");

        var ko = Dict(channels[0], "Ko");
        var en = Dict(channels[0], "En");

        // 결함이 있던 형태: GenerateDataENG 가 En 이 아니라 Ko 에 같은 키를 다시 넣어
        // Dictionary.Add 가 ArgumentException 을 던지고 En 이 빈 채로 남았다.
        Assert.AreEqual(2, ko.Count, "한국어 대사 2줄");
        Assert.AreEqual(2, en.Count, "영어 대사가 En 에 담기지 않으면 여기가 0 이 된다");
        Assert.AreEqual("Press B to seal!", en[1]);
    }

    [UnityTest]
    public IEnumerator BonginTalk_EnglishLocaleAdvancesAndDoesNotFreeze()
    {
        Begin();

        yield return LoadStage("Maze_Stage");
        yield return SelectLocale(0);   // 0 = English (GameManager.LangENG)

        var mgr = Object.FindObjectOfType<BonginTalkManager>();
        Assert.IsNotNull(mgr, "Maze_Stage 에 BonginTalkManager 가 없다");

        object ch = Channels(mgr)[0];
        GameObject panel = (GameObject)Field(ch, "Panel");
        Text label = (Text)Field(ch, "Label");

        // 트리거가 하는 일을 대신한다. 패널을 열고 게임을 멈춘다.
        panel.SetActive(true);
        Time.timeScale = 0f;
        Write(ch, 1);

        // 스페이스 키가 하는 일. 키 자체는 만들어낼 수 없어 코루틴을 직접 부른다.
        Advance(mgr, ch);
        yield return null;

        Assert.AreEqual("Press B to seal!", label.text,
            "영어 대사를 못 읽으면 KeyNotFoundException 이 나고 진행도가 안 오른다");
        Assert.AreEqual(2, Read(ch), "대사 한 줄을 넘기면 진행도가 1 오른다");

        // 마지막 줄까지 넘기면 End(3)에 닿아 Update 가 패널을 닫아야 한다.
        Advance(mgr, ch);
        Assert.AreEqual(3, Read(ch));

        yield return null;   // Update 가 종료 판정을 도는 프레임

        Assert.IsFalse(panel.activeSelf, "종료 진행도에 닿으면 패널이 닫혀야 한다");
        Assert.AreEqual(1f, Time.timeScale,
            "timeScale 이 0 으로 남으면 게임이 멈춘다 - 이것이 원래 결함의 증상이다");
    }

    // ── followups 대응: 바람 보스 패턴 (D단계 수정) ─────────────────────────────

    [UnityTest]
    public IEnumerator WindBoss_BulletIsAnIndependentPattern()
    {
        Begin();

        yield return LoadStage("4_StageBoss");
        Wind_Boss boss = null;
        yield return SpawnWindBoss(b => boss = b);

        var patterns = Patterns(boss);

        // 고치기 전에는 [2] 와 [3] 이 둘 다 SpawnTornado 라 SpawnBullet 이 단독으로
        // 선택될 수 없었다. 항상 토네이도 뒤에만 나왔다.
        CollectionAssert.AreEqual(
            new[] { "Teleport", "Dash", "SpawnTornado", "SpawnBullet" },
            patterns.Select(p => p.Method.Name).ToArray(),
            "패턴 순서가 기존 switch 의 case 번호와 같아야 한다");
    }

    [UnityTest]
    public IEnumerator WindBoss_BulletPatternFires60Bullets()
    {
        Begin();

        yield return LoadStage("4_StageBoss");
        Wind_Boss boss = null;
        yield return SpawnWindBoss(b => boss = b);

        Assert.IsNotNull(boss.bullet, "탄환 프리팹이 인스펙터에 연결돼 있지 않다");

        string cloneName = boss.bullet.name + "(Clone)";

        // Start 가 띄운 패턴 스케줄러를 멈춘다. 그대로 두면 랜덤 패턴이 겹쳐 개수가 는다.
        boss.StopAllCoroutines();

        boss.StartCoroutine(Invoke<IEnumerator>(boss, "SpawnBullet"));

        // 탄환은 2초 뒤 파괴되고 버스트는 1.0/1.5/2.0/2.5초에 나간다. 한 시점에 60발이
        // 다 모이는 구간이 2.5~3.0초뿐이라 최대값을 본다.
        int peak = 0;
        for (float t = 0f; t < 3.5f; t += Time.deltaTime)
        {
            peak = Mathf.Max(peak, CountClones(cloneName));
            yield return null;
        }

        // BurstOffsets 4회 x (360/25 = 15발)
        Assert.AreEqual(60, peak, "버스트 4회 x 15발 = 60발이 동시에 존재해야 한다");
    }

    // ── followups 6 대응: 대지 탄환 데미지 ────────────────────────────────────
    //
    // 이건 손으로 플레이해도 확인할 수 없다. EarthBullet_Damage 와 IceWave_Damage 가
    // 프리팹 5개에서 전부 1 이라 어느 쪽을 읽든 화면에 같은 결과가 나온다.
    // 테스트에서는 두 값을 다르게 줄 수 있어서 어느 쪽을 읽는지 드러난다.

    [UnityTest]
    public IEnumerator EarthBullet_ReadsItsOwnDamageNotIceWave()
    {
        Begin();

        const int OwnDamage = 7;
        const int IceDamage = 3;

        // 다른 씬의 콜라이더에 먼저 부딪히지 않도록 멀리 떨어진 곳에 세운다.
        Vector3 origin = new Vector3(5000f, 5000f, 0f);

        Player_UsingItem.UsingActiveShield = false;

        // 보스 대역. 비활성으로 두어 Start 가 씬 태그를 찾다 실패하지 않게 한다.
        var bossGo = new GameObject("BossStub");
        bossGo.SetActive(false);
        var boss = bossGo.AddComponent<Basic_Boss>();
        boss.EarthBullet_Damage = OwnDamage;
        boss.IceWave_Damage = IceDamage;

        // 플레이어 대역.
        var playerGo = new GameObject("PlayerStub");
        playerGo.tag = "Player";            // Earth_Bullet 이 CompareTag 로 거른다
        playerGo.transform.position = origin + new Vector3(1.5f, 0f, 0f);
        playerGo.AddComponent<BoxCollider2D>();
        var playerBody = playerGo.AddComponent<Rigidbody2D>();
        playerBody.bodyType = RigidbodyType2D.Static;
        var unit = playerGo.AddComponent<AllUnits.Unit>();
        unit.clip_attacked = new AudioClip[0];              // 비면 SfxManager 를 안 부른다
        unit.Player_Attacked_Effect = new GameObject("HitFx");
        unit.me = playerGo;

        // 탄환.
        var bulletGo = new GameObject("EarthBulletStub");
        bulletGo.transform.position = origin;
        bulletGo.AddComponent<BoxCollider2D>();
        var bulletBody = bulletGo.AddComponent<Rigidbody2D>();
        bulletBody.gravityScale = 0f;
        var bullet = bulletGo.AddComponent<Earth_Bullet>();
        bullet.EarthBullet_Damage = boss;

        yield return null;   // Start 가 도는 프레임
        yield return null;   // Unit.Update 가 currentHealth 를 25 로 조인다

        int before = unit.currentHealth;
        Assert.Greater(before, 0, "플레이어 대역의 체력이 설정되지 않았다");

        // 충돌할 때까지 기다린다. 속도 20 이라 몇 프레임이면 닿는다.
        for (float t = 0f; t < 2f && unit.currentHealth == before; t += Time.deltaTime)
        {
            yield return null;
        }

        int lost = before - unit.currentHealth;
        Assert.AreNotEqual(0, lost, "탄환이 플레이어에 닿지 않았다 - 테스트 배치 문제다");
        Assert.AreEqual(OwnDamage, lost,
            "IceWave_Damage(" + IceDamage + ")를 읽으면 이 값이 " + IceDamage + " 가 된다");

        Object.Destroy(bossGo);
        Object.Destroy(playerGo);
        Object.Destroy(unit.Player_Attacked_Effect);
    }

    // ── followups 11 대응: 보스 체력이 프리팹 에셋에 쓰인다 ──────────────────
    //
    // Boss_Spawn 이 난이도별 체력을 이렇게 적용했다.
    //
    //   Boss_prefabs[3].GetComponent<Wind_Boss>().Monster_hpMax = Stage_4[0];
    //   Instantiate(Boss_prefabs[3], ...);
    //
    // Boss_prefabs[3] 은 **프리팹 에셋을 가리키는 참조**다. 만들어진 인스턴스가 아니라
    // 원본에 쓴다. 그래서 에디터에서 스테이지를 플레이하면 .prefab 파일이 실제로 바뀌고,
    // 플레이를 끝내도 되돌아오지 않는다. 빌드에서는 프리팹이 읽기 전용이라 드러나지 않아
    // 지금까지 눈에 띄지 않았다.
    //
    // 이 테스트가 보는 것은 두 가지다.
    //   1. 인스턴스가 난이도 체력을 그대로 받는가   (고치면서 동작이 틀어지지 않았는가)
    //   2. 프리팹 에셋이 그대로인가                 (결함이 사라졌는가)
    // 2번이 수정 전에는 실패한다.

    [UnityTest]
    public IEnumerator BossSpawn_WritesHpToTheInstanceNotThePrefabAsset()
    {
        Begin();

        // 난이도를 둘 다 끈 채로 한 번 띄운다. 그래야 Boss_Spawn 이 아무것도 쓰지 않아서
        // **프리팹의 원래 값을 먼저 읽어 둘 수 있다.** 값을 코드에 박아 두면 나중에
        // 프리팹을 손봤을 때 테스트가 엉뚱한 이유로 실패한다.
        // (대역은 LoadStage 가 세우고 Begin 이 난이도를 꺼 둔다)
        yield return LoadStage("4_StageBoss");
        Mode_Select mode = ModeStub();

        Boss_Spawn spawner = Object.FindObjectOfType<Boss_Spawn>();
        Assert.IsNotNull(spawner, "4_StageBoss 에 Boss_Spawn 이 없다");
        Assert.GreaterOrEqual(spawner.Boss_prefabs.Length, 4, "Boss_prefabs 가 4개 미만이다");
        Assert.GreaterOrEqual(spawner.Stage_4.Length, 1, "이 씬이 Stage_4 를 채워 주지 않았다");

        GameObject prefab = spawner.Boss_prefabs[3];
        int pristine = prefab.GetComponent<Monster_Stats>().Monster_hpMax;
        int stageHp = spawner.Stage_4[0];

        Assert.IsNull(Object.FindObjectOfType<Wind_Boss>(),
            "난이도가 둘 다 꺼져 있으면 보스가 생기지 않아야 한다");

        // 전제 확인. 둘이 같으면 아래 두 단언이 동시에 참이 되어 아무것도 못 잡는다.
        Assert.AreNotEqual(pristine, stageHp,
            "프리팹 기본값(" + pristine + ")과 스테이지 체력(" + stageHp + ")이 같으면 이 테스트는 무의미하다");

        // 이번에는 이지 난이도로 실제 소환을 돌린다.
        mode.Easy = true;
        yield return LoadScene("4_StageBoss");

        Wind_Boss boss = Object.FindObjectOfType<Wind_Boss>();
        Assert.IsNotNull(boss, "Boss_Spawn 이 보스를 만들지 않았다");

        // 1. 동작이 그대로인가
        Assert.AreEqual(stageHp, boss.Monster_hpMax,
            "인스턴스가 난이도 체력을 받지 못했다");

        // 2. 결함이 사라졌는가
        Assert.AreEqual(pristine, prefab.GetComponent<Monster_Stats>().Monster_hpMax,
            "프리팹 에셋의 Monster_hpMax 가 " + stageHp + " 로 바뀌었다. "
            + "저장소의 WindBoss.prefab 파일이 더러워진다");
    }

    // ── 같은 결함이 Monster_Spawn 에도 있었다 ────────────────────────────────
    //
    // Monster_Spawn 이 일반 몬스터 3종에 완전히 같은 짓을 한다. 게다가 foreach 안에
    // 있어서 소환 지점마다 프리팹에 다시 쓴다.
    //
    // **이지 난이도로는 이 결함을 볼 수 없다.** 1_Stage 의 몬스터 프리팹에 남아 있는
    // 값이 8 / 4 / 6 인데 그 씬의 이지 체력이 정확히 8 / 4 / 6 이다. 프리팹에 쓰든
    // 인스턴스에 쓰든 결과가 같아서 아무것도 드러나지 않는다.
    //
    // 그 값들이 애초에 **에디터에서 이지로 플레이한 잔재**라서 그렇다. 잔재가 자기를
    // 만든 결함을 가리고 있다. 그래서 하드(10 / 6 / 8)로 본다.

    [UnityTest]
    public IEnumerator MonsterSpawn_WritesHpToTheInstanceNotThePrefabAsset()
    {
        Begin();

        yield return LoadStage("1_Stage");
        Mode_Select mode = ModeStub();

        Monster_Spawn spawner = Object.FindObjectOfType<Monster_Spawn>();
        Assert.IsNotNull(spawner, "1_Stage 에 Monster_Spawn 이 없다");
        Assert.GreaterOrEqual(spawner.Monster_prefabs.Length, 3, "Monster_prefabs 가 3개 미만이다");

        GameObject[] prefabs = spawner.Monster_prefabs;
        int[] stageHp = { spawner.Normal_Hp[1], spawner.far_Hp[1], spawner.repeat_Hp[1] };
        string[] label = { "근거리", "원거리", "패트롤" };

        int[] pristine = new int[3];
        for (int i = 0; i < 3; i++)
        {
            pristine[i] = prefabs[i].GetComponent<Monster_Stats>().Monster_hpMax;
            Assert.AreNotEqual(pristine[i], stageHp[i],
                label[i] + " 프리팹 기본값과 하드 체력이 같아 이 테스트가 무의미하다");
        }

        // 이번에는 하드 난이도로 실제 소환을 돌린다.
        mode.Hard = true;
        yield return LoadScene("1_Stage");

        // 1. 동작이 그대로인가 - 인스턴스가 하드 체력을 받는다
        AssertClonesHaveHp<Normal_Monster>(stageHp[0], label[0]);
        AssertClonesHaveHp<Far_Monster>(stageHp[1], label[1]);
        AssertClonesHaveHp<Monster_Repeat>(stageHp[2], label[2]);

        // 2. 결함이 사라졌는가 - 프리팹 에셋은 그대로다
        for (int i = 0; i < 3; i++)
        {
            Assert.AreEqual(pristine[i], prefabs[i].GetComponent<Monster_Stats>().Monster_hpMax,
                label[i] + " 프리팹 에셋의 Monster_hpMax 가 " + stageHp[i] + " 로 바뀌었다");
        }
    }

    // ── followups 3 대응: 보스 클리어 후 포탈이 열리는지 ──────
    //
    // B단계에서 GroundBossPortal / IceBossPortal / FireBossPortal / WindBossPortal
    // 네 클래스를 BossClearPortal 하나 + StageId 값으로 합쳬다. 합치면서 생길 수 있는
    // 사고가 "어느 스테이지를 깨든 다 열린다"라서, **자기 스테이지에만 반응하는지**를 본다.
    //
    // 씬을 띄우지 않는다. BoolManager 의 클리어 플래그는 static 이고 포탈은 그것만
    // 보므로, 씬을 끌어오면 검증과 상관없는 것이 잔릿 딸려 올 뿐이다.

    [UnityTest]
    public IEnumerator BossClearPortal_OpensOnlyForItsOwnStage()
    {
        Begin();

        BoolManager.ResetBossCleared();

        GameObject door = new GameObject("PortalStub");
        door.SetActive(false);

        GameObject holder = new GameObject("BossClearPortalStub");
        BossClearPortal portal = holder.AddComponent<BossClearPortal>();
        portal.stage = StageId.Second;   // 얼음 스테이지 담당
        portal.Portal = door;

        yield return null;
        Assert.IsFalse(door.activeSelf, "아무것도 안 깨었는데 포탈이 열렸다");

        BoolManager.SetBossCleared(StageId.First);
        yield return null;
        Assert.IsFalse(door.activeSelf, "다른 스테이지를 깨었는데 열렸다 - 합치면서 stage 값을 안 보게 된 것이다");

        BoolManager.SetBossCleared(StageId.Second);
        yield return null;
        Assert.IsTrue(door.activeSelf, "담당 스테이지를 깨었는데 안 열렸다");

        // A단계에서 매 프레임 SetActive 를 반복하던 것을 멈추게 했다. 그것도 함께 본다.
        Assert.IsFalse(portal.enabled, "포탈이 열린 뒤에는 Update 를 멈춰야 한다");

        BoolManager.ResetBossCleared();   // static 이라 다음 테스트로 샜다
        Object.DestroyImmediate(holder);
        Object.DestroyImmediate(door);
    }

    // ── followups 2 대응: 씬 전환 시 Destroy / DestroyPL ──────────────
    //
    // A단계에서 DestroyPL 을 Destroy 의 상속으로 바꿨다. 둘의 차이는 **파괴할 씬 목록**
    // 하나뿐이다. DestroyPL 은 EndingScene 에서 살아남아야 한다 - 엔딩 장면에도
    // 플레이어가 보여야 하기 때문이다.
    //
    // **진짜 씬을 띄우지 않는다.** 이 두 클래스가 보는 것은 활성 씬의 **이름**뿐이라
    // 이름만 같은 빈 씬으로 충분하고, 그러면 그 씬의 다른 스크립트가 매 프레임 예외를
    // 던져 다음 테스트를 죽이는 일도 없다. 실제로 그 사고를 두 번 겪었다.

    [UnityTest]
    public IEnumerator SceneSwitch_DestroyAndDestroyPLFollowTheirOwnSceneLists()
    {
        Begin();

        // 4_StageBoss 로 치운다. UI_Main 을 띄우면 나중에 같은 이름의 합성 씬을
        // 만들 때 이름이 겹친다. 대역까지 세워 주므로 매 프레임 예외도 안 난다.
        yield return LoadStage("4_StageBoss");
        yield return SwitchTo("TestStage");    // 목록에 없는 이름 = 게임 진행 중

        GameObject common = new GameObject("CommonStub");
        common.AddComponent<Destroy>();
        GameObject carried = new GameObject("PlayerCarryStub");
        carried.AddComponent<DestroyPL>();

        yield return null;   // Start 가 돌아 DontDestroyOnLoad 로 옮겨진다
        yield return null;

        Assert.IsTrue(common != null, "게임 진행 중에는 공용 오브젝트가 남아야 한다");
        Assert.IsTrue(carried != null, "게임 진행 중에는 플레이어가 남아야 한다");

        // 여기서 갈린다.
        yield return SwitchTo("EndingScene");
        Assert.IsTrue(common == null, "Destroy 는 EndingScene 에서 사라져야 한다");
        Assert.IsTrue(carried != null, "DestroyPL 은 EndingScene 에서 남아야 한다 - 엔딩에도 플레이어가 보인다");

        // 메인 메뉴로 나가면 둘 다 사라진다.
        yield return SwitchTo("UI_Main");
        Assert.IsTrue(carried == null, "DestroyPL 은 UI_Main 에서 사라져야 한다");

        // UI_Main 으로 치운다. 보스 씬을 남기면 Boss_HpBar 가 다시 살아나 매 프레임
        // 예외를 던진다. 합성 UI_Main 은 위에서 이미 다 썼으므로 이름이 겹치지 않는다.
        yield return LoadScene("UI_Main");
    }

    // ── followups 4 대응: 대화 트리거가 발동하는지 ────────────────
    //
    // B단계에서 NPCTri / NPCTri2 / NPCTri3 / NPCTri4 네 클래스를 TalkTrigger 하나 +
    // TalkChannel 값으로 합쳬다. 합치면서 생길 수 있는 사고가 **채널과 진행도 카운터가
    // 어긋나는 것**이다. 어긋나면 이미 끝낸 대화가 다시 열리거나 열려야 할 대화가 안 열린다.
    //
    // 두 가지를 나눠서 본다.
    //   1. 실제 접촉으로 발동하는가        - 물리 충돌로 확인
    //   2. 채널마다 자기 진행도를 읽는가   - 게이트 로직만 직접 호출해 확인
    // 2를 물리로 하면 8번을 부딪혀야 하고 그만큼 불안정해진다. 어긋날 수 있는 자리는
    // 게이트 로직이지 Unity 의 충돌 전달이 아니다.

    [UnityTest]
    public IEnumerator TalkTrigger_FiresOnContactAndReadsItsOwnChannel()
    {
        Begin();

        TalkManager.DataNum = 0;
        TalkManager.DataNum2 = 0;
        EndTalkManager.DataNum = 0;
        EndTalkManager.DataNum2 = 0;

        // 1. 실제로 부딪히면 열리는가.
        bool contact = false;
        yield return FireByContact(TalkChannel.MainFirst, 3, r => contact = r);
        Assert.IsTrue(contact, "플레이어가 닿았는데 대화창이 열리지 않았다");

        // 2. 채널마다 자기 진행도를 읽는가.
        //    MainFirst 만 한도에 닿게 해 두면 그 채널만 막혀야 한다.
        TalkManager.DataNum = 3;
        Assert.IsFalse(Fires(TalkChannel.MainFirst, 3), "MainFirst 는 진행도가 한도에 닿아 막혀야 한다");
        Assert.IsTrue(Fires(TalkChannel.MainSecond, 3), "MainSecond 가 TalkManager.DataNum 을 읽고 있다");
        Assert.IsTrue(Fires(TalkChannel.EndFirst, 3), "EndFirst 가 TalkManager.DataNum 을 읽고 있다");
        Assert.IsTrue(Fires(TalkChannel.EndSecond, 3), "EndSecond 가 TalkManager.DataNum 을 읽고 있다");

        //    이번에는 EndSecond 만 막는다. 반대 방향으로도 섞이지 않는지 확인한다.
        TalkManager.DataNum = 0;
        EndTalkManager.DataNum2 = 3;
        Assert.IsTrue(Fires(TalkChannel.MainFirst, 3), "MainFirst 가 EndTalkManager.DataNum2 를 읽고 있다");
        Assert.IsFalse(Fires(TalkChannel.EndSecond, 3), "EndSecond 는 진행도가 한도에 닿아 막혀야 한다");

        // 3. Player 가 아닌 것에는 반응하지 않는가.
        Assert.IsFalse(Fires(TalkChannel.MainFirst, 3, false), "Player 가 아닌 것에 반응했다");

        TalkManager.DataNum = 0;
        EndTalkManager.DataNum2 = 0;
        Time.timeScale = 1f;
        yield return null;
    }

    // ── 도우미 ────────────────────────────────────────────────────────────────

    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    // 스테이지 씬을 게임이 실제로 거치는 순서대로 띄운다.
    //
    // 스테이지 씬에는 플레이어도 사운드 매니저도 들어 있지 않다. 둘 다 앞 씬에서
    // DontDestroyOnLoad 로 넘어온다. 스테이지 씬을 단독으로 로드하면
    //
    //   SpawnPoint.Awake      FindWithTag("Player") 가 null -> NRE
    //   Camera_test.Start     같은 이유로 playerTransform 미할당
    //   Basic_Boss.Start      Player_Hit / Player / Target 을 못 찾는다
    //   Wind_Boss.SpawnBullet SfxManager.instance 가 null -> 탄환이 하나도 안 나간다
    //   TalkManagerBase.Update PL 이 null -> 매 프레임 예외로 Update 가 중단된다
    //
    // 가 전부 터진다. 처음엔 스테이지만 로드했다가 이걸 다 맞았다.
    //
    // UI_Main 을 먼저 띄우는 이유가 사운드 매니저 같은 싱글턴을 만들기 위해서다.
    // 플레이어는 캐릭터 선택을 거쳐야 만들어지므로 대역을 세워 넘긴다.
    static IEnumerator LoadStage(string name)
    {
        // 씬을 띄우기 **전에** 세운다. UI_Main 이 로드되는 동안에도 Player 를 찾는
        // 코드가 돌기 때문이다. 뒤에 세우면 그 사이에 예외가 난다.
        EnsurePlayerStub();
        EnsureModeStub();
        yield return LoadScene("UI_Main");
        yield return LoadScene(name);
        QuietBossHpBars();
    }

    // 보스 씬의 체력바를 끈다.
    //
    // Boss_HpBar.Start 가 FindWithTag("Monster") 로 보스를 찾아 stat 에 담는데, 널 검사가
    // 없다. 보스가 없으면 stat 이 null 로 남고 **Update 가 매 프레임 예외를 던진다.**
    // 실제 게임에서는 Boss_Spawn.Awake 가 Start 보다 먼저 보스를 만들어 주므로 안 걸린다.
    //
    // 테스트는 난이도 대역을 꺼 두어 보스가 생기지 않는 상태로 보스 씬을 띄우는 일이
    // 많다. 그 예외 로그가 **다음 테스트에 귀속되어** 그쪽을 죽인다. 실제로 셋이 죽었다.
    // 우리가 보는 것은 체력바가 아니므로 꺼서 소음을 없앤다.
    static void QuietBossHpBars()
    {
        foreach (Boss_HpBar bar in Object.FindObjectsOfType<Boss_HpBar>())
        {
            bar.enabled = false;
        }
    }

    // 난이도 대역. 난이도를 둘 다 끈 채로 세워 둔다 (followups 14).
    //
    // Boss_Spawn.Awake 와 Monster_Spawn.Start 가 FindObjectOfType<Mode_Select>() 를
    // 널 검사 없이 쓴다. 실제 게임에서는 UI_Select 의 DontDestroy 오브젝트가 따라오는데
    // 테스트는 그 씬을 거치지 않아서, 없으면 매번 NullReferenceException 이 콘솔에
    // 쌓였다. 무해한 예외가 늘 떠 있으면 진짜 문제가 생겨도 묻힌다.
    //
    // 둘 다 꺼 두면 스포너가 아무것도 소환하지 않으므로, 보스를 직접 세우는 테스트가
    // 보스를 두 마리 보는 일도 없다. 난이도가 필요한 테스트만 플래그를 켠다.
    static void EnsureModeStub()
    {
        if (modeStub != null) return;
        modeStub = new GameObject("ModeSelectStub");
        Mode_Select mode = modeStub.AddComponent<Mode_Select>();
        mode.Easy = false;
        mode.Hard = false;
        Object.DontDestroyOnLoad(modeStub);
    }

    static Mode_Select ModeStub()
    {
        EnsureModeStub();
        return modeStub.GetComponent<Mode_Select>();
    }

    // 보스를 세운다.
    //
    // 보스는 씬에 들어 있지 않다. 4_StageBoss 가 참조하는 프리팹은 Boss_Spawn,
    // Portal, SpawnPoint 셋뿐이고 보스는 Boss_Spawn.Awake 가 만든다.
    //
    // **그런데 Boss_Spawn 을 그대로 쓰지 않고 프리팹만 빌려 직접 세운다.**
    //
    // 원래 이유는 결함이었다. Boss_Spawn 이 인스턴스가 아니라 프리팹 에셋에 체력을 써서
    // 테스트를 돌릴 때마다 WindBoss.prefab 이 더러워졌다. 그 결함은 고쳤고
    // BossSpawn_WritesHpToTheInstanceNotThePrefabAsset 이 회귀를 잡는다.
    //
    // 고친 뒤에도 직접 세우는 것을 유지한다. 여기서 보는 것은 **보스의 탄막 패턴**이지
    // 소환 절차가 아니다. Boss_Spawn 을 거치면 Mode_Select 대역까지 딸려 와서, 패턴이
    // 깨졌을 때 원인이 어느 쪽인지 바로 드러나지 않는다.
    static IEnumerator SpawnWindBoss(System.Action<Wind_Boss> assign)
    {
        Boss_Spawn spawner = Object.FindObjectOfType<Boss_Spawn>();
        Assert.IsNotNull(spawner, "4_StageBoss 에 Boss_Spawn 이 없다");
        Assert.GreaterOrEqual(spawner.Boss_prefabs.Length, 4, "Boss_prefabs 가 4개 미만이다");

        GameObject point = GameObject.FindGameObjectWithTag("Boss_Spawn");
        Assert.IsNotNull(point, "Boss_Spawn 태그를 단 소환 위치가 없다");

        GameObject go = Object.Instantiate(spawner.Boss_prefabs[3],
                                           point.transform.position, point.transform.rotation);
        Wind_Boss boss = go.GetComponent<Wind_Boss>();
        Assert.IsNotNull(boss, "Boss_prefabs[3] 에 Wind_Boss 가 없다");

        // LoadStage 가 이미 껐지만, 이 도우미를 LoadStage 없이 부르는 경우를 대비한다.
        QuietBossHpBars();

        yield return null;   // 보스의 Awake / Start 가 도는 프레임
        assign(boss);
    }

    // 소환된 몬스터만 본다. 씬에 미리 놓인 몬스터가 있으면 그쪽은 스포너가 체력을
    // 건드리지 않으므로 함께 보면 안 된다. Instantiate 가 붙이는 "(Clone)" 으로 가른다.
    static void AssertClonesHaveHp<T>(int expected, string what) where T : Monster_Stats
    {
        T[] clones = Object.FindObjectsOfType<T>()
                           .Where(m => m.name.EndsWith("(Clone)")).ToArray();
        Assert.Greater(clones.Length, 0, what + " 몬스터가 하나도 소환되지 않았다");
        foreach (T m in clones)
        {
            Assert.AreEqual(expected, m.Monster_hpMax, what + " 인스턴스가 난이도 체력을 받지 못했다");
        }
    }

    // 이름만 가진 빈 씬으로 활성 씬을 바꿈다. activeSceneChanged 가 실제로 발생한다.
    static IEnumerator SwitchTo(string name)
    {
        SceneManager.SetActiveScene(SceneManager.CreateScene(name));
        yield return null;   // activeSceneChanged -> Apply 가 파괴를 예약한다
        yield return null;   // 파괴가 반영되는 프레임
    }

    // 대화 트리거 한 벌을 세운다. 다른 씬 콜라이더에 걸리지 않도록 멀리 떨어뜨린다.
    static void BuildTalkTrigger(TalkChannel channel, int maxDataNum, bool playerTag,
                                 out TalkTrigger trigger, out GameObject panel,
                                 out GameObject other, Vector3 origin)
    {
        panel = new GameObject("TalkPanelStub");
        panel.SetActive(false);

        GameObject go = new GameObject("TalkTriggerStub");
        go.transform.position = origin;
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2f, 2f);

        trigger = go.AddComponent<TalkTrigger>();
        trigger.TalkPannel = panel;
        trigger.channel = channel;
        trigger.maxDataNum = maxDataNum;
        trigger.requireBossSeal = false;

        other = new GameObject("TalkOtherStub");
        // 태그를 Player 로 두면 트리거가 받아들여야 한다. 아니면 무시해야 한다.
        other.tag = playerTag ? "Player" : "Untagged";
        other.AddComponent<BoxCollider2D>();
    }

    // 게이트 로직만 본다. 물리 없이 OnTriggerEnter2D 를 직접 부른다.
    static bool Fires(TalkChannel channel, int maxDataNum, bool playerTag = true)
    {
        Vector3 origin = new Vector3(7000f, 7000f, 0f);
        TalkTrigger trigger;
        GameObject panel, other;
        BuildTalkTrigger(channel, maxDataNum, playerTag, out trigger, out panel, out other, origin);

        var m = typeof(TalkTrigger).GetMethod("OnTriggerEnter2D", Any);
        m.Invoke(trigger, new object[] { other.GetComponent<Collider2D>() });
        bool fired = panel.activeSelf;

        // 트리거는 열면서 timeScale 을 0 으로 만들고 끝난다. 되돌리지 않으면
        // 다음 확인의 물리와 WaitForSeconds 가 멈춘다.
        Time.timeScale = 1f;
        Object.DestroyImmediate(trigger.gameObject);
        Object.DestroyImmediate(panel);
        Object.DestroyImmediate(other);
        return fired;
    }

    // 실제 물리 충돌로 발동하는지 본다.
    static IEnumerator FireByContact(TalkChannel channel, int maxDataNum, System.Action<bool> result)
    {
        Vector3 origin = new Vector3(8000f, 8000f, 0f);
        TalkTrigger trigger;
        GameObject panel, mover;
        BuildTalkTrigger(channel, maxDataNum, true, out trigger, out panel, out mover, origin);

        mover.transform.position = origin + new Vector3(3f, 0f, 0f);
        Rigidbody2D body = mover.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.velocity = new Vector2(-6f, 0f);

        yield return null;

        bool fired = false;
        // timeScale 이 0 이 되면 물리가 멈추므로 unscaled 로 센다.
        for (float t = 0f; t < 2f && !fired; t += Time.unscaledDeltaTime)
        {
            fired = panel.activeSelf;
            yield return null;
        }

        Time.timeScale = 1f;
        Object.DestroyImmediate(trigger.gameObject);
        Object.DestroyImmediate(panel);
        Object.DestroyImmediate(mover);
        result(fired);
    }

    static GameObject modeStub;

    static GameObject playerStub;

    // 실제 플레이어 프리팹(Player_Sword 등)을 쓰지 않는다. 플레이모드 테스트에서는
    // 에셋을 경로로 불러올 수 없다. 대신 씬 코드가 실제로 요구하는 것만 갖춘다 -
    // Player 태그, AllUnits.Unit, CurMapName, Inventory, 그리고 실제 플레이어
    // 프리팹이 다는 태그를 단 자식들.
    // Target 태그는 보스 프리팹 안에 있어서 대역이 필요 없다.
    // 테스트마다 새로 만들지 않고 실행 내내 하나를 쓴다.
    //
    // 처음에는 [TearDown] 에서 지웠는데, 그러면 앞 씬의 Camera_test 가 이미 없어진
    // Transform 을 계속 참조해서 MissingReferenceException 이 매 FixedUpdate 마다
    // 났다. 그 로그는 **다음 테스트에 귀속되어** 그쪽을 실패시켰다. 테스트 4개가
    // 그것 때문에 죽었다.
    //
    // 실제 게임에서도 플레이어는 한 번 만들어져 계속 따라다닌다. 그쪽이 맞다.
    static void EnsurePlayerStub()
    {
        if (playerStub != null) return;

        playerStub = new GameObject("PlayerStub");
        playerStub.tag = "Player";
        playerStub.AddComponent<CurMapName>();

        AllUnits.Unit unit = playerStub.AddComponent<AllUnits.Unit>();
        unit.clip_attacked = new AudioClip[0];   // 비면 SfxManager 를 안 부른다
        unit.me = playerStub;
        unit.Player_Attacked_Effect = new GameObject("HitEffectStub");
        unit.Player_Attacked_Effect.transform.SetParent(playerStub.transform);

        // 실제 플레이어 프리팹(Player_Sword / Player_Spear / Player_shield)이 다는 태그
        // 전부다. 프리팹 YAML 의 m_TagString 을 뽑아서 맞췄다.
        //
        // 처음에는 Player_Hit 만 넣었다가 1_Stage 를 띄우고 물렸다. Far_Monster.Start 가
        // FindGameObjectWithTag("Far_Attack_Pos").transform 을 널 검사 없이 부르는데,
        // 대역에 그 태그가 없어서 playerTransform 이 null 로 남고 Update 가 매 프레임
        // 예외를 던졌다. 그 로그가 **다음 테스트에 귀속되어** WindBoss 테스트 2개를 죽였다.
        //
        // 하나씩 겪지 않으려고 스크립트가 FindWithTag 로 찾는 태그를 전부 뽑아
        // 플레이어 프리팹이 다는 것과 대조했다. 빠진 것이 셋이었다.
        foreach (string childTag in new[] { "Player_Hit", "Far_Attack_Pos",
                                            "Monster_Skill_Pos", "Player_Weapon" })
        {
            GameObject child = new GameObject(childTag + "Stub");
            child.tag = childTag;
            child.transform.SetParent(playerStub.transform);
        }

        // 인벤토리는 실제 게임에서도 **플레이어 프리팹(sel_HeroKnight*)에 붙어** 씬을
        // 넘어간다. 그래서 여기 둔다.
        //
        // 없으면 1_Stage 의 InventoryUI 가 Update 마다 NRE 를 던진다. inven 은
        // Inventory.instance 가 있을 때만 채워지는데, 없을 때 가는 else 가 하필
        // `Inventory.instance = inven`(= null) 이라 끝까지 null 로 남는다.
        // 그 예외 로그가 **다음 테스트에 귀속되어** WindBoss 테스트 2개를 죽였다.
        playerStub.AddComponent<Inventory>();

        Object.DontDestroyOnLoad(playerStub);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        // 전부 끝난 뒤에만 지운다. 테스트 사이에 지우면 앞 씬이 없어진 것을 참조해
        // 예외를 쏟는다.
        if (playerStub != null)
        {
            Object.Destroy(playerStub);
            playerStub = null;
        }
        if (modeStub != null)
        {
            Object.Destroy(modeStub);
            modeStub = null;
        }
    }

    static IEnumerator LoadScene(string name)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
        while (!op.isDone) yield return null;
        yield return null;   // Awake 다음 프레임. Start 가 돈다
    }

    static IEnumerator SelectLocale(int index)
    {
        var init = LocalizationSettings.InitializationOperation;
        while (!init.IsDone) yield return null;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
        yield return null;
    }

    // TalkManagerBase.channels 는 protected 다. Channel 도 protected 중첩 클래스라
    // 타입 이름을 쓸 수 없어 object 로 다룬다.
    static object[] Channels(TalkManagerBase mgr)
    {
        return (object[])typeof(TalkManagerBase).GetField("channels", Any).GetValue(mgr);
    }

    static object Field(object channel, string name)
    {
        return channel.GetType().GetField(name, Any).GetValue(channel);
    }

    static Dictionary<int, string> Dict(object channel, string name)
    {
        return (Dictionary<int, string>)Field(channel, name);
    }

    static int Read(object channel)
    {
        return (int)((System.Func<int>)Field(channel, "Read")).Invoke();
    }

    static void Write(object channel, int value)
    {
        ((System.Action<int>)Field(channel, "Write")).Invoke(value);
    }

    // 코루틴의 완료를 기다리면 안 된다. Advance 는 대사를 세팅하고 진행도를 올린 뒤
    // WaitForSeconds(0.5f) 로 끝나는데, 대화 중에는 timeScale 이 0 이라 그 대기가
    // 영원히 안 끝난다. 게임에서도 그 코루틴은 일시정지 동안 매달린 채로 남는다 -
    // 눈에 보이는 일은 첫 yield 전에 이미 다 끝났기 때문에 문제가 되지 않을 뿐이다.
    //
    // StartCoroutine 은 첫 yield 까지를 그 자리에서 동기로 실행한다. 그래서 부르기만
    // 하면 대사 세팅과 진행도 증가가 끝나 있다.
    static void Advance(MonoBehaviour mgr, object channel)
    {
        var m = typeof(TalkManagerBase).GetMethod("Advance", Any);
        mgr.StartCoroutine((IEnumerator)m.Invoke(mgr, new object[] { channel }));
    }

    static System.Func<IEnumerator>[] Patterns(Basic_Boss boss)
    {
        var m = typeof(Basic_Boss).GetMethod("BuildPatterns", Any);
        return (System.Func<IEnumerator>[])m.Invoke(boss, null);
    }

    static T Invoke<T>(object target, string method)
    {
        return (T)target.GetType().GetMethod(method, Any).Invoke(target, null);
    }

    static int CountClones(string cloneName)
    {
        int n = 0;
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
        {
            if (t.name == cloneName) n++;
        }
        return n;
    }
}
#endif

