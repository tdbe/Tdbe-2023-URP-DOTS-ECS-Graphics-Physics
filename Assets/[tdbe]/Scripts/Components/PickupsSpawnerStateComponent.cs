using Unity.Entities;

namespace GameWorld.Pickups
{
    public struct PickupsSpawnerStateComponent : IComponentData
    {
        public enum State{
        Inactive = 0,
        InGameSpawn = 1
        }
        public State state;
    
    }
}
