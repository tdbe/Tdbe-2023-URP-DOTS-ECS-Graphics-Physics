using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;

namespace GameWorld
{
    [BurstCompile]
    public partial struct PrefabSpawnerSystem : ISystem
    {
        private EntityQuery spawnerEQG;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PrefabSpawnerComponent>();
            state.RequireForUpdate<PrefabAndParentBufferComponent>();
            spawnerEQG = state.GetEntityQuery(ComponentType.ReadOnly<PrefabSpawnerComponent>());
            
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            PrefabSpawnerComponent spawnerComp = SystemAPI.GetSingleton<PrefabSpawnerComponent>();
            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            Debug.Log("[PrefabSpawner][InitialSpawn] spawning. ");

            Entity stateCompEnt = SystemAPI.GetSingletonEntity<PrefabSpawnerComponent>();
            var prefabsAndParents = SystemAPI.GetBuffer<PrefabAndParentBufferComponent>(stateCompEnt);
            new SpawnerJob
            {
                ecbp = ecb.AsParallelWriter(),
                prefabsAndParents = prefabsAndParents
            }.ScheduleParallel(spawnerEQG);
        
            state.Enabled = false;
        }
    }

    [BurstCompile]
    public partial struct SpawnerJob:IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecbp;
        [ReadOnly]
        public DynamicBuffer<PrefabAndParentBufferComponent> prefabsAndParents;
        [BurstCompile]
        private void Execute([ChunkIndexInQuery] int ciqi, in PrefabSpawnerComponent spawnerComp)
        {
            //var spawnerCompArr = spawnerEQG.ToEntityArray(Allocator.Temp);
            for(uint i = 0; i < spawnerComp.spawnNumber; i++){
                Entity prefabInstance = ecbp.Instantiate(ciqi, prefabsAndParents[0].prefab);
                
                ecbp.AddComponent<Unity.Transforms.Parent>(ciqi, prefabInstance, new Unity.Transforms.Parent{ 
                    Value = prefabsAndParents[0].parent
                });

                ecbp.SetComponent<BoundsTagComponent>(ciqi, prefabInstance, new BoundsTagComponent{
                    boundsID = i
                });
            }
            ecbp.DestroyEntity(ciqi, prefabsAndParents[0].prefab);     
            //spawnerCompArr.Dispose();
        }
    }
}
