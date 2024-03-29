using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

using Unity.Physics;
//using Unity.Physics.Extensions;

using Unity.Jobs;

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

    // 2 systems in this file: AsteroidVRSpawnerSystem and AsteroidTargetedSpawnerSystem.
    
    // Variable rate spawner system for asteroids. Shares "state" with AsteroidSpawnerSystem.
    // Each spawn tweaks the rate a bit for better pcg.
    //[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    [UpdateInGroup(typeof(AsteroidSpawnerVRUpdateGroup))]
    //[UpdateBefore(typeof(TransformSystemGroup))]
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
            
            Entity tagCompEnt = SystemAPI.GetSingletonEntity<AsteroidVRSpawnerTag>();
            uint rate = SystemAPI.GetComponent<VariableRateComponent>(tagCompEnt).currentSpawnRate_ms;
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
            state.RequireForUpdate<RandomedAttributesComponent>();
            state.RequireForUpdate<PrefabAndParentBufferComponent>();
            state.RequireForUpdate<RandomnessComponent>();
            //state.RequireForUpdate<AsteroidSpawnerStateComponent>();
            state.RequireForUpdate<AsteroidVRSpawnerTag>();
            
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
                return;
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
                ecbp = ecb.AsParallelWriter(),
                targetAreaBL = targetAreaBL,
                targetAreaTR = targetAreaTR,
                existingCount = existingCount,
                rga = rga,
                prefabsAndParents = prefabsAndParents,
                spawnCap = SystemAPI.GetComponent<SpawnCapComponent>(stateCompEnt),
                spawnAspect = SystemAPI.GetAspectRO<RandomSpawnedSetupAspect>(stateCompEnt)
            }.Schedule((int)spawnAmount, 1, state.Dependency);

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
                // queue switch to AsteroidSpawnerStateComponent.State.InGameSpawn
                SetNewState(ref state, AsteroidSpawnerStateComponent.State.InGameSpawn);
            }
            else if(spawnerState.state == AsteroidSpawnerStateComponent.State.InGameSpawn)
            {
                Entity stateCompEnt = SystemAPI.GetSingletonEntity<AsteroidVRSpawnerTag>();
                var rateComponent = SystemAPI.GetComponent<VariableRateComponent>(stateCompEnt);
                
                if(!rateComponent.refreshSystemRateRequest)
                {
                    int existingCount = m_asteroidsGroup.CalculateEntityCount();
                    var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
                    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                    DoSpawnOnMap(ref state, ref ecb, ref stateCompEnt, spawnerState.state, existingCount);
                }
            }
        }
    }


    // 2 systems in this file: AsteroidVRSpawnerSystem and AsteroidTargetedSpawnerSystem.
    
    // Spawner system for asteroids. Shares "state" with AsteroidVRSpawnerSystem.
    // Monitors the SpawnOneoffRequestsBufferComponent and each frame clears it by
    // spawning asteroid prefab from PrefabAndParentBufferComponent just like the 
    // AsteroidVRSpawnerSystem, except in a small area: the locaiton  and size of 
    // the previously dead asteroid.
    [UpdateAfter(typeof(SimpleSpawnerSystem))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(GameSystem))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [BurstCompile]
    public partial struct AsteroidTargetedSpawnerSystem : ISystem
    {
        private EntityQuery m_liveAsteroidsGroup;
        private EntityQuery m_dedAsteroidsGroup;
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
            state.RequireForUpdate<RandomedAttributesComponent>();
            state.RequireForUpdate<PrefabAndParentBufferComponent>();
            state.RequireForUpdate<RandomnessComponent>();
            //state.RequireForUpdate<AsteroidSpawnerStateComponent>();
            state.RequireForUpdate<AsteroidTargetedSpawnerTag>();

            m_liveAsteroidsGroup = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<AsteroidSizeComponent>()
                .WithNone<DeadDestroyTag>()
                );
            m_dedAsteroidsGroup = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<AsteroidSizeComponent, DeadDestroyTag>()
                );

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
                ecbp = ecb.AsParallelWriter(),
                targetAreaBL = targetAreaBL,
                targetAreaTR = targetAreaTR,
                existingCount = existingCount,
                rga = rga,
                prefabsAndParents = prefabsAndParents,
                spawnCap = SystemAPI.GetComponent<SpawnCapComponent>(stateCompEnt),
                spawnAspect = SystemAPI.GetAspectRO<RandomSpawnedSetupAspect>(stateCompEnt)

            }.Schedule((int)spawnAmount, 1, state.Dependency);

            jhandle.Complete();
            //ecb.DestroyEntity(asteroidSpawnAspect.asteroidPrefab);
        }

        [BurstCompile]
        private void DoTargetedSpawn(ref SystemState state, ref EntityCommandBuffer ecb, ref Entity sysEnt, 
                                    AsteroidSpawnerStateComponent.State spawnerState, int existingCount
                                    )
        {
            RandomSpawnedSetupAspect spawnAspect = SystemAPI.GetAspectRW<RandomSpawnedSetupAspect>(sysEnt);

            /*
            oneoffTargetedSpawnQueue{
                - size of dead asteroid
                - dead asteroid position
                - children count
                - colision position (maybe)
                - dead asteroid velocity and angular velocity (maybe)
            }   
            */
    
            var rga = SystemAPI.GetComponent<RandomnessComponent>(sysEnt).randomGeneratorArr;
            var prefabsAndParents = SystemAPI.GetBuffer<PrefabAndParentBufferComponent>(sysEnt);
            state.Dependency = new AsteroidTargetedSpawnerJob
            {
                ecbp = ecb.AsParallelWriter(),
                existingCount = existingCount,
                rga = rga,
                prefabsAndParents = prefabsAndParents,
                spawnCap = SystemAPI.GetComponent<SpawnCapComponent>(sysEnt)
            }.ScheduleParallel(m_dedAsteroidsGroup, state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            AsteroidSpawnerStateComponent spawnerState = SystemAPI.GetSingleton<AsteroidSpawnerStateComponent>();
         
            // Ways to handle game state: Tags, SharedComponents, Component values (this case), ComponentSystemGroup as "state".
            // This spawner system needs to run all the time (at a certain rate, unless game is paused), and otherwise it can be made `state.Enabled = false;`.
            if(spawnerState.state == AsteroidSpawnerStateComponent.State.InitialSpawn)
            {
                Entity stateCompEnt = SystemAPI.GetSingletonEntity<AsteroidTargetedSpawnerTag>();

                int existingCount = m_liveAsteroidsGroup.CalculateEntityCount();
                SpawnCapComponent spawnCap = SystemAPI.GetComponent<SpawnCapComponent>(stateCompEnt);
                
                if(existingCount < spawnCap.maxNumber)
                {
                    Debug.Log("[AsteroidSpawner][InitialSpawn] spawning. ");
                    var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
                    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                    DoSpawnOnMap(ref state, ref ecb, ref stateCompEnt, spawnerState.state, existingCount);
                    
                    {
                    // queue switch to AsteroidSpawnerStateComponent.State.InGameSpawn
                    SetNewState(ref state, AsteroidSpawnerStateComponent.State.InGameSpawn);
                    }
                }
                
            }
            else if(spawnerState.state == AsteroidSpawnerStateComponent.State.InGameSpawn)
            {
                Entity sysEnt = SystemAPI.GetSingletonEntity<AsteroidTargetedSpawnerTag>();
                
                var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
                var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                int existingCount = m_liveAsteroidsGroup.CalculateEntityCount();
                DoTargetedSpawn(ref state, ref ecb, ref sysEnt, spawnerState.state, existingCount);
            }
        }
    }

    // A generic spawning job over a random area + velocity, doesn't have to be used just by asteroid spawner.
    [BurstCompile]
    public partial struct AsteroidSpawnerJob:IJobParallelFor
    {
        [Unity.Collections.LowLevel.Unsafe.NativeSetThreadIndex]
        private int thri;
        public EntityCommandBuffer.ParallelWriter ecbp;
        [ReadOnly]
        public int existingCount;
        [ReadOnly]
        public float3 targetAreaBL;
        [ReadOnly]
        public float3 targetAreaTR;
        [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
        [Unity.Collections.LowLevel.Unsafe.NativeDisableContainerSafetyRestriction]
        public NativeArray<Unity.Mathematics.Random> rga;
        [ReadOnly]
        public DynamicBuffer<PrefabAndParentBufferComponent> prefabsAndParents;
        [ReadOnly]
        public RandomSpawnedSetupAspect spawnAspect;
        [ReadOnly]
        public SpawnCapComponent spawnCap;

        // IJobParallelFor
        [BurstCompile]
        public void Execute(int parfi)
        {
            Unity.Mathematics.Random rg = rga[thri];
            int i = parfi;
            {
                if(i + existingCount > spawnCap.maxNumber){
                    //Debug.LogWarning("[AsteroidSpawner][RandomSpawn] Reached max number of spawned asteroids! ");
                    return;
                }
                Entity ent = ecbp.Instantiate(parfi, prefabsAndParents[0].prefab);

                // TODO: do a spherecast loop to make sure we're not spawning an asteroid on a player
                LocalTransform newTransform = spawnAspect.GetTransform(ref rg, targetAreaBL, targetAreaTR);
                ecbp.SetComponent<LocalTransform>(parfi, ent, newTransform);
                // TODO: I'll use physicsVelocity applyImpulse when I get to the Player move forces.
                ecbp.SetComponent<PhysicsVelocity>(parfi, ent, spawnAspect.GetPhysicsVelocity(ref rg));

                if(prefabsAndParents.Length>0){
                    ecbp.AddComponent<Unity.Transforms.Parent>(parfi, ent, new Unity.Transforms.Parent{ 
                            Value = prefabsAndParents[0].parent
                    });
                }
            }
            rga[thri] = rg;
        }
    }


    // Fairly generic but only makes sense for asteroids.
    // Spawns children according to dead asteroid position and scale etc.
    // Still uses RandomSpawnedSetupAspect area, but the area is the size of the dead asteroid.
    [BurstCompile]
    public partial struct AsteroidTargetedSpawnerJob:IJobEntity
    {
        [Unity.Collections.LowLevel.Unsafe.NativeSetThreadIndex]
        private int thri;
        public EntityCommandBuffer.ParallelWriter ecbp;
        [ReadOnly]
        public int existingCount;
        [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
        [Unity.Collections.LowLevel.Unsafe.NativeDisableContainerSafetyRestriction]
        public NativeArray<Unity.Mathematics.Random> rga;
        [ReadOnly]
        public DynamicBuffer<PrefabAndParentBufferComponent> prefabsAndParents;
        [ReadOnly]
        public SpawnCapComponent spawnCap;

        [BurstCompile]
        private void Execute([ChunkIndexInQuery] int ciqi, Entity asteroidEnt, 
                            in RandomSpawnedSetupAspect spawnAspect, in AsteroidSizeComponent aSizeComp,
                            in DeadDestroyTag ded, in LocalToWorld ltow)
        {           // this is cool because we can cheaply process all destroyed asteroids in parallel.
            if(aSizeComp.childrenToSpawn + existingCount >= spawnCap.maxNumber){
                //Debug.LogWarning("[AsteroidSpawner][RandomSpawn] Reached max number of spawned asteroids! ");
                return;
            }
            // smallest asteroids die without any spawns
            if(aSizeComp.currentSize <= aSizeComp.minSize){
                return;
            }
            Unity.Mathematics.Random rg = rga[thri];

            float aRadius = aSizeComp.currentSize/2;
            float aRadCubed = aRadius*aRadius*aRadius;
            float pi43 = 4.1888f;// (4/3*PI)
            float aVol = pi43 * aRadCubed;
            float offset = -aRadius;
            for(int r = 0; r<aSizeComp.childrenToSpawn; r++)
            {                
                Entity ent = ecbp.Instantiate(ciqi, prefabsAndParents[0].prefab);

                LocalTransform newTransform = spawnAspect.GetTransform(
                    ref rg, 
                    ltow.Position + new float3(-offset, -offset, 0), 
                    ltow.Position + new float3(offset, offset, 0)
                );
                // actual volue based size calculation, so we can spawn, with correct size, more than 2 children if we want
                newTransform.Scale = (aVol/aSizeComp.childrenToSpawn)/pi43;
                newTransform.Scale = math.pow(newTransform.Scale, 1.0f/3.0f)*2;
                ecbp.SetComponent<LocalTransform>(ciqi, ent, newTransform);

                // TODO: I'll use physicsVelocity applyImpulse when I get to the Player move forces.
                ecbp.SetComponent<PhysicsVelocity>(ciqi, ent, spawnAspect.GetPhysicsVelocity(ref rg));

                ecbp.SetComponent<AsteroidSizeComponent>(ciqi, ent, new AsteroidSizeComponent{
                    defaultSize = aSizeComp.defaultSize,
                    currentSize = newTransform.Scale,
                    childrenToSpawn = newTransform.Scale <= aSizeComp.minSize? 0 : aSizeComp.childrenToSpawn,
                    minSize = aSizeComp.minSize
                });

                if(prefabsAndParents.Length>0){
                    ecbp.AddComponent<Unity.Transforms.Parent>(ciqi, ent, new Unity.Transforms.Parent{ 
                            Value = prefabsAndParents[0].parent
                    });
                }
            }
            rga[thri] = rg;
        }
    }

}
