using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

using Unity.Physics;
//using Unity.Physics.Extensions;

namespace GameWorld.NPCs
{
    public class NpcSpawnerVRUpdateGroup:ComponentSystemGroup
    {
        public NpcSpawnerVRUpdateGroup()
        {
            // NOTE: Unity.Entities.RateUtils.VariableRateManager.MinUpdateRateMS
            RateManager = new RateUtils.VariableRateManager(16, true);
        }

        public void SetRateManager(uint ms, bool pushToWorld){
            RateManager = new RateUtils.VariableRateManager(ms, pushToWorld);
        }
    }

    // TODO: should try organizing in some writegroups and jobs and/or some externals here e.g. across ufo, asteroid, powerup spawning.
    // But also, conceptually spealking, in general gamedev, these are 3 categories of things that normally shouldn't have common links.
    [UpdateInGroup(typeof(NpcSpawnerVRUpdateGroup))]
    [BurstCompile]
    public partial struct NPCSpawnerSystem : ISystem
    {
        private EntityQuery m_UFOsGroup;
        private EntityQuery m_boundsGroup;
        private NPCSpawnerStateComponent.State prevState;
        // TODO: this is me screwing around. There has to be a better way, but there are no docs yet.
        private bool justUpdatedRate;

        // Set from other systems, because variable rate. (would like to debate how good and bad of an idea this is)
        public void SetNewRateState(ref SystemState state, NPCSpawnerStateComponent.State newState)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            SetNewRateState(ref state, ref ecb, newState);
        }

        private void SetNewRateState(ref SystemState state, ref EntityCommandBuffer ecb, NPCSpawnerStateComponent.State newState)
        {
            // handle transition to next state, and also prepare the variable rate update rate
            
            Entity stateCompEnt = SystemAPI.GetSingletonEntity<NPCSpawnerStateComponent>();

            if(newState == NPCSpawnerStateComponent.State.InGameSpawn)
            {
                
                prevState = SystemAPI.GetSingleton<NPCSpawnerStateComponent>().state;
                ecb.SetComponent<NPCSpawnerStateComponent>(
                    stateCompEnt,
                    new NPCSpawnerStateComponent{
                        state = newState
                    });
                SetNewVariableRate(ref state, SystemAPI.GetSingleton<UFOSpawnComponent>().inGameSpawnRate_ms);
            }
            else{

                prevState = SystemAPI.GetSingleton<NPCSpawnerStateComponent>().state;
                ecb.SetComponent<NPCSpawnerStateComponent>(
                    stateCompEnt,
                    new NPCSpawnerStateComponent{
                        state = newState
                    });
                SetNewVariableRate(ref state, 16);
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NPCSpawnerStateComponent>();

            state.RequireForUpdate<PrefabAndParentBufferComponent>();
            state.RequireForUpdate<RandomnessComponent>();
            state.RequireForUpdate<UFOSpawnComponent>();
            //state.RequireForUpdate<UFOSpawnerAspect>();

            m_UFOsGroup = state.GetEntityQuery(ComponentType.ReadOnly<UFOComponent>());

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
                                    NPCSpawnerStateComponent.State spawnerState, int existingCount)
        {
            UFOSpawnerAspect UFOSpawnAspect = SystemAPI.GetAspectRW<UFOSpawnerAspect>(stateCompEnt);
            uint spawnAmount = 0;
            var targetArea = (float3.zero, float3.zero);
            
            if(spawnerState == NPCSpawnerStateComponent.State.InGameSpawn)
            {
                spawnAmount = 1;
                targetArea = GetCorners2(ref state, m_boundsGroup.ToEntityArray(Allocator.Temp));
            }
            else if(spawnerState == NPCSpawnerStateComponent.State.Inactive){
                return;
            }

            var rga = SystemAPI.GetComponent<RandomnessComponent>(stateCompEnt).randomGeneratorArr;
            var prefabsAndParents = SystemAPI.GetBuffer<PrefabAndParentBufferComponent>(stateCompEnt);
            var jhandle = new NPCSpawnerJob
            {
                ecb = ecb,
                spawnAmount = spawnAmount,
                targetArea = targetArea,
                existingCount = existingCount,
                rga = rga,
                prefabsAndParents = prefabsAndParents
            }.Schedule(state.Dependency);

           jhandle.Complete();
            //ecb.DestroyEntity(UFOSpawnAspect.UFOPrefab);
        }

        private void SetNewVariableRate(ref SystemState state, uint rate)
        {
            // so this rate manager setting is only available as managed class, so no burst. 
            // Weird because a default variable rate manager is a burstable struct.
            // TODO: am I missing something here?
            var asvrUpdateGroup = state.World.GetExistingSystemManaged<NpcSpawnerVRUpdateGroup>();
            asvrUpdateGroup.SetRateManager(rate, false);
            justUpdatedRate = true;
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NPCSpawnerStateComponent spawnerState = SystemAPI.GetSingleton<NPCSpawnerStateComponent>();
         
            // Ways to handle game state: Tags, SharedComponents, Component values (this case), ComponentSystemGroup as "state".
            // This spawner system needs to run all the time (at a certain rate, unless game is paused), and otherwise it can be made `state.Enabled = false;`.
            if(spawnerState.state == NPCSpawnerStateComponent.State.InGameSpawn)
            {
                if(!justUpdatedRate)
                {
                    //Debug.Log("[NPCSpawner][InGameSpawn] UFOe! "+existingUFOCount.ToString());
                    //TODO: I would actually like this mode to spawn UFOs from the edges only

                    Entity stateCompEnt = SystemAPI.GetSingletonEntity<NPCSpawnerStateComponent>();
                    var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                    int existingCount = m_UFOsGroup.CalculateEntityCount();
                    UFOSpawnComponent ufoComp = SystemAPI.GetSingleton<UFOSpawnComponent>();

                    DoSpawnOnMap(ref state, ref ecb, ref stateCompEnt, spawnerState.state, existingCount);

                    {
                    var rga = SystemAPI.GetComponent<RandomnessComponent>(stateCompEnt).randomGeneratorArr;
                    Unity.Mathematics.Random rg = rga[0];
                    uint newRate = (uint)(math.min(20000, math.max(5000, ufoComp.inGameSpawnRate_ms + rg.NextInt(-5000,5000))));
                    ufoComp.inGameSpawnRate_ms = newRate;
                    rga[0] = rg;
                    SetNewVariableRate(ref state, newRate);
                    ecb.SetComponent<UFOSpawnComponent>(stateCompEnt, ufoComp);
                    }
                }
                else{
                    justUpdatedRate = false;
                }
            }
        }
    }

    // this could be shared with other spawn jobs (e.g. asteroids, pickups, ufos)
    //[BurstCompile]
    public partial struct NPCSpawnerJob:IJobEntity
    {
        [Unity.Collections.LowLevel.Unsafe.NativeSetThreadIndex]
        private int thri;
        public EntityCommandBuffer ecb;
        public uint spawnAmount;
        public int existingCount;
        public (float3, float3) targetArea;
        [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
        public NativeArray<Unity.Mathematics.Random> rga;
        public DynamicBuffer<PrefabAndParentBufferComponent> prefabsAndParents;

        private void Execute(UFOSpawnerAspect UFOSpawnAspect)
        {
            Unity.Mathematics.Random rg = rga[thri];
            for(uint i = 0; i < spawnAmount; i++){
                if(i + existingCount >= UFOSpawnAspect.maxNumber){
                    break;
                }
                
                Entity UFO = ecb.Instantiate(prefabsAndParents[0].prefab);

                ecb.SetComponent<LocalTransform>(UFO, UFOSpawnAspect.GetUFOTransform(ref rg, targetArea));

                ecb.AddComponent<Unity.Transforms.Parent>(UFO, new Unity.Transforms.Parent{ 
                        Value = prefabsAndParents[0].parent
                });
            }
            rga[thri] = rg;
        }
    }

}
