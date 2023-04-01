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

    // 2 systems in this file: AsteroidVRSpawnerSystem and AsteroidSpawnerSystem.
    
    // Variable rate spawner system for asteroids. Shares "state" with AsteroidSpawnerSystem.
    // Each spawn tweaks the rate a bit for better pcg.
    [UpdateInGroup(typeof(AsteroidSpawnerVRUpdateGroup))]
    [BurstCompile]
    public partial struct AsteroidVRSpawnerSystem : ISystem
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
            
            if(spawnerState == AsteroidSpawnerStateComponent.State.InitialSpawn)
            {
                spawnAmount = spawnAspect.initialNumber;
                GetCorners2(ref state, m_boundsGroup.ToEntityArray(Allocator.Temp), out targetAreaBL, out targetAreaTR);
            }
            else if(spawnerState == AsteroidSpawnerStateComponent.State.InGameSpawn)
            {
                spawnAmount = 1;
                GetCorners2(ref state, m_boundsGroup.ToEntityArray(Allocator.Temp), out targetAreaBL, out targetAreaTR);
            }
            else //if(spawnerState == AsteroidSpawnerStateComponent.State.Inactive)
            {
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
            // Using a constantly changing variable rate this way is kind of a hack 
            // but I wanted to play with variable rate instead of creating a timer job

            AsteroidSpawnerStateComponent spawnerState = SystemAPI.GetSingleton<AsteroidSpawnerStateComponent>();
         
            // Ways to handle game state: Tags, SharedComponents, Component values (this case), ComponentSystemGroup as "state".
            // This spawner system needs to run all the time (at a certain rate, unless game is paused), and otherwise it can be made `state.Enabled = false;`.
            if(spawnerState.state == AsteroidSpawnerStateComponent.State.InitialSpawn)
            {
            }
            else if(spawnerState.state == AsteroidSpawnerStateComponent.State.InGameSpawn)
            {
                Entity stateCompEnt = SystemAPI.GetSingletonEntity<AsteroidSpawnerStateComponent>();
                var rateComponent = SystemAPI.GetComponent<VariableRateComponent>(stateCompEnt);
                
                if(!rateComponent.refreshSystemRateRequest)
                {
                    int existingCount = m_asteroidsGroup.CalculateEntityCount();
                    var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                    DoSpawnOnMap(ref state, ref ecb, ref stateCompEnt, spawnerState.state, existingCount);
                }
            }
        }
    }


    // 2 systems in this file: AsteroidVRSpawnerSystem and AsteroidSpawnerSystem.
    
    // Spawner system for asteroids. Shares "state" with AsteroidVRSpawnerSystem.
    // Monitors the SpawnOneoffRequestsBufferComponent and each frame clears it by
    // spawning asteroid prefab from PrefabAndParentBufferComponent just like the 
    // AsteroidVRSpawnerSystem, except in a small area: the locaiton  and size of 
    // the previously dead asteroid.
    [UpdateAfter(typeof(SimpleSpawnerSystem))]
    [BurstCompile]
    public partial struct AsteroidSpawnerSystem : ISystem
    {
        private EntityQuery m_asteroidsGroup;
        private EntityQuery m_boundsGroup;

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
            
            if(spawnerState == AsteroidSpawnerStateComponent.State.InitialSpawn)
            {
                spawnAmount = spawnAspect.initialNumber;
                GetCorners2(ref state, m_boundsGroup.ToEntityArray(Allocator.Temp), out targetAreaBL, out targetAreaTR);
            }
            else //if(spawnerState == AsteroidSpawnerStateComponent.State.Inactive)
            {
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
        private void DoTargetedSpawn(ref SystemState state, ref EntityCommandBuffer ecb, ref Entity stateCompEnt, 
                                    AsteroidSpawnerStateComponent.State spawnerState, int existingCount,
                                    DynamicBuffer<SpawnOneoffRequestsBufferComponent> spawnOneoffBuffer)
        {
            RandomSpawnedSetupAspect spawnAspect = SystemAPI.GetAspectRW<RandomSpawnedSetupAspect>(stateCompEnt);

            /*
            oneoffTargetedSpawnQueue{
                - size of dead asteroid
                - dead asteroid position
                - children count
                - colision position (maybe)
                - dead asteroid velocity and angular velocity (maybe)
            }   
            */
    
            var rga = SystemAPI.GetComponent<RandomnessComponent>(stateCompEnt).randomGeneratorArr;
            var prefabsAndParents = SystemAPI.GetBuffer<PrefabAndParentBufferComponent>(stateCompEnt);
            var jhandle = new AsteroidTargetedSpawnerJob
            {
                ecb = ecb,
                existingCount = existingCount,
                rga = rga,
                prefabsAndParents = prefabsAndParents,
                spawnOneoffBuffer = spawnOneoffBuffer
            }.Schedule(state.Dependency);

            jhandle.Complete();
            spawnOneoffBuffer.Clear();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            AsteroidSpawnerStateComponent spawnerState = SystemAPI.GetSingleton<AsteroidSpawnerStateComponent>();
         
            // Ways to handle game state: Tags, SharedComponents, Component values (this case), ComponentSystemGroup as "state".
            // This spawner system needs to run all the time (at a certain rate, unless game is paused), and otherwise it can be made `state.Enabled = false;`.
            if(spawnerState.state == AsteroidSpawnerStateComponent.State.InitialSpawn)
            {
                Entity stateCompEnt = SystemAPI.GetSingletonEntity<AsteroidSpawnerStateComponent>();

                int existingCount = m_asteroidsGroup.CalculateEntityCount();
                SpawnCapComponent spawnCap = SystemAPI.GetComponent<SpawnCapComponent>(stateCompEnt);
                
                if(existingCount < spawnCap.maxNumber)
                {
                    Debug.Log("[AsteroidSpawner][InitialSpawn] spawning. ");
                    var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                    DoSpawnOnMap(ref state, ref ecb, ref stateCompEnt, spawnerState.state, existingCount);
                    
                    {
                    // switch to AsteroidSpawnerStateComponent.State.InGameSpawn
                    SetNewState(ref state, AsteroidSpawnerStateComponent.State.InGameSpawn);
                    }
                }
                
            }
            else if(spawnerState.state == AsteroidSpawnerStateComponent.State.InGameSpawn)
            {
                Entity stateCompEnt = SystemAPI.GetSingletonEntity<AsteroidSpawnerStateComponent>();
                var spawnOneoffBuffer = SystemAPI.GetBuffer<SpawnOneoffRequestsBufferComponent>(stateCompEnt);
                if (!spawnOneoffBuffer.IsEmpty)
                {
                    var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                    int existingCount = m_asteroidsGroup.CalculateEntityCount();
                    DoTargetedSpawn(ref state, ref ecb, ref stateCompEnt, spawnerState.state, existingCount, spawnOneoffBuffer);
                }
                
            }
        }
    }

    // TODO: spawn these for loops in parallel instead
    // A generic spawning job over a random area + velocity, doesn't have to be used just by asteroid spawner..
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
        private void Execute(in RandomSpawnedSetupAspect spawnAspect, in SpawnCapComponent spawnCap, in AsteroidSpawnerStateComponent assctag)
        {
            Unity.Mathematics.Random rg = rga[thri];
            for(uint i = 0; i < spawnAmount; i++){
                if(i + existingCount >= spawnCap.maxNumber){
                    //Debug.LogWarning("[AsteroidSpawner][RandomSpawn] Reached max number of spawned asteroids! ");
                    break;
                }
                
                Entity ent = ecb.Instantiate(prefabsAndParents[0].prefab);

                // TODO: do a spherecast loop to make sure we're not spawning an asteroid on a player

                LocalTransform newTransform = spawnAspect.GetTransform(ref rg, targetAreaBL, targetAreaTR);
                ecb.SetComponent<LocalTransform>(ent, newTransform);
                // TODO: I'll use physicsVelocity applyImpulse when I get to the Player move forces.
                ecb.SetComponent<PhysicsVelocity>(ent, spawnAspect.GetPhysicsVelocity(ref rg));

                if(prefabsAndParents.Length>0){
                    ecb.AddComponent<Unity.Transforms.Parent>(ent, new Unity.Transforms.Parent{ 
                            Value = prefabsAndParents[0].parent
                    });
                }
            }
            rga[thri] = rg;
        }
    }


    // Fairly generic but only makes sense for asteroids.
    // Spawns countRequest children according to SpawnOneoffRequestsBufferComponent,
    // in a random area the size of scaleRequest, at positionRequest.
    [BurstCompile]
    public partial struct AsteroidTargetedSpawnerJob:IJobEntity
    {
        [Unity.Collections.LowLevel.Unsafe.NativeSetThreadIndex]
        private int thri;
        public EntityCommandBuffer ecb;
        [ReadOnly]
        public int existingCount;
        [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
        public NativeArray<Unity.Mathematics.Random> rga;
        [ReadOnly]
        public DynamicBuffer<PrefabAndParentBufferComponent> prefabsAndParents;
        [ReadOnly]
        public DynamicBuffer<SpawnOneoffRequestsBufferComponent> spawnOneoffBuffer;

        // TODO: maybe do parallel spawning
        [BurstCompile]
        private void Execute(in RandomSpawnedSetupAspect spawnAspect, in SpawnCapComponent spawnCap, in AsteroidSpawnerStateComponent assctag)
        {
            Unity.Mathematics.Random rg = rga[thri];
            int i = 0;
            foreach(var spawnReq in spawnOneoffBuffer)
            {
                if(i * spawnReq.countRequest + existingCount >= spawnCap.maxNumber){
                    //Debug.LogWarning("[AsteroidSpawner][RandomSpawn] Reached max number of spawned asteroids! ");
                    break;
                }

                float offset = -spawnReq.scaleRequest/2;
                for(int r = 0; r<spawnReq.countRequest; r++)
                {                
                    Entity ent = ecb.Instantiate(prefabsAndParents[0].prefab);

                    LocalTransform newTransform = spawnAspect.GetTransform(
                        ref rg, 
                        spawnReq.positionRequest + new float3(-offset, -offset, 0), 
                        spawnReq.positionRequest + new float3(offset, offset, 0)
                    );
                    newTransform.Scale = spawnReq.scaleRequest;
                    ecb.SetComponent<LocalTransform>(ent, newTransform);

                    // TODO: I'll use physicsVelocity applyImpulse when I get to the Player move forces.
                    ecb.SetComponent<PhysicsVelocity>(ent, spawnAspect.GetPhysicsVelocity(ref rg));

                    ecb.SetComponent<AsteroidSizeComponent>(ent, new AsteroidSizeComponent{
                        defaultSize = spawnReq.defaultScale,
                        currentSize = spawnReq.scaleRequest
                    });

                    if(prefabsAndParents.Length>0){
                        ecb.AddComponent<Unity.Transforms.Parent>(ent, new Unity.Transforms.Parent{ 
                                Value = prefabsAndParents[0].parent
                        });
                    }
                }
                i++;
            }
            rga[thri] = rg;
        }
    }

}
