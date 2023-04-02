using UnityEngine;
using Unity.Entities;

namespace GameWorld.Asteroid
{
    public class AsteroidSizeAuthoring : MonoBehaviour
    {
        [Header("This is for in-game, to know what stage of spawned asteroid we are at.")]
        public float defaultSize = 1.0f;
        public float minSize = 0.25f;
        public float currentSize = 1.0f;
        public uint childrenToSpawn = 2;
        public class AsteroidSizeBaker : Baker<AsteroidSizeAuthoring>
        {
            public override void Bake(AsteroidSizeAuthoring authoring)
            {
                AddComponent<AsteroidSizeComponent>(new AsteroidSizeComponent{
                    defaultSize = authoring.defaultSize, 
                    currentSize = authoring.currentSize,
                    minSize = authoring.minSize,
                    childrenToSpawn = authoring.childrenToSpawn
                });
            }
        }
    }

    
}