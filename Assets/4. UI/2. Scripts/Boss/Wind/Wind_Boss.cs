using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wind_Boss : Basic_Boss
{
    public Transform Tornado1, Tornado2, Tornado3, Tornado4, BulletPos; 
    public GameObject TornadoPrefab;
    public GameObject bullet;

    public AudioClip[] clip; // 0 = 토네이도, 1 = 탄막

    // ── 공통 골격에 넘기는 값 ──────────────────────────────
    // DashAnimParam("Dash"), PatternInterval(2.0), TeleportPreDelay(0.5) 는
    // 기본값과 같아 재정의하지 않는다. 텔레포트 후딜만 1.0초로 다르다.

    protected override float TeleportPostDelay => 1.0f;

    protected override System.Func<IEnumerator>[] BuildPatterns()
    {
        return new System.Func<IEnumerator>[]
        {
            Teleport,     // 0
            Dash,         // 1
            SpawnTornado, // 2
            SpawnTornado, // 3 - 2번과 동일. 토네이도가 50% 확률로 나오는 기존 동작을 그대로 둔다
        };
    }

    protected override void SetStageCleared()
    {
        BoolManager.FourthStageBossDie = true;
    }

    protected override void Start()
    {
        base.Start();
        StartCoroutine(PatternLoop());
    }

    // 탄막 발사 위치의 회전만 바람 보스 고유 처리다.
    protected override void Update()
    {
        base.Update();
        BulletPos.transform.rotation = transform.rotation;
    }

    IEnumerator SpawnTornado()
    {
        anim.SetBool("Tornado", true);
        SfxManger.instance.SfxPlay("Wind_Skill_Tornado", clip[0]);
        yield return new WaitForSeconds(1f);
        Instantiate(TornadoPrefab, Tornado1.position, Tornado1.rotation);
        Instantiate(TornadoPrefab, Tornado2.position, Tornado1.rotation);
        Instantiate(TornadoPrefab, Tornado3.position, Tornado1.rotation);
        Instantiate(TornadoPrefab, Tornado4.position, Tornado1.rotation);
        yield return new WaitForSeconds(0.5f);
        anim.SetBool("Tornado", false);
        yield return new WaitForSeconds(3.5f);

        // 토네이도는 유일하게 스케줄러로 복귀하지 않고 탄막으로 이어진다.
        // yield return 으로 감싸야 스케줄러가 연쇄 전체(10.5초)를 기다린다.
        yield return StartCoroutine(SpawnBullet());
    }

    // 버스트 4회의 시작 각도. 매번 조금씩 틀어 쏴야 탄막에 빈틈이 생기지 않는다.
    static readonly int[] BurstOffsets = { 0, 10, 17, 24 };

    IEnumerator SpawnBullet()
    {
        LookPlayer();
        anim.SetBool("Bullet", true);
        SfxManger.instance.SfxPlay("Wind_Skill_smallTor", clip[1]);
        yield return new WaitForSeconds(1f);

        for (int b = 0; b < BurstOffsets.Length; b++)
        {
            // 대기는 버스트 사이에만 넣는다. 마지막 뒤에 붙이면 총 시간이 0.5초 늘어난다.
            if (b > 0) yield return new WaitForSeconds(0.5f);
            FireBurst(BurstOffsets[b]);
        }

        yield return new WaitForSeconds(3f);
        anim.SetBool("Bullet", false);
    }

    // 25도 간격으로 15발을 원형으로 발사한다.
    void FireBurst(int offset)
    {
        for (int angle = 0; angle < 360; angle += 25)
        {
            GameObject temp = Instantiate(bullet);
            Destroy(temp, 2f);
            temp.transform.position = BulletPos.transform.position;
            temp.transform.rotation = Quaternion.Euler(0, 0, angle + offset);
        }
    }
}
