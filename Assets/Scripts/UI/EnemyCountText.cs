using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class EnemyCountText : MonoBehaviour
{
    public TMP_Text enemycounttext;

    public int curkillcount;

    void Update()
    {
        // 스테이지 씬이 아니어도 킬 수는 갱신한다. 기존 동작 유지.
        curkillcount = EnemyCountManager.instance.KillMonsterCount;

        if (!EnemyCountManager.TryGetStage(SceneManager.GetActiveScene().name, out StageId stage)) return;

        int total = EnemyCountManager.instance.GetStageTotal(stage);

        ILocalesProvider availableLocales = LocalizationSettings.AvailableLocales;
        if (LocalizationSettings.SelectedLocale == availableLocales.GetLocale("en"))
        {
            enemycounttext.text = "REMAINING ENEMIES  " + curkillcount + " /" + total;
        }
        else if (LocalizationSettings.SelectedLocale == availableLocales.GetLocale("ko-KR"))
        {
            enemycounttext.text = "남은 적 : " + curkillcount + " /" + total;
        }
    }
}
