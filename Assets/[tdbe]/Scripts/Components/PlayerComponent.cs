using Unity.Entities;

namespace GameWorld.Players
{
    public struct PlayerComponent : IComponentData
    {
        public double spawnTime;
        // isCollisionInvulnerable as tag 
    }
}
