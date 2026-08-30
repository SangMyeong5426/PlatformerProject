using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 어느 대화 진행도를 볼지 고른다. 값은 씬에 직렬화되므로 순서를 바꾸지 않는다.
public enum TalkChannel
{
    MainFirst = 0,  // TalkManager.DataNum
    MainSecond = 1, // TalkManager.DataNum2
    EndFirst = 2,   // EndTalkManager.DataNum
    EndSecond = 3,  // EndTalkManager.DataNum2
}

// 플레이어가 들어오면 대화창을 열고 게임을 멈춘다.
// NPCTri / NPCTri2 / NPCTri3 / NPCTri4 네 클래스를 채널과 임계값으로 대체한다.
public class TalkTrigger : MonoBehaviour
{
    public GameObject TalkPannel;
    public TalkChannel channel;

    // 진행도가 이 값보다 낮을 때만 열린다. 다 본 대화는 다시 열리지 않는다.
    public int maxDataNum;

    // 보스 봉인이 풀린 뒤에만 열리는 트리거용. 기존 NPCTri3 만 해당한다.
    public bool requireBossSeal;

    B_Test bossSeal;

    void Start()
    {
        if (requireBossSeal) bossSeal = GetComponent<B_Test>();
    }

    int Progress()
    {
        switch (channel)
        {
            case TalkChannel.MainFirst: return TalkManager.DataNum;
            case TalkChannel.MainSecond: return TalkManager.DataNum2;
            case TalkChannel.EndFirst: return EndTalkManager.DataNum;
            default: return EndTalkManager.DataNum2;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 판정 순서는 기존 NPCTri3 와 같다. 봉인 -> 태그 -> 진행도.
        if (requireBossSeal && !bossSeal.Boss_seal) return;
        if (collision.tag != "Player") return;
        if (Progress() >= maxDataNum) return;

        TalkPannel.SetActive(true);
        Time.timeScale = 0f;
    }
}
