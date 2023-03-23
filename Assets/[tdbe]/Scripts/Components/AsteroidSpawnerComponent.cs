using Unity.Entities;

namespace World.Asteroid
{
    public struct AsteroidSpawnerComponent : IComponentData
    {
        public Entity asteroidPrefab;
        public Entity asteroidParent;
        public uint maxNumber; 
        public uint initialNumber;
    }
}
