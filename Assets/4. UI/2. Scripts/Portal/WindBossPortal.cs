using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindBossPortal : MonoBehaviour
{
    public GameObject WindBossPor;

    // Update is called once per frame
    void Update()
    {
        if (BoolManager.IsBossCleared(StageId.Fourth))
        {
            WindBossPor.SetActive(true);
        }
    }
}
