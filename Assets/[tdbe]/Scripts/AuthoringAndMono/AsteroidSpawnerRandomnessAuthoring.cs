using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace World.Asteroid
{
    public class AsteroidSpawnerRandomnessAuthoring : MonoBehaviour
    {
       
        public uint randomSeed = 1;


        public class AsteroidSpawnerRandomnessBaker : Baker<AsteroidSpawnerRandomnessAuthoring>
        {
            public override void Bake(AsteroidSpawnerRandomnessAuthoring authoring)
            {
                AddComponent<AsteroidSpawnerRandomnessComponent>(new AsteroidSpawnerRandomnessComponent{
                    randomValue = Unity.Mathematics.Random.CreateFromIndex(authoring.randomSeed)
                });
            }
        }
    }

    
}