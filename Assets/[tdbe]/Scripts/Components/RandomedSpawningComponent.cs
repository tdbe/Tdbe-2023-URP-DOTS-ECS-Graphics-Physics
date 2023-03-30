using Unity.Entities;

namespace GameWorld
{
    public struct RandomedSpawningComponent : IComponentData
    {
        public float zRange;
        public float randScaleMin;
        public float randScaleMax;
        public float scaleBump;
        public bool doCoinTossOnScaleBump;
        public uint initialNumber;
        public float initialImpulse;
    }
}
