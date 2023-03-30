using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

namespace GameWorld
{
    // Affects anything that moves and collides with the BoundsTagComponent triggers
    //[UpdateAfter(typeof(Unity.Physics.Systems.PhysicsSimulationGroup))]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    [BurstCompile]
    public partial struct PositionalWarpingSystem : ISystem
    {
        ComponentLookup<BoundsTagComponent> m_boundsTCL;
        ComponentLookup<WarpableTag> m_warpableTCL;
        ComponentLookup<LocalTransform> m_ltransTCL;
        public void OnCreate(ref SystemState state)
        {
            m_boundsTCL = state.GetComponentLookup<BoundsTagComponent>(true);
            m_warpableTCL = state.GetComponentLookup<WarpableTag>(false);
            m_ltransTCL = state.GetComponentLookup<LocalTransform>(false);

            state.RequireForUpdate<WarpableTag>();
            state.RequireForUpdate<BoundsTagComponent>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // EndSimulationEntityCommandBufferSystem
            var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            m_boundsTCL.Update(ref state);
            m_warpableTCL.Update(ref state);
            m_ltransTCL.Update(ref state);
            var jhandle = new PositionalWarpingJob
            {
                ecb = ecb,
                time = SystemAPI.Time.ElapsedTime,
                boundsTagComponent = m_boundsTCL,
                warpableTagComponent = m_warpableTCL,
                localTransformComponent = m_ltransTCL
            };
            state.Dependency = jhandle.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
            // X_X singlethreaded physics lookups
        }

    }

    [BurstCompile]
    public partial struct PositionalWarpingJob:ITriggerEventsJob
    {
        public EntityCommandBuffer ecb;
        [ReadOnly]
        public double time;
        [ReadOnly]
        public ComponentLookup<BoundsTagComponent> boundsTagComponent;
        public ComponentLookup<WarpableTag> warpableTagComponent;
        public ComponentLookup<LocalTransform> localTransformComponent;
        // note: PhysicsVelocity or LimitDOFJoint

        [BurstCompile]
        // TODO: Here I have to collect a DynamicBuffer of trigger events here. Because a lot of things can hit the bounds at the same time.
        // And avoid multiple collisions between the same object and wall, with state awareness.
        // But also I don't have time right now so let's say this works precise enough. Using time isntead of state.
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity entA = triggerEvent.EntityA;
            Entity entB = triggerEvent.EntityB;

            bool isBoundsA = boundsTagComponent.HasComponent(entA);
            bool isBoundsB = boundsTagComponent.HasComponent(entB);

            if(!isBoundsA && !isBoundsB){
                // only run on bounds vs moving things
                // also, bounds don't have a physicsbody
                return;
            }
            
            bool isWarpableA = warpableTagComponent.HasComponent(entA);
            bool isWarpableB = warpableTagComponent.HasComponent(entB);
            Entity warpableEnt = isWarpableB? entB : entA;
            WarpableTag warpableTag;
            warpableTagComponent.TryGetComponent(warpableEnt, out warpableTag);
            if(time - warpableTag.lastWarpTime > warpableTag.warpImmunityPeriod)
            {
                ecb.SetComponent<WarpableTag>(warpableEnt, new WarpableTag{warpImmunityPeriod = warpableTag.warpImmunityPeriod, lastWarpTime = time});
                
                Entity boundsEnt = isBoundsA? entA : entB;    
                
                // TODO: maybe do something with ColliderCastHit or some other warp rules

                LocalTransform newTransform;
                localTransformComponent.TryGetComponent(warpableEnt, out newTransform);
                
                float3 dirToCenter = math.normalize(newTransform.Position);
                newTransform.Position = -newTransform.Position;
                newTransform.Position += dirToCenter*1.2f;// obviously this is also a hack, and doesn't even check the warpable's collider radius..
                ecb.SetComponent<LocalTransform>(warpableEnt, newTransform);
            }
        }
    }

}
