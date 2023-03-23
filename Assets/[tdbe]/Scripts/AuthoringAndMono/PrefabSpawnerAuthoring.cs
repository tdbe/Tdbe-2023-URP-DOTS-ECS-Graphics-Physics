using UnityEngine;
using Unity.Entities;

namespace World.Asteroid
{
    public class PrefabSpawnerAuthoring : MonoBehaviour
    {
        // these could be made into an array or something, to spawn multiple types of prefabs
        // keep this pretty generic
        public GameObject prefab;
        public GameObject prefabParent;
        public uint spawnNumber = 4;


        public class AsteroidPrefabBaker : Baker<PrefabSpawnerAuthoring>
        {
            public override void Bake(PrefabSpawnerAuthoring authoring)
            {
                AddComponent<PrefabSpawnerComponent>(new PrefabSpawnerComponent{
                    prefab = GetEntity(authoring.prefab),
                    prefabParent = GetEntity(authoring.prefabParent),
                    spawnNumber = authoring.spawnNumber
                });
            }
        }
    }
}