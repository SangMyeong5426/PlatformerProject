using UnityEngine;

public class Monster_Spawn : MonoBehaviour
{
    // 프리팹 스테이지 배치 후 프리팹 언팩 해줘야함. (실수로 프리팹 수치 변경하면 다 변경되서)
    GameObject[] Spawn_Position1;
    GameObject[] Spawn_Position2;
    GameObject[] Spawn_Position3;
    public GameObject[] Monster_prefabs; // 몬스터 프리팹 0 = 근거리, 1 = 원거리, 2 = 패트롤

    // 난이도 별 몬스터 체력 0 = 이지, 1 = 하드
    public int[] Normal_Hp;
    public int[] far_Hp;
    public int[] repeat_Hp;

    const int EasyIndex = 0;
    const int HardIndex = 1;

    Mode_Select Mode;

    public Sprite[] sprite;
    void Awake()
    {
        Mode = FindObjectOfType<Mode_Select>();

        // 소환 할 위치에 오브젝트 만들고 소환 시킬 몬스터 태그 지정
        Spawn_Position1 = GameObject.FindGameObjectsWithTag("NormalMonster_Spawn");
        Spawn_Position2 = GameObject.FindGameObjectsWithTag("farMonster_Spawn");
        Spawn_Position3 = GameObject.FindGameObjectsWithTag("repeatMonster_Spawn");

    }

    void Start()
    {

        if (Mode.Easy == true) // 이지난이도
        {
            SpawnAll(EasyIndex);
        }
        if (Mode.Hard == true) // 하드난이도
        {
            SpawnAll(HardIndex);
        }
    }

    void SpawnAll(int difficulty)
    {
        SpawnAt(Spawn_Position1, 0, Normal_Hp, difficulty);
        SpawnAt(Spawn_Position2, 1, far_Hp, difficulty);
        SpawnAt(Spawn_Position3, 2, repeat_Hp, difficulty);
    }

    void SpawnAt(GameObject[] points, int prefabIndex, int[] hpByDifficulty, int difficulty)
    {
        foreach (GameObject spawn in points)
        {
            // 체력을 루프 **안에서** 읽는 것은 원래 동작 그대로다. 소환 지점이 하나도
            // 없으면 표를 읽지 않으므로, 표가 비어 있어도 예외가 나지 않는다.
            // 루프 밖으로 빼면 그 경우에 없던 예외가 생긴다.
            int hp = hpByDifficulty[difficulty];

            // **프리팹이 아니라 인스턴스에 쓴다.** Monster_prefabs[i] 는 프리팹 에셋을
            // 가리키는 참조라, 여기에 쓰면 원본이 바뀌고 플레이를 끝내도 되돌아오지
            // 않는다. 이쪽은 소환 지점마다 다시 쓰기까지 했다.
            //
            // 이 순서는 지켜야 한다. 체력을 읽는 쪽은 Monster_Stats.Start 이고 이
            // 메서드는 Monster_Spawn.Start 에서 불린다. 이 프레임에 새로 만들어진
            // 오브젝트의 Start 는 그다음에 돌므로, Instantiate 직후에 써도 늦지 않다.
            // 몬스터 3종 중 Awake 에서 체력을 읽는 것은 없다.
            //
            // 몬스터 3종은 전부 Monster_Stats 를 상속한다. 프리팹마다 그 계열 컴포넌트가
            // 하나뿐이라, 구체 타입 3개를 하나로 받아도 같은 것을 잡는다.
            GameObject monster = Instantiate(Monster_prefabs[prefabIndex],
                                             spawn.transform.position, spawn.transform.rotation);
            monster.GetComponent<Monster_Stats>().Monster_hpMax = hp;
        }
    }
}
