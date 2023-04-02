using Unity.Entities;

namespace GameWorld.Asteroid
{
    public struct AsteroidSizeComponent : IComponentData
    {
        public float defaultSize;    
        public float minSize;
        public float currentSize;    
        public uint childrenToSpawn;
        
    }
}
