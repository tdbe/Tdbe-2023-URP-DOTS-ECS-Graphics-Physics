using Unity.Entities;

namespace GameWorld
{
    public struct PrefabSpawnerComponent : IComponentData
    {
        // these could be made into an array or something, to spawn multiple types of prefabs
        // keep this pretty generic
        public Entity prefab;
        public Entity prefabParent;
        public uint spawnNumber;
    }
}
