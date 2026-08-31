using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextScene : MonoBehaviour
{
    public string NextMapName;
    public CurMapName Player;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            // 씬을 넘나드는 플레이어라 트리거 시점에 찾는다.
            // 이전에는 쓰지도 않는 참조를 매 프레임 갱신하고 있었다.
            Player = FindObjectOfType<CurMapName>();
            EnemyCountManager.instance.KillMonsterCount = 0;

            Player.CurMapname = NextMapName;
            LoadingSceneController.LoadScene(NextMapName);
        }
    }
}
