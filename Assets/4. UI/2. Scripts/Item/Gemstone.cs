using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Gemstone : MonoBehaviour
{
    public Sprite GroundGem, IceGem, FireGem ,WindGem;
    public Image GroundGemimg, IceGemimg, FireGemimg ,WindGemimg;
    // Update is called once per frame
    void Update()
    {   
        if (BoolManager.IsBossCleared(StageId.First))
        {
            GroundGemimg.sprite = GroundGem;
        }
        /*else
        {
            GroundGemimg.sprite = null;
        }*/
        if (BoolManager.IsBossCleared(StageId.Second))
        {
            IceGemimg.sprite = IceGem;
        }
        if (BoolManager.IsBossCleared(StageId.Third))
        {
            FireGemimg.sprite = FireGem;
        }
        if (BoolManager.IsBossCleared(StageId.Fourth))
        {
            WindGemimg.sprite = WindGem;
        }
    }
}
