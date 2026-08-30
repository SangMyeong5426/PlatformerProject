using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class One_Stage_Boss : Basic_Boss
{
    //public BoxCollider2D HitBox;
    public GameObject EarthGrowSkill, Pre_EarthGrow; // 2번째 스킬
    public GameObject EarthGrow_1, EarthGrow_2, EarthGrow_3;
    Transform Player_Head;
    public Transform Earth_skill_pos_1, Earth_skill_pos_1_1, Earth_skill_pos_2, Earth_skill_pos_2_2, 
        Earth_skill_pos_3, Earth_skill_pos_3_3, Earth_Bullet_Pos; //락 스킬 올라오는 transform 1,2,3  // 바위 굴러가는 포지션
    public GameObject EarthBullet; // 바위

    public AudioClip[] clip; // 0 = 돌진, 1 = 스킬

    // ── 공통 골격에 넘기는 값 ──────────────────────────────
    // 돌진 애니메이션이 "Run"이고, 텔레포트 후딜이 1.5초로 가장 길다.
    // 4종 중 유일하게 돌진에 효과음이 붙는다.

    protected override string DashAnimParam => "Run";
    protected override float TeleportPostDelay => 1.5f;

    protected override void PlayDashSfx()
    {
        SfxManger.instance.SfxPlay("Rock_Rush", clip[0]);
    }

    protected override System.Func<IEnumerator>[] BuildPatterns()
    {
        return new System.Func<IEnumerator>[]
        {
            EarthRock, // 0
            EarthGrow, // 1
            Dash,      // 2
            Teleport,  // 3
        };
    }

    protected override StageId? ClearedStage => StageId.First;

    protected override void Start()
    {
        base.Start();
        StartCoroutine(PatternLoop());
        Player_Head = GameObject.FindGameObjectWithTag("Monster_Skill_Pos").GetComponent<Transform>();

    }

    IEnumerator EarthGrow()
    {
        base.LookPlayer();
        anim.SetBool("Attack_2", true); // 애니메이션 실행
        SfxManger.instance.SfxPlay("Rock_Skill_1", clip[1]);
        yield return new WaitForSeconds(1f); // 1초뒤에
        GameObject Skill_1_pos = Instantiate(EarthGrow_1, Earth_skill_pos_1.position, Quaternion.Euler(0, 0, 0)); // 첫번째 위치
        GameObject Skill_1_1_pos = Instantiate(EarthGrow_1, Earth_skill_pos_1_1.position, Quaternion.Euler(0, 0, 0)); // 첫번째 위치
        yield return new WaitForSeconds(0.5f); // 0.5초뒤에
        //Destroy(Skill_1_pos); // 준비 스킬 삭제
        GameObject Skill_2_pos = Instantiate(EarthGrow_2, Earth_skill_pos_2.position, Quaternion.Euler(0, 0, 0)); // 두번째 위치
        GameObject Skill_2_2_pos = Instantiate(EarthGrow_2, Earth_skill_pos_2_2.position, Quaternion.Euler(0, 0, 0)); // 두번째 위치
        yield return new WaitForSeconds(0.5f); // 0.5초뒤에
        GameObject Skill_3_pos = Instantiate(EarthGrow_3, Earth_skill_pos_3.position, Quaternion.Euler(0, 0, 0)); // 세번째 위치
        GameObject Skill_3_3_pos = Instantiate(EarthGrow_3, Earth_skill_pos_3_3.position, Quaternion.Euler(0, 0, 0)); // 세번째 위치

        //GameObject Skill_1 = Instantiate(EarthGrowSkill, Skill_1_pos.transform.position, Quaternion.Euler(0, 0, 0)); // 플레이어 위치에 스킬 뜸
        Destroy(Skill_1_pos, 0.5f);
        Destroy(Skill_1_1_pos, 0.5f);
        Destroy(Skill_2_pos, 0.5f);
        Destroy(Skill_2_2_pos, 0.5f);
        Destroy(Skill_3_pos, 0.5f);
        Destroy(Skill_3_3_pos, 0.5f);
        //Destroy(Skill_1, 2f); // 1초뒤에 삭제
        anim.SetBool("Attack_2", false); // 애니메이션 Idle로
    }

    IEnumerator EarthRock()
    {
        base.LookPlayer();
        anim.SetBool("Attack", true);
        yield return new WaitForSeconds(0.3f);
        GameObject Skill_Bullet = Instantiate(EarthBullet, Attack_Pos.position, transform.rotation);
        yield return new WaitForSeconds(1.5f);
        anim.SetBool("Attack", false);
        Destroy(Skill_Bullet, 3.5f);
    }
   
    private void OnDrawGizmos() // 추적 범위
    {
        Gizmos.color = Color.red;
        //Gizmos.DrawWireSphere(transform.position, Radius);

    }
}
