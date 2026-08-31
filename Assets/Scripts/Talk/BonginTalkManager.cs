using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 봉인 안내 대화. 채널 1개.
public class BonginTalkManager : TalkManagerBase
{
    public GameObject TalkPannel;
    public Image Portrait;
    public Text talk;

    public static int DataNum;

    protected override Channel[] BuildChannels()
    {
        return new Channel[]
        {
            new Channel
            {
                Panel = TalkPannel, Portrait = Portrait, Label = talk, End = 3,
                Read = () => DataNum, Write = v => DataNum = v,
                // 조건 없이 항상 캐릭터 초상화다. 이 매니저는 신 초상화를 쓰지 않는다.
                PortraitFor = n => CharSprite,
            },
        };
    }

    protected override void GenerateData()
    {
        channels[0].Ko.Add(1, "B를 눌러 봉인을 하자!");
        channels[0].Ko.Add(2, "");
    
    }

    protected override void GenerateDataENG()
    {
        channels[0].En.Add(1, "Press B to seal!");
        channels[0].En.Add(2, "");
    
    }
}
