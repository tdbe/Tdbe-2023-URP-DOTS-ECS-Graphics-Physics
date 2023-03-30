using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace GameWorld
{
    public struct RandomnessSingleThreadedComponent : IComponentData
    {
        public Unity.Mathematics.Random randomGenerator;
    }
}
