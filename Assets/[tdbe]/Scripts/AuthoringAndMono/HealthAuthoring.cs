using UnityEngine;
using Unity.Entities;

namespace GameWorld
{
    public class HealthAuthoring : MonoBehaviour
    {
        [TextArea(1,3)]
        public string info = "Stores health and spawn time and time to live. Used by game systems.";
        public class HealthBaker : Baker<HealthAuthoring>
        {
            public override void Bake(HealthAuthoring authoring)
            {
                AddComponent<HealthComponent>(new HealthComponent{
                });
            }
        }
    }

    
}