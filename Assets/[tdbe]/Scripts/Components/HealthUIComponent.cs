using Unity.Entities;

namespace GameWorld.UI
{
    public struct HealthUIComponent : IComponentData
    {
        public Entity healthUIEntity;
        public Entity healthBarEntity;
        public float healthBarValueNormalized;
    }
}
