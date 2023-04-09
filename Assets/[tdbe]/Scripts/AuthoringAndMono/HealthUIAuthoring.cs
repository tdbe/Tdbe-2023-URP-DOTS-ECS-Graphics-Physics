using UnityEngine;
using Unity.Entities;

namespace GameWorld.UI
{
    public class HealthUIAuthoring : MonoBehaviour
    {
        [Header("Health GUI that is shown next to a character.\nParented to the owning character.")]
        public GameObject healthUIEntity;
        public GameObject healthBarEntity;
        public float healthBarValueNormalized = 1;
        
        public class HealthUIBaker : Baker<HealthUIAuthoring>
        {
            public override void Bake(HealthUIAuthoring authoring)
            {
                AddComponent<HealthUIComponent>(new HealthUIComponent{
                    healthUIEntity = GetEntity(authoring.healthUIEntity),
                    healthBarEntity = GetEntity(authoring.healthBarEntity),
                    healthBarValueNormalized = authoring.healthBarValueNormalized
                });
            }
        }
    }

    
}