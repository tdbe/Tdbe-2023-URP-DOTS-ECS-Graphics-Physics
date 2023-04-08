using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;

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
            state.Dependency.Complete();// make sure previous jobs on this, have finished.
            NativeList<TriggerEvent> warpTriggerEvents = new NativeList<TriggerEvent>(Allocator.TempJob);
            var jhandle1 = new PositionalWarpingTriggerCollectorJob
            {
                warpTriggerEvents = warpTriggerEvents
            };
            state.Dependency = jhandle1.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
            // X_X singlethreaded physics lookups, so:
            // NOTE: to multithread the calculations, I'm using NativeList<TriggerEvent> ^ and multithreading below
            state.Dependency.Complete();

            // EndSimulationEntityCommandBufferSystem
            var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            m_boundsTCL.Update(ref state);
            m_warpableTCL.Update(ref state);
            m_ltransTCL.Update(ref state);
            var jhandle2 = new PositionalWarpingJob
            {
                ecbp = ecb.AsParallelWriter(),
                time = SystemAPI.Time.ElapsedTime,
                boundsTagComponent = m_boundsTCL,
                warpableTagComponent = m_warpableTCL,
                localTransformComponent = m_ltransTCL,
                warpTriggerEvents = warpTriggerEvents
            };
            state.Dependency = jhandle2.Schedule(warpTriggerEvents.Length, 1, state.Dependency);
        }

    }

    [BurstCompile]
    public partial struct PositionalWarpingTriggerCollectorJob:ITriggerEventsJob
    {
        public NativeList<TriggerEvent> warpTriggerEvents;

        [BurstCompile]
        // TODO: might want to actually get collision position
        public void Execute(TriggerEvent triggerEvent)
        {
            warpTriggerEvents.Add(triggerEvent);
        }
    }

    [BurstCompile]
    public partial struct PositionalWarpingJob:IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ecbp;
        [ReadOnly]
        public double time;
        [ReadOnly]
        public ComponentLookup<BoundsTagComponent> boundsTagComponent;
        [ReadOnly]
        public ComponentLookup<WarpableTag> warpableTagComponent;
        [ReadOnly]
        public ComponentLookup<LocalTransform> localTransformComponent;
        // note: PhysicsVelocity or LimitDOFJoint

        [ReadOnly]
        public NativeList<TriggerEvent> warpTriggerEvents;

        [BurstCompile]
        // TODO: might want to actually get collision position
        public void Execute(int parfi)
        {
            var triggerEvent = warpTriggerEvents[parfi];
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
                ecbp.SetComponent<WarpableTag>(parfi, warpableEnt, new WarpableTag{warpImmunityPeriod = warpableTag.warpImmunityPeriod, lastWarpTime = time});
                
                Entity boundsEnt = isBoundsA? entA : entB;    
                
                // TODO: maybe do something with ColliderCastHit or some other warp rules

                LocalTransform newTransform;
                localTransformComponent.TryGetComponent(warpableEnt, out newTransform);
                
                float3 dirToCenter = math.normalize(newTransform.Position);
                newTransform.Position = -newTransform.Position;
                newTransform.Position += dirToCenter*1.2f;// obviously this is also a hack, and doesn't even check the warpable's collider radius..
                ecbp.SetComponent<LocalTransform>(parfi, warpableEnt, newTransform);
            }
        }
    }

}
