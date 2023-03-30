using UnityEngine;
using Unity.Entities;

namespace GameWorld.Projectiles
{
    public class ShootingSysAuthoring : MonoBehaviour
    {

        public class AsteroidPrefabBaker : Baker<ShootingSysAuthoring>
        {
            public override void Bake(ShootingSysAuthoring authoring)
            {
                AddComponent<ShootingSysComponent>(new ShootingSysComponent{
                });
            }
        }
    }
}