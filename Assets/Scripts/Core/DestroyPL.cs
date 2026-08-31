using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 플레이어용. 파괴 대상 씬 목록만 다르고 나머지 동작은 Destroy 와 같다.
// EndingScene 이 빠져 있어 엔딩에서도 플레이어가 유지된다.
public class DestroyPL : Destroy
{
    static readonly string[] Scenes = { "UI_Main", "UI_Select" };
    protected override string[] DestroyScenes => Scenes;
}
