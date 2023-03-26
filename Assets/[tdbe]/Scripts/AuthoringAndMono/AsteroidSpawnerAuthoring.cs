using UnityEngine;
using Unity.Entities;

namespace GameWorld.Asteroid
{
    // we could have some sort of inheritance or generics here e.g. across ufo, asteroid, powerup spawning.
    // but conceptually spealking these are 3 categories of things that normally shouldn't have common links.
    public class AsteroidSpawnerAuthoring : MonoBehaviour
    {
        [Header("Have a hard upper limit of asteroid number \nfor some sort of cap assurance.")]
        public uint maxNumber = 1000; 
        [Header("Playing with the depth of the asteroid field.")]
        public float zRange = 2;
        public float decorativeRandomScaleBump = 0.1f;
        [Header("The number of asteroids on game start / reset.")]
        public uint initialNumber = 10; 
        [Header("Value for initial impulse force.")]
        public float initialImpulse = 0.05f; 
        [Space()]
        public uint inGameSpawnRate_ms = 1000;// TODO: if I decide to change this gradually, maybe move to own separate component

        public class AsteroidPrefabBaker : Baker<AsteroidSpawnerAuthoring>
        {
            public override void Bake(AsteroidSpawnerAuthoring authoring)
            {
                AddComponent<AsteroidSpawnerComponent>(new AsteroidSpawnerComponent{
                    maxNumber = authoring.maxNumber,
                    zRange = authoring.zRange,
                    decorativeRandomScaleBump = authoring.decorativeRandomScaleBump,
                    initialNumber = authoring.initialNumber,
                    initialImpulse = authoring.initialImpulse,
                    inGameSpawnRate_ms = authoring.inGameSpawnRate_ms
                });
            }
        }
    }
}