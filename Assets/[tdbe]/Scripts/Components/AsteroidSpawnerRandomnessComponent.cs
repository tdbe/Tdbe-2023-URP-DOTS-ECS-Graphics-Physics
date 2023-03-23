using Unity.Entities;
using Unity.Mathematics;

namespace World.Asteroid
{
    public struct AsteroidSpawnerRandomnessComponent : IComponentData
    {
        public Random randomValue;
    }
}
