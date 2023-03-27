using UnityEngine;
using Unity.Entities;

namespace GameWorld
{
    // we could have some sort of inheritance or generics here e.g. across ufo, asteroid, powerup spawning.
    // but conceptually spealking these are 3 categories of things that normally shouldn't have common links.
    public class SpawnerAuthoring : MonoBehaviour
    {
        [Header("Have an upper limit of spawned things.")]
        public uint maxNumber = 1000; 
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
        public bool coinTossOnScaleMod = true;
        [Header("The number spawned on game start / reset.")]
        public uint initialNumber = 10; 
        [Header("Value for an initial impulse force.")]
        public float initialImpulse = 0.05f; 

        public class SpawnerBaker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                AddComponent<SpawnerComponent>(new SpawnerComponent{
                    maxNumber = authoring.maxNumber,
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