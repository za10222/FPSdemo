using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    public static float playerHp=1;
    public static float bossHp = 0;
    public static string guntype = "xxx";
    public static int cutNum = 0;
    public static int maxNum = 999;
    public static int point = 999;
    public static bool needtoconnect = false;

    public Image playerhealth;

    public Image hit;
    public Image bosshealth;
    public Text guntypetext; 
    public Text pointtext;
    public Text cunumber;
    public GameObject Ingame;
    public GameObject Begin;
    public GameObject Die;
    public Text diepoint;
    // Start is called before the first frame update
    void Start()
    {
               playerHp = 1;
          bossHp = 0;
          guntype = "xxx";
         cutNum = 0;
         maxNum = 999;
          point = 999;
         needtoconnect = false;
}

    // Update is called once per frame
    void Update()
    {
        if(needtoconnect)
        {
            Ingame.SetActive(true);
            Begin.SetActive(false);
            Die.SetActive(false);
            CursorTest.ToHideCursor();
            playerhealth.fillAmount = playerHp ;
            bosshealth.fillAmount = bossHp ;
            guntypetext.text = guntype;
            pointtext.text =string.Format( "分数:{0}",point);
            cunumber.text = string.Format("子弹:无限");
            var t = hit.color;
            t.a =0.2f-0.2f*playerHp;
            hit.color = t; 
            if(playerHp<=0.001f)
            {
                Ingame.SetActive(false);
                Die.SetActive(true);
                diepoint.text = string.Format("你的分数:{0}", point);
                CursorTest.ToShowCursor();
            }
        }
        else
        { 
            Ingame.SetActive(false);
            Begin.SetActive(true);
            Die.SetActive(false);
        }

    }
}
