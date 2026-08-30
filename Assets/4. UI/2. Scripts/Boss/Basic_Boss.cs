using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Basic_Boss : Monster_Stats
{
    //public float Monster_Hp = 10f;//체력
    //public float Monster_Damage = 1f; // 공격력
    public GameObject DashPos; // 대쉬할때 생기는 콜라이더(보스 전방에만 생김)
    public float speed; //대쉬 속도 변수 
    //public GameObject Child_anim;
    public bool isDash;
    
    // 스킬 공격력 변수
    public int EarthBullet_Damage = 1;

    public int IceBullet_Damage = 1;
    public int IceWave_Damage = 1;

    public int Dash_Damage = 1;
    public int FireBreath_Damage = 1;
    public int FireMeteor_Damage = 1;

    public int WindTornado_Damage = 1;
    public int WindBullet_Damage = 1;

    public Transform Attack_Pos;
    public float Attack_Radius;

    public LayerMask P_Layer;
    //public Animator anim;
    public Transform Target, DashDir;

    public AllUnits.Unit player_Hp;
    Transform Player;

    public bool Boss_Die = false;

    [Header("디버그")]
    [Tooltip("-1이면 랜덤. 0 이상이면 해당 인덱스의 패턴만 반복 실행 (검증용)")]
    [SerializeField] protected int debugForcePattern = -1;

    // ── 보스별로 달라지는 값 ──────────────────────────────
    // 인스펙터가 아닌 코드에서 덮어쓴다. 프리팹 입력이 필요 없어 값 누락 사고가 없다.

    protected virtual string DashAnimParam => "Dash";   // 대지/얼음은 "Run"
    protected virtual float PatternInterval => 2.0f;    // 패턴 사이 경직 시간
    protected virtual float TeleportPreDelay => 0.5f;
    protected virtual float TeleportPostDelay => 0.8f;

    protected virtual void PlayDashSfx() { }            // 효과음 있는 보스만 재정의
    protected virtual void PlayTeleportSfx() { }

    // 각 보스의 패턴 목록. 배열 인덱스가 기존 switch의 case 번호와 같아야 한다.
    protected virtual System.Func<IEnumerator>[] BuildPatterns()
    {
        return new System.Func<IEnumerator>[0];
    }

    // 이 보스가 담당하는 스테이지. 사망 시 클리어 플래그를 기록한다.
    // 스테이지에 속하지 않는 보스(모드 보스)는 null 을 유지한다.
    protected virtual StageId? ClearedStage => null;

    System.Func<IEnumerator>[] patterns;

    // ── 공통 패턴 스케줄러 ────────────────────────────────
    // 기존에는 각 패턴이 끝에서 RandomPattern()을 다시 호출하는 재귀 구조였다.
    // 여기서는 루프가 패턴 종료를 기다리므로, 각 패턴은 자기 일만 하고 끝내면 된다.
    protected IEnumerator PatternLoop()
    {
        patterns = BuildPatterns();

        while (true)
        {
            yield return new WaitForSeconds(PatternInterval);

            if (MonsterDie) yield break;

            int index = debugForcePattern >= 0
                ? Mathf.Min(debugForcePattern, patterns.Length - 1)
                : Random.Range(0, patterns.Length);

            yield return StartCoroutine(patterns[index]());
        }
    }

    protected IEnumerator Dash()
    {
        LookPlayer(); // 플레이어 방향 바라보기
        isDash = true;
        PlayDashSfx();
        DashPos.SetActive(true);
        yield return new WaitForSeconds(1.5f); // 패턴 피할 시간
        // 실제 돌진 이동은 Update()의 isDash 블록이 담당한다.
        // 아래 한 줄은 1프레임분만 움직이지만 기존 동작 유지를 위해 남겨둔다.
        transform.position = Vector2.MoveTowards(transform.position, DashDir.position, speed * Time.deltaTime);
        yield return new WaitForSeconds(2.5f);
        isDash = false;
        anim.SetBool(DashAnimParam, false);
        DashPos.SetActive(false);
    }

    protected IEnumerator Teleport()
    {
        transform.position = Target.transform.position;
        yield return new WaitForSeconds(TeleportPreDelay);
        LookPlayer();
        anim.SetBool("Attack", true);
        PlayTeleportSfx();
        yield return new WaitForSeconds(TeleportPostDelay);
        anim.SetBool("Attack", false);
    }

    protected override void Start()
    {
        base.Start();
        //anim = Child_anim.GetComponent<Animator>();
        Target = GameObject.FindGameObjectWithTag("Player_Hit").GetComponent<Transform>();
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        DashDir = GameObject.FindGameObjectWithTag("Target").GetComponent<Transform>();
        player_Hp = Player.GetComponent<AllUnits.Unit>();
    }

    bool defeated; // OnBossDefeated() 를 한 번만 실행시키는 가드

    // Update is called once per frame
    // 돌진 이동과 사망 처리는 4종이 동일해 여기서 처리한다.
    // 스테이지 클리어 플래그만 ClearedStage 로 갈라진다.
    protected override void Update()
    {
        base.Update();

        if (isDash == true)
        {
            transform.position = Vector2.MoveTowards(transform.position, DashDir.position, speed * Time.deltaTime);
            anim.SetBool(DashAnimParam, true);
        }

        // 사망 처리는 죽는 프레임에 한 번만 돈다. 돌진 블록보다 뒤에 두어야
        // 죽는 프레임의 이동 1회분이 기존과 같이 유지된다.
        if (MonsterDie && !defeated)
        {
            defeated = true;
            OnBossDefeated();
        }
    }

    // 사망 시 1회 실행. 기존에는 이 처리가 매 프레임 돌면서 GetComponent 를
    // 2회씩 반복 호출했다.
    protected virtual void OnBossDefeated()
    {
        isDash = false;
        GetComponent<Rigidbody2D>().simulated = false;
        GetComponent<Collider2D>().enabled = false;
        if (ClearedStage.HasValue) BoolManager.SetBossCleared(ClearedStage.Value);
    }
    public void LookPlayer()
    {
        if (transform.position.x < Player.transform.position.x)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }

    }
    protected virtual void Collider_Atk() // 텔포 공격 플레이어의 체력을 깎음
    {
        Collider2D collider2d = Physics2D.OverlapCircle(Attack_Pos.position, Attack_Radius, P_Layer); 
        // Attack_Pos 오브젝트로 포지션 지정, Attack_Radius 공격 범위를 지정
        // 인스펙터에서 P_Layer에 Player 레이어로 지정

        if (collider2d)
        {
            if (player_Hp != null) // 평타 공격 - 기본 공격 애니메이션 이벤트에 Collider()함수 추가
            {
                Debug.Log("PlayerHP =" + (player_Hp.currentHealth - Monster_Damage));
                player_Hp.TakeDamage(Monster_Damage);
                // 체력 감소
            }
/*            else if (isDash) // 대쉬 공격 - 달리는 애니메이션 이벤트에 맨 앞에 Collider()함수 추가 + 달리는 애니메이션 스피드 0.7로 지정
            {
                Debug.Log("PlayerHP =" + (player_Hp.currentHealth - Dash_Damage));
                player_Hp.TakeDamage(Dash_Damage);
            }*/
        }
    }

    protected virtual void Collider_Dash() // 텔포 공격 플레이어의 체력을 깎음
    {
        Collider2D collider2d = Physics2D.OverlapCircle(Attack_Pos.position, Attack_Radius, P_Layer);
        // Attack_Pos 오브젝트로 포지션 지정, Attack_Radius 공격 범위를 지정
        // 인스펙터에서 P_Layer에 Player 레이어로 지정

        if (collider2d)
        {
            if (isDash) // 대쉬 공격 - 달리는 애니메이션 이벤트에 맨 앞에 Collider()함수 추가 + 달리는 애니메이션 스피드 0.7로 지정
            {
                Debug.Log("PlayerHP =" + (player_Hp.currentHealth - Dash_Damage));
                player_Hp.TakeDamage(Dash_Damage);
            }

        }
    }
    private void OnDrawGizmosSelected()
    {
        // 몬스터 주변에 공격 범위를 나타내는 원 그리기
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Attack_Pos.position, Attack_Radius);
    }
}
