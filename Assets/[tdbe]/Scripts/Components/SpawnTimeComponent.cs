using Unity.Entities;

namespace GameWorld.Players
{
    public struct SpawnTimeComponent : IComponentData
    {
        // to store when something was spawned. For score etc.
        public double spawnTime;
    }
}
