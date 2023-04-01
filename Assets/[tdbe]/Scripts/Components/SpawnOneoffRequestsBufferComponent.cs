using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace GameWorld
{
    // When something like a collision event happens that should trigger a spawn, 
    // the spawn trigger is appended to this buffer.
    public struct SpawnOneoffRequestsBufferComponent : IBufferElementData 
    {
        public float3 positionRequest;
        public uint countRequest;
        public float scaleRequest;
        public float defaultScale;
    }
}
