using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class BoolReset : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (SceneManager.GetActiveScene().name == "UI_Main")
        {
            BoolManager.ResetBossCleared();

            BoolManager.BonginCom = false;
            BoolManager.isShake = false;
            BoolManager.Ending = false;

            EnemyCountManager.ResetCounts();

        }
    }
}
