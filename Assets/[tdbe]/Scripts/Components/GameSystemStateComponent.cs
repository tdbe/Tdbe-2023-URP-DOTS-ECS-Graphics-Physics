using Unity.Entities;

namespace World
{
    public struct GameSystemStateComponent : IComponentData
    {
        public enum State{
            Inactive = 0,
            Starting = 1,
            Running = 2,
            Ending = 3,
            Pausing = 4,
            Resuming = 5
        }
        public State state;
    } 
}
