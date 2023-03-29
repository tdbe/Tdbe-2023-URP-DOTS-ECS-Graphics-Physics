using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;

using GameWorld.Players;
using Unity.Physics;

namespace GameWorld
{
    [BurstCompile]
    public partial struct SimpleSpawnerSystem : ISystem
    {
        private EntityQuery spawnerEQG;
        private EntityQuery spawnerPlayerEQG;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimpleSpawnerComponent>();
            state.RequireForUpdate<PrefabAndParentBufferComponent>();
            //spawnerEQG = state.GetEntityQuery(ComponentType.ReadOnly<SimpleSpawnerComponent>());
            spawnerEQG = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SimpleSpawnerComponent>()
                .WithAbsent<PlayerSpawnerTag>()
                );
            spawnerPlayerEQG = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SimpleSpawnerComponent, PlayerSpawnerTag>()
                );
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            Debug.Log("[SimpleSpawner][InitialSpawn] spawning. Bounds, Players, initial shield pickup.. ");

            //Entity stateCompEnt = SystemAPI.GetSingletonEntity<SimpleSpawnerComponent>();
            //var prefabsAndParents = SystemAPI.GetBuffer<PrefabAndParentBufferComponent>(stateCompEnt);
            state.Dependency = new SpawnerJob
            {
                ecbp = ecb.AsParallelWriter(),
            }.ScheduleParallel(spawnerEQG, state.Dependency);

            ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            new SpawnerJobPlayer
            {
                ecbp = ecb.AsParallelWriter(),
                time = SystemAPI.Time.ElapsedTime
            }.ScheduleParallel(spawnerPlayerEQG);
        
            state.Enabled = false;
        }
    }
    

    [BurstCompile]
    public partial struct SpawnerJob:IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecbp;
        [BurstCompile]
        private void Execute([ChunkIndexInQuery] int ciqi, in DynamicBuffer<PrefabAndParentBufferComponent> prefabsAndParents, in SimpleSpawnerComponent spawnerComp)
        {
            //var spawnerCompArr = spawnerEQG.ToEntityArray(Allocator.Temp);
            for(uint i = 0; i < spawnerComp.spawnNumber; i++){
                Entity prefabInstance = ecbp.Instantiate(ciqi, prefabsAndParents[0].prefab);

                if(prefabsAndParents.Length>0){
                    ecbp.AddComponent<Unity.Transforms.Parent>(ciqi, prefabInstance, new Unity.Transforms.Parent{ 
                        Value = prefabsAndParents[0].parent
                    });
                }
            }
            //ecbp.DestroyEntity(ciqi, prefabsAndParents[0].prefab);     
            //spawnerCompArr.Dispose();
        }
    }

    [BurstCompile]
    public partial struct SpawnerJobPlayer:IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecbp;
        [ReadOnly]
        public double time;
        
        [BurstCompile]
        private void Execute([ChunkIndexInQuery] int ciqi, in DynamicBuffer<PrefabAndParentBufferComponent> prefabsAndParents, in SimpleSpawnerComponent spawnerComp)
        {
            //var spawnerCompArr = spawnerEQG.ToEntityArray(Allocator.Temp);
            for(uint i = 0; i < spawnerComp.spawnNumber; i++){
                Entity prefabInstance = ecbp.Instantiate(ciqi, prefabsAndParents[0].prefab);

                ecbp.AddComponent<PhysicsVelocity>(ciqi, prefabInstance, new PhysicsVelocity());
                ecbp.AddComponent<PhysicsMass>(ciqi, prefabInstance, 
                    PhysicsMass.CreateDynamic(MassProperties.UnitSphere, 1)
                );
     
                ecbp.AddComponent<SpawnTimeComponent>(ciqi, prefabInstance, new SpawnTimeComponent{
                    spawnTime = time
                });

                if(prefabsAndParents.Length>0){
                    ecbp.AddComponent<Unity.Transforms.Parent>(ciqi, prefabInstance, new Unity.Transforms.Parent{ 
                        Value = prefabsAndParents[0].parent
                    });
                }
            }
            ecbp.DestroyEntity(ciqi, prefabsAndParents[0].prefab);     
            //spawnerCompArr.Dispose();
        }
    }
}
