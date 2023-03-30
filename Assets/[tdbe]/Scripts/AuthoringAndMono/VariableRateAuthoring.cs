using UnityEngine;
using Unity.Entities;

namespace GameWorld.Pickups
{
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