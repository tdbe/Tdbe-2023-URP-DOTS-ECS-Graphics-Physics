using Unity.Entities;

namespace GameWorld.Asteroid
{
    public struct AsteroidSizeComponent : IComponentData
    {
        public float defaultSize;    
        public float currentSize;    
    }
}
