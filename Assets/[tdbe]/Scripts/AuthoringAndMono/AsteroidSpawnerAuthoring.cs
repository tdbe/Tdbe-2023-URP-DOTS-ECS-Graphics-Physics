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
        [Header("Playing with the depth of the asteroid field.")]
        public float zRange = 2;
        [Header("The number of asteroids on game start / reset.")]
        public uint initialNumber = 10; 
        [Header("Value for initial impulse force.")]
        public float initialImpulse = 0.05f; 

        public class AsteroidPrefabBaker : Baker<AsteroidSpawnerAuthoring>
        {
            public override void Bake(AsteroidSpawnerAuthoring authoring)
            {
                AddComponent<AsteroidSpawnerComponent>(new AsteroidSpawnerComponent{
                    asteroidPrefab = GetEntity(authoring.asteroidPrefab),
                    asteroidParent = GetEntity(authoring.asteroidParent),
                    maxNumber = authoring.maxNumber,
                    zRange = authoring.zRange,
                    initialNumber = authoring.initialNumber,
                    initialImpulse = authoring.initialImpulse
                });
            }
        }
    }
}