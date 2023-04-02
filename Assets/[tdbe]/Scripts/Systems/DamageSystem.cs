using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Physics.Extensions;

using GameWorld.Pickups;

namespace GameWorld
{
    
    //[UpdateAfter(typeof(GameSystem))]
    //[UpdateAfter(typeof(GameWorld.Asteroid.AsteroidTargetedSpawnerSystem))]
    //[UpdateAfter(typeof(GameWorld.Players.PlayerProjectileSystem))]
    //
    //[UpdateBefore(typeof(HealthSystem))]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [BurstCompile]
    public partial struct  DamageSystem : ISystem
    {
        ComponentLookup<DamageComponent> m_damageCompsTCL;   
        ComponentLookup<HealthComponent> m_healthCompsTCL;   
        ComponentLookup<EquippedShieldDataComponent> m_shieldsTCL;   
        ComponentLookup<InvulnerableTag> m_invulnsTCL;   
  
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
 
            state.RequireForUpdate<DamageComponent>();
            state.RequireForUpdate<HealthComponent>();
            
            m_damageCompsTCL = state.GetComponentLookup<DamageComponent>(true);
            m_healthCompsTCL = state.GetComponentLookup<HealthComponent>(true);
            m_shieldsTCL = state.GetComponentLookup<EquippedShieldDataComponent>(true);
            m_invulnsTCL = state.GetComponentLookup<InvulnerableTag>(true);

            //m_damageEQG = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
            //    .WithAll<DamageComponent>()
            //    //.WithNone<InvulnerableTag>()
            //    );
            //m_invulnerableEQG = state.GetEntityQuery(ComponentType.ReadOnly<InvulnerableTag>());
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

            m_damageCompsTCL.Update(ref state);
            m_healthCompsTCL.Update(ref state);
            m_shieldsTCL.Update(ref state);
            m_invulnsTCL.Update(ref state);

            state.Dependency = new SetCollisionDamageJob
            {
                ecb = ecb,
                damageCompsTCL = m_damageCompsTCL,
                healthCompsTCL = m_healthCompsTCL,
                shieldsTCL = m_shieldsTCL,
                invulnsTCL = m_invulnsTCL
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);

        }
    }

    [BurstCompile]
    public partial struct SetCollisionDamageJob : ITriggerEventsJob
    {
        public EntityCommandBuffer ecb;
        [ReadOnly]
        public ComponentLookup<DamageComponent> damageCompsTCL;
        [ReadOnly]
        public ComponentLookup<HealthComponent> healthCompsTCL;
        [ReadOnly]
        public ComponentLookup<EquippedShieldDataComponent> shieldsTCL;
        [ReadOnly]
        public ComponentLookup<InvulnerableTag> invulnsTCL;

        public void Execute(TriggerEvent triggerEvent)
        {
            Entity entA = triggerEvent.EntityA;
            Entity entB = triggerEvent.EntityB;

            bool isDamagerA = damageCompsTCL.HasComponent(entA);
            bool isDamagerB = damageCompsTCL.HasComponent(entB);

            bool isHealthA = healthCompsTCL.HasComponent(entA);
            bool isHealthB = healthCompsTCL.HasComponent(entB);

            // note that this can be symmetrical; and we will handle both cases.
            // e.g. 2 things damaging each other. health can go negative why not.

            // NOTE: this applies damage every collision hit.
            // Which is far from perfect in any more advanced game.

            // TODO: check if player, say YOU DIED etc.

            if(isDamagerA && isHealthB)
            {
                InvulnerableTag invuln;
                if(!invulnsTCL.TryGetComponent(entB, out invuln))
                {
                    DamageComponent damageComp;
                    damageCompsTCL.TryGetComponent(entA, out damageComp);
                    HealthComponent healthComp;
                    healthCompsTCL.TryGetComponent(entB, out healthComp);

                    EquippedShieldDataComponent shieldComp;
                    bool hasShield = shieldsTCL.TryGetComponent(entB, out shieldComp);
                    
                    if(hasShield && shieldComp.active){
                        // TODO: maybe have the shield go down?
                        // Right now shield also practially means invulnerable for x seconds.
                    }
                    else
                    {
                        healthComp.currentHealth -= damageComp.damagePerHit;
                        ecb.SetComponent<HealthComponent>(entB, healthComp);
                    }
                }
            }

            if(isDamagerB && isHealthA)
            {
                InvulnerableTag invuln;
                if(!invulnsTCL.TryGetComponent(entA, out invuln))
                {
                    DamageComponent damageComp;
                    damageCompsTCL.TryGetComponent(entB, out damageComp);
                    HealthComponent healthComp;
                    healthCompsTCL.TryGetComponent(entA, out healthComp);

                    EquippedShieldDataComponent shieldComp;
                    bool hasShield = shieldsTCL.TryGetComponent(entA, out shieldComp);
                    
                    if(hasShield && shieldComp.active){
                        // TODO: maybe have the shield go down?
                        // Right now shield also practially means invulnerable for x seconds.
                    }
                    else
                    {
                        healthComp.currentHealth -= damageComp.damagePerHit;
                        ecb.SetComponent<HealthComponent>(entA, healthComp);
                    }
                }
            }
        }
    }


}
