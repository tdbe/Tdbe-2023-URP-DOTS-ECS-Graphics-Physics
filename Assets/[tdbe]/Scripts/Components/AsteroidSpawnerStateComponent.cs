using Unity.Entities;
using Unity.Mathematics;

namespace GameWorld.Asteroid
{
    public struct AsteroidSpawnerStateComponent : IComponentData
    {
        public enum State{
            Inactive = 0,
            InitialSpawn_oneoff = 1,
            InGameSpawn = 2,
            TargetedSpawn_oneoff = 3
        }
        public State state;
    }
}
