using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoolManager : MonoBehaviour
{   
    // 스테이지별 보스 클리어 여부. 인덱스는 StageId 값과 맞춘다.
    static readonly bool[] bossCleared = new bool[4];

    public static void SetBossCleared(StageId stage) { bossCleared[(int)stage] = true; }
    public static bool IsBossCleared(StageId stage) { return bossCleared[(int)stage]; }
    public static void ResetBossCleared() { System.Array.Clear(bossCleared, 0, bossCleared.Length); }
    public static bool IsTutorial;
    public static bool IsBongin;
    public static bool PlayerDie;
    public static bool BonginCom;
    public static bool Ending;
    public static bool isShake;


    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        ResetBossCleared();
        IsBongin = false;
        PlayerDie = false;
        BonginCom = false;
        Ending = false;

    }
    void Update()
    {

    }
}
