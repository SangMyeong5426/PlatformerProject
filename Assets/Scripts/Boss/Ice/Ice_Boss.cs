using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ice_Boss : Basic_Boss
{
    //public BoxCollider2D HitBox;
    public GameObject Attack_Skill_2, Ice_Arrow, Pre_Ice_Spike;
    public Transform self;
    public Transform Skill_pos_2, Ice_Arrow_pos;
    public AudioClip[] clip; // 0 = 얼음 강타, 1 = 얼음 슬램

    //public Transform self_tr;
    //public Vector2 monster_boxSize;
    //public BoxCollider2D mon_attack;

    public Transform Attack_Pos_slam;


    // ── 공통 골격에 넘기는 값 ──────────────────────────────
    // 돌진 애니메이션이 "Run"이고, 텔레포트 선딜이 0.8초로 유일하게 다르다.
    // 돌진·텔레포트 모두 효과음은 없다.

    protected override string DashAnimParam => "Run";
    protected override float TeleportPreDelay => 0.8f;

    protected override System.Func<IEnumerator>[] BuildPatterns()
    {
        return new System.Func<IEnumerator>[]
        {
            Ice_Bullet,    // 0
            Dash,          // 1
            Second_Attack, // 2
            Teleport,      // 3
        };
    }

    protected override StageId? ClearedStage => StageId.Second;

    protected override void Start()
    {
        base.Start();
        // 스케줄러보다 먼저 대입해야 한다. 지금은 진입 대기 2초 덕에 우연히 동작하지만
        // 대기 시간을 줄이는 순간 Ice_Bullet에서 NullReferenceException이 난다.
        Ice_Arrow_pos = GameObject.FindGameObjectWithTag("Monster_Skill_Pos").GetComponent<Transform>();
        StartCoroutine(PatternLoop());

        
        
    }


    IEnumerator Ice_Bullet()
    {
        base.LookPlayer();
        anim.SetBool("Attack", true); // 애니메이션 실행
        SfxManager.instance.SfxPlay("Ice_Skill_1", clip[0]);

        /*yield return new WaitForSeconds(1f); // 1초뒤에
        GameObject Skill_1_pos = Instantiate(Pre_Ice_Spike, Ice_Arrow_pos.position, Quaternion.Euler(0, 0, 0)); // 플레이어 위치에 준비 스킬뜨고
        anim.SetBool("Attack", false); // 애니메이션 Idle로
        */
        yield return new WaitForSeconds(0.5f); // 1초뒤에

        GameObject Skill_1 = Instantiate(Ice_Arrow, Ice_Arrow_pos.transform.position, Quaternion.Euler(0, 0, 0)); // 플레이어 위치에 스킬 뜸
        anim.SetBool("Attack", false);
        //Destroy(Skill_1_pos); // 준비 스킬 삭제
        Destroy(Skill_1, 0.8f); // 1초뒤에 삭제
    }

    
    IEnumerator Second_Attack()
    {
        base.LookPlayer();
        
        anim.SetBool("Attack_2", true);
        SfxManager.instance.SfxPlay("Ice_Skill_explosion", clip[1]);
        yield return new WaitForSeconds(1f); // 1초 뒤에
        GameObject Skill_2 = Instantiate(Attack_Skill_2, Skill_pos_2.position, Skill_pos_2.rotation); //인스턴시에이트
        Destroy(Skill_2, 2f);
        anim.SetBool("Attack_2", false);
    }


    /*public void en_Attack()
    {
        mon_attack.enabled = true;
    }
    public void de_Attack()
    {
        mon_attack.enabled = false;
    }*/

    /*private void OnTriggerEnter2D(Collider2D collision)
    {
        Collider2D[] collider2Ds = Physics2D.OverlapBoxAll(self_tr.position, monster_boxSize, 0);
        foreach (Collider2D collider in collider2Ds)
            if (collider.tag == "Player")
            {
                AllUnits.Unit player_Hp = collision.gameObject.GetComponent<AllUnits.Unit>();
                if (player_Hp != null)
                {

                    player_Hp.TakeDamage(Monster_Damage);
                    // 체력 감소

                }
            }

    }*/

    private void OnDrawGizmos() // 추적 범위
    {
        Gizmos.color = Color.red;
        //Gizmos.DrawWireSphere(transform.position, Radius);

    }
}
