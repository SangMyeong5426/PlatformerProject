using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

// 대화 매니저 3종(TalkManager / EndTalkManager / BonginTalkManager)의 공통 골격.
// 셋은 다루는 대화 채널 수만 다르고 하는 일이 같았다.
//
// 채널 하나 = 대화 패널 하나. 패널·초상화·텍스트·한영 대사 두 벌·진행도·종료 임계값을 묶는다.
// 진행도(DataNum)는 static 이고 TalkTrigger 가 읽으므로 각 매니저에 그대로 남긴다.
// 채널은 그 값을 람다로 읽고 쓴다.
public abstract class TalkManagerBase : MonoBehaviour
{
    public Sprite God, Sword, Spear, Shield;
    public GameObject PL;
    public int CharCodecopy;

    protected Sprite CharSprite;
    AllUnits.Unit unit;

    protected class Channel
    {
        public GameObject Panel;
        public Image Portrait;
        public Text Label;

        public readonly Dictionary<int, string> Ko = new Dictionary<int, string>();
        public readonly Dictionary<int, string> En = new Dictionary<int, string>();

        public int End;                 // 진행도가 이 값이 되면 패널을 닫는다
        public Func<int> Read;          // 진행도 읽기
        public Action<int> Write;       // 진행도 쓰기

        // 진행도에 따라 어떤 초상화를 쓸지. null 을 돌려주면 그대로 둔다.
        public Func<int, Sprite> PortraitFor;

        public Action OnClose;          // 패널을 닫은 뒤 추가로 할 일
    }

    protected Channel[] channels;

    protected abstract Channel[] BuildChannels();
    protected abstract void GenerateData();
    protected abstract void GenerateDataENG();

    protected virtual void Awake()
    {
        channels = BuildChannels();
        foreach (Channel c in channels)
        {
            c.Write(1);
        }
        GenerateData();
        GenerateDataENG();
    }

    protected virtual void Update()
    {
        PL = GameObject.FindWithTag("Player");
        unit = PL.GetComponent<AllUnits.Unit>();

        if (unit.CharCode == 0)
        {
            CharSprite = Sword;
        }
        if (unit.CharCode == 1)
        {
            CharSprite = Spear;
        }
        if (unit.CharCode == 2)
        {
            CharSprite = Shield;
        }

        // 세 판정을 채널별로 묶지 않고 종류별로 돈다. 기존 코드가
        // "스페이스 전부 -> 종료 전부 -> 초상화 전부" 순서였는데, 채널별로 묶으면
        // 한 채널의 진행도 변화가 다음 채널 판정보다 앞서게 되어 순서가 달라진다.
        foreach (Channel c in channels)
        {
            if (Input.GetKeyDown(KeyCode.Space) && c.Panel.activeSelf == true)
            {
                StartCoroutine(Advance(c));
            }
        }

        foreach (Channel c in channels)
        {
            if (c.Read() == c.End)
            {
                Close(c);
            }
        }

        foreach (Channel c in channels)
        {
            Sprite sprite = c.PortraitFor(c.Read());
            if (sprite != null)
            {
                c.Portrait.sprite = sprite;
            }
        }
    }

    // 대사 한 줄을 넘긴다. 로케일 0 이 영어, 1 이 한국어다(GameManager.LangENG/LangKOR).
    IEnumerator Advance(Channel c)
    {
        if (LocalizationSettings.SelectedLocale == LocalizationSettings.AvailableLocales.Locales[0])
        {
            c.Label.text = c.En[c.Read()];
            c.Write(c.Read() + 1);
            yield return new WaitForSeconds(0.5f);
        }
        else if (LocalizationSettings.SelectedLocale == LocalizationSettings.AvailableLocales.Locales[1])
        {
            c.Label.text = c.Ko[c.Read()];
            c.Write(c.Read() + 1);
            yield return new WaitForSeconds(0.5f);
        }
    }

    void Close(Channel c)
    {
        Time.timeScale = 1f;
        c.Panel.SetActive(false);
        c.Write(c.Read() + 1);
        if (c.OnClose != null)
        {
            c.OnClose();
        }
    }
}
