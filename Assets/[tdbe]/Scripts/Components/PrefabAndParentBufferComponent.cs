using Unity.Entities;
using Unity.Collections;

namespace GameWorld
{

    public struct PrefabAndParentBufferComponent : IBufferElementData 
    {
        public Entity prefab;
        public Entity parent;
    }
}
