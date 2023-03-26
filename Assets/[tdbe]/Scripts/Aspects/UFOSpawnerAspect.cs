using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

using Unity.Physics;

namespace GameWorld.NPCs
{
    public readonly partial struct UFOSpawnerAspect : IAspect
    {
        public readonly Entity entity;
        //private readonly TransformAspect m_transformAspect;
        [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
        private readonly RefRO<UFOSpawnComponent> m_UFOSpawnComponent;

        public uint maxNumber => m_UFOSpawnComponent.ValueRO.maxNumber;


        
        private float3 CalcRandPos(ref Unity.Mathematics.Random rnd, (float3, float3) corners2)
        {
            corners2.Item1.z = -m_UFOSpawnComponent.ValueRO.zRange;
            corners2.Item2.z = m_UFOSpawnComponent.ValueRO.zRange;
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
            return math.max(1, m_UFOSpawnComponent.ValueRO.decorativeRandomScaleBump * rnd.NextUInt(0, 2));
            
        }

        public LocalTransform GetUFOTransform(ref Unity.Mathematics.Random rnd, (float3, float3) corners2)
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