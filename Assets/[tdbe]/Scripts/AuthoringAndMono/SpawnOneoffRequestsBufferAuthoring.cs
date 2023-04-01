using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace GameWorld
{
    public class SpawnOneoffRequestsBufferAuthoring : MonoBehaviour
    {
        [TextArea(1,3)]
        string info = "When something like a collision event happens that should trigger a spawn, the spawn trigger is appended to this buffer.";

        public class AsteroidPrefabBaker : Baker<SpawnOneoffRequestsBufferAuthoring>
        {
            public override void Bake(SpawnOneoffRequestsBufferAuthoring authoring)
            {
                AddBuffer<SpawnOneoffRequestsBufferComponent>();
            }
        }
    }

    
}