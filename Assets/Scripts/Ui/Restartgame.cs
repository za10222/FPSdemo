using FPSdemo;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Restartgame : MonoBehaviour
{
    private void Start()
    {　　/*查找按钮组件并添加事件(点击事件)*/
        this.GetComponent<Button>().onClick.AddListener(OnClick);
    }

    /*点击时触发*/
    private void OnClick()
    {
        SceneManager.LoadScene(0);
        GameBootstrap.serverworld.Dispose();
        GameBootstrap.clientworld.Dispose();
        var t = new GameBootstrap();
        t.Initialize("defaultworld");

    }


}