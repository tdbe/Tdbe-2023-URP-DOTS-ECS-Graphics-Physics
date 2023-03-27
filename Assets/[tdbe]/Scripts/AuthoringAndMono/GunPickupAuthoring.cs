using UnityEngine;
using Unity.Entities;

namespace GameWorld.Pickups
{
    public class GunPickupAuthoring : MonoBehaviour
    {
        public GameObject playerGunPrefab;// which has GunPickupPlayerTag on it
        public class GunPickupBaker : Baker<GunPickupAuthoring>
        {
            public override void Bake(GunPickupAuthoring authoring)
            {
                AddComponent<GunPickupTag>(new GunPickupTag{
                   playerGunPrefab = GetEntity(authoring.playerGunPrefab),
                });
            }
        }
    }
    
}