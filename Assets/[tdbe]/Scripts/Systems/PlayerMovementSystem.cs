using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;

namespace GameWorld.Players
{
    
    //[UpdateAfter(typeof(GameSystem))]
    //[UpdateAfter(typeof(PlayerInputUpdateSystemBase))]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [BurstCompile]
    public partial struct  PlayerMovementSystem : ISystem
    {
        private EntityQuery m_playersEQG;
        private EntityQuery m_boundsGroup;
        

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // at least one player in the scene
            state.RequireForUpdate<PlayerComponent>();
            state.RequireForUpdate<PlayerInputComponent>();
            //state.RequireForUpdate<PlayerMovementSystemTag>();
            state.RequireForUpdate<BoundsTagComponent>();// used for teleport spawning, not for movement
            state.RequireForUpdate<RandomedAttributesComponent>();
            
            m_playersEQG = state.GetEntityQuery(ComponentType.ReadOnly<PlayerComponent>());
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
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            float3 targetAreaBL = float3.zero;
            float3 targetAreaTR = float3.zero;
            GetCorners2(ref state, m_boundsGroup.ToEntityArray(Allocator.Temp), out targetAreaBL, out targetAreaTR);

            state.Dependency = new MovementJob
            {
                deltaTime = Time.deltaTime,
                ecbp = ecb.AsParallelWriter(),
                targetAreaBL = targetAreaBL,
                targetAreaTR = targetAreaTR
            }.ScheduleParallel(m_playersEQG, state.Dependency);
            state.Dependency.Complete();
        }
    }

    [BurstCompile]
    public partial struct MovementJob : IJobEntity
    {
        [ReadOnly]
        public float deltaTime;
        public EntityCommandBuffer.ParallelWriter ecbp;
        [ReadOnly]
        public float3 targetAreaBL;
        [ReadOnly]
        public float3 targetAreaTR;
        public void Execute([ChunkIndexInQuery] int ciqi, in PlayerComponent plComp, 
                            ref PhysicsVelocity velocity, in Entity ent, 
                            in RandomSpawnedSetupAspect spawnerAspect, 
                            in RandomnessSingleThreadedComponent rgc, 
                            in PlayerInputComponent input, in PhysicsMass mass, 
                            in LocalTransform ltrans, in WorldTransform wtrans )
        {
            float rotateSpeed = plComp.rotateSpeed;
            float moveSpeed = plComp.moveSpeed;
            // rotate
            if(input.Left.keyVal)
                velocity.ApplyAngularImpulse(mass, new float3(0, 0, +rotateSpeed * deltaTime));
            if(input.Right.keyVal)
                velocity.ApplyAngularImpulse(mass, new float3(0, 0, -rotateSpeed * deltaTime));
            // move
            if(input.Up.keyVal)
                velocity.ApplyImpulse(mass, ltrans.Position, ltrans.Rotation, ltrans.Up() * moveSpeed * deltaTime, wtrans.Position);
            
            if(input.Down.keyVal)
                velocity.ApplyImpulse(mass, ltrans.Position, ltrans.Rotation, -ltrans.Up() * moveSpeed * deltaTime, wtrans.Position);
        
            // teleport / hyperspace
            if(input.Teleport.keyVal){
                Unity.Mathematics.Random rg = rgc.randomGenerator;
                ecbp.SetComponent<LocalTransform>(ciqi, ent, spawnerAspect.GetTransform(ref rg, targetAreaBL, targetAreaTR));
                ecbp.SetComponent<RandomnessSingleThreadedComponent>(ciqi, ent, new RandomnessSingleThreadedComponent{
                    randomGenerator = rg
                });
            }
        }

    }

}
