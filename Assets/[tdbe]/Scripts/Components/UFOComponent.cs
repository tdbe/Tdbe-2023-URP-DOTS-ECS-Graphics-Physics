using Unity.Entities;

namespace GameWorld.NPCs
{
    public struct UFOComponent : IComponentData
    {
        public float moveSpeed;
        public float rotateSpeed;
        public float maxChaseDist;
        public float minChaseDist;
    }
}
