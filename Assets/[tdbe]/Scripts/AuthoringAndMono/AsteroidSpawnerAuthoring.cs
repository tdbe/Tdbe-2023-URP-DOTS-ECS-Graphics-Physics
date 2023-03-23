using UnityEngine;
using Unity.Entities;

namespace World.Asteroid
{
    public class AsteroidSpawnerAuthoring : MonoBehaviour
    {
        public GameObject asteroidPrefab;
        public GameObject asteroidParent;
        [Header("Have a hard upper limit of asteroid number \nfor some sort of memory cap assurance.")]
        public uint maxNumber = 1000; 
        [Header("The number of asteroids on game start / reset.")]
        public uint initialNumber = 10; 
        

        public class AsteroidPrefabBaker : Baker<AsteroidSpawnerAuthoring>
        {
            public override void Bake(AsteroidSpawnerAuthoring authoring)
            {
                AddComponent<AsteroidSpawnerComponent>(new AsteroidSpawnerComponent{
                    asteroidPrefab = GetEntity(authoring.asteroidPrefab),
                    asteroidParent = GetEntity(authoring.asteroidParent),
                    maxNumber = authoring.maxNumber,
                    initialNumber = authoring.initialNumber
                });
            }
        }
    }
}