using Unity.Entities;

namespace GameWorld
{
    public struct VariableRateComponent : IComponentData
    {
        public uint burstSpawnRate_ms;
        public uint inGameSpawnRate_ms;
        public uint currentSpawnRate_ms;

        // TODO: these flags are me screwing around. There has to be a better way, but there are no docs yet.
        // Maybe IRateManager? No examples, no time to experiment for now.
        // Problem is, when you set the RateManager for a ComponentSystemGroup,
        // you automatically trigger an -instant- OnUpdate for that group. gg
        // https://forum.unity.com/search/23815189/?q=iratemanager&o=date&c[node]=823+147+641+422+425
        public bool refreshSystemRateRequest;
        public double lastUpdateRateTime;
    }
}
