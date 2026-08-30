using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Destroy : MonoBehaviour
{
    // 이 씬들에 들어오면 스스로를 파괴하고, 그 밖의 씬에서는 전환에도 살아남는다.
    // 하위 클래스가 목록만 바꿔 쓴다.
    static readonly string[] Scenes = { "UI_Main", "EndingScene", "UI_Select" };
    protected virtual string[] DestroyScenes => Scenes;

    // 구독/해제를 OnEnable/OnDisable 이 아니라 Awake/OnDestroy 에 둔다.
    // 비활성 상태에서 씬이 바뀌면 OnEnable 방식은 전환을 놓친다.
    void Awake()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    // 생성 시점의 씬도 한 번 판정해야 한다. activeSceneChanged 는
    // 이미 열려 있던 씬에 대해서는 발생하지 않는다.
    void Start()
    {
        Apply(SceneManager.GetActiveScene());
    }

    void OnActiveSceneChanged(Scene from, Scene to)
    {
        Apply(to);
    }

    void Apply(Scene scene)
    {
        if (System.Array.IndexOf(DestroyScenes, scene.name) >= 0)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
