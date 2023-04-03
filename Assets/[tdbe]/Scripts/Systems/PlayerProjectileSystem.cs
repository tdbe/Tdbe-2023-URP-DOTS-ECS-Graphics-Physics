using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;

using GameWorld.Pickups;
using GameWorld.Projectiles;

namespace GameWorld.Players
{
    // Shoot with current equipped projectile. Or request the destruction 
    // of currently equipped projectile if it ran out.  
    [UpdateAfter(typeof(GameSystem))]
    [UpdateAfter(typeof(PlayerInputUpdateSystemBase))]
    [BurstCompile]
    public partial struct  PlayerProjectileSystem : ISystem
    {
        private EntityQuery m_playersEQG;        

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // at least one player in the scene
            state.RequireForUpdate<PlayerComponent>();
            state.RequireForUpdate<PlayerInputComponent>();
            
            m_playersEQG = state.GetEntityQuery(ComponentType.ReadOnly<PlayerComponent>());
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new FireProjectileJob
            {
                currentTime = Time.timeAsDouble,
                deltaTime = Time.deltaTime,
                ecbp = ecb.AsParallelWriter(),
            }.ScheduleParallel(m_playersEQG, state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct FireProjectileJob : IJobEntity
    {
        [ReadOnly]
        public double currentTime;
        [ReadOnly]
        public float deltaTime;
        public EntityCommandBuffer.ParallelWriter ecbp;

        public void Execute([ChunkIndexInQuery] int ciqi, in PlayerComponent plComp, 
                            in EquippedProjectileDataComponent equippedProjectile,
                            in Entity ent, in PlayerInputComponent input,
                            in LocalTransform ltrans, in WorldTransform wtrans,
                            in PickupProjectileDataComponent defaultProjectileData )
        {
            // check if projectile equipment slot is expired
            if(equippedProjectile.pickupTimeToLive >-1)// -1 means infinite
            {
                if( currentTime >=
                    equippedProjectile.pickupTime + equippedProjectile.pickupTimeToLive
                ){
                    //revert to default builtin pickup for next timewe shoot
                    
                    // a custom memberwise copy would be nice :P
                    EquippedProjectileDataComponent newEquipped = new EquippedProjectileDataComponent{
                        active = true,
                        isCollisionInvulnerable = defaultProjectileData.isCollisionInvulnerable,
                        timeToLive = defaultProjectileData.timeToLive,
                        owner = ent,
                        prefab = defaultProjectileData.prefab,
                        speed = defaultProjectileData.speed,
                        scale = defaultProjectileData.scale,
                        pickupTime = currentTime,
                        pickupTimeToLive = -1
                    };
                    ecbp.SetComponent<EquippedProjectileDataComponent>(ciqi, ent, newEquipped);
                    ecbp.AddComponent<DeadDestroyTag>(ciqi, equippedProjectile.spawnedVisual);
                }
            }

            // shoot
            if(input.Shoot.keyVal && equippedProjectile.active && equippedProjectile.prefab != Entity.Null)
            {
                Entity spawnedProj = ecbp.Instantiate(ciqi, equippedProjectile.prefab);
                float3 spawnPos = ltrans.Position + ltrans.Up() * 0.5f * ltrans.Scale;
                ecbp.SetComponent<ProjectileComponent>(ciqi, spawnedProj, new ProjectileComponent{
                    owner = equippedProjectile.owner,
                    timeToLive = equippedProjectile.timeToLive
                });
                ecbp.SetComponent<LocalTransform>(ciqi, spawnedProj, new LocalTransform{
                    Position = spawnPos,
                    Rotation = ltrans.Rotation,
                    Scale = equippedProjectile.scale
                });
                if(equippedProjectile.isCollisionInvulnerable)
                {
                    // this means it does not get destroyed on collision
                    // in other words, health won't go down while invulnerable.
                    ecbp.AddComponent<InvulnerableTag>(ciqi, spawnedProj, new InvulnerableTag());
                }
                ecbp.AddComponent<HealthComponent>(ciqi, spawnedProj, new HealthComponent{
                    timeToLive = equippedProjectile.timeToLive,
                    spawnTime = currentTime,
                    currentHealth = 1,
                    maxHealth = 1
                });
                var mass = PhysicsMass.CreateDynamic(MassProperties.UnitSphere, 1);
                var velocity = new PhysicsVelocity();
                velocity.ApplyImpulse(mass, spawnPos, ltrans.Rotation, ltrans.Up() * equippedProjectile.speed * deltaTime, spawnPos);
                velocity.ApplyAngularImpulse(mass, new float3(0, + equippedProjectile.speed * deltaTime, 0));
                ecbp.AddComponent<PhysicsVelocity>(ciqi, spawnedProj, velocity);
                ecbp.AddComponent<PhysicsMass>(ciqi, spawnedProj, 
                    mass
                );
            }
        }

    }

}
