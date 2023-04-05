using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;


namespace GameWorld
{
    public class RandomnessAuthoring : MonoBehaviour
    {
        // Note: I am aware these Randoms are not strictly thread safe because they write back
        // but, i use one per system, and, I support one per computer thread,
        // and, I have only one state and thread group at a time, that accesses this.
        
        // Also "Nested native containers are illegal in jobs", but if you just send this native array
        // directly to the job, you get the bonus of being able to write back to it so you save
        // the randomness state.
        [Header("Uses Unity.Math.Rand. The seed is a relative offset, \nto make each randomness system in the game unique.\nInternally there is another time based offset, \nto make each game unique.")]
        public uint randomSeed = 1;

        public class RandomnessBaker : Baker<RandomnessAuthoring>
        {
            public override void Bake(RandomnessAuthoring authoring)
            {
                // TODO: theoretically since this is the global game uniqueness random seed, you might want it calculated in one place, not every time you author a randomness component..
                long unixTimeMs = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
                uint randSeed2 = (uint)(unixTimeMs % 100000000000);
                
                Unity.Mathematics.Random rs = Unity.Mathematics.Random.CreateFromIndex(authoring.randomSeed+randSeed2);
                NativeArray<Unity.Mathematics.Random> rga = 
                    new NativeArray<Unity.Mathematics.Random>(
                        System.Environment.ProcessorCount*2, // I don't like having to guess the thread count
                        Allocator.Persistent);
                
                for (int i = 0; i < rga.Length; i++)
                {
                    rga[i] = new Unity.Mathematics.Random((uint)rs.NextInt());
                }
                AddComponent<RandomnessComponent>(new RandomnessComponent{
                    randomGeneratorArr = rga
                });
            }
        }
    }

    
}