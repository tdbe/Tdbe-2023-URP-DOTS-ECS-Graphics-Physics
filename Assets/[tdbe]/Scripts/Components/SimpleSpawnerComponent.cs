using Unity.Entities;

namespace GameWorld
{
    public struct SimpleSpawnerComponent : IComponentData
    {
        // these could be made into an array or something, to spawn multiple types of prefabs
        // keep this pretty generic
        public uint spawnNumber;
    }
}
