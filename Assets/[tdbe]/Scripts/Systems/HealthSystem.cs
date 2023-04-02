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

            // TODO: if I need custom systems/jobs, use writegroups for health stuff

            state.Dependency = new CheckHealthJob
            {
                currentTime = Time.timeAsDouble,
                ecbp = ecb.AsParallelWriter(),
            }.ScheduleParallel(m_healthEQG_notded, state.Dependency);
            
            // TODO: this one needs to be optimized and synced better
            state.Dependency.Complete();
            state.Dependency = new CheckShieldsJob
            {
                currentTime = Time.timeAsDouble,
                ecbp = ecb.AsParallelWriter(),
            }.ScheduleParallel(m_shieldsEQG, state.Dependency);

            // destroy everything in one place :)
            var ecbSingletoEnd = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecbEnd = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            state.Dependency = new DeadDestroyJob
            {
                ecbp = ecbEnd.AsParallelWriter(),
            }.ScheduleParallel(m_DeadDestroyEQG, state.Dependency);
            state.Dependency.Complete();
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
            // TODO: maybe ISharedComponent based on equippedShieldComp.active?
            if( equippedShieldComp.active &&
                equippedShieldComp.pickupTime + equippedShieldComp.pickupTimeToLive
                < currentTime 
            ){
                ecbp.SetComponent<EquippedShieldDataComponent>(ciqi, ent, new EquippedShieldDataComponent{
                    active = false
                });

                if(equippedShieldComp.spawnedVisual != Entity.Null)
                    ecbp.AddComponent<DeadDestroyTag>(ciqi, equippedShieldComp.spawnedVisual);
            }
            
        }
    }

    [BurstCompile]
    public partial struct DeadDestroyJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecbp;

        public void Execute([ChunkIndexInQuery] int ciqi, 
                            in DeadDestroyTag dedtag, 
                            in Entity ent)
        {
            ecbp.DestroyEntity(ciqi, ent);
            // TODO: get all children and destroy -- pickups don't get destroyed.
        }
    }

}
