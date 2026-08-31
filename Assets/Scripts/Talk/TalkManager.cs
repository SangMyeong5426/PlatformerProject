using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 튜토리얼 대화. 채널 3개.
public class TalkManager : TalkManagerBase
{
    public GameObject TalkPannel, TalkPannel2, TalkPannel3;
    public Image Portrait, Portrait2, Portrait3;
    public Text talk, talk2, talk3;

    // TalkTrigger 가 읽는 진행도. 채널이 람다로 읽고 쓴다.
    public static int DataNum, DataNum2, DataNum3;

    protected override Channel[] BuildChannels()
    {
        return new Channel[]
        {
            new Channel
            {
                Panel = TalkPannel, Portrait = Portrait, Label = talk, End = 15,
                Read = () => DataNum, Write = v => DataNum = v,
                PortraitFor = n => (n == 2 || n == 4 || n == 6 || n == 9 || n == 12 || n == 14)
                    ? CharSprite : God,
            },
            new Channel
            {
                Panel = TalkPannel2, Portrait = Portrait2, Label = talk2, End = 4,
                Read = () => DataNum2, Write = v => DataNum2 = v,
                PortraitFor = n => n == 2 ? CharSprite : God,
            },
            new Channel
            {
                Panel = TalkPannel3, Portrait = Portrait3, Label = talk3, End = 3,
                Read = () => DataNum3, Write = v => DataNum3 = v,
                // 기존 코드에 else 가 없다. 조건 밖에서는 초상화를 그대로 둔다.
                PortraitFor = n => (n == 1 || n == 2) ? CharSprite : null,
            },
        };
    }

    private void Start()
    {
        TalkPannel3.SetActive(true);
        Time.timeScale = 0f;

        BoolManager.Ending = false;
    }

    protected override void Update()
    {
        base.Update();

        if (Time.timeScale == 0)
        {
            PL.GetComponent<Player_Move>().enabled = false; // 플레이어 스크립트 비활성화
        }
        else if (Time.timeScale == 1)
        {
            PL.GetComponent<Player_Move>().enabled = true;
        }
    }

    protected override void GenerateData()
    {
        channels[0].Ko.Add(1, "크레아토르? 제가 왜 이런 곳에 오게 된거죠?");
        channels[0].Ko.Add(2, "당신은 과로로 인해 죽게 됐고, 크레아토르로 환생하게 됐어요.");
        channels[0].Ko.Add(3, "그럴리가... 전 분명 야근을...");
        channels[0].Ko.Add(4, "크레아토르에는 지구와는 달리 마왕이 존재하는데 당신은 마왕의 하수인을 무찌르고 마왕을 재봉인하기 위해 선택된 용사예요.");
        channels[0].Ko.Add(5, "아니 잠깐…. 마왕? 그게 무슨 말이에요?");
        channels[0].Ko.Add(6, "봉인되어있던 마왕이 계략을 꾸몄고 봉인의 축인 정령왕이 타락하게 되면서 봉인이 해제되고 있어요.");
        channels[0].Ko.Add(7, "당신의 역할은 마왕의 하수인을 무찌르고 타락한 정령왕을 정화해 본래대로 되돌려놓고, 정령의 파편을 모아 봉인을 고쳐야 해요.");
        channels[0].Ko.Add(8, "꼭 제가 해야 하는 거예요? 제가 무슨 수로 마왕의 하수인을 무찔러요?");
        channels[0].Ko.Add(9, "선택받은 용사만이 할 수 있는 일이라 그래요.");
        channels[0].Ko.Add(10, "만약, 도와주신다면 당신을 지구로 돌려보내 드리고 소원을 들어줄게요.");
        channels[0].Ko.Add(11, "거부하면 어떻게 되죠?");
        channels[0].Ko.Add(12, "정말 안타깝지만, 당신은 순리대로 죽게 되겠죠...");
        channels[0].Ko.Add(13, "...알겠어요. 당신 말대로 할게요");
        channels[0].Ko.Add(14, "");
        
        channels[1].Ko.Add(1, "뭐, 조금은요. 이제.. 뭘 더 하면 되죠?");                                                                                                                                                                 
        channels[1].Ko.Add(2, "이제 옆에 보이는 포탈을 통해 크레아토르로 가서 마왕의 하수인을 물리치고 타락한 정령들을 원래의 모습으로 돌려놓아주세요.");
        channels[1].Ko.Add(3, "");

        channels[2].Ko.Add(1, "앞에 저 여자는 누구지..? 여기가 어딘지 물어봐야겠어");
        channels[2].Ko.Add(2, "");
    
    }

    protected override void GenerateDataENG()
    {
        channels[0].En.Add(1, "kreaitor? What brought me here?");
        channels[0].En.Add(2, "You died from fatigue and have been reincarnated here in kreaitor.");
        channels[0].En.Add(3, "That's impossible... I was just working overtime...");
        channels[0].En.Add(4, "Unlike Earth, there is a demon living in Creator and you have been chosen to defeat the demon's underlings and imprison him.");
        channels[0].En.Add(5, "Wait what? A demon? What are you talking about?");
        channels[0].En.Add(6, "The imprisoned demon tricked the elemental lord, corrupted him, and escaped his imprisonment.");
        channels[0].En.Add(7, "You need to defeat the demon's underlings and cleanse the corrupted elemental lord, gather the broken elemental shards and fix the seal of imprisonment.");
        channels[0].En.Add(8, "Why me? How do you expect me to defeat the demon's underlings?");
        channels[0].En.Add(9, "Only the chosen one can do this.");
        channels[0].En.Add(10, "If you are successful, I shall return you to Earth and grant you one wish.");
        channels[0].En.Add(11, "What happens if I refuse?");
        channels[0].En.Add(12, "Then, you will die as intended.");
        channels[0].En.Add(13, "...Alright. I'll do as you say.");
        channels[0].En.Add(14, "");

        channels[1].En.Add(1, "I guess. What do I do now?");
        channels[1].En.Add(2, "This portal will take you to kreaitor. Defeat the demon's underlings and cleanse the corrupted elemental lord.");
        channels[1].En.Add(3, "");

        channels[2].En.Add(1, "Who is that woman in front of you? I need to ask where I am");
        channels[2].En.Add(2, "");
    
    }
}
