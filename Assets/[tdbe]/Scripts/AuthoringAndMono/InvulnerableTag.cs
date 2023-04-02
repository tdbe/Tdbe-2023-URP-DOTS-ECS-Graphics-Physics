using UnityEngine;
using Unity.Entities;

namespace GameWorld
{
    public class InvulnerableTagAuthoring : MonoBehaviour
    {

        public class InvulnerableTagBaker : Baker<InvulnerableTagAuthoring>
        {
            public override void Bake(InvulnerableTagAuthoring authoring)
            {
                AddComponent<InvulnerableTag>(new InvulnerableTag{
                });
            }
        }
    }
}