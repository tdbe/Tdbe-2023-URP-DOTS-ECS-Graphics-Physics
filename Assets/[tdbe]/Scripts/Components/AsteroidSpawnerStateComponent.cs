using Unity.Entities;
using Unity.Mathematics;

namespace GameWorld.Asteroid
{
    public struct AsteroidSpawnerStateComponent : IComponentData
    {
        public enum State{
            Inactive = 0,
            InitialSpawn = 1,
            InGameSpawn = 2
        }
        public State state;
    }
}
