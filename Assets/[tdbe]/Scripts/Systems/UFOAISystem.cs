using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

namespace GameWorld.NPCs
{
    [UpdateAfter(typeof(GameSystem))]
    public partial struct UFOAISystem : ISystem
    {
        private EntityQuery m_boundsGroup;
        private bool m_cameraInitialized;

 
        public void OnCreate(ref SystemState state)
        {

            state.RequireForUpdate<BoundsTagComponent>();

            m_boundsGroup = state.GetEntityQuery(ComponentType.ReadOnly<BoundsTagComponent>());
        }

        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // TODO: range + patrol + chase?

        }
    }

    [BurstCompile]
    public partial struct UFOChaseJob:IJobEntity
    {
        public EntityCommandBuffer ecb;
        private void Execute(in Entity bndEnt, in LocalTransform lt, in BoundsTagComponent tagC)
        {
            /*
            float3 pos = HackyGlobals.WorldBounds._boundsPosAndScaleArrayBottomClockwise[tagC.boundsID].Item1;
            pos.z = lt.Position.z;
            
            ecb.SetComponent<LocalTransform>(
                bndEnt,
                new LocalTransform{
                        Position = HackyGlobals.WorldBounds._boundsPosAndScaleArrayBottomClockwise[tagC.boundsID].Item1,
                        Rotation = quaternion.RotateZ(-1.570796f * tagC.boundsID),
                        Scale = 1
                });
            // Hey folks, did you know there isn't any form of ecs collider that can have its size changed at runtime without replacing it with a new and unique (not shared) collider?
            // sonofabitch! what is this, Unreal? :) https://forum.unity.com/threads/to-change-scale-and-collider-radius-in-ecs-physic.722462/#post-8145500
            // well, screw it I just made the shared collider big enough to work in all situations.
            
            PostTransformScale new_pts = new PostTransformScale{
                    Value = float3x3.Scale( // this became quite annoying, although I get why it needs to be post transform
                        HackyGlobals.WorldBounds._boundsPosAndScaleArrayBottomClockwise[tagC.boundsID].Item2.x,
                        HackyGlobals.WorldBounds._boundsPosAndScaleArrayBottomClockwise[tagC.boundsID].Item2.y,
                        HackyGlobals.WorldBounds._boundsPosAndScaleArrayBottomClockwise[tagC.boundsID].Item2.z
                    )
                };

            ecb.AddComponent<PostTransformScale>(
                bndEnt,
                new_pts
                );*/
            
        }
    }
}
