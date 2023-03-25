using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;


namespace GameWorld.Asteroid
{
    public class AsteroidSpawnerRandomnessAuthoring : MonoBehaviour
    {
        // Note: I am aware these Randoms are not strictly thread safe because they write back
        // but, they are only in the asteroid assembly, and, I use one per computer thread,
        // and, I have only one system with one state at a time, that accesses this.
        // "Nested native containers are illegal in jobs", but if you just send this native array
        // directly to the job, you get the bonus of being able to write back to it so you save
        // the randomness state.
        public uint randomSeed = 1;

        public class AsteroidSpawnerRandomnessBaker : Baker<AsteroidSpawnerRandomnessAuthoring>
        {
            public override void Bake(AsteroidSpawnerRandomnessAuthoring authoring)
            {
                Unity.Mathematics.Random rs = Unity.Mathematics.Random.CreateFromIndex(authoring.randomSeed);
                NativeArray<Unity.Mathematics.Random> rga = 
                    new NativeArray<Unity.Mathematics.Random>(System.Environment.ProcessorCount+1, Allocator.Persistent);
                
                for (int i = 0; i < rga.Length; i++)
                {
                    rga[i] = new Unity.Mathematics.Random((uint)rs.NextInt());
                }
                AddComponent<AsteroidSpawnerRandomnessComponent>(new AsteroidSpawnerRandomnessComponent{
                    randomGeneratorArr = rga
                });
            }
        }
    }

    
}