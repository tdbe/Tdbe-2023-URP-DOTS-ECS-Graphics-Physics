using UnityEngine;
using Unity.Entities;

namespace GameWorld.Pickups
{
    // we could have some sort of inheritance or generics here e.g. across Pickups, asteroid, powerup spawning.
    // but conceptually spealking these are 3 categories of things that normally shouldn't have common links.
    public class VariableRateAuthoring : MonoBehaviour
    {
        public uint burstSpawnRate_ms = 16;// NOTE: Unity.Entities.RateUtils.VariableRateManager.MinUpdateRateMS
        public uint inGameSpawnRate_ms = 1000;
        public uint currentSpawnRate_ms = 16;

        public class PickupsPrefabBaker : Baker<VariableRateAuthoring>
        {
            public override void Bake(VariableRateAuthoring authoring)
            {
                AddComponent<VariableRateComponent>(new VariableRateComponent{
                    burstSpawnRate_ms = authoring.burstSpawnRate_ms,
                    inGameSpawnRate_ms = authoring.inGameSpawnRate_ms,
                    currentSpawnRate_ms = authoring.currentSpawnRate_ms
                });
            }
        }
    }

    
}