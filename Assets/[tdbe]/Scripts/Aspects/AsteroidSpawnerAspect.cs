using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;


namespace World.Asteroid
{
    public readonly partial struct AsteroidSpawnerAspect : IAspect
    {
        public readonly Entity entity;
        private readonly TransformAspect m_transformAspect;
        private readonly RefRO<AsteroidSpawnerComponent> m_asteroidSpawnerComponent;
        private readonly RefRW<AsteroidSpawnerRandomnessComponent> m_asteroidSpawnerRandomnessComponent;

        public Entity asteroidParent => m_asteroidSpawnerComponent.ValueRO.asteroidParent;
        public Entity asteroidPrefab => m_asteroidSpawnerComponent.ValueRO.asteroidPrefab;
        public uint maxNumber => m_asteroidSpawnerComponent.ValueRO.maxNumber;
        public uint initialNumber => m_asteroidSpawnerComponent.ValueRO.initialNumber;

        
        private float3 CalcRandPos((float3, float3) corners2)
        {
            return m_asteroidSpawnerRandomnessComponent.ValueRW.randomValue.NextFloat3(corners2.Item1, corners2.Item2);
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

        // TODO: add starting force and angular momentum and shit

    }
}