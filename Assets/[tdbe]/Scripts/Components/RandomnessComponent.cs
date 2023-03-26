using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace GameWorld
{
    public struct RandomnessComponent : IComponentData
    {
        // Note: I am aware these Randoms are not strictly thread safe because they write back
        // but, i use one per system, and, I support one per computer thread,
        // and, I have only one state and thread group at a time, that accesses this.
        
        // Also "Nested native containers are illegal in jobs", but if you just send this native array
        // directly to the job, you get the bonus of being able to write back to it so you save
        // the randomness state.
        public NativeArray<Unity.Mathematics.Random> randomGeneratorArr;
    }
}
