using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameBotton : MonoBehaviour
{
    private void Start()
    {����/*���Ұ�ť���������¼�(����¼�)*/
        this.GetComponent<Button>().onClick.AddListener(OnClick);
    }

    /*���ʱ����*/
    private void OnClick()
    {
        /*��״̬����false�����˳���Ϸ*/
        UiManager.needtoconnect = true;    
    } 


}