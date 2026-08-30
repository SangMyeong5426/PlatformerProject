using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fire_Boss : Basic_Boss
{
    public Transform BreathPos, MeteorPos;
    public GameObject BreathPrepab, MeteorPrepab;

    public AudioClip[] clip; // 0 = 불 평타, 1 = 브레스, 2 = 메테오

    // ── 공통 골격에 넘기는 값 ──────────────────────────────
    // DashAnimParam("Dash"), PatternInterval(2.0), TeleportPreDelay(0.5),
    // TeleportPostDelay(0.8) 은 모두 기본값과 같아 재정의하지 않는다.

    protected override void PlayTeleportSfx()
    {
        SfxManger.instance.SfxPlay("Fire_Attack", clip[0]);
    }

    protected override System.Func<IEnumerator>[] BuildPatterns()
    {
        return new System.Func<IEnumerator>[]
        {
            FireBreath, // 0
            Dash,       // 1
            FireMeteor, // 2
            Teleport,   // 3
        };
    }

    protected override void SetStageCleared()
    {
        BoolManager.ThirdStageBossDie = true;
    }

    protected override void Start()
    {
        base.Start();
        StartCoroutine(PatternLoop());
    }

    protected override void Update()
    {
        base.Update();

        if (isDash == true)
        {
            transform.position = Vector2.MoveTowards(transform.position, DashDir.position, speed * Time.deltaTime);
            anim.SetBool(DashAnimParam, true);
        }

        if (MonsterDie)
        {
            SetStageCleared();
            isDash = false;
        }
    }

    IEnumerator FireBreath()
    {
        LookPlayer();

        anim.SetBool("Breath", true);
        SfxManger.instance.SfxPlay("Fire_Skill_breath", clip[1]);
        yield return new WaitForSeconds(0.3f);
        Instantiate(BreathPrepab, BreathPos.position, BreathPos.rotation);
        yield return new WaitForSeconds(2.0f);
        anim.SetBool("Breath", false);
    }

    IEnumerator FireMeteor()
    {
        anim.SetBool("Meteor", true);
        SfxManger.instance.SfxPlay("Fire_Skill_Meteor", clip[2]);
        Instantiate(MeteorPrepab, MeteorPos.position, MeteorPos.rotation);
        yield return new WaitForSeconds(2.0f);
        anim.SetBool("Meteor", false);
    }
}
