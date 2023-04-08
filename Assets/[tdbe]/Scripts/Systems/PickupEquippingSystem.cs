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
    // Collision system PlayerComponent / UFO? <-> PickupTag that 
    // on trigger modifies the data
    // of colliding EquippedProjectile or EquippedShield components.
    // Ufo's could also consume pickups: add equip slot and add ufo trigger component.
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    [BurstCompile]
    public partial struct PickupEquippingSystem : ISystem
    {
        //EntityQuery m_eqProjectileGroup;
        //EntityQuery m_eqShieldGroup;
        
        ComponentLookup<PlayerComponent> m_playersTCL;// TODO: this could be made more generic, ie also UFOs
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

            m_equipProjectileTCL = state.GetComponentLookup<EquippedProjectileDataComponent>(true);
            m_equipShieldTCL = state.GetComponentLookup<EquippedShieldDataComponent>(true);
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

            // so many tags...
            m_playersTCL.Update(ref state);
            m_pickupTCL.Update(ref state);
            m_equipProjectileTCL.Update(ref state);
            m_equipShieldTCL.Update(ref state);
            m_pickupProjectileTCL.Update(ref state);
            m_pickupShieldTCL.Update(ref state);

            var jhandle = new EquippablePickupJob
            {
                time = Time.timeAsDouble,
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
            // NOTE: can do  NativeList<TriggerEvent> and multithread afterwards
        }

    }

    // Here we take the data from the pickup and populate the Equip component on 
    // whoever has it and collided with the pickup.
    // We're not really planning to do a lot of work here, but if we need multithreading, 
    // we can export the collisions into a NativeList<TriggerEvent>, and return them and pass them to a parallel thread.
    [BurstCompile]
    public partial struct EquippablePickupJob:ITriggerEventsJob
    {
        [ReadOnly]
        public double time;
        public EntityCommandBuffer ecb;
        [ReadOnly]
        public ComponentLookup<PlayerComponent> playersTCL;
        [ReadOnly]
        public ComponentLookup<PickupTag> pickupTCL;
        [ReadOnly]
        public ComponentLookup<EquippedProjectileDataComponent> equipProjectileTCL;
        [ReadOnly]
        public ComponentLookup<EquippedShieldDataComponent> equipShieldTCL;
        
        public ComponentLookup<PickupProjectileDataComponent> pickupProjectileTCL;
        
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
                double time,
                EquippedProjectileDataComponent equipProjectileComp,
                Entity equipProjectileEnt,
                PickupProjectileDataComponent pickupProjectileComp,
                Entity pickupProjectileEnt,
                EntityCommandBuffer ecb,
                ref ComponentLookup<PickupProjectileDataComponent> pickupProjectileTCL
            ){
                if(!pickupProjectileComp.active)
                    return;
                // a custom memberwise copy would be nice :P
                equipProjectileComp.active = true;
                equipProjectileComp.isCollisionInvulnerable = pickupProjectileComp.isCollisionInvulnerable;
                equipProjectileComp.timeToLive = pickupProjectileComp.timeToLive;
                equipProjectileComp.owner = equipProjectileEnt;
                equipProjectileComp.prefab = pickupProjectileComp.prefab;
                equipProjectileComp.speed = pickupProjectileComp.speed;
                equipProjectileComp.scale = pickupProjectileComp.scale;
                equipProjectileComp.pickupTime = time;
                equipProjectileComp.pickupTimeToLive = pickupProjectileComp.pickupTimeToLive;


                {
                    if(equipProjectileComp.activeVisual != Entity.Null){
                        ecb.AddComponent<DeadDestroyTag>(equipProjectileComp.activeVisual);
                    }

                    if(pickupProjectileComp.activeVisual != Entity.Null)
                    {
                        Entity prefabInstance = ecb.Instantiate(pickupProjectileComp.activeVisual);
                        // Adding a component that already exists on an entity through the command buffer should result in just setting the value
                        // Otherwise we can also add a requirement in baking.
                        ecb.AddComponent<Parent>(prefabInstance, new Parent{
                            Value = equipProjectileEnt
                        });
                        equipProjectileComp.activeVisual = prefabInstance;
                    }
                }
                
                ecb.SetComponent<EquippedProjectileDataComponent>(
                    equipProjectileEnt,
                    equipProjectileComp
                );
                ecb.AddComponent<DeadDestroyTag>(pickupProjectileEnt);

                // especially important to avoid duplicates since we instantiate etc.
                // so we set this component here immediately (no ecb) and check for it in the beginning of this function.
                var comp = pickupProjectileTCL[pickupProjectileEnt];
                comp.active = false;
                pickupProjectileTCL[pickupProjectileEnt] = comp;
            }

            void setShield(
                double time,
                EquippedShieldDataComponent equipShieldComp, 
                Entity equipShieldEnt,
                PickupShieldDataComponent pickupShieldComp,
                Entity pickupShieldEnt,
                EntityCommandBuffer ecb,
                ref ComponentLookup<PickupShieldDataComponent> pickupShieldTCL
            ){
                if(!pickupShieldComp.active)
                    return;
                equipShieldComp.active = true;
                equipShieldComp.pickupTime = time;
                equipShieldComp.pickupTimeToLive = pickupShieldComp.pickupTimeToLive;
                equipShieldComp.owner = equipShieldEnt;

                {
                    if(equipShieldComp.activeVisual != Entity.Null){
                        ecb.AddComponent<DeadDestroyTag>(equipShieldComp.activeVisual);
                    }

                    if(pickupShieldComp.activeVisual != Entity.Null)
                    {
                        Entity prefabInstance = ecb.Instantiate(pickupShieldComp.activeVisual);
                        // Adding a component that already exists on an entity through the command buffer should result in just setting the value
                        // Otherwise we can also add a requirement in baking.
                        ecb.AddComponent<Parent>(prefabInstance, new Parent{
                            Value = equipShieldEnt
                        });
                        equipShieldComp.activeVisual = prefabInstance;
                    }
                }

                ecb.SetComponent<EquippedShieldDataComponent>(
                    equipShieldEnt,
                    equipShieldComp
                );
                ecb.AddComponent<DeadDestroyTag>(pickupShieldEnt);

                // especially important to avoid duplicates since we instantiate etc.
                // so we set this component here immediately (no ecb) and check for it in the beginning of this function.
                var comp = pickupShieldTCL[pickupShieldEnt];
                comp.active = false;
                pickupShieldTCL[pickupShieldEnt] = comp;
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

                    setProjectile(time, equipProjectileComp, equipProjectileEnt, pickupProjectileComp, 
                                    pickupProjectileEnt, ecb, ref pickupProjectileTCL);
                }
                else if(equipShieldTCL.HasComponent(entA) &&
                        pickupShieldTCL.HasComponent(entB) )
                {
                    Entity equipShieldEnt = entA;
                    EquippedShieldDataComponent equipShieldComp;
                    equipShieldTCL.TryGetComponent(entA, out equipShieldComp);
                    ecb.SetComponentEnabled<EquippedShieldDataComponent>(equipShieldEnt, true);

                    Entity pickupShieldEnt = entB;
                    PickupShieldDataComponent pickupShieldComp;
                    pickupShieldTCL.TryGetComponent(entB, out pickupShieldComp);

                    setShield(time, equipShieldComp, equipShieldEnt, pickupShieldComp, 
                                pickupShieldEnt, ecb, ref pickupShieldTCL);
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

                    setProjectile(time, equipProjectileComp, equipProjectileEnt, pickupProjectileComp, 
                                    pickupProjectileEnt, ecb, ref pickupProjectileTCL);
                }
                else if(equipShieldTCL.HasComponent(entB) &&
                        pickupShieldTCL.HasComponent(entA) )
                {
                    Entity equipShieldEnt = entB;
                    EquippedShieldDataComponent equipShieldComp;
                    equipShieldTCL.TryGetComponent(entB, out equipShieldComp);
                    ecb.SetComponentEnabled<EquippedShieldDataComponent>(equipShieldEnt, true);

                    Entity pickupShieldEnt = entA;
                    PickupShieldDataComponent pickupShieldComp;
                    pickupShieldTCL.TryGetComponent(entA, out pickupShieldComp);

                    setShield(time, equipShieldComp, equipShieldEnt, pickupShieldComp, 
                                pickupShieldEnt, ecb, ref pickupShieldTCL);
                }
            }
            
        }
    }

}
