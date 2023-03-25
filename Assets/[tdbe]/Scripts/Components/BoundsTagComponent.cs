using Unity.Entities;

namespace GameWorld
{
    // this is a "tag component" hybrid; both a tag, and data for id within this tag.
    public struct BoundsTagComponent : IComponentData
    {
        // 4 bounds, 0 is bottom, clockwise.
        public uint boundsID;

    }
}
