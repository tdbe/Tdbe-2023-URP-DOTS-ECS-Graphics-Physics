using Unity.Entities;

namespace GameWorld.Asteroid
{
    public struct AsteroidSpawnerComponent : IComponentData
    {
        public Entity asteroidPrefab;
        public Entity asteroidParent;
        public uint maxNumber; 
        public float zRange;
        public float decorativeRandomScaleBump;
        public uint initialNumber;
        public float initialImpulse;
        public uint inGameSpawnRate_ms;// TODO: if I decide to change this gradually, move to own separate component
    }
}
