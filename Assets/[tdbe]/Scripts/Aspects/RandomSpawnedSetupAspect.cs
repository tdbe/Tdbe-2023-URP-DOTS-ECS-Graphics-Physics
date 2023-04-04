using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

using Unity.Physics;

namespace GameWorld
{
    public readonly partial struct RandomSpawnedSetupAspect : IAspect
    {
        public readonly Entity entity;
        //private readonly TransformAspect m_transformAspect;
        [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
        private readonly RefRO<RandomedAttributesComponent> m_spawnerAspectComponent;
        public uint initialNumber => m_spawnerAspectComponent.ValueRO.initialNumber;

        
        private float3 CalcRandPos(ref Unity.Mathematics.Random rnd, float3 cornerBL, float3 cornerTR)
        {
            cornerBL.z -= m_spawnerAspectComponent.ValueRO.zRange;
            cornerTR.z += m_spawnerAspectComponent.ValueRO.zRange;
            return rnd.NextFloat3(
                cornerBL, cornerTR
                );
        }

        private float3 CalcRandDir(ref Unity.Mathematics.Random rnd)
        {
            return rnd.NextFloat3(
                new float3(-1,-1,1), new float3(1,1,1)
                );
        }

        private float CalcRandScale(ref Unity.Mathematics.Random rnd){
            float coinTossMod = m_spawnerAspectComponent.ValueRO.scaleBump;
            if(m_spawnerAspectComponent.ValueRO.doCoinTossOnScaleBump){
                coinTossMod *= rnd.NextUInt(0, 2);
            }
            float rand = 0;
            if(m_spawnerAspectComponent.ValueRO.randScaleMin != m_spawnerAspectComponent.ValueRO.randScaleMax)
            {
                rand = rnd.NextFloat(
                    m_spawnerAspectComponent.ValueRO.randScaleMin, 
                    m_spawnerAspectComponent.ValueRO.randScaleMax);
            }
            return 1 + coinTossMod + rand;
        }

        public LocalTransform GetTransform(ref Unity.Mathematics.Random rnd, float3 cornerBL, float3 cornerTR)
        {
            return new LocalTransform 
            {
                Position = CalcRandPos(ref rnd, cornerBL, cornerTR),
                Rotation = quaternion.identity,
                Scale = CalcRandScale(ref rnd)
            };
        }

        // hacking velocity directly and instantly, totally a good idea.
        // I'll do it right when I get to the Player move forces.
        public PhysicsVelocity GetPhysicsVelocity(ref Unity.Mathematics.Random rnd){
            float3 rando = CalcRandDir(ref rnd)*m_spawnerAspectComponent.ValueRO.initialImpulse;
            return new PhysicsVelocity{
                        Linear = new float3(rando.x, rando.y, 0),
                        Angular = rando
                    };
            // Aaand of course Rigidbody's freeze functionality is NOT authored into ECS.
            // So I imported the [external] Unity_JAC_shit folder for Joint Authoring Components...
        }

        public PhysicsVelocity GetPhysicsVelocity(float3 dirPos, float3 dirRot){
            float3 dirP = dirPos * m_spawnerAspectComponent.ValueRO.initialImpulse;
            float3 dirR = dirRot * m_spawnerAspectComponent.ValueRO.initialImpulse;
            return new PhysicsVelocity{
                        Linear = dirP,
                        Angular = dirR
                    };
        }

    }
}