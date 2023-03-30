using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;


namespace GameWorld
{
    public class RandomnessSingleThreadedAuthoring : MonoBehaviour
    {
        public uint randomSeed = 1;

        public class RandomnessSingleThreadedBaker : Baker<RandomnessSingleThreadedAuthoring>
        {
            public override void Bake(RandomnessSingleThreadedAuthoring authoring)
            {
                AddComponent<RandomnessSingleThreadedComponent>(new RandomnessSingleThreadedComponent{
                    randomGenerator = Unity.Mathematics.Random.CreateFromIndex(authoring.randomSeed)
                });
            }
        }
    }

    
}