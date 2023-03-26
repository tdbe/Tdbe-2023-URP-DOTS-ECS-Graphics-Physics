using UnityEngine;
using Unity.Entities;

namespace GameWorld.Pickups
{
    // we could have some sort of inheritance or generics here e.g. across Pickups, asteroid, powerup spawning.
    // but conceptually spealking these are 3 categories of things that normally shouldn't have common links.
    public class PickupsSpawnerAuthoring : MonoBehaviour
    {
        [Header("Have a hard upper limit of Pickups number.")]
        public uint maxNumber = 2; 
        [Header("Playing with the depth of the Pickups field.")]
        public float zRange = 1;
        [Range(1,2)]
        public float decorativeRandomScaleBump = 2;
        [Space()]
        public uint inGameSpawnRate_ms = 1000;// TODO: if I decide to change this gradually, maybe move to own separate component

        public class PickupsPrefabBaker : Baker<PickupsSpawnerAuthoring>
        {
            public override void Bake(PickupsSpawnerAuthoring authoring)
            {
                AddComponent<PickupsSpawnerComponent>(new PickupsSpawnerComponent{
                    maxNumber = authoring.maxNumber,
                    zRange = authoring.zRange,
                    decorativeRandomScaleBump = authoring.decorativeRandomScaleBump,
                    inGameSpawnRate_ms = authoring.inGameSpawnRate_ms
                });
            }
        }
    }

    
}