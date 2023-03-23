using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

using World.Asteroid;

namespace World
{
    
    public partial struct GameSystem : ISystem
    {
        EntityQuery m_boundsGroup;
        bool m_cameraInitialized;

 
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSystemStateComponent>();
            state.RequireForUpdate<AsteroidSpawnerStateComponent>();
            state.RequireForUpdate<BoundsTagComponent>();

            m_boundsGroup = state.GetEntityQuery(ComponentType.ReadOnly<BoundsTagComponent>());
        }

        public void OnDestroy(ref SystemState state)
        {

        }

        void SetBounds(ref SystemState state, bool init){
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
                
                if(!m_cameraInitialized || HackyGlobals.WorldBounds._haveChanged){
                    m_cameraInitialized = true;
                    // move the bounds entities in this subscene

                    // this is from unity's own 1.0 samples but seems dumb because it can't use command buffers (existing sync points):
                    // foreach (var (lToW, tagc) in SystemAPI.Query<RefRO<LocalToWorld>, RefRO<BoundsTagComponent>>())
                    // not to mention local to world is no longer the way you change non-uniform scale

                    // doing it the ECB way with getEntityQuery:
                    var boundsEnts = m_boundsGroup.ToEntityArray(Allocator.Temp);
                    foreach(var bndEnt in boundsEnts)
                    {
                        
                        LocalTransform lt = SystemAPI.GetComponent<LocalTransform>(bndEnt);// TODO: is this struct getter efficient or should I use m_boundsGroup.ToComponentArray for potentially less cache misses?
                        BoundsTagComponent tagC = SystemAPI.GetComponent<BoundsTagComponent>(bndEnt);

                        float3 pos = HackyGlobals.WorldBounds._boundsPosAndScaleArrayBottomClockwise[tagC.boundsID].Item1;
                        pos.z = lt.Position.z;
                        
                        ecb.SetComponent<LocalTransform>(
                            bndEnt,
                            new LocalTransform{
                                    Position = HackyGlobals.WorldBounds._boundsPosAndScaleArrayBottomClockwise[tagC.boundsID].Item1,
                                    Rotation = quaternion.RotateZ(-1.570796f * tagC.boundsID),
                                    Scale = 1
                            });
                        // Hey folks, did you know there isn't any form of ecs collider that can have its size changed at runtime without replacing it with a new and unique (not shared) collider?
                        // sonofabitch! what is this, Unreal? https://forum.unity.com/threads/to-change-scale-and-collider-radius-in-ecs-physic.722462/#post-8145500
                        // well, screw it I just made the shared collider big enough to work in all situations.
                        
                        PostTransformScale new_pts = new PostTransformScale{
                                Value = float3x3.Scale( // this became quite annoying, although I get why
                                    HackyGlobals.WorldBounds._boundsPosAndScaleArrayBottomClockwise[tagC.boundsID].Item2.x,
                                    HackyGlobals.WorldBounds._boundsPosAndScaleArrayBottomClockwise[tagC.boundsID].Item2.y,
                                    HackyGlobals.WorldBounds._boundsPosAndScaleArrayBottomClockwise[tagC.boundsID].Item2.z
                                )
                            };
                        if(SystemAPI.HasComponent<PostTransformScale>(bndEnt)){
                            ecb.SetComponent<PostTransformScale>(
                                bndEnt,
                                new_pts
                                );
                        }
                        else{
                            ecb.AddComponent<PostTransformScale>(
                                bndEnt,
                                new_pts
                                );
                        }
                        
                    }
                }

                ecb.Playback(state.EntityManager);
        }

        //[BurstCompile] Note: reason for not bursting is `HackyGlobals.WorldBounds`
        public void OnUpdate(ref SystemState state)
        {
            Entity stateCompEnt = SystemAPI.GetSingletonEntity<GameSystemStateComponent>();
            GameSystemStateComponent gameState = SystemAPI.GetComponent<GameSystemStateComponent>(stateCompEnt);
         
            // Ways to handle game state: Tags, SharedComponents, Component values (this case), ComponentSystemGroup as "state".
            // This is the main governing game system, so I made a GameSystemStateComponent.
            if(gameState.state == GameSystemStateComponent.State.Starting)
            {
                // TODO: make sure game is cleaned up on start. E.g. in case we're (re)starting from a previous game.
                // note that each system is also responsible for making sure it's not going out if its own bounds. 
                // Ie asteroid spawner doesn't spawn over limit, no matter what.

                SetBounds(ref state, true);

                SystemAPI.SetSingleton<AsteroidSpawnerStateComponent>(new AsteroidSpawnerStateComponent{
                    state = AsteroidSpawnerStateComponent.State.InitialSpawn
                });

                // transition to running
                SystemAPI.SetComponent<GameSystemStateComponent>(
                    stateCompEnt, new GameSystemStateComponent{
                        state = GameSystemStateComponent.State.Running
                    });
            }
            else if(gameState.state == GameSystemStateComponent.State.Running)
            {
                SetBounds(ref state, false);
            }
            else if(gameState.state == GameSystemStateComponent.State.Ending)
            {
                // TODO:
                // End Game stuff; Switch scene or show some larger gui with score etc.
                
                // transition to inactive
            }
            else if(gameState.state == GameSystemStateComponent.State.Pausing)
            {
                // TODO:
                // pause stuff
                
                // transition to inactive, or to some fancy arcade idle/demo screen or smth
            }
            else if(gameState.state == GameSystemStateComponent.State.Resuming)
            {
                // TODO:
                // resume everything
                
                // transition to Running
            }
            else if(gameState.state == GameSystemStateComponent.State.Inactive)
            {
                // game is inactive
                // do nothing, or show some fancy arcade idle/demo screen or smth
            }

            
        }
    }
}
