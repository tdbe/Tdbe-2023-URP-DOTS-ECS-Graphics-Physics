using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

using Unity.Physics;
//using Unity.Physics.Extensions;

namespace GameWorld.Pickups
{
    public class PickupsSpawnerVRUpdateGroup:ComponentSystemGroup
    {
        public PickupsSpawnerVRUpdateGroup()
        {
            // NOTE: Unity.Entities.RateUtils.VariableRateManager.MinUpdateRateMS
            RateManager = new RateUtils.VariableRateManager(16, true);
        }

        public void SetRateManager(uint ms, bool pushToWorld){
            RateManager = new RateUtils.VariableRateManager(ms, pushToWorld);
        }
    }

    // TODO: should try organizing in some writegroups and jobs and/or some externals here e.g. across Pickups, asteroid, powerup spawning.
    // But also, conceptually spealking, in general gamedev, these are 3 categories of things that normally shouldn't have common links.
    [UpdateInGroup(typeof(PickupsSpawnerVRUpdateGroup))]
    [BurstCompile]
    public partial struct PickupsSpawnerSystem : ISystem
    {
        private EntityQuery m_PickupsGroupShield;
        private EntityQuery m_PickupsGroupGun;
        private EntityQuery m_boundsGroup;
        private PickupsSpawnerStateComponent.State prevState;
        // TODO: this is me screwing around. There has to be a better way, but there are no docs yet.
        private bool justUpdatedRate;

        // Set from other systems, because variable rate. (would like to debate how good and bad of an idea this is)
        public void SetNewRateState(ref SystemState state, PickupsSpawnerStateComponent.State newState)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            SetNewRateState(ref state, ref ecb, newState);
        }

        private void SetNewRateState(ref SystemState state, ref EntityCommandBuffer ecb, PickupsSpawnerStateComponent.State newState)
        {
            // handle transition to next state, and also prepare the variable rate update rate
            
            Entity stateCompEnt = SystemAPI.GetSingletonEntity<PickupsSpawnerStateComponent>();

            if(newState == PickupsSpawnerStateComponent.State.InGameSpawn)
            {
                
                prevState = SystemAPI.GetSingleton<PickupsSpawnerStateComponent>().state;
                ecb.SetComponent<PickupsSpawnerStateComponent>(
                    stateCompEnt,
                    new PickupsSpawnerStateComponent{
                        state = newState
                    });
                SetNewVariableRate(ref state, SystemAPI.GetSingleton<PickupsSpawnerComponent>().inGameSpawnRate_ms);
            }
            else{

                prevState = SystemAPI.GetSingleton<PickupsSpawnerStateComponent>().state;
                ecb.SetComponent<PickupsSpawnerStateComponent>(
                    stateCompEnt,
                    new PickupsSpawnerStateComponent{
                        state = newState
                    });
                SetNewVariableRate(ref state, 16);
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PickupsSpawnerStateComponent>();

            state.RequireForUpdate<PrefabAndParentBufferComponent>();
            state.RequireForUpdate<RandomnessComponent>();
            state.RequireForUpdate<PickupsSpawnerComponent>();
            //state.RequireForUpdate<PickupsSpawnerAspect>();

            // TODO: is there a filter for one OR another? as in, one || another?
            m_PickupsGroupShield = state.GetEntityQuery(ComponentType.ReadOnly<ShieldPickupTag>());
            m_PickupsGroupGun = state.GetEntityQuery(ComponentType.ReadOnly<GunPickupTag>());

            state.RequireForUpdate<BoundsTagComponent>();
            m_boundsGroup = state.GetEntityQuery(ComponentType.ReadOnly<BoundsTagComponent>());

        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        // should have just stored a couple corners entities :)
        // used for random min max
        [BurstCompile]
        private (float3, float3) GetCorners2(ref SystemState state, NativeArray<Entity> boundsEnts){
            float3 bl = float3.zero;
            float3 tr = float3.zero;
            foreach(Entity bndEnt in boundsEnts){
                uint id = SystemAPI.GetComponent<BoundsTagComponent>(bndEnt).boundsID;
                if(id == 0)
                    bl.y = SystemAPI.GetComponent<LocalTransform>(bndEnt).Position.y+1.01f;
                else
                if(id == 1)
                    bl.x = SystemAPI.GetComponent<LocalTransform>(bndEnt).Position.x+1.01f;
                else
                if(id == 2)
                    tr.y = SystemAPI.GetComponent<LocalTransform>(bndEnt).Position.y-1.01f;
                else
                if(id == 3)
                    tr.x = SystemAPI.GetComponent<LocalTransform>(bndEnt).Position.x-1.01f;
            }
            boundsEnts.Dispose();
            return (bl, tr);
        }

        private void DoSpawnOnMap(  ref SystemState state, ref EntityCommandBuffer ecb, ref Entity stateCompEnt, 
                                    PickupsSpawnerStateComponent.State spawnerState, int existingCount)
        {
            PickupsSpawnerAspect PickupsSpawnAspect = SystemAPI.GetAspectRW<PickupsSpawnerAspect>(stateCompEnt);
            uint spawnAmount = 0;
            var targetArea = (float3.zero, float3.zero);
            
            if(spawnerState == PickupsSpawnerStateComponent.State.InGameSpawn)
            {
                spawnAmount = 2;// note: not one of each, just 2 random ones
                targetArea = GetCorners2(ref state, m_boundsGroup.ToEntityArray(Allocator.Temp));
            }
            else if(spawnerState == PickupsSpawnerStateComponent.State.Inactive){
                return;
            }

            var rga = SystemAPI.GetComponent<RandomnessComponent>(stateCompEnt).randomGeneratorArr;
            var prefabsAndParents = SystemAPI.GetBuffer<PrefabAndParentBufferComponent>(stateCompEnt);
            var jhandle = new PickupsSpawnerJob
            {
                ecb = ecb,
                spawnAmount = spawnAmount,
                targetArea = targetArea,
                existingCount = existingCount,
                rga = rga,
                prefabsAndParents = prefabsAndParents
            }.Schedule(state.Dependency);

           jhandle.Complete();
            //ecb.DestroyEntity(PickupsSpawnAspect.PickupsPrefab);
        }

        private void SetNewVariableRate(ref SystemState state, uint rate)
        {
            // so this rate manager setting is only available as managed class, so no burst. 
            // Weird because a default variable rate manager is a burstable struct.
            // TODO: am I missing something here?
            var asvrUpdateGroup = state.World.GetExistingSystemManaged<PickupsSpawnerVRUpdateGroup>();
            asvrUpdateGroup.SetRateManager(rate, false);
            justUpdatedRate = true;
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            PickupsSpawnerStateComponent spawnerState = SystemAPI.GetSingleton<PickupsSpawnerStateComponent>();
         
            // Ways to handle game state: Tags, SharedComponents, Component values (this case), ComponentSystemGroup as "state".
            // This spawner system needs to run all the time (at a certain rate, unless game is paused), and otherwise it can be made `state.Enabled = false;`.
            if(spawnerState.state == PickupsSpawnerStateComponent.State.InGameSpawn)
            {
                if(!justUpdatedRate)
                {
                    //Debug.Log("[PickupsSpawner][InGameSpawn] Pickup! ");

                    Entity stateCompEnt = SystemAPI.GetSingletonEntity<PickupsSpawnerStateComponent>();
                    var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                    int existingCount = m_PickupsGroupShield.CalculateEntityCount() + m_PickupsGroupGun.CalculateEntityCount();
                    PickupsSpawnerComponent pickupsComp = SystemAPI.GetSingleton<PickupsSpawnerComponent>();
                    
                    if(existingCount < pickupsComp.maxNumber){
                        DoSpawnOnMap(ref state, ref ecb, ref stateCompEnt, spawnerState.state, existingCount);

                        {
                        var rga = SystemAPI.GetComponent<RandomnessComponent>(stateCompEnt).randomGeneratorArr;
                        Unity.Mathematics.Random rg = rga[0];
                        uint newRate = (uint)(math.min(20000, math.max(5000, pickupsComp.inGameSpawnRate_ms + rg.NextInt(-5000,5000))));
                        pickupsComp.inGameSpawnRate_ms = newRate;
                        rga[0] = rg;
                        SetNewVariableRate(ref state, newRate);
                        ecb.SetComponent<PickupsSpawnerComponent>(stateCompEnt, pickupsComp);
                        }
                    }
                }
                else{
                    justUpdatedRate = false;
                }
            }
        }
    }

    // if you really wanted, this could be shared with other spawn jobs (e.g. asteroids, pickups, ufos)
    //[BurstCompile]
    public partial struct PickupsSpawnerJob:IJobEntity
    {
        [Unity.Collections.LowLevel.Unsafe.NativeSetThreadIndex]
        private int thri;
        public EntityCommandBuffer ecb;
        [ReadOnly]
        public uint spawnAmount;
        [ReadOnly]
        public int existingCount;
        [ReadOnly]
        public (float3, float3) targetArea; 
        [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
        public NativeArray<Unity.Mathematics.Random> rga;
        [ReadOnly]
        public DynamicBuffer<PrefabAndParentBufferComponent> prefabsAndParents;
        //[BurstCompile]
        private void Execute(PickupsSpawnerAspect pickupsSpawnAspect)
        {
            Unity.Mathematics.Random rg = rga[thri];
            for(uint i = 0; i < spawnAmount; i++){
                
                if(i + existingCount >= pickupsSpawnAspect.maxNumber){
                    break;
                }
                int which = rg.NextInt(0, prefabsAndParents.Length);

                Entity pickup = ecb.Instantiate(prefabsAndParents[which].prefab);

                ecb.SetComponent<LocalTransform>(pickup, pickupsSpawnAspect.GetPickupsTransform(ref rg, targetArea));

                ecb.SetComponent<PhysicsVelocity>(pickup, pickupsSpawnAspect.GetPhysicsVelocity());

                ecb.AddComponent<Unity.Transforms.Parent>(pickup, new Unity.Transforms.Parent{ 
                        Value = prefabsAndParents[which].parent
                });
            }
            rga[thri] = rg;
        }
    }

}
