using UnityEngine;
using Unity.Entities;

namespace GameWorld.Asteroid
{
    public class AsteroidTargetedSpawnerTagAuthoring : MonoBehaviour
    {

        public class AsteroidTargetedSpawnerTagBaker : Baker<AsteroidTargetedSpawnerTagAuthoring>
        {
            public override void Bake(AsteroidTargetedSpawnerTagAuthoring authoring)
            {
                AddComponent<AsteroidTargetedSpawnerTag>(new AsteroidTargetedSpawnerTag{
                });
            }
        }
    }
}