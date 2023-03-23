using Unity.Entities;

namespace World.Asteroid
{
    public struct AsteroidSizeComponent : IComponentData
    {
        public float defaultSize;    
        public float sizeMultiplier;    
    }
}
