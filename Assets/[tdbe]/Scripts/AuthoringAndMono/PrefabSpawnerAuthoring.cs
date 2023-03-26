using UnityEngine;
using Unity.Entities;

namespace GameWorld.Asteroid
{
    public class PrefabSpawnerAuthoring : MonoBehaviour
    {
        // these could be made into an array or something, to spawn multiple types of prefabs
        // keep this pretty generic
        public uint spawnNumber = 4;


        public class AsteroidPrefabBaker : Baker<PrefabSpawnerAuthoring>
        {
            public override void Bake(PrefabSpawnerAuthoring authoring)
            {
                AddComponent<PrefabSpawnerComponent>(new PrefabSpawnerComponent{
                    spawnNumber = authoring.spawnNumber
                });
            }
        }
    }
}