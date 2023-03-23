using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

namespace World.Asteroid
{
    //[UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateInGroup(typeof(VariableRateSimulationSystemGroup))]
    //[UpdateAfter(typeof())]
    [BurstCompile]
    public partial struct AsteroidSpawnerSystem : ISystem
    {
        EntityQuery m_asteroidsGroup;
        EntityQuery m_boundsGroup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AsteroidSpawnerComponent>();
            state.RequireForUpdate<AsteroidSpawnerRandomnessComponent>();
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

        // should have just stored corners entities :)
        [BurstCompile]
        private (float3, float3) GetCorners2(ref SystemState state, NativeArray<Entity> boundsEnts){
            float3 bl = float3.zero;
            float3 tr = float3.zero;
            foreach(Entity bndEnt in boundsEnts){
                uint id = SystemAPI.GetComponent<BoundsTagComponent>(bndEnt).boundsID;
                if(id == 0)
                    bl.y = SystemAPI.GetComponent<LocalTransform>(bndEnt).Position.y;
                else
                if(id == 1)
                    bl.x = SystemAPI.GetComponent<LocalTransform>(bndEnt).Position.x;
                else
                if(id == 2)
                    tr.y = SystemAPI.GetComponent<LocalTransform>(bndEnt).Position.y;
                else
                if(id == 3)
                    tr.x = SystemAPI.GetComponent<LocalTransform>(bndEnt).Position.x;
            }
            return (bl, tr);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity stateCompEnt = SystemAPI.GetSingletonEntity<AsteroidSpawnerComponent>();
            AsteroidSpawnerAspect asteroidSpawnAspect = SystemAPI.GetAspectRW<AsteroidSpawnerAspect>(stateCompEnt);
            AsteroidSpawnerStateComponent spawnerState = SystemAPI.GetComponent<AsteroidSpawnerStateComponent>(stateCompEnt);
         
            // Ways to handle game state: Tags, SharedComponents, Component values (this case), ComponentSystemGroup as "state".
            // This spawner system needs to run all the time (at a certain rate, unless game is paused), and otherwise it can be made `state.Enabled = false;`.
            if(spawnerState.state == AsteroidSpawnerStateComponent.State.InitialSpawn)
            {
                EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
                Debug.Log("[AsteroidSpawner][InitialSpawn] spawning. ");
                
                var corners2 = GetCorners2(ref state, m_boundsGroup.ToEntityArray(Allocator.Temp));

                int existingAsteroidCount = m_asteroidsGroup.CalculateEntityCount();
                for(int i = 0; i < asteroidSpawnAspect.initialNumber; i++){
                    if(i + existingAsteroidCount >= asteroidSpawnAspect.maxNumber){
                        Debug.LogWarning("[AsteroidSpawner][InitialSpawn] Reached max number of spawned asteroids! ");
                        break;
                    }
                    Entity asteroid = ecb.Instantiate(asteroidSpawnAspect.asteroidPrefab);
                    LocalTransform newAsteroidLTransform = asteroidSpawnAspect.GetAsteroidTransform(corners2);
                    ecb.SetComponent<LocalTransform>(asteroid, newAsteroidLTransform);

                    //TODO: still wanted like this? change to authoring?
                    ecb.AddSharedComponent<AsteroidStateSharedComponent>(asteroid, new AsteroidStateSharedComponent{ 
                            asteroidState = AsteroidStateSharedComponent.AsteroidState.Inactive
                    });

                    ecb.AddComponent<Unity.Transforms.Parent>(asteroid, new Unity.Transforms.Parent{ 
                            Value = asteroidSpawnAspect.asteroidParent
                    });

                }

                //ecb.DestroyEntity(asteroidSpawnAspect.asteroidPrefab)

                SystemAPI.SetComponent<AsteroidSpawnerStateComponent>(
                    stateCompEnt, 
                    new AsteroidSpawnerStateComponent{
                        state = AsteroidSpawnerStateComponent.State.Inactive
                    });

                ecb.Playback(state.EntityManager);
            }

            // existingAsteroidCount = m_asteroidsGroup.CalculateEntityCount();



            // TODO: change VariableRateSimulationSystemGroup

        }
    }
}
