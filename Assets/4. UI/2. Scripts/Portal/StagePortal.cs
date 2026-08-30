using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StagePortal : MonoBehaviour
{
    public GameObject Portal;
    // 남은 몬스터가 없어지면 포탈을 연다. 태그 탐색이라 매 프레임 도는 비용이
    // 있어, 한 번 열린 뒤에는 컴포넌트를 꺼서 더 찾지 않는다.
    // 기존에도 열린 포탈이 다시 닫히는 경로는 없었다.
    void Update()
    {
        if (GameObject.FindWithTag("Monster") != null) return;

        Portal.gameObject.SetActive(true);
        enabled = false;
    }
}
