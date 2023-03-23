using UnityEngine;
using Unity.Entities;

namespace World.Asteroid
{
    // this is a "tag component" hybrid; both a tag, and data for id within this tag.
    public class BoundsTagAuthoring : MonoBehaviour
    {
        [Header("4 bounds: 0 is bottom, clockwise.")]
        public uint boundsID = 0;
        public class BoundsTagBaker : Baker<BoundsTagAuthoring>
        {
            public override void Bake(BoundsTagAuthoring authoring)
            {
                AddComponent<BoundsTagComponent>(new BoundsTagComponent{
                    boundsID = authoring.boundsID 
                });
            }
        }
    }

    
}