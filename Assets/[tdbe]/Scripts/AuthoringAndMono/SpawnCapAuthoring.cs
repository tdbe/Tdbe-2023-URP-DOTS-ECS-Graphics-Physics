using UnityEngine;
using Unity.Entities;

namespace GameWorld
{
    public class SpawnCapAuthoring : MonoBehaviour
    {
        [Header("Have an upper limit of spawned things.")]
        public uint maxNumber = 1000; 
        
        public class SpawnerBaker : Baker<SpawnCapAuthoring>
        {
            public override void Bake(SpawnCapAuthoring authoring)
            {
                AddComponent<SpawnCapComponent>(new SpawnCapComponent{
                    maxNumber = authoring.maxNumber
                });
            }
        }
    }
}