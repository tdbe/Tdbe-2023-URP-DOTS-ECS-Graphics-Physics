using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

using Unity.Physics;
//using Unity.Physics.Extensions;

namespace GameWorld.Asteroid
{

    public class AsteroidSpawnerVRUpdateGroup:ComponentSystemGroup
    {
        public AsteroidSpawnerVRUpdateGroup()
        {
            // NOTE: Unity.Entities.RateUtils.VariableRateManager.MinUpdateRateMS
            RateManager = new RateUtils.VariableRateManager(16, true);
        }

        public void SetRateManager(uint ms, bool pushToWorld){
            RateManager = new RateUtils.VariableRateManager(ms, pushToWorld);
            RateManager.Timestep = 0.015f;
        }
    }

    [UpdateInGroup(typeof(AsteroidSpawnerVRUpdateGroup))]
    [BurstCompile]
    public partial struct AsteroidSpawnerSystem : ISystem
    {
        private EntityQuery m_asteroidsGroup;
        private EntityQuery m_boundsGroup;

        // Need to set variable rate from other systems
        public void SetNewRate(ref SystemState state)
        {
            // var ecb = new EntityCommandBuffer(Allocator.Temp);
            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            Entity stateCompEnt = SystemAPI.GetSingletonEntity<AsteroidSpawnerStateComponent>();
            uint rate = SystemAPI.GetComponent<VariableRateComponent>(stateCompEnt).currentSpawnRate_ms;
            var asvrUpdateGroup = state.World.GetExistingSystemManaged<AsteroidSpawnerVRUpdateGroup>();
            asvrUpdateGroup.SetRateManager(rate, true);
        }

        public void SetNewState(ref SystemState state, AsteroidSpawnerStateComponent.State newState)
        {
            // var ecb = new EntityCommandBuffer(Allocator.Temp);
            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            Entity stateCompEnt = SystemAPI.GetSingletonEntity<AsteroidSpawnerStateComponent>();
            //prevState = SystemAPI.GetSingleton<AsteroidSpawnerStateComponent>().state;
            ecb.SetComponent<AsteroidSpawnerStateComponent>(
                stateCompEnt,
                new AsteroidSpawnerStateComponent{
                    state = newState
                });
        }
        

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RandomedSpawningComponent>();
            state.RequireForUpdate<PrefabAndParentBufferComponent>();
            state.RequireForUpdate<RandomnessComponent>();
            state.RequireForUpdate<AsteroidSpawnerStateComponent>();

            m_asteroidsGroup = state.GetEntityQuery(ComponentType.ReadOnly<AsteroidSizeComponent>());

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
                                    AsteroidSpawnerStateComponent.State spawnerState, int existingCount)
        {
            RandomSpawnedSetupAspect spawnAspect = SystemAPI.GetAspectRW<RandomSpawnedSetupAspect>(stateCompEnt);
            uint spawnAmount = 0;
            float3 targetAreaBL = float3.zero;
            float3 targetAreaTR = float3.zero;
            
            if(spawnerState == AsteroidSpawnerStateComponent.State.InitialSpawn_oneoff)
            {
                spawnAmount = spawnAspect.initialNumber;
                GetCorners2(ref state, m_boundsGroup.ToEntityArray(Allocator.Temp), out targetAreaBL, out targetAreaTR);
            }
            else if(spawnerState == AsteroidSpawnerStateComponent.State.InGameSpawn)
            {
                spawnAmount = 1;
                GetCorners2(ref state, m_boundsGroup.ToEntityArray(Allocator.Temp), out targetAreaBL, out targetAreaTR);
            }
            else if(spawnerState == AsteroidSpawnerStateComponent.State.TargetedSpawn_oneoff)
            {
                // TODO:
                // - the physics target mode needs the collision system to write what needs to be spawned where.
                // the collision system needs to write to a oneoffTargetedSpawnQueue because some gun might hit 2 things at once

                //TODO: COLLISION POINT location, then this function with this oneoff state once per point in oneoffTargetedSpawnQueue
                // as a queue, of what was hit: position and size
                //spawnAmount = 2;
                //targetArea = ( , ); -- a tight area th size of the dead asteroid

                /*
                oneoffTargetedSpawnQueue{
                    - size of dead asteroid
                    - dead asteroid position
                    - colision position (maybe)
                    - dead asteroid velocity and angular velocity
                }   
                */
            }
            else if(spawnerState == AsteroidSpawnerStateComponent.State.Inactive){
                return;
            }

            var rga = SystemAPI.GetComponent<RandomnessComponent>(stateCompEnt).randomGeneratorArr;
            var prefabsAndParents = SystemAPI.GetBuffer<PrefabAndParentBufferComponent>(stateCompEnt);
            var jhandle = new AsteroidSpawnerJob
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
            //ecb.DestroyEntity(asteroidSpawnAspect.asteroidPrefab);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // I want the game to constantly spawn new asteroids.
            // Using variable rate this way is kind of a hack but I want to handle all asteroid spawning from one system,
            // and wanted to quickly play with variable rate instead of creating a timer job

            AsteroidSpawnerStateComponent spawnerState = SystemAPI.GetSingleton<AsteroidSpawnerStateComponent>();
         
            // Ways to handle game state: Tags, SharedComponents, Component values (this case), ComponentSystemGroup as "state".
            // This spawner system needs to run all the time (at a certain rate, unless game is paused), and otherwise it can be made `state.Enabled = false;`.
            if(spawnerState.state == AsteroidSpawnerStateComponent.State.InitialSpawn_oneoff)
            {
                Entity stateCompEnt = SystemAPI.GetSingletonEntity<AsteroidSpawnerStateComponent>();
                var rateComponent = SystemAPI.GetComponent<VariableRateComponent>(stateCompEnt);
                
                if(!rateComponent.refreshSystemRateRequest && SystemAPI.Time.ElapsedTime - rateComponent.lastUpdateRateTime >= Time.deltaTime)
                {
                    Debug.Log("[AsteroidSpawner][InitialSpawn] spawning. ");

                    int existingCount = m_asteroidsGroup.CalculateEntityCount();
                    SpawnCapComponent spawnCap = SystemAPI.GetComponent<SpawnCapComponent>(stateCompEnt);
                    
                    if(existingCount < spawnCap.maxNumber)
                    {
                        var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                        DoSpawnOnMap(ref state, ref ecb, ref stateCompEnt, spawnerState.state, existingCount);
                        
                        {
                        // revert to AsteroidSpawnerStateComponent.State.InGameSpawn
                        SetNewState(ref state, AsteroidSpawnerStateComponent.State.InGameSpawn);
                        rateComponent.currentSpawnRate_ms = rateComponent.inGameSpawnRate_ms; 
                        rateComponent.refreshSystemRateRequest = true;
                        ecb.SetComponent<VariableRateComponent>(stateCompEnt, rateComponent);
                        }
                    }
                }
            }
            else if(spawnerState.state == AsteroidSpawnerStateComponent.State.InGameSpawn)
            {
                Entity stateCompEnt = SystemAPI.GetSingletonEntity<AsteroidSpawnerStateComponent>();
                var rateComponent = SystemAPI.GetComponent<VariableRateComponent>(stateCompEnt);
                
                if(!rateComponent.refreshSystemRateRequest && SystemAPI.Time.ElapsedTime - rateComponent.lastUpdateRateTime >= Time.deltaTime)
                {
                    int existingCount = m_asteroidsGroup.CalculateEntityCount();
                    var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                    DoSpawnOnMap(ref state, ref ecb, ref stateCompEnt, spawnerState.state, existingCount);
                }
                
            }
            else if(spawnerState.state == AsteroidSpawnerStateComponent.State.TargetedSpawn_oneoff)
            {
                Entity stateCompEnt = SystemAPI.GetSingletonEntity<AsteroidSpawnerStateComponent>();
                var rateComponent = SystemAPI.GetComponent<VariableRateComponent>(stateCompEnt);
                
                if(!rateComponent.refreshSystemRateRequest && SystemAPI.Time.ElapsedTime - rateComponent.lastUpdateRateTime >= Time.deltaTime)
                {
                    var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                    int existingCount = m_asteroidsGroup.CalculateEntityCount();
                    DoSpawnOnMap(ref state, ref ecb, ref stateCompEnt, spawnerState.state, existingCount);

                    {
                    // revert to AsteroidSpawnerStateComponent.State.InGameSpawn
                    SetNewState(ref state, AsteroidSpawnerStateComponent.State.InGameSpawn);
                    rateComponent.currentSpawnRate_ms = rateComponent.inGameSpawnRate_ms; 
                    rateComponent.refreshSystemRateRequest = true;
                    ecb.SetComponent<VariableRateComponent>(stateCompEnt, rateComponent);
                    }
                }
                
            }
        }
    }

    // if you really wanted this job could be shared (e.g. asteroids, pickups, ufos). TODO: spawn these for loops in parallel instead
    [BurstCompile]
    public partial struct AsteroidSpawnerJob:IJobEntity
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

        // TODO: maybe do parallel spawning, like this: ecb.parallelwriter, a pre-spawned query of entities to send to this parallel thread, and here add components to each in parallel
        [BurstCompile]
        private void Execute(in RandomSpawnedSetupAspect asteroidSpawnAspect, in SpawnCapComponent spawnCap, in AsteroidSpawnerStateComponent assctag)
        {
            Unity.Mathematics.Random rg = rga[thri];
            for(uint i = 0; i < spawnAmount; i++){
                if(i + existingCount >= spawnCap.maxNumber){
                    //Debug.LogWarning("[AsteroidSpawner][RandomSpawn] Reached max number of spawned asteroids! ");
                    break;
                }
                
                Entity ent = ecb.Instantiate(prefabsAndParents[0].prefab);

                // TODO: send a buffer of player worldpos to this random asteroid spawner thread here.
                // because we don't want to spawn a random asteroid, on the player :)

                ecb.SetComponent<LocalTransform>(ent, asteroidSpawnAspect.GetTransform(ref rg, targetAreaBL, targetAreaTR));
                // TODO: I'll use physicsVelocity applyImpulse when I get to the Player move forces.
                ecb.SetComponent<PhysicsVelocity>(ent, asteroidSpawnAspect.GetPhysicsVelocity(ref rg));

                //TODO: still wanted like this? change to authoring?
                ecb.AddSharedComponent<AsteroidStateSharedComponent>(ent, new AsteroidStateSharedComponent{ 
                        asteroidState = AsteroidStateSharedComponent.AsteroidState.Inactive
                });

                if(prefabsAndParents.Length>0){
                    ecb.AddComponent<Unity.Transforms.Parent>(ent, new Unity.Transforms.Parent{ 
                            Value = prefabsAndParents[0].parent
                    });
                }
            }
            rga[thri] = rg;
        }
    }

}
