using UnityEngine;
using Unity.Entities;

namespace GameWorld.Pickups
{
    public class PickupTagAuthoring : MonoBehaviour
    {

        public class PickupTagBaker : Baker<PickupTagAuthoring>
        {
            public override void Bake(PickupTagAuthoring authoring)
            {
                AddComponent<PickupTag>(new PickupTag{
                });
            }
        }
    }
}