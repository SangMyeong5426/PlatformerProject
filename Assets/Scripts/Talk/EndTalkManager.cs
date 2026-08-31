using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 엔딩 대화. 채널 2개와 페이드아웃.
public class EndTalkManager : TalkManagerBase
{
    public GameObject TalkPannel, TalkPannel2;
    public Image Portrait, Portrait2;
    public Text talk, talk2;

    // TalkTrigger 가 읽는 진행도. 채널이 람다로 읽고 쓴다.
    public static int DataNum, DataNum2;

    public CanvasGroup Endingpannel;
    public float fadeCount, count;
    public GameObject endingpannel;

    protected override Channel[] BuildChannels()
    {
        return new Channel[]
        {
            new Channel
            {
                Panel = TalkPannel, Portrait = Portrait, Label = talk, End = 3,
                Read = () => DataNum, Write = v => DataNum = v,
                PortraitFor = n => (n == 1 || n == 2) ? CharSprite : God,
            },
            new Channel
            {
                Panel = TalkPannel2, Portrait = Portrait2, Label = talk2, End = 4,
                Read = () => DataNum2, Write = v => DataNum2 = v,
                // 다른 채널과 반대다. 2·4 에서 신, 그 외에 캐릭터.
                PortraitFor = n => (n == 2 || n == 4) ? God : CharSprite,
                OnClose = () => { BoolManager.Ending = true; },
            },
        };
    }

    private void Start()
    {
        fadeCount = 0f;
    }

    protected override void Update()
    {
        base.Update();

        if (BoolManager.Ending == true)
        {
            endingpannel.SetActive(true);
            StartCoroutine(Ending());
        }

        if (count >= 150)
        {
            LoadingSceneController.LoadScene("UI_Main");
        }
    }

    IEnumerator Ending()
    {
        while (fadeCount < 1.0f)
        {
            fadeCount += 0.0001f;
            yield return new WaitForSeconds(0.1f);
            Endingpannel.alpha = fadeCount;
        }

        count += 0.1f;
    }

    protected override void GenerateData()
    {
        channels[0].Ko.Add(1, "앞에 신이 있다. 대화를 걸어보자");
        channels[0].Ko.Add(2, "");

        channels[1].Ko.Add(1, "알겠어요, 크레아토르를 구해주셔서 감사합니다 용사여  당신의 소원은 뭐죠?");
        channels[1].Ko.Add(2, "내 소원은…!");
        channels[1].Ko.Add(3, "");
    
    }

    protected override void GenerateDataENG()
    {
        channels[0].En.Add(1, "There's the goddess, I'll try to talk to her.");
        channels[0].En.Add(2, "");

        channels[1].En.Add(1, "Alright, you've done well. What is your wish?");
        channels[1].En.Add(2, "My wish is...");
        channels[1].En.Add(3, "");
    
    }
}
