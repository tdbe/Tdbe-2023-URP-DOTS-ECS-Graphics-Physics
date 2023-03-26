using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

using Unity.Physics;

namespace GameWorld.Pickups
{
    public readonly partial struct PickupsSpawnerAspect : IAspect
    {
        public readonly Entity entity;
        //private readonly TransformAspect m_transformAspect;
        [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
        private readonly RefRO<PickupsSpawnerComponent> m_PickupsSpawnComponent;

        public uint maxNumber => m_PickupsSpawnComponent.ValueRO.maxNumber;


        
        private float3 CalcRandPos(ref Unity.Mathematics.Random rnd, (float3, float3) corners2)
        {
            corners2.Item1.z = -m_PickupsSpawnComponent.ValueRO.zRange;
            corners2.Item2.z = m_PickupsSpawnComponent.ValueRO.zRange;
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
            return math.max(1, m_PickupsSpawnComponent.ValueRO.decorativeRandomScaleBump * rnd.NextUInt(0, 2));
            
        }

        public LocalTransform GetPickupsTransform(ref Unity.Mathematics.Random rnd, (float3, float3) corners2)
        {
            return new LocalTransform 
            {
                Position = CalcRandPos(ref rnd, corners2),
                Rotation = quaternion.identity,
                Scale = CalcRandScale(ref rnd)
            };
        }
    }
}