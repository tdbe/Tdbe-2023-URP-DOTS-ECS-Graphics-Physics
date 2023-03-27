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
        // TODO: this is me screwing around. There has to be a better way, but there are no docs yet.
        private double lastUpdateRateTime;

        // Need to set variable rate from other systems
        public void SetNewRate(ref SystemState state)
        {
            // var ecb = new EntityCommandBuffer(Allocator.Temp);
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            Entity stateCompEnt = SystemAPI.GetSingletonEntity<PickupsSpawnerStateComponent>();
            uint rate = SystemAPI.GetComponent<VariableRateComponent>(stateCompEnt).currentSpawnRate_ms;

            var asvrUpdateGroup = state.World.GetExistingSystemManaged<PickupsSpawnerVRUpdateGroup>();
            asvrUpdateGroup.SetRateManager(rate, true);
            lastUpdateRateTime = SystemAPI.Time.ElapsedTime;
        }

        public void SetNewState(ref SystemState state, PickupsSpawnerStateComponent.State newState)
        {
            // var ecb = new EntityCommandBuffer(Allocator.Temp);
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
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
            state.RequireForUpdate<SpawnerComponent>();

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
            SpawnerAspect spawnerAspect = SystemAPI.GetAspectRW<SpawnerAspect>(stateCompEnt);
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
                if(SystemAPI.Time.ElapsedTime - lastUpdateRateTime >= rateComponent.burstSpawnRate_ms)
                {
                    //Debug.Log("[PickupsSpawner][InGameSpawn] Pickup! ");

                    var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                    int existingCount = m_PickupsGroupShield.CalculateEntityCount() + m_PickupsGroupGun.CalculateEntityCount();
                    SpawnerComponent stateComp = SystemAPI.GetComponent<SpawnerComponent>(stateCompEnt);
                    
                    if(existingCount < stateComp.maxNumber){
                        DoSpawnOnMap(ref state, ref ecb, ref stateCompEnt, spawnerState.state, existingCount);

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

    // if you really wanted this job could be shared (e.g. asteroids, pickups, ufos). TODO: spawn these for loops in parallel instead
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
        private void Execute(SpawnerAspect spawnerAspect, PickupsSpawnerStateComponent pssctag)
        {
            Unity.Mathematics.Random rg = rga[thri];
            for(uint i = 0; i < spawnAmount; i++){
                
                if(i + existingCount >= spawnerAspect.maxNumber){
                    break;
                }
                int which = rg.NextInt(0, prefabsAndParents.Length);

                Entity ent = ecb.Instantiate(prefabsAndParents[which].prefab);

                ecb.SetComponent<LocalTransform>(ent, spawnerAspect.GetTransform(ref rg, targetAreaBL, targetAreaTR));
                // I'll use physicsVelocity when I get to the Player move forces.
                // Actually I do need to set e.g. velocity/mass so it doesn't stop spinning.
                ecb.SetComponent<PhysicsVelocity>(ent, spawnerAspect.GetPhysicsVelocity());

                ecb.AddComponent<Unity.Transforms.Parent>(ent, new Unity.Transforms.Parent{ 
                        Value = prefabsAndParents[which].parent
                });
            }
            rga[thri] = rg;
        }
    }

}