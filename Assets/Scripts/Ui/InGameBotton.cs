using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameBotton : MonoBehaviour
{
    private void Start()
    {　　/*查找按钮组件并添加事件(点击事件)*/
        this.GetComponent<Button>().onClick.AddListener(OnClick);
    }

    /*点击时触发*/
    private void OnClick()
    {
        /*将状态设置false才能退出游戏*/
        UiManager.needtoconnect = true;
        CursorTest.ToHideCursor();
    } 


}