using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 담당 스테이지의 보스를 잡으면 포탈을 연다.
// 스테이지마다 따로 있던 GroundBossPortal / IceBossPortal / FireBossPortal /
// WindBossPortal 네 클래스를 stage 값 하나로 대체한다.
public class BossClearPortal : MonoBehaviour
{
    public StageId stage;
    public GameObject Portal;

    void Update()
    {
        if (!BoolManager.IsBossCleared(stage)) return;

        Portal.SetActive(true);
        // 한 번 열린 포탈이 다시 닫히는 경로는 없다. 매 프레임 SetActive 를
        // 반복 호출하던 기존 동작을 여기서 멈춘다.
        enabled = false;
    }
}
