using Unity.Entities;

namespace GameWorld.Players
{
    public struct PlayerComponent : IComponentData
    {
        public float moveSpeed;
        public float rotateSpeed;
    }
}
