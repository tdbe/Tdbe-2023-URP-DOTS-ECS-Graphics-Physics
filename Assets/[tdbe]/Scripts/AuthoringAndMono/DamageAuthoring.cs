using UnityEngine;
using Unity.Entities;

namespace GameWorld
{
    public class DamageAuthoring : MonoBehaviour
    {
        public float damagePerHit = 1;
        public class DamageBaker : Baker<DamageAuthoring>
        {
            public override void Bake(DamageAuthoring authoring)
            {
                AddComponent<DamageComponent>(new DamageComponent{
                    damagePerHit = authoring.damagePerHit
                });
            }
        }
    }

    
}