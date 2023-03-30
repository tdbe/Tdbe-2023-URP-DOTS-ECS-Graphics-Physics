using UnityEngine;
using Unity.Entities;

namespace GameWorld.Pickups
{
    public class EquippedShieldDataAuthoring : MonoBehaviour
    {
        [Header("This is the shield data that gets read by the system.")]
        public bool active = false;
        public GameObject owner;
        public GameObject activeVisual;
        public float timeToLive = 2000;
        
        public class EquippedShieldDataBaker : Baker<EquippedShieldDataAuthoring>
        {
            public override void Bake(EquippedShieldDataAuthoring authoring)
            {
                AddComponent<EquippedShieldDataComponent>(new EquippedShieldDataComponent{
                    active = authoring.active,
                    owner = GetEntity(authoring.owner),
                    activeVisual = GetEntity(authoring.activeVisual),
                    timeToLive = authoring.timeToLive
                });
            }
        }
    }

    
}