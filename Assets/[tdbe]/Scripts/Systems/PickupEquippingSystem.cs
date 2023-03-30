using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

using GameWorld.Players;

namespace GameWorld.Pickups
{
    // Collision system that on trigger modifies the data 
    // of colliding EquippedProjectile or EquippedShield components.
    // Actually, I specify players vs pickups, so ufo's won't pick up for now.
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    [BurstCompile]
    public partial struct PickupEquippingSystem : ISystem
    {
        //EntityQuery m_eqProjectileGroup;
        //EntityQuery m_eqShieldGroup;
        
        ComponentLookup<PlayerComponent> m_playersTCL;// this could be made more generic, ie also UFOs
        ComponentLookup<PickupTag> m_pickupTCL;

        ComponentLookup<EquippedProjectileDataComponent> m_equipProjectileTCL;
        ComponentLookup<EquippedShieldDataComponent> m_equipShieldTCL;
        ComponentLookup<PickupProjectileDataComponent> m_pickupProjectileTCL;
        ComponentLookup<PickupShieldDataComponent> m_pickupShieldTCL;
        
        public void OnCreate(ref SystemState state)
        {
            //state.RequireForUpdate<PickupEquippingSysTag>();
            /*
            [WriteGroup(typeof(EquippedProjectileDataComponent))]
            m_eqProjectileGroup = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithOptions(EntityQueryOptions.FilterWriteGroup)
                .WithAny<EquippedProjectileDataComponent>());
                
            m_eqShieldGroup = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithOptions(EntityQueryOptions.FilterWriteGroup)
                .WithAny<EquippedShieldDataComponent>());
            */

            m_playersTCL = state.GetComponentLookup<PlayerComponent>(true);
            m_pickupTCL = state.GetComponentLookup<PickupTag>(true);

            m_equipProjectileTCL = state.GetComponentLookup<EquippedProjectileDataComponent>(false);
            m_equipShieldTCL = state.GetComponentLookup<EquippedShieldDataComponent>(false);
            m_pickupProjectileTCL = state.GetComponentLookup<PickupProjectileDataComponent>(false);
            m_pickupShieldTCL = state.GetComponentLookup<PickupShieldDataComponent>(false);

            state.RequireForUpdate<PlayerComponent>();
            state.RequireForUpdate<PickupTag>();
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

            // too many tags...
            m_playersTCL.Update(ref state);
            m_pickupTCL.Update(ref state);
            m_equipProjectileTCL.Update(ref state);
            m_equipShieldTCL.Update(ref state);
            m_pickupProjectileTCL.Update(ref state);
            m_pickupShieldTCL.Update(ref state);

            var jhandle = new EquippablePickupJob
            {
                ecb = ecb,
                playersTCL = m_playersTCL,
                pickupTCL = m_pickupTCL,
                equipProjectileTCL = m_equipProjectileTCL,
                equipShieldTCL = m_equipShieldTCL,
                pickupProjectileTCL = m_pickupProjectileTCL,
                pickupShieldTCL = m_pickupShieldTCL

            };
            state.Dependency = jhandle.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
            // X_X singlethreaded physics lookups
        }

    }

    // Here we take the data from the pickup and populate the Equip component on 
    // whoever has it and collided with the pickup
    [BurstCompile]
    public partial struct EquippablePickupJob:ITriggerEventsJob
    {
        public EntityCommandBuffer ecb;
        [ReadOnly]
        public ComponentLookup<PlayerComponent> playersTCL;
        [ReadOnly]
        public ComponentLookup<PickupTag> pickupTCL;
        [ReadOnly]
        public ComponentLookup<EquippedProjectileDataComponent> equipProjectileTCL;
        [ReadOnly]
        public ComponentLookup<EquippedShieldDataComponent> equipShieldTCL;
        [ReadOnly]
        public ComponentLookup<PickupProjectileDataComponent> pickupProjectileTCL;
        [ReadOnly]
        public ComponentLookup<PickupShieldDataComponent> pickupShieldTCL;

        [BurstCompile]
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity entA = triggerEvent.EntityA;
            Entity entB = triggerEvent.EntityB;

            bool isEquiperA = playersTCL.HasComponent(entA);
            bool isEquiperB = playersTCL.HasComponent(entB);

            bool isPickupA = pickupTCL.HasComponent(entA);
            bool isPickupB = pickupTCL.HasComponent(entB);

            // same types should not collide with each other,
            // so no need for checks for now
            // we should always have the correct combo of components

            // still a lot of copy pasta.

            void setProjectile(
                EquippedProjectileDataComponent equipProjectileComp,
                Entity equipProjectileEnt,
                PickupProjectileDataComponent pickupProjectileComp,
                Entity pickupProjectileEnt,
                EntityCommandBuffer ecb
            ){
                equipProjectileComp.active = true;
                equipProjectileComp.isCollisionInvulnerable = pickupProjectileComp.isCollisionInvulnerable;
                equipProjectileComp.timeToLive = pickupProjectileComp.timeToLive;
                equipProjectileComp.owner = equipProjectileEnt;
                equipProjectileComp.prefab = pickupProjectileComp.prefab;
                equipProjectileComp.activeVisual = pickupProjectileComp.activeVisual;
                ecb.SetComponent<EquippedProjectileDataComponent>(
                    equipProjectileEnt,
                    equipProjectileComp
                );
                ecb.DestroyEntity(pickupProjectileEnt);
            }

            void setShield(
                EquippedShieldDataComponent equipShieldComp, 
                Entity equipShieldEnt,
                PickupShieldDataComponent pickupShieldComp,
                Entity pickupShieldEnt,
                EntityCommandBuffer ecb
            ){
                equipShieldComp.active = true;
                equipShieldComp.timeToLive = pickupShieldComp.timeToLive;
                equipShieldComp.owner = equipShieldEnt;
                equipShieldComp.activeVisual = pickupShieldComp.activeVisual;
                ecb.SetComponent<EquippedShieldDataComponent>(
                    equipShieldEnt,
                    equipShieldComp
                );
                ecb.DestroyEntity(pickupShieldEnt);
            }

            if(isEquiperA && isPickupB){
                if( equipProjectileTCL.HasComponent(entA) &&
                    pickupProjectileTCL.HasComponent(entB))
                {
                    Entity equipProjectileEnt = entA;
                    EquippedProjectileDataComponent equipProjectileComp;
                    equipProjectileTCL.TryGetComponent(entA, out equipProjectileComp);

                    Entity pickupProjectileEnt = entB;
                    PickupProjectileDataComponent pickupProjectileComp;
                    pickupProjectileTCL.TryGetComponent(entB, out pickupProjectileComp);

                    setProjectile(equipProjectileComp, equipProjectileEnt, pickupProjectileComp, pickupProjectileEnt, ecb);
                }
                else if(equipShieldTCL.HasComponent(entA) &&
                        pickupShieldTCL.HasComponent(entB) )
                {
                    Entity equipShieldEnt = entA;
                    EquippedShieldDataComponent equipShieldComp;
                    equipShieldTCL.TryGetComponent(entA, out equipShieldComp);

                    Entity pickupShieldEnt = entB;
                    PickupShieldDataComponent pickupShieldComp;
                    pickupShieldTCL.TryGetComponent(entB, out pickupShieldComp);

                    setShield(equipShieldComp, equipShieldEnt, pickupShieldComp, pickupShieldEnt, ecb);
                }
            }
            else if(isEquiperB && isPickupA){
                if( equipProjectileTCL.HasComponent(entB) &&
                    pickupProjectileTCL.HasComponent(entA))
                {
                    Entity equipProjectileEnt = entB;
                    EquippedProjectileDataComponent equipProjectileComp;
                    equipProjectileTCL.TryGetComponent(entB, out equipProjectileComp);

                    Entity pickupProjectileEnt = entA;
                    PickupProjectileDataComponent pickupProjectileComp;
                    pickupProjectileTCL.TryGetComponent(entA, out pickupProjectileComp);

                    setProjectile(equipProjectileComp, equipProjectileEnt, pickupProjectileComp, pickupProjectileEnt, ecb);
                }
                else if(equipShieldTCL.HasComponent(entB) &&
                        pickupShieldTCL.HasComponent(entA) )
                {
                    Entity equipShieldEnt = entB;
                    EquippedShieldDataComponent equipShieldComp;
                    equipShieldTCL.TryGetComponent(entB, out equipShieldComp);

                    Entity pickupShieldEnt = entA;
                    PickupShieldDataComponent pickupShieldComp;
                    pickupShieldTCL.TryGetComponent(entA, out pickupShieldComp);

                    setShield(equipShieldComp, equipShieldEnt, pickupShieldComp, pickupShieldEnt, ecb);
                }
            }
            
        }
    }

}
