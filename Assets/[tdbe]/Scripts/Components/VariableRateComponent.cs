using Unity.Entities;

namespace GameWorld
{
    public struct VariableRateComponent : IComponentData
    {
        public uint burstSpawnRate_ms;
        public uint inGameSpawnRate_ms;
        public uint currentSpawnRate_ms;
        public bool refreshSystemRateRequest;
    }
}
