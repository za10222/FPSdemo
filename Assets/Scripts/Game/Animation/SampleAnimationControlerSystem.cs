using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace FPSdemo
{


    [Serializable]
    public struct SampleAnimationController : IComponentData
    {
        public float switchgap;
        public float TransitionDuration;


        public float _lastTimeSwitchedAnimation;

        public state currentState;

        public state lastState;

        public enum state
        {
            none = 0,
            idle,
            walk,
            attack,
            dieing,
            shoot,
            bigShoot,
            taunt
        }

        [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
        [UpdateAfter(typeof(EnemyAnimationUpdateSystem))]
        public class SampleAnimationControlerSystem : SystemBase
        {
            protected override void OnUpdate()
            {
                float time = (float)Time.ElapsedTime;

                Entities
                    .ForEach((
                        ref SimpleAnimation simpleAnimation,
                        ref DynamicBuffer<SimpleAnimationClipData> simpleAnimationClipDatas,
                        ref SampleAnimationController animationController) =>
                    {
                        if (time >= animationController._lastTimeSwitchedAnimation + animationController.switchgap)
                        {
                            animationController._lastTimeSwitchedAnimation = time;
                            if (animationController.currentState == animationController.lastState)
                            {
                                return;
                            }
                            animationController.lastState = animationController.currentState;
                            var clipIndex = getClipIndex(in animationController.currentState, simpleAnimationClipDatas);
                            simpleAnimation.TransitionTo(clipIndex, animationController.TransitionDuration, ref simpleAnimationClipDatas, false);
                            simpleAnimation.SetSpeed(1f, clipIndex, ref simpleAnimationClipDatas);

                        }
                    }).Schedule();
            }
            static int getClipIndex(in SampleAnimationController.state currentState, in DynamicBuffer<SimpleAnimationClipData> simpleAnimationClipDatas)
            {
                int find = 0;
                for (int i = 0; i < simpleAnimationClipDatas.Length; ++i)
                {
                    if (simpleAnimationClipDatas[i].state == currentState)
                    {
                        find = i;
                        break;
                    }
                }
                return find;
            }
        }
    }
}
