using UnityEngine;
using Unity.Entities;

namespace GameWorld.Asteroid
{
    public class AsteroidSizeAuthoring : MonoBehaviour
    {
        [Header("Size is dictated by AsteroidSystem and settings, \nshould go: 1, 0.5, 0.25")]
        public float defaultSize = 1.0f;
        public float sizeMultiplier = 1.0f;

        public class AsteroidSizeBaker : Baker<AsteroidSizeAuthoring>
        {
            public override void Bake(AsteroidSizeAuthoring authoring)
            {
                AddComponent<AsteroidSizeComponent>(new AsteroidSizeComponent{
                    defaultSize = authoring.defaultSize, 
                    sizeMultiplier = authoring.sizeMultiplier
                });
            }
        }
    }

    
}