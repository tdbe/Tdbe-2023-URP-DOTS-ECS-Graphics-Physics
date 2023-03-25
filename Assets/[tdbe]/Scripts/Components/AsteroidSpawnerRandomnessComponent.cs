using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace GameWorld.Asteroid
{
    public struct AsteroidSpawnerRandomnessComponent : IComponentData
    {
        // Note: I am aware these Randoms are not strictly thread safe because they write back
        // but, they are only in the asteroid assembly, and, I use one per computer thread,
        // and, I have only one system with one state at a time, that accesses this.
        // "Nested native containers are illegal in jobs", but if you just send this native array
        // directly to the job, you get the bonus of being able to write back to it so you save
        // the randomness state.
        public NativeArray<Unity.Mathematics.Random> randomGeneratorArr;
    }
}
