using UnityEngine;
using Unity.Entities;

namespace GameWorld.Asteroid
{
    public class AsteroidVRSpawnerTagAuthoring : MonoBehaviour
    {

        public class AsteroidVRSpawnerTagBaker : Baker<AsteroidVRSpawnerTagAuthoring>
        {
            public override void Bake(AsteroidVRSpawnerTagAuthoring authoring)
            {
                AddComponent<AsteroidVRSpawnerTag>(new AsteroidVRSpawnerTag{
                });
            }
        }
    }
}