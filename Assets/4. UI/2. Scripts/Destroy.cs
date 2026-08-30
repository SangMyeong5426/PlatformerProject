using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Destroy : MonoBehaviour
{
    // 이 씬들에 들어오면 스스로를 파괴하고, 그 밖의 씬에서는 전환에도 살아남는다.
    // 하위 클래스가 목록만 바꿔 쓴다. 매 프레임 도는 코드라 배열을 새로 만들지 않는다.
    static readonly string[] Scenes = { "UI_Main", "EndingScene", "UI_Select" };
    protected virtual string[] DestroyScenes => Scenes;

    protected virtual void Update()
    {
        if (System.Array.IndexOf(DestroyScenes, SceneManager.GetActiveScene().name) >= 0)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
