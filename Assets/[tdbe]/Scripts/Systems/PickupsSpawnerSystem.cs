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
            RateManager.Timestep = 0.015f;
        }
    }

    [UpdateInGroup(typeof(PickupsSpawnerVRUpdateGroup))]
    [BurstCompile]
    public partial struct PickupsSpawnerSystem : ISystem
    {
        private EntityQuery m_allPickups;
        private EntityQuery m_boundsGroup;

        // Need to set variable rate from other systems
        public void SetNewRate(ref SystemState state)
        {
            // var ecb = new EntityCommandBuffer(Allocator.Temp);
            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            Entity stateCompEnt = SystemAPI.GetSingletonEntity<PickupsSpawnerStateComponent>();
            uint rate = SystemAPI.GetComponent<VariableRateComponent>(stateCompEnt).currentSpawnRate_ms;

            var asvrUpdateGroup = state.World.GetExistingSystemManaged<PickupsSpawnerVRUpdateGroup>();
            asvrUpdateGroup.SetRateManager(rate, true);
        }

        public void SetNewState(ref SystemState state, PickupsSpawnerStateComponent.State newState)
        {
            // var ecb = new EntityCommandBuffer(Allocator.Temp);
            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            Entity stateCompEnt = SystemAPI.GetSingletonEntity<PickupsSpawnerStateComponent>();
            ecb.SetComponent<PickupsSpawnerStateComponent>(
                stateCompEnt,
                new PickupsSpawnerStateComponent{
                    state = newState
                });
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PickupsSpawnerStateComponent>();

            state.RequireForUpdate<PrefabAndParentBufferComponent>();
            state.RequireForUpdate<RandomnessComponent>();
            state.RequireForUpdate<RandomedSpawningComponent>();

            // GetComponentLookup
            //m_allPickups = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp).WithAny<ShieldPickupTag, GunPickupTag>());
            m_allPickups = state.GetEntityQuery(ComponentType.ReadOnly<PickupTag>());

            state.RequireForUpdate<BoundsTagComponent>();
            m_boundsGroup = state.GetEntityQuery(ComponentType.ReadOnly<BoundsTagComponent>());

        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        // should have just stored a couple corners entities :)
        // used for spawn min max
        [BurstCompile]
        private void GetCorners2(ref SystemState state, NativeArray<Entity> boundsEnts, out float3 targetAreaBL, out float3 targetAreaTR){
            float3 bl = float3.zero;
            float3 tr = float3.zero;
            foreach(Entity bndEnt in boundsEnts){
                uint id = SystemAPI.GetComponent<BoundsTagComponent>(bndEnt).boundsID;
                if(id == 0)
                    bl.y = SystemAPI.GetComponent<LocalTransform>(bndEnt).Position.y+1.01f;
                else if(id == 1)
                    bl.x = SystemAPI.GetComponent<LocalTransform>(bndEnt).Position.x+1.01f;
                else if(id == 2)
                    tr.y = SystemAPI.GetComponent<LocalTransform>(bndEnt).Position.y-1.01f;
                else if(id == 3)
                    tr.x = SystemAPI.GetComponent<LocalTransform>(bndEnt).Position.x-1.01f;
            }
            boundsEnts.Dispose();
            targetAreaBL = bl;
            targetAreaTR = tr;
        }

        [BurstCompile]
        private void DoSpawnOnMap(  ref SystemState state, ref EntityCommandBuffer ecb, ref Entity stateCompEnt, 
                                    PickupsSpawnerStateComponent.State spawnerState, int existingCount)
        {
            RandomSpawnedSetupAspect spawnerAspect = SystemAPI.GetAspectRW<RandomSpawnedSetupAspect>(stateCompEnt);
            uint spawnAmount = 0;
            float3 targetAreaBL = float3.zero;
            float3 targetAreaTR = float3.zero;
            
            if(spawnerState == PickupsSpawnerStateComponent.State.InGameSpawn)
            {
                spawnAmount = 2;// note: not one of each, just 2 random ones
                GetCorners2(ref state, m_boundsGroup.ToEntityArray(Allocator.Temp), out targetAreaBL, out targetAreaTR);
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
                targetAreaBL = targetAreaBL,
                targetAreaTR = targetAreaTR,
                existingCount = existingCount,
                rga = rga,
                prefabsAndParents = prefabsAndParents
            }.Schedule(state.Dependency);

            jhandle.Complete();
            //ecb.DestroyEntity(PickupsSpawnAspect.PickupsPrefab);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            PickupsSpawnerStateComponent spawnerState = SystemAPI.GetSingleton<PickupsSpawnerStateComponent>();
         
            // Ways to handle game state: Tags, SharedComponents, Component values (this case), ComponentSystemGroup as "state".
            // This spawner system needs to run all the time (at a certain rate, unless game is paused), and otherwise it can be made `state.Enabled = false;`.
            if(spawnerState.state == PickupsSpawnerStateComponent.State.InGameSpawn)
            {
                Entity stateCompEnt = SystemAPI.GetSingletonEntity<PickupsSpawnerStateComponent>();
                var rateComponent = SystemAPI.GetComponent<VariableRateComponent>(stateCompEnt);
                if(!rateComponent.refreshSystemRateRequest)
                {
                    int existingCount = m_allPickups.CalculateEntityCount();
                    SpawnCapComponent spawnCap = SystemAPI.GetComponent<SpawnCapComponent>(stateCompEnt);
                    if(existingCount < spawnCap.maxNumber)
                    {
                        Debug.Log("[PickupsSpawner][InGameSpawn] spawning pickups. ");
                        var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                        DoSpawnOnMap(ref state, ref ecb, ref stateCompEnt, spawnerState.state, existingCount);

                        // we want to slightly randomize the updategroup update rate
                        {
                        var rga = SystemAPI.GetComponent<RandomnessComponent>(stateCompEnt);
                        Unity.Mathematics.Random rg = rga.randomGeneratorArr[0];
                        uint newRate = (uint)(math.min(20000, math.max(5000, rateComponent.inGameSpawnRate_ms + rg.NextInt(-5000,5000))));
                        rga.randomGeneratorArr[0] = rg;
                        ecb.SetComponent<RandomnessComponent>(stateCompEnt, rga);

                        rateComponent.currentSpawnRate_ms = newRate;
                        rateComponent.refreshSystemRateRequest = true;
                        ecb.SetComponent<VariableRateComponent>(stateCompEnt, rateComponent);
                        }
                    }
                }
 
            }
        }
    }

    // TODO: spawn these for loops in parallel instead
    // A generic spawning job over a random area + velocity, doesn't have to be used just by pickups spawner..
    [BurstCompile]
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
        public float3 targetAreaBL;
        [ReadOnly]
        public float3 targetAreaTR; 
        [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
        public NativeArray<Unity.Mathematics.Random> rga;
        [ReadOnly]
        public DynamicBuffer<PrefabAndParentBufferComponent> prefabsAndParents;
        [BurstCompile]
        private void Execute(in RandomSpawnedSetupAspect spawnerAspect, in SpawnCapComponent spawnCap, in PickupsSpawnerStateComponent pssctag)
        {
            Unity.Mathematics.Random rg = rga[thri];
            for(uint i = 0; i < spawnAmount; i++){
                
                if(i + existingCount >= spawnCap.maxNumber){
                    break;
                }
                int which = rg.NextInt(0, prefabsAndParents.Length);

                // TODO: WTF:
                // - this is a BeginInitialization ECB
                // - I instantiate on it
                // - then I set local transform on it
                // And yet, the physics ITriggerEventsJob picks it up
                // for 1 frame, at 0,0,0 even though it should have never
                // been at 0,0,0, because it was spawned with a nonzero localtransform!
                // wut?

                Entity ent = ecb.Instantiate(prefabsAndParents[which].prefab);

                ecb.SetComponent<LocalTransform>(ent, spawnerAspect.GetTransform(ref rg, targetAreaBL, targetAreaTR));
                // I'll use physicsVelocity when I get to the Player move forces.
                // Actually I do need to set e.g. velocity/mass so it doesn't stop spinning.
                ecb.SetComponent<PhysicsVelocity>(ent, spawnerAspect.GetPhysicsVelocity());

                if(prefabsAndParents.Length>which){
                    ecb.AddComponent<Unity.Transforms.Parent>(ent, new Unity.Transforms.Parent{ 
                            Value = prefabsAndParents[which].parent
                    });
                }
            }
            rga[thri] = rg;
        }
    }

}
