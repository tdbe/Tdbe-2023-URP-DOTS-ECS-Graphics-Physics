using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;


namespace GameWorld.UI
{

    // Every entity that is not dead and has a health component and a health ui component,
    // will have the health ui update constantly.
    // Since we are in a sort of bullethell game, it makes sense to just check every frame. (we don't set unless we need to)
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [BurstCompile]
    public partial struct  HealthUISystem : ISystem
    {
        private EntityQuery m_healthUIEQG_notdead;        

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<HealthComponent>();
            state.RequireForUpdate<HealthUIComponent>();
            
            m_healthUIEQG_notdead = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<HealthComponent, HealthUIComponent>()
                .WithNone<DeadDestroyTag>()
                );
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

            state.Dependency = new UpdateHealthBarJob
            {
                ecbp = ecb.AsParallelWriter()
            }.ScheduleParallel(m_healthUIEQG_notdead, state.Dependency);
            state.Dependency.Complete();
        }
    }

    // scales the health bar horizontally based on currenthealth/maxHealth
    [BurstCompile]
    public partial struct UpdateHealthBarJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecbp;
        public void Execute([ChunkIndexInQuery] int ciqi, 
                            in HealthComponent healthComp, 
                            in HealthUIComponent healthUIComp, 
                            in Entity ent)
        {
            float healthNormalized = healthComp.currentHealth/healthComp.maxHealth;
            if(math.round(healthNormalized*100) != math.round(healthUIComp.healthBarValueNormalized*100))
            {
                HealthUIComponent nUIc = healthUIComp;
                nUIc.healthBarValueNormalized = healthNormalized;
                ecbp.SetComponent<HealthUIComponent>(ciqi, ent, nUIc);
                ecbp.AddComponent<Unity.Transforms.PostTransformScale>(ciqi, healthUIComp.healthBarEntity, new Unity.Transforms.PostTransformScale{
                    Value =  float3x3.Scale(new float3(healthNormalized, 1, 1))                    
                });
            }
        }
    }

}
