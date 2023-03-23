using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;

namespace World
{
    [BurstCompile]
    public partial struct PrefabSpawnerSystem : ISystem
    {

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PrefabSpawnerComponent>();
            
            
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            PrefabSpawnerComponent spawnerComp = SystemAPI.GetSingleton<PrefabSpawnerComponent>();

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            Debug.Log("[PrefabSpawner][InitialSpawn] spawning. ");

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
            ecb.Playback(state.EntityManager);
        
        
            state.Enabled = false;
        }
    }
}
