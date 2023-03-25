using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

namespace GameWorld.Asteroid
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
                // we need this because it's unavailable by default, and it's the only way to change non uniform scale
                // PS this already changed to PostTransformMatrix -- which is lovely because it doesn't have "scale" or "uniform" in its name..
                // #discoverability
                AddComponent<PostTransformScale>(new PostTransformScale());
            }
        }
    }

    
}