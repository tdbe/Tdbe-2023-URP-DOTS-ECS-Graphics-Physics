using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;


namespace GameWorld
{
    public class RandomnessSingleThreadedAuthoring : MonoBehaviour
    {
        [Header("This is a relative offset, to make each \nrandomness system in the game unique.\nInternally there is another time based offset, \nto make each game unique.")]
        public uint randomSeed = 1;

        public class RandomnessSingleThreadedBaker : Baker<RandomnessSingleThreadedAuthoring>
        {
            public override void Bake(RandomnessSingleThreadedAuthoring authoring)
            {   
                // TODO: theoretically since this is the global game uniqueness random seed, you might want it calculated in one place, not every time you author a randomness component..
                long unixTimeMs = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
                uint randSeed2 = (uint)(unixTimeMs % 100000000000);
                
                AddComponent<RandomnessSingleThreadedComponent>(new RandomnessSingleThreadedComponent{
                    randomGenerator = Unity.Mathematics.Random.CreateFromIndex(
                        authoring.randomSeed + randSeed2
                    )
                });
            }
        }
    }

    
}