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
        unit.clip_attacked = new AudioClip[0];              // 비면 SfxManger 를 안 부른다
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
    //   Wind_Boss.SpawnBullet SfxManger.instance 가 null -> 탄환이 하나도 안 나간다
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
        yield return LoadScene("UI_Main");
        yield return LoadScene(name);
    }

    // 보스를 세운다.
    //
    // 보스는 씬에 들어 있지 않다. 4_StageBoss 가 참조하는 프리팹은 Boss_Spawn,
    // Portal, SpawnPoint 셋뿐이고 보스는 Boss_Spawn.Awake 가 만든다.
    //
    // **그런데 Boss_Spawn 을 그대로 쓰지 않는다.** 그 코드가 이렇게 되어 있다.
    //
    //   Boss_prefabs[3].GetComponent<Wind_Boss>().Monster_hpMax = Stage_4[0];
    //
    // 인스턴스가 아니라 **프리팹 에셋에** 값을 쓴다. 프리팹의 Monster_hpMax 는 10 인데
    // 씬이 넘기는 Stage_4[0] 은 120 이라, 에디터에서 실행하면 WindBoss.prefab 이
    // 실제로 바뀐다. 테스트가 저장소를 더럽히면 안 되므로 프리팹만 빌려 직접 세운다.
    // 이 결함 자체는 docs/followups.md 에 올렸다.
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

        // HP 바는 자기 Start 에서 보스를 못 찾고 이미 죽었다(FindWithTag("Monster")).
        // 실제 게임에서는 Boss_Spawn.Awake 가 Start 보다 먼저 보스를 만들어 준다.
        // 우리 관심사가 아니므로 꺼서 소음을 없앤다.
        foreach (Boss_HpBar bar in Object.FindObjectsOfType<Boss_HpBar>())
        {
            bar.enabled = false;
        }

        yield return null;   // 보스의 Awake / Start 가 도는 프레임
        assign(boss);
    }

    static GameObject playerStub;

    // 실제 플레이어 프리팹(Player_Sword 등)을 쓰지 않는다. 플레이모드 테스트에서는
    // 에셋을 경로로 불러올 수 없다. 대신 씬 코드가 실제로 요구하는 것만 갖춘다 -
    // Player 태그, AllUnits.Unit, CurMapName, 그리고 Player_Hit 태그를 단 자식.
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
        unit.clip_attacked = new AudioClip[0];   // 비면 SfxManger 를 안 부른다
        unit.me = playerStub;
        unit.Player_Attacked_Effect = new GameObject("HitEffectStub");
        unit.Player_Attacked_Effect.transform.SetParent(playerStub.transform);

        GameObject hit = new GameObject("PlayerHitStub");
        hit.tag = "Player_Hit";
        hit.transform.SetParent(playerStub.transform);

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

