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
        }
    }

    // TODO: should try organizing in some writegroups and jobs and/or some externals here e.g. across ufo, asteroid, powerup spawning.
    // But also, conceptually spealking, in general gamedev, these are 3 categories of things that normally shouldn't have common links.
    [UpdateInGroup(typeof(AsteroidSpawnerVRUpdateGroup))]
    [BurstCompile]
    public partial struct AsteroidSpawnerSystem : ISystem
    {
        private EntityQuery m_asteroidsGroup;
        private EntityQuery m_boundsGroup;
        private AsteroidSpawnerStateComponent.State prevState;

        // Set from other systems, because variable rate. (would like to debate how good and bad of an idea this is)
        public void SetNewRateState(ref SystemState state, AsteroidSpawnerStateComponent.State newState)
        {
            // var ecb = new EntityCommandBuffer(Allocator.Temp);
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            SetNewRateState(ref state, ref ecb, newState);
            //ecb.Playback(state.EntityManager);
        }
        
        private void SetNewRateState(ref SystemState state, ref EntityCommandBuffer ecb, AsteroidSpawnerStateComponent.State newState)
        {
            // handle transition to next state, and also prepare the variable rate update rate
            
            Entity stateCompEnt = SystemAPI.GetSingletonEntity<AsteroidSpawnerComponent>();

            if(newState == AsteroidSpawnerStateComponent.State.InGameSpawn)
            {
                
                prevState = SystemAPI.GetSingleton<AsteroidSpawnerStateComponent>().state;
                ecb.SetComponent<AsteroidSpawnerStateComponent>(
                    stateCompEnt,
                    new AsteroidSpawnerStateComponent{
                        state = newState
                    });
                SetNewVariableRate(ref state, false);
            }
            else{

                prevState = SystemAPI.GetSingleton<AsteroidSpawnerStateComponent>().state;
                ecb.SetComponent<AsteroidSpawnerStateComponent>(
                    stateCompEnt,
                    new AsteroidSpawnerStateComponent{
                        state = newState
                    });
                SetNewVariableRate(ref state, true);
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AsteroidSpawnerComponent>();
            state.RequireForUpdate<PrefabAndParentBufferComponent>();
            state.RequireForUpdate<RandomnessComponent>();
            state.RequireForUpdate<AsteroidSpawnerStateComponent>();
            //state.RequireForUpdate<AsteroidSpawnerAspect>();

            m_asteroidsGroup = state.GetEntityQuery(ComponentType.ReadOnly<AsteroidSizeComponent>());

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
                                    AsteroidSpawnerStateComponent.State spawnerState, int existingCount)
        {
            AsteroidSpawnerAspect asteroidSpawnAspect = SystemAPI.GetAspectRW<AsteroidSpawnerAspect>(stateCompEnt);
            uint spawnAmount = 0;
            var targetArea = (float3.zero, float3.zero);
            
            if(spawnerState == AsteroidSpawnerStateComponent.State.InitialSpawn_oneoff)
            {
                spawnAmount = asteroidSpawnAspect.initialNumber;
                targetArea = GetCorners2(ref state, m_boundsGroup.ToEntityArray(Allocator.Temp));
            }
            else if(spawnerState == AsteroidSpawnerStateComponent.State.InGameSpawn)
            {
                spawnAmount = 1;
                targetArea = GetCorners2(ref state, m_boundsGroup.ToEntityArray(Allocator.Temp));
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
            new AsteroidSpawnerJob
            {
                ecb = ecb,
                spawnAmount = spawnAmount,
                targetArea = targetArea,
                existingCount = existingCount,
                rga = rga,
                prefabsAndParents = prefabsAndParents
            }.Schedule();

           
            //ecb.DestroyEntity(asteroidSpawnAspect.asteroidPrefab);
        }

        private void SetNewVariableRate(ref SystemState state, bool fast)
        {
            uint rate = 16;
            if(!fast)
            {
                rate = SystemAPI.GetSingleton<AsteroidSpawnerComponent>().inGameSpawnRate_ms;
            }
            /*
            // me figuring this out...
            var rateManager = new RateUtils.VariableRateManager(rate, true);
            //var variableRateSystem = state.World.GetExistingSystem<VariableRateSimulationSystemGroup>();
            var variableRateSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<VariableRateSimulationSystemGroup>();
            variableRateSystem.SetRateManagerCreateAllocator(rateManager);
            */
            // https://forum.unity.com/threads/enable-disable-systems-programmatically-in-1-0.1389423/#post-8760574


            // so this rate manager setting is only available as managed class, so no burst. 
            // Weird because a default variable rate manager is a burstable struct.
            // TODO: am I missing something here?
            var asvrUpdateGroup = state.World.GetExistingSystemManaged<AsteroidSpawnerVRUpdateGroup>();
            asvrUpdateGroup.SetRateManager(rate, true);
        }

        //[BurstCompile]
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
                Debug.Log("[AsteroidSpawner][InitialSpawn] spawning. ");
                Entity stateCompEnt = SystemAPI.GetSingletonEntity<AsteroidSpawnerComponent>();
                var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                
                int existingCount = m_asteroidsGroup.CalculateEntityCount();
                DoSpawnOnMap(ref state, ref ecb, ref stateCompEnt, spawnerState.state, existingCount);
                
                ecb = new EntityCommandBuffer(Allocator.Temp);
                SetNewRateState(ref state, ref ecb, AsteroidSpawnerStateComponent.State.InGameSpawn);
                ecb.Playback(state.EntityManager);
            }
            else if(spawnerState.state == AsteroidSpawnerStateComponent.State.InGameSpawn)
            {
                Entity stateCompEnt = SystemAPI.GetSingletonEntity<AsteroidSpawnerComponent>();
                var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                
                int existingCount = m_asteroidsGroup.CalculateEntityCount();
                DoSpawnOnMap(ref state, ref ecb, ref stateCompEnt, spawnerState.state, existingCount);
                
            }
            else if(spawnerState.state == AsteroidSpawnerStateComponent.State.TargetedSpawn_oneoff)
            {
                Entity stateCompEnt = SystemAPI.GetSingletonEntity<AsteroidSpawnerComponent>();
                var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

                int existingCount = m_asteroidsGroup.CalculateEntityCount();
                DoSpawnOnMap(ref state, ref ecb, ref stateCompEnt, spawnerState.state, existingCount);

                ecb = new EntityCommandBuffer(Allocator.Temp);
                SetNewRateState(ref state, ref ecb, prevState);
                ecb.Playback(state.EntityManager);
            }
        }
    }

    // if you really wanted, this could be shared with other spawn jobs (e.g. asteroids, pickups, ufos)
    //[BurstCompile]
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
        public (float3, float3) targetArea;
        [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
        public NativeArray<Unity.Mathematics.Random> rga;
        [ReadOnly]
        public DynamicBuffer<PrefabAndParentBufferComponent> prefabsAndParents;

        // TODO: can you do parallel spawning, like this?: ecb.parallelwriter, a pre-spawned query of entities to send to this parallel thread, and here add components to each in parallel
        //[BurstCompile]
        private void Execute(AsteroidSpawnerAspect asteroidSpawnAspect)
        {
            Unity.Mathematics.Random rg = rga[thri];
            for(uint i = 0; i < spawnAmount; i++){
                if(i + existingCount >= asteroidSpawnAspect.maxNumber){
                    //Debug.LogWarning("[AsteroidSpawner][RandomSpawn] Reached max number of spawned asteroids! ");
                    break;
                }
                
                Entity asteroid = ecb.Instantiate(prefabsAndParents[0].prefab);

                ecb.SetComponent<LocalTransform>(asteroid, asteroidSpawnAspect.GetAsteroidTransform(ref rg, targetArea));

                ecb.SetComponent<PhysicsVelocity>(asteroid, asteroidSpawnAspect.GetPhysicsVelocity(ref rg));

                //TODO: still wanted like this? change to authoring?
                ecb.AddSharedComponent<AsteroidStateSharedComponent>(asteroid, new AsteroidStateSharedComponent{ 
                        asteroidState = AsteroidStateSharedComponent.AsteroidState.Inactive
                });

                ecb.AddComponent<Unity.Transforms.Parent>(asteroid, new Unity.Transforms.Parent{ 
                        Value = prefabsAndParents[0].parent
                });
            }
            rga[thri] = rg;
        }
    }

}
