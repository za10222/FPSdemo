using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExitGameBotton : MonoBehaviour
{
    private void Start()
    {����/*���Ұ�ť���������¼�(����¼�)*/
        this.GetComponent<Button>().onClick.AddListener(OnClick);
    }

    /*���ʱ����*/
    private void OnClick()
    {
        /*��״̬����false�����˳���Ϸ*/
        // UnityEditor.EditorApplication.isPlaying = false;
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }


}