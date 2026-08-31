using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class EnemyCountManager : MonoBehaviour
{
    public static EnemyCountManager instance;

    // 스테이지별 총 몬스터 수. 인덱스는 StageId 값과 맞춘다.
    public int[] stageTotal = new int[4];
    public int KillMonsterCount;

    // 스테이지마다 한 번만 세도록 하는 가드
    static readonly bool[] counted = new bool[4];
    static readonly string[] StageScenes = { "1_Stage", "2_Stage", "3_Stage", "4_Stage" };

    // 씬 이름을 StageId 로 바꾼다. 스테이지 씬이 아니면 false.
    public static bool TryGetStage(string sceneName, out StageId stage)
    {
        int i = System.Array.IndexOf(StageScenes, sceneName);
        stage = (StageId)i;
        return i >= 0;
    }

    public static void ResetCounts()
    {
        System.Array.Clear(counted, 0, counted.Length);
    }

    public int GetStageTotal(StageId stage)
    {
        return stageTotal[(int)stage];
    }

    private void Awake()
    {
        //게임매니저를 싱글턴 처리
        if (instance == null) instance = this; //인스턴스가 존재하지 않으면 현재 인스턴스로 
        else Destroy(this);                    //인스턴스가 존재하면 현재 인스턴스를 삭제 
    }

    void Start()
    {
        ResetCounts();
    }

    void Update()
    {
        if (!TryGetStage(SceneManager.GetActiveScene().name, out StageId stage)) return;

        int i = (int)stage;
        if (counted[i]) return;

        stageTotal[i] = GameObject.FindGameObjectsWithTag("Monster").Length;
        counted[i] = true;
    }
}
