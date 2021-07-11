using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using FPSdemo;
using UnityEngine;
using System.Collections.Generic;
using static FPSdemo.GunManager;

public struct Point :IComponentData{
    
    [GhostField]
    public int point;
}

[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
public class UiSystem : SystemBase
{
    Dictionary<int, string> myDictionary ;

    protected override void OnCreate()
    {
        myDictionary = new Dictionary<int, string>();
        TextAsset txt = Resources.Load("gunname") as TextAsset;
        // ������ı�������

        // �Ի��з���Ϊ�ָ�㣬�����ı��ָ���������ַ����������������ʽ������ÿ���ַ���������
        string[] str = txt.text.Split('\n');
        // �����ı��е��ַ������


        // ��ÿ���ַ����������Զ�����Ϊ�ָ�㣬����ÿ���ո�ָ����ַ������ݱ������
        foreach (string strs in str)
        {
            string[] ss = strs.Split(' ');
            int intA = 0;
            int.TryParse(ss[0], out intA);
            myDictionary.Add(intA, ss[1]);
            Debug.Log(ss[1]);
        }

    }

    protected override void OnUpdate()
    {
        // ��test01 �е����ݼ��ؽ�txt�ı���
          
        if(HasSingleton<EnemyBoss>())
        {
            var boss = GetSingletonEntity<EnemyBoss>();
            var bosshp=GetComponent<HealthData>(boss);
            UiManager.bossHp = bosshp.currentHp/bosshp.maxHp;
          
        }
        else
        {
            UiManager.bossHp = -1;
        }

        EntityQuery query
            = GetEntityQuery(typeof(CharacterControllerInternalData),
                            typeof (PredictedGhostComponent), typeof(HealthData),typeof(Point));

        if(query.CalculateEntityCount()==1)
        {
            var e=query.GetSingletonEntity();
            var childs = GetBuffer<Child>(e);
            Entity find = Entity.Null;
            for (int i = 0; i < childs.Length; i++)
            {
                find = childs[i].Value;
                if (HasComponent<PlayerGunData>(find))
                    break;
            }
            if (find == Entity.Null)
            {
                Debug.Log("headλ�ö�ȡ����");
                return;
            }
            var playerGunData = GetComponent<PlayerGunData>(find);
            UiManager.guntype=myDictionary[playerGunData.gunTypeIndex];

            var positions = query.ToComponentDataArray<HealthData>(Allocator.Temp);
            var points = query.ToComponentDataArray<Point>(Allocator.Temp);
            UiManager.playerHp = positions[0].currentHp / positions[0].maxHp;
            UiManager.point= points[0].point;
        }
        else
        {
            
        }
        
    }
}
