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
    {����/*���Ұ�ť���������¼�(����¼�)*/
        this.GetComponent<Button>().onClick.AddListener(OnClick);
    }

    /*���ʱ����*/
    private void OnClick()
    {
        SceneManager.LoadScene(0);
        GameBootstrap.serverworld.Dispose();
        GameBootstrap.clientworld.Dispose();
        var t = new GameBootstrap();
        t.Initialize("defaultworld");

    }


}