using UnityEngine;
using UnityEngine.SceneManagement;

public class Boss_Spawn : MonoBehaviour
{
    // 프리팹 스테이지 배치 후 프리팹 언팩 해줘야함. (실수로 프리팹 수치 변경하면 다 변경되서)
    GameObject Spawn_Position1;


    public GameObject[] Boss_prefabs; // 몬스터 프리팹 0 = 근거리, 1 = 원거리, 2 = 패트롤

    // 난이도 별 몬스터 체력 0 = 이지, 1 = 하드
    public int[] Stage_1;
    public int[] Stage_2;
    public int[] Stage_3;
    public int[] Stage_4;

    // 이 배열의 순서가 곧 Boss_prefabs 의 인덱스이자 체력 표의 순서다.
    // 0 = 대지, 1 = 얼음, 2 = 불, 3 = 바람.
    static readonly string[] BossScenes =
        { "1_StageBoss", "2_StageBoss", "3_StageBoss", "4_StageBoss" };

    const int EasyIndex = 0;
    const int HardIndex = 1;

    Mode_Select Mode;

    void Awake()
    {
        Mode = FindObjectOfType<Mode_Select>();

        // 소환 할 위치에 오브젝트 만들고 소환 시킬 몬스터 태그 지정
        Spawn_Position1 = GameObject.FindGameObjectWithTag("Boss_Spawn");

        if (Mode.Easy == true) // 이지난이도
        {
            SpawnBoss(EasyIndex);
        }
        if (Mode.Hard == true) // 하드난이도
        {
            SpawnBoss(HardIndex);
        }

    }

    void SpawnBoss(int difficulty)
    {
        int stage = System.Array.IndexOf(BossScenes, SceneManager.GetActiveScene().name);
        if (stage < 0) return; // 보스 씬이 아니면 아무것도 하지 않는다

        int[][] hpByStage = { Stage_1, Stage_2, Stage_3, Stage_4 };

        // 체력을 **소환보다 먼저** 읽는다. 표가 비어 있으면 여기서 걸리는데, 그래야
        // 예전처럼 보스가 만들어지기 전에 실패한다. 순서를 바꾸면 반쯤 만들어진 보스가
        // 남는다.
        int hp = hpByStage[stage][difficulty];

        // **프리팹이 아니라 인스턴스에 쓴다.** Boss_prefabs[stage] 는 프리팹 에셋을
        // 가리키는 참조라, 여기에 쓰면 원본이 바뀌고 플레이를 끝내도 되돌아오지 않는다.
        //
        // 이 순서는 지켜야 한다. 체력을 읽는 쪽은 Monster_Stats.Start 와 Boss_HpBar.Start
        // 인데 둘 다 Start 이고 이 메서드는 Boss_Spawn.Awake 에서 불린다. Unity 는 그
        // 프레임의 Awake 를 전부 끝낸 뒤 Start 를 돌리므로, Instantiate 직후에 써도
        // 읽는 쪽보다 항상 먼저다.
        //
        // 보스 4종은 전부 Basic_Boss 를 거쳐 Monster_Stats 를 상속한다. 프리팹마다 그
        // 계열 컴포넌트가 하나뿐이라, 구체 타입 4개를 하나로 받아도 같은 것을 잡는다.
        GameObject boss = Instantiate(Boss_prefabs[stage],
                                      Spawn_Position1.transform.position,
                                      Spawn_Position1.transform.rotation);
        boss.GetComponent<Monster_Stats>().Monster_hpMax = hp;
    }
}
