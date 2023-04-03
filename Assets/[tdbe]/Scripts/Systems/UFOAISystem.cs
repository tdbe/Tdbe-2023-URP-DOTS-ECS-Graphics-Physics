using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics;
using Unity.Physics.Systems;


using GameWorld.Players;
using GameWorld.NPCs;

namespace GameWorld.NPCs
{
    // This system runs for each UFO, and finds the closest player, 
    // and if within chaseRange, moves the ufo toward the player, 
    // also accounting for shortest distance vs distance via portals.
    // Otherwise the ufo patrols horizontally.
    [UpdateAfter(typeof(GameSystem))]
    public partial struct UFOAISystem : ISystem
    {
        private EntityQuery m_playersEQG;
        private EntityQuery m_UFOsEQG;
        //private EntityQuery m_boundsGroup;
        private bool m_cameraInitialized;

 
        public void OnCreate(ref SystemState state)
        {

            //state.RequireForUpdate<PlayerComponent>();
            state.RequireForUpdate<UFOComponent>();
            state.RequireForUpdate<BoundsTagComponent>();

            m_playersEQG = state.GetEntityQuery(ComponentType.ReadOnly<PlayerComponent>());
            m_UFOsEQG = state.GetEntityQuery(ComponentType.ReadOnly<UFOComponent>());
            //m_boundsGroup = state.GetEntityQuery(ComponentType.ReadOnly<BoundsTagComponent>());
        }

        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            // The smart thing to do here would be to have a job that takes all players
            // and inserts them into some kind of partitioning tree or hash map.
            // Then in the ufo move job, fetch the closest player to the ufo's position.

            // But since we have portals, and I don't have time,
            // I'll just quickly calculate portal-aware distances from each ufo to all players.
            // This is fine because we have few players.

            int i = 0;
            NativeArray<float3> playerPosArr = new NativeArray<float3>(m_playersEQG.CalculateEntityCount(), Allocator.TempJob);
            foreach (var lToW in SystemAPI.Query<RefRO<LocalToWorld>>().WithAll<PlayerComponent>())
            {
                playerPosArr[i] = lToW.ValueRO.Position;
                i++;
            }

            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            PhysicsWorld physWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            state.Dependency = new UFOChaseJob
            {
                deltaTime = Time.deltaTime,
                ecbp = ecb.AsParallelWriter(),
                playerPosArr = playerPosArr,
                physWorld = physWorld
            }.ScheduleParallel(m_UFOsEQG, state.Dependency);
            state.Dependency.Complete();
        }
    }

    [BurstCompile]
    public partial struct UFOChaseJob:IJobEntity
    {
        [ReadOnly]
        public float deltaTime;
        public EntityCommandBuffer.ParallelWriter ecbp;
        [ReadOnly]
        public NativeArray<float3> playerPosArr;
        [ReadOnly]
        public PhysicsWorld physWorld;

        enum layer
        {
            // TODO: but really would be nice to actually access the Layers defined in settings.
            WorldBounds = (1 << 9)
        };

        [BurstCompile]
        private void Execute([ChunkIndexInQuery] int ciqi, in LocalToWorld ufoLtoW, in LocalTransform ufoLtrans, in UFOComponent ufoC, in Entity ufoEnt)
        {
            float least_sqDistEucliOrPortal = float.MaxValue;
            float3 target_forLeastDist = ufoLtrans.Position;
            bool playersExist = false;
            // for now we don't care about any further knowledge than just, closest distance one or another.
            foreach(float3 playerPos in playerPosArr)
            {
                playersExist = true;
                float sqDistEuclid = math.distancesq(playerPos, ufoLtoW.Position);
                if(least_sqDistEucliOrPortal > sqDistEuclid)
                {
                    least_sqDistEucliOrPortal = sqDistEuclid;
                    target_forLeastDist = playerPos;
                }
                
                if(sqDistEuclid>ufoC.minChaseDist)
                {
                    float sqDistPortal = float.MaxValue;
                    float3 dirToPlayer = math.normalize(playerPos - ufoLtoW.Position);
                    RaycastInput raycastInput = new RaycastInput()
                    {
                        Start = ufoLtoW.Position,
                        End = -dirToPlayer * 100,
                        Filter = new CollisionFilter
                        {
                            BelongsTo = (uint)layer.WorldBounds,
                            CollidesWith = (uint)layer.WorldBounds,
                            GroupIndex = 0
                        }
                    };
                    if (physWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit hit))
                    {
                        float3 posOnBound = hit.Position;
                        float sqDistToBound = math.distancesq(posOnBound, ufoLtoW.Position);
                        float sqDistBoundToPlayer = math.distancesq(-posOnBound, playerPos);
                        sqDistPortal = sqDistToBound + sqDistBoundToPlayer;
                        if(least_sqDistEucliOrPortal > sqDistPortal)
                        {
                            least_sqDistEucliOrPortal = sqDistPortal;
                            // we heading to bound not to player
                            target_forLeastDist = posOnBound;
                        }
                    }
                }
            } 

            // move towards player or patrol
            {
                float totalDist = math.sqrt(least_sqDistEucliOrPortal);
                if(playersExist && totalDist <= ufoC.maxChaseDist && totalDist >= ufoC.minChaseDist)
                {
                    var newLtrans = new LocalTransform{
                        Position = ufoLtrans.Position,
                        Rotation = ufoLtrans.Rotation,
                        Scale = ufoLtrans.Scale
                    };
                    newLtrans.Position += deltaTime * ufoC.moveSpeed * math.normalize(target_forLeastDist - ufoLtoW.Position);
                    ecbp.SetComponent<LocalTransform>(ciqi, ufoEnt, newLtrans);
                }
                else{// "patrol state"
                    var newLtrans = new LocalTransform{
                        Position = ufoLtrans.Position,
                        Rotation = ufoLtrans.Rotation,
                        Scale = ufoLtrans.Scale
                    };
                    newLtrans.Position += deltaTime * ufoC.moveSpeed * newLtrans.Right();
                    ecbp.SetComponent<LocalTransform>(ciqi, ufoEnt, newLtrans);
                }
            }
        }
    }
}
