using UnityEngine;
using Unity.Entities;

namespace GameWorld
{
    public class RandomedSpawningAuthoring : MonoBehaviour
    {
        [Header("Maybe play with +- the depth of spawn.")]
        public float zRange = 0;
        [Header("Scale + rand(min, max).")]
        [Range(-2f,2)]
        public float randScaleMin = 0f;
        [Range(-2f,2)]
        public float randScaleMax = 0f;
        [Header("Scale add, which is optionally applied on a coin toss.")]
        public float scaleBump = 0f;
        [Header("Coin toss on whether ^ scale mods are applied.\n Off means always applied.")]
        public bool coinTossOnScaleMod;
        [Header("The number spawned on game start / reset.")]
        public uint initialNumber = 10; 
        [Header("Value for an initial impulse force.")]
        public float initialImpulse = 0.05f; 

        public class SpawnerBaker : Baker<RandomedSpawningAuthoring>
        {
            public override void Bake(RandomedSpawningAuthoring authoring)
            {
                AddComponent<RandomedSpawningComponent>(new RandomedSpawningComponent{
                    zRange = authoring.zRange,
                    randScaleMin = authoring.randScaleMin,
                    randScaleMax = authoring.randScaleMax,
                    scaleBump = authoring.scaleBump,
                    doCoinTossOnScaleBump = authoring.coinTossOnScaleMod,
                    initialNumber = authoring.initialNumber,
                    initialImpulse = authoring.initialImpulse
                });
            }
        }
    }
}