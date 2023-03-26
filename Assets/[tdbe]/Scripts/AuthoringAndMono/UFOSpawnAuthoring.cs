using UnityEngine;
using Unity.Entities;

namespace GameWorld.NPCs
{
    // we could have some sort of inheritance or generics here e.g. across ufo, asteroid, powerup spawning.
    // but conceptually spealking these are 3 categories of things that normally shouldn't have common links.
    public class UFOSpawnAuthoring : MonoBehaviour
    {
        [Header("Have a hard upper limit of ufo number.")]
        public uint maxNumber = 2; 
        [Header("Playing with the depth of the ufo field.")]
        public float zRange = 1;
        [Range(1,2)]
        public float decorativeRandomScaleBump = 2;
        [Space()]
        public uint inGameSpawnRate_ms = 1000;// TODO: if I decide to change this gradually, maybe move to own separate component

        public class UfoPrefabBaker : Baker<UFOSpawnAuthoring>
        {
            public override void Bake(UFOSpawnAuthoring authoring)
            {
                AddComponent<UFOSpawnComponent>(new UFOSpawnComponent{
                    maxNumber = authoring.maxNumber,
                    zRange = authoring.zRange,
                    decorativeRandomScaleBump = authoring.decorativeRandomScaleBump,
                    inGameSpawnRate_ms = authoring.inGameSpawnRate_ms
                });
            }
        }
    }

    
}