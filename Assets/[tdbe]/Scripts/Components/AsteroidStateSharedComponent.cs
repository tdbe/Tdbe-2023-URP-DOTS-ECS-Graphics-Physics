using Unity.Entities;

namespace World.Asteroid
{
    public struct AsteroidStateSharedComponent : ISharedComponentData
    {
        public enum AsteroidState{
            Inactive = 0, //Inactive: in pool. 
            Active = 1 // Active: in-game state.
        }

        public AsteroidState asteroidState;    
    }
}