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

        // 보스 4종은 전부 Basic_Boss 를 거쳐 Monster_Stats 를 상속한다. 프리팹마다 그
        // 계열 컴포넌트가 하나뿐이라, 구체 타입 4개를 하나로 받아도 같은 것을 잡는다.
        Boss_prefabs[stage].GetComponent<Monster_Stats>().Monster_hpMax = hpByStage[stage][difficulty];
        Instantiate(Boss_prefabs[stage], Spawn_Position1.transform.position, Spawn_Position1.transform.rotation);
    }
}
