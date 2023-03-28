using UnityEngine;
using Unity.Entities;

namespace GameWorld.Asteroid
{
    public class SimpleSpawnerAuthoring : MonoBehaviour
    {
        // these could be made into an array or something, to spawn multiple types of prefabs
        // keep this pretty generic
        public uint spawnNumber = 4;


        public class AsteroidPrefabBaker : Baker<SimpleSpawnerAuthoring>
        {
            public override void Bake(SimpleSpawnerAuthoring authoring)
            {
                AddComponent<SimpleSpawnerComponent>(new SimpleSpawnerComponent{
                    spawnNumber = authoring.spawnNumber
                });
            }
        }
    }
}