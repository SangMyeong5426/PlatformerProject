using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextScene1 : MonoBehaviour
{
    public string NextMapName;
    public string HardMapName;
    public CurMapName Player;

    Mode_Select mode;
    void Start()
    {
        mode = FindObjectOfType<Mode_Select>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            // 씬을 넘나드는 플레이어라 트리거 시점에 찾는다.
            Player = FindObjectOfType<CurMapName>();
            if(mode.Hard == true)
            {
                Player.CurMapname = HardMapName;
                LoadingSceneController.LoadScene(HardMapName);
            }
            else 
            {
                Player.CurMapname = NextMapName;
                LoadingSceneController.LoadScene(NextMapName);
            }
        }
    }
}