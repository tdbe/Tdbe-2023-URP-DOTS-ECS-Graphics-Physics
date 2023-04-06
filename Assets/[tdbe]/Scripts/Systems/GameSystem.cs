using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

using GameWorld.Asteroid;
using GameWorld.NPCs;
using GameWorld.Pickups;
using GameWorld.Players;

namespace GameWorld
{
    [UpdateAfter(typeof(SimpleSpawnerSystem))]
    public partial struct GameSystem : ISystem
    {
        private EntityQuery m_boundsGroup;
        private bool m_cameraInitialized;

 
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSystemStateComponent>();
            state.RequireForUpdate<AsteroidSpawnerStateComponent>();
            state.RequireForUpdate<AsteroidVRSpawnerTag>();
            state.RequireForUpdate<NPCSpawnerStateComponent>();
            state.RequireForUpdate<BoundsTagComponent>();

            m_boundsGroup = state.GetEntityQuery(ComponentType.ReadOnly<BoundsTagComponent>());
        }

        public void OnDestroy(ref SystemState state)
        {

        }

        void InitBounds(ref SystemState state, ref EntityCommandBuffer ecb){
            state.Dependency = new InitBoundsJob
            {
                ecb = ecb
            }.Schedule(m_boundsGroup, state.Dependency);
            state.Dependency.Complete();
        }

        void SetBounds(ref SystemState state, ref EntityCommandBuffer ecb, bool init)
        {
            if(!m_cameraInitialized || HackyGlobals.WorldBounds._haveChanged){
                m_cameraInitialized = true;

                // this idiomatic foreach from unity's own 1.0 samples seems bad for component changes because it can't use entity command buffers (existing sync points):
                // foreach (var (lToW, tagc) in SystemAPI.Query<RefRO<LocalToWorld>, RefRO<BoundsTagComponent>>())
                // not to mention local to world is no longer the way you change non-uniform scale

                // move the bounds entities in this subscene
                state.Dependency = new SetBoundsJob
                {
                    ecb = ecb
                }.Schedule(m_boundsGroup, state.Dependency);
                state.Dependency.Complete();
            }
        }

        //[BurstCompile] Note: reason for not bursting is mono side `HackyGlobals.WorldBounds. 
        // But this is the unity game management system, and a simple one, so I won't optimize this right now.
        public void OnUpdate(ref SystemState state)
        {
            Entity stateCompEnt = SystemAPI.GetSingletonEntity<GameSystemStateComponent>();
            GameSystemStateComponent gameState = SystemAPI.GetComponent<GameSystemStateComponent>(stateCompEnt);
         
            // Ways to handle game state: Tags, SharedComponents, Component values (this case), ComponentSystemGroup as "state".
            // This is the main governing game system, so I made a GameSystemStateComponent.
            if(gameState.state == GameSystemStateComponent.State.Starting)
            {
                // TODO: make sure game is cleaned up on start if we're (re)starting from a previous game.
                // note that each system is also responsible for making sure it's not going out if its own bounds. 
                // Ie asteroid spawner doesn't spawn over limit, no matter what.

                var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                InitBounds(ref state, ref ecb);
                
                // init update systems states/rates
                {
                    var entSingleton = SystemAPI.GetSingletonEntity<AsteroidVRSpawnerTag>();
                    var variRateComp = SystemAPI.GetComponent<VariableRateComponent>(entSingleton);
                    {
                        var sysHandle = World.DefaultGameObjectInjectionWorld.GetExistingSystem<AsteroidVRSpawnerSystem>();
                        var asteroidSpawner = World.DefaultGameObjectInjectionWorld.Unmanaged.GetUnsafeSystemRef<AsteroidVRSpawnerSystem>(sysHandle);
                        // I don't like doing this but variable rate groups are annoying...
                        asteroidSpawner.SetNewState(ref state, AsteroidSpawnerStateComponent.State.InitialSpawn);
                        asteroidSpawner.SetNewRate(ref state);
                        variRateComp.refreshSystemRateRequest = false;
                        variRateComp.lastUpdateRateTime = SystemAPI.Time.ElapsedTime;
                        ecb.SetComponent<VariableRateComponent>(entSingleton, variRateComp);
                    }
                }
                {
                    var entSingleton = SystemAPI.GetSingletonEntity<NPCSpawnerStateComponent>();
                    var variRateComp = SystemAPI.GetComponent<VariableRateComponent>(entSingleton);
                    {
                        var sysHandle = World.DefaultGameObjectInjectionWorld.GetExistingSystem<NPCSpawnerSystem>();
                        var npcSpawner = World.DefaultGameObjectInjectionWorld.Unmanaged.GetUnsafeSystemRef<NPCSpawnerSystem>(sysHandle);
                        // I don't like doing this but variable rate groups are annoying...
                        npcSpawner.SetNewRate(ref state);
                        npcSpawner.SetNewState(ref state, NPCSpawnerStateComponent.State.InGameSpawn);
                        variRateComp.refreshSystemRateRequest = false;
                        variRateComp.lastUpdateRateTime = SystemAPI.Time.ElapsedTime;
                        ecb.SetComponent<VariableRateComponent>(entSingleton, variRateComp);
                    }
                }
                {
                    var entSingleton = SystemAPI.GetSingletonEntity<PickupsSpawnerStateComponent>();
                    var variRateComp = SystemAPI.GetComponent<VariableRateComponent>(entSingleton);
                    {
                        var sysHandle = World.DefaultGameObjectInjectionWorld.GetExistingSystem<PickupsSpawnerSystem>();
                        var pickupsSpawner = World.DefaultGameObjectInjectionWorld.Unmanaged.GetUnsafeSystemRef<PickupsSpawnerSystem>(sysHandle);
                        // I don't like doing this but variable rate groups are annoying...
                        pickupsSpawner.SetNewRate(ref state);
                        pickupsSpawner.SetNewState(ref state, PickupsSpawnerStateComponent.State.InGameSpawn);
                        variRateComp.refreshSystemRateRequest = false;
                        variRateComp.lastUpdateRateTime = SystemAPI.Time.ElapsedTime;
                        ecb.SetComponent<VariableRateComponent>(entSingleton, variRateComp);
                    }
                }

                // transition to Running state
                ecb.SetComponent<GameSystemStateComponent>(
                    stateCompEnt,
                    new GameSystemStateComponent{
                        state = GameSystemStateComponent.State.Running
                    });
            }
            else if(gameState.state == GameSystemStateComponent.State.Running)
            {
                // TODO: game should end when:
                // - there is a moment when there are 0 asteroids and ufos on screen: |Stage Clear|
                // - you died / all players died: |You Died|

                var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                // update systems states/rates
                {
                    var entSingleton = SystemAPI.GetSingletonEntity<AsteroidVRSpawnerTag>();
                    var variRateComp = SystemAPI.GetComponent<VariableRateComponent>(entSingleton);
                    if(variRateComp.refreshSystemRateRequest)
                    {
                        var sysHandle = World.DefaultGameObjectInjectionWorld.GetExistingSystem<AsteroidVRSpawnerSystem>();
                        var asteroidSpawner = World.DefaultGameObjectInjectionWorld.Unmanaged.GetUnsafeSystemRef<AsteroidVRSpawnerSystem>(sysHandle);
                        //asteroidSpawner.SetNewState(ref state, AsteroidSpawnerStateComponent.State.InGameSpawn);
                        asteroidSpawner.SetNewRate(ref state);
                        variRateComp.refreshSystemRateRequest = false;
                        variRateComp.lastUpdateRateTime = SystemAPI.Time.ElapsedTime;
                        ecb.SetComponent<VariableRateComponent>(entSingleton, variRateComp);
                    }
                }
                {
                    var entSingleton = SystemAPI.GetSingletonEntity<NPCSpawnerStateComponent>();
                    var variRateComp = SystemAPI.GetComponent<VariableRateComponent>(entSingleton);
                    if(variRateComp.refreshSystemRateRequest)
                    {
                        var sysHandle = World.DefaultGameObjectInjectionWorld.GetExistingSystem<NPCSpawnerSystem>();
                        var npcSpawner = World.DefaultGameObjectInjectionWorld.Unmanaged.GetUnsafeSystemRef<NPCSpawnerSystem>(sysHandle);
                        //npcSpawner.SetNewState(ref state, NPCSpawnerStateComponent.State.InGameSpawn);
                        npcSpawner.SetNewRate(ref state);
                        variRateComp.refreshSystemRateRequest = false;
                        variRateComp.lastUpdateRateTime = SystemAPI.Time.ElapsedTime;
                        ecb.SetComponent<VariableRateComponent>(entSingleton, variRateComp);
                    }
                }
                {
                    var entSingleton = SystemAPI.GetSingletonEntity<PickupsSpawnerStateComponent>();
                    var variRateComp = SystemAPI.GetComponent<VariableRateComponent>(entSingleton);
                    if(variRateComp.refreshSystemRateRequest)
                    {
                        var sysHandle = World.DefaultGameObjectInjectionWorld.GetExistingSystem<PickupsSpawnerSystem>();
                        var pickupsSpawner = World.DefaultGameObjectInjectionWorld.Unmanaged.GetUnsafeSystemRef<PickupsSpawnerSystem>(sysHandle);
                        //pickupsSpawner.SetNewState(ref state, PickupsSpawnerStateComponent.State.InGameSpawn);
                        pickupsSpawner.SetNewRate(ref state);
                        variRateComp.refreshSystemRateRequest = false;
                        variRateComp.lastUpdateRateTime = SystemAPI.Time.ElapsedTime;
                        ecb.SetComponent<VariableRateComponent>(entSingleton, variRateComp);
                    }
                }

                ecb = new EntityCommandBuffer(Allocator.TempJob);
                SetBounds(ref state, ref ecb, false);
                ecb.Playback(state.EntityManager);
                ecb.Dispose();
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

    public partial struct InitBoundsJob:IJobEntity
    {
        public uint globalCount;
        public EntityCommandBuffer ecb;
        //[BurstCompile]
        private void Execute(in Entity bndEnt, in BoundsTagComponent tagC)
        {
            ecb.SetComponent<BoundsTagComponent>(bndEnt, new BoundsTagComponent{boundsID = globalCount});
            globalCount++;
        }
    }

    //[BurstCompile]
    // it's ok we don't burst here, window resize is not a realtime feature.
    public partial struct SetBoundsJob:IJobEntity
    {
        public EntityCommandBuffer ecb;
        //[BurstCompile]
        private void Execute(in Entity bndEnt, in DynamicBuffer<Child> children, in LocalTransform lt, in BoundsTagComponent tagC)
        {
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
            // sonofabitch! what is this, Unreal? :) https://forum.unity.com/threads/to-change-scale-and-collider-radius-in-ecs-physic.722462/#post-8145500
            // well, screw it I just made the shared collider big enough to work in all situations.
            float3 newScale = HackyGlobals.WorldBounds._boundsPosAndScaleArrayBottomClockwise[tagC.boundsID].Item2;
            PostTransformScale new_pts = new PostTransformScale{
                    Value = float3x3.Scale( // this became quite annoying, although I get why it needs to be post transform
                        newScale
                    )
                };
            ecb.AddComponent<PostTransformScale>(
                bndEnt,
                new_pts
                );
            ecb.AddComponent<PostTransformScale>(children[0].Value, 
                new_pts
            );
        }
    }
}
