using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

using Unity.Physics;

namespace GameWorld.Asteroid
{
    public readonly partial struct AsteroidSpawnerAspect : IAspect
    {
        public readonly Entity entity;
        //private readonly TransformAspect m_transformAspect;
        [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
        private readonly RefRO<AsteroidSpawnerComponent> m_asteroidSpawnerComponent;
        public uint maxNumber => m_asteroidSpawnerComponent.ValueRO.maxNumber;
        public uint initialNumber => m_asteroidSpawnerComponent.ValueRO.initialNumber;

        
        private float3 CalcRandPos(ref Unity.Mathematics.Random rnd, (float3, float3) corners2)
        {
            corners2.Item1.z = -m_asteroidSpawnerComponent.ValueRO.zRange;
            corners2.Item2.z = m_asteroidSpawnerComponent.ValueRO.zRange;
            return rnd.NextFloat3(
                corners2.Item1, corners2.Item2
                );
        }

        private float3 CalcRandDir(ref Unity.Mathematics.Random rnd)
        {
            return rnd.NextFloat3(
                new float3(-1,-1,1), new float3(1,1,1)
                );
        }

        private float CalcRandScale(ref Unity.Mathematics.Random rnd){
            return rnd.NextFloat(
                1-m_asteroidSpawnerComponent.ValueRO.decorativeRandomScaleBump, 
                1+m_asteroidSpawnerComponent.ValueRO.decorativeRandomScaleBump);
            
        }

        public LocalTransform GetAsteroidTransform(ref Unity.Mathematics.Random rnd, (float3, float3) corners2)
        {
            return new LocalTransform 
            {
                Position = CalcRandPos(ref rnd, corners2),
                Rotation = quaternion.identity,
                Scale = CalcRandScale(ref rnd)
            };
        }

        // setting velocity directly and instantly, totally a good idea.
        // In my defense, it's on spawn, nothing collides with anything; I just want the ateroids to move a bit.
        public PhysicsVelocity GetPhysicsVelocity(ref Unity.Mathematics.Random rnd){
            float3 rando = CalcRandDir(ref rnd)*m_asteroidSpawnerComponent.ValueRO.initialImpulse;
            return new PhysicsVelocity{
                        Linear = new float3(rando.x, rando.y, 0),
                        Angular = rando
                    };
            // Aaand of course Rigidbody's freeze functionality is NOT authored into ECS.
            // So I imported the [external] Unity_JAC_shit folder for Joint Authoring Components...
        }


    }
}