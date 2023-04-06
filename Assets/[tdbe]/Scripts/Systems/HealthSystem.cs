using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;


using GameWorld.Pickups;

namespace GameWorld
{

    // checks health of everything not dead, and makes them dead if need be.
    // checks shield slots and updates shield status or disables the shield.
    // goes through all dead tags and queues their actual destruction.

    // so it's technically 3 related systems in one, but I had no need rn to split
    
    //[UpdateAfter(typeof(GameSystem))]
    //[UpdateAfter(typeof(GameWorld.Asteroid.AsteroidTargetedSpawnerSystem))]
    //[UpdateAfter(typeof(GameWorld.Players.PlayerProjectileSystem))]
    //
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [BurstCompile]
    public partial struct  HealthSystem : ISystem
    {
        private EntityQuery m_healthEQG_notded;        
        private EntityQuery m_shieldsEQG;        
        private EntityQuery m_DeadDestroyEQG;        

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // TODO: HOW TO: Require for update This -OR- This?
            //state.RequireForUpdate<HealthComponent>();
            
            m_healthEQG_notded = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<HealthComponent>()
                .WithNone<DeadDestroyTag>()
                );
            m_shieldsEQG = state.GetEntityQuery(ComponentType.ReadOnly<EquippedShieldDataComponent>());
        
            m_DeadDestroyEQG = state.GetEntityQuery(ComponentType.ReadOnly<DeadDestroyTag>());
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var ecbSingletonEnd = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecbEnd = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // TODO: if I need varied custom systems/jobs, use writegroups for health stuff

            state.Dependency = new CheckHealthJob
            {
                currentTime = Time.timeAsDouble,
                ecbp = ecb.AsParallelWriter(),
            }.ScheduleParallel(m_healthEQG_notded, state.Dependency);
            state.Dependency.Complete();
            
            // this one will only run on entities with equipped shield slot components which are not disabled.
            state.Dependency = new CheckShieldsJob
            {
                currentTime = Time.timeAsDouble,
                ecbp = ecb.AsParallelWriter(),
            }.ScheduleParallel(m_shieldsEQG, state.Dependency);
            state.Dependency.Complete();

            // destroy everything in one place :)
            state.Dependency = new DeadDestroyJob
            {
                ecbp = ecbEnd.AsParallelWriter(),
            }.ScheduleParallel(m_DeadDestroyEQG, state.Dependency);
            state.Dependency.Complete();
            // NOTE: ^ we don't -have to- complete these jobs this update stage frame, 
            // if it's not enough time, we can store the job handle and force completion
            // next time we need another one created.
        }
    }

    [BurstCompile]
    public partial struct CheckHealthJob : IJobEntity
    {
        [ReadOnly]
        public double currentTime;
        public EntityCommandBuffer.ParallelWriter ecbp;

        public void Execute([ChunkIndexInQuery] int ciqi, 
                            in HealthComponent healthComp, 
                            in Entity ent)
        {
            if( healthComp.timeToLive >-1 && healthComp.spawnTime + healthComp.timeToLive < currentTime )
            {
                ecbp.AddComponent<DeadDestroyTag>(ciqi, ent);
            }
            else if(healthComp.currentHealth <= 0.0f)
            {
                ecbp.AddComponent<DeadDestroyTag>(ciqi, ent);
            }
        }
    }

    [BurstCompile]
    public partial struct CheckShieldsJob : IJobEntity
    {
        [ReadOnly]
        public double currentTime;
        public EntityCommandBuffer.ParallelWriter ecbp;

        public void Execute([ChunkIndexInQuery] int ciqi, 
                            in EquippedShieldDataComponent equippedShieldComp, 
                            in Entity ent)
        {
            // ISharedComponent would be unnecessarily expensive
            if( equippedShieldComp.active &&
                equippedShieldComp.pickupTime + equippedShieldComp.pickupTimeToLive
                < currentTime 
            ){
                ecbp.SetComponent<EquippedShieldDataComponent>(ciqi, ent, new EquippedShieldDataComponent{
                    active = false
                });
                // disable this component so it won't waste thread resources
                ecbp.SetComponentEnabled<EquippedShieldDataComponent>(ciqi, ent, false);

                // destroy shield visual on owner if one exists
                if(equippedShieldComp.activeVisual != Entity.Null)
                    ecbp.AddComponent<DeadDestroyTag>(ciqi, equippedShieldComp.activeVisual);
            }
            
        }
    }

    [BurstCompile]
    public partial struct DeadDestroyJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecbp;

        public void Execute([ChunkIndexInQuery] int ciqi, 
                            in DeadDestroyTag dedtag, 
                            in Entity ent,
                            in DynamicBuffer<Child> children)
        {
            foreach(var child in children)
            {
                ecbp.DestroyEntity(ciqi, child.Value);
            }
            ecbp.DestroyEntity(ciqi, ent);
        }
    }

}
