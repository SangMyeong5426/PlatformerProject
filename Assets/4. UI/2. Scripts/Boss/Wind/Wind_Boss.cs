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

    // Update is called once per frame
    protected override void Update()
    {
        if (isDash == true)
        {
            transform.position = Vector2.MoveTowards(transform.position, DashDir.position, speed * Time.deltaTime);
            anim.SetBool(DashAnimParam, true);
        }

        BulletPos.transform.rotation = transform.rotation;

        if (MonsterDie)
        {
            SetStageCleared();
            isDash = false;
        }
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

    IEnumerator SpawnBullet()
    {
        LookPlayer();
        anim.SetBool("Bullet", true);
        SfxManger.instance.SfxPlay("Wind_Skill_smallTor", clip[1]);
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < 360; i += 25)
        {
            GameObject temp = Instantiate(bullet);
            Destroy(temp, 2f);
            temp.transform.position = BulletPos.transform.position;
            temp.transform.rotation = Quaternion.Euler(0, 0, i);
        }
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < 360; i += 25)
        {
            GameObject temp = Instantiate(bullet);
            Destroy(temp, 2f);
            temp.transform.position = BulletPos.transform.position;
            temp.transform.rotation = Quaternion.Euler(0, 0, i+10);
        }
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < 360; i += 25)
        {
            GameObject temp = Instantiate(bullet);
            Destroy(temp, 2f);
            temp.transform.position = BulletPos.transform.position;
            temp.transform.rotation = Quaternion.Euler(0, 0, i + 17);
        }
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < 360; i += 25)
        {
            GameObject temp = Instantiate(bullet);
            Destroy(temp, 2f);
            temp.transform.position = BulletPos.transform.position;
            temp.transform.rotation = Quaternion.Euler(0, 0, i + 24);
        }

        yield return new WaitForSeconds(3f);
        anim.SetBool("Bullet", false);
    }
}
