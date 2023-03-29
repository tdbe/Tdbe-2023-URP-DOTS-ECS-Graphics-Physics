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
    [UpdateAfter(typeof(PlayerInputUpdateSystemBase))]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [BurstCompile]
    public partial struct  PlayerMovementSystem : ISystem
    {
        private EntityQuery playersEQG;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // at least one player in the scene
            state.RequireForUpdate<PlayerComponent>();
            state.RequireForUpdate<PlayerInputComponent>();
            
            //state.RequireForUpdate<GameSystem>();
            
            playersEQG = state.GetEntityQuery(ComponentType.ReadOnly<PlayerComponent>());
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
            state.Dependency = new MovementJob
            {
                deltaTime = Time.deltaTime,
                ecbp = ecb.AsParallelWriter(),
            }.Schedule(playersEQG, state.Dependency);
            state.CompleteDependency();
        }
    }

    [BurstCompile]
    public partial struct MovementJob : IJobEntity
    {
        [ReadOnly]
        public float deltaTime;
        public EntityCommandBuffer.ParallelWriter ecbp;

        public void Execute([ChunkIndexInQuery] int ciqi, in PlayerComponent plComp, ref PhysicsVelocity velocity, in Entity ent, in PlayerInputComponent input, in PhysicsMass mass, in LocalTransform ltrans, in WorldTransform wtrans )
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
               
        }
        
    }
}
