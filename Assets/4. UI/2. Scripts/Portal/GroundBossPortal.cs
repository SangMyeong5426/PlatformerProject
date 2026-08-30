using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundBossPortal: MonoBehaviour
{
    public GameObject GroundBossPor;

    // Update is called once per frame
    void Update()
    {
        if (BoolManager.IsBossCleared(StageId.First))
        {
            GroundBossPor.SetActive(true);
        }
    }
}
