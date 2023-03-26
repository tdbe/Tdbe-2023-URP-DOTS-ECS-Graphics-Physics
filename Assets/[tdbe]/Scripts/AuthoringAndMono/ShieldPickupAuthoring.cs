using UnityEngine;
using Unity.Entities;

namespace GameWorld.Pickups
{
    public class ShieldPickupAuthoring : MonoBehaviour
    {
        public class ShieldPickupBaker : Baker<ShieldPickupAuthoring>
        {
            public override void Bake(ShieldPickupAuthoring authoring)
            {
                AddComponent<ShieldPickupTag>(new ShieldPickupTag{
                   
                });
            }
        }
    }

    
}