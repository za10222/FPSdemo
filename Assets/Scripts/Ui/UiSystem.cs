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
        // 输出该文本的内容

        // 以换行符作为分割点，将该文本分割成若干行字符串，并以数组的形式来保存每行字符串的内容
        string[] str = txt.text.Split('\n');
        // 将该文本中的字符串输出


        // 将每行字符串的内容以逗号作为分割点，并将每个空格分隔的字符串内容遍历输出
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
        // 将test01 中的内容加载进txt文本中
          
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
                Debug.Log("head位置读取错误");
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
