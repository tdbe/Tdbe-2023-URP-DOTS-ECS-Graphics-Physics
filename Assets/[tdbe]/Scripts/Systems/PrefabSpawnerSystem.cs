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

            new SpawnerJob
            {
                ecb = ecb
            }.Schedule(spawnerEQG);
        
            state.Enabled = false;
        }
    }

    [BurstCompile]
    public partial struct SpawnerJob:IJobEntity
    {
        public EntityCommandBuffer ecb;
        private void Execute(in PrefabSpawnerComponent spawnerComp)
        {
            //var spawnerCompArr = spawnerEQG.ToEntityArray(Allocator.Temp);
            for(uint i = 0; i < spawnerComp.spawnNumber; i++){
                
                Entity prefabInstance = ecb.Instantiate(spawnerComp.prefab);
                
                ecb.AddComponent<Unity.Transforms.Parent>(prefabInstance, new Unity.Transforms.Parent{ 
                    Value = spawnerComp.prefabParent
                });

                ecb.SetComponent<BoundsTagComponent>(prefabInstance, new BoundsTagComponent{
                    boundsID = i
                });

            }
            ecb.DestroyEntity(spawnerComp.prefab);     
            //spawnerCompArr.Dispose();
        }
    }
}
