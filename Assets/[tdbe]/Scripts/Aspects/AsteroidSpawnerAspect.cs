using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

using Unity.Physics;

namespace World.Asteroid
{
    public readonly partial struct AsteroidSpawnerAspect : IAspect
    {
        public readonly Entity entity;
        private readonly TransformAspect m_transformAspect;
        private readonly RefRO<AsteroidSpawnerComponent> m_asteroidSpawnerComponent;
        // TODO: Not necessary to pass the random component around and try make it work with multithreaded jobs. Can just pass a seed + time + entity id or something.
        private readonly RefRW<AsteroidSpawnerRandomnessComponent> m_asteroidSpawnerRandomnessComponent;

        public Entity asteroidParent => m_asteroidSpawnerComponent.ValueRO.asteroidParent;
        public Entity asteroidPrefab => m_asteroidSpawnerComponent.ValueRO.asteroidPrefab;
        public uint maxNumber => m_asteroidSpawnerComponent.ValueRO.maxNumber;
        public uint initialNumber => m_asteroidSpawnerComponent.ValueRO.initialNumber;

        
        private float3 CalcRandPos((float3, float3) corners2)
        {
            corners2.Item1.z = -m_asteroidSpawnerComponent.ValueRO.zRange;
            corners2.Item2.z = m_asteroidSpawnerComponent.ValueRO.zRange;
            return m_asteroidSpawnerRandomnessComponent.ValueRW.randomValue.NextFloat3(
                corners2.Item1, corners2.Item2
                );
        }

        private float3 CalcRandDir()
        {
            return m_asteroidSpawnerRandomnessComponent.ValueRW.randomValue.NextFloat3(new float3(-1,-1,1), new float3(1,1,1));
        }

        public LocalTransform GetAsteroidTransform((float3, float3) corners2)
        {
            return new LocalTransform 
            {
                Position = CalcRandPos(corners2),
                Rotation = quaternion.identity,
                Scale = 1
            };
        }

        // setting velocity directly and instantly, totally a good idea.
        // In my defense, it's on spawn, nothing collides with anything, and I just want the ateroids to move a bit.
        public PhysicsVelocity GetPhysicsVelocity(){
            float3 rando = CalcRandDir()*m_asteroidSpawnerComponent.ValueRO.initialImpulse;
            return new PhysicsVelocity{
                        Linear = new float3(rando.x, rando.y, 0),
                        Angular = rando
                    };
            // Aaand of course Rigidbody's freeze functionality is NOT authored into ECS.
            // So I imported the [external] Unity_JAC_shit folder for joint authoring components...
        }

        // TODO: add starting force and angular momentum and shit

    }
}