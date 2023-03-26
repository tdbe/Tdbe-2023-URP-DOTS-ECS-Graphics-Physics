using Unity.Entities;

namespace GameWorld.NPCs
{
    public struct NPCSpawnerStateComponent : IComponentData
    {
        public enum State{
        Inactive = 0,
        InGameSpawn = 1
        }
        public State state;
    
    }
}
