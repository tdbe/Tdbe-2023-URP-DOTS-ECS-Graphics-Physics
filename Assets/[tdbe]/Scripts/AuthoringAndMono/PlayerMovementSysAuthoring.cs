using UnityEngine;
using Unity.Entities;

namespace GameWorld.Players
{
    public class PlayerMovementSysAuthoring : MonoBehaviour
    {

        public class AsteroidPrefabBaker : Baker<PlayerMovementSysAuthoring>
        {
            public override void Bake(PlayerMovementSysAuthoring authoring)
            {
                AddComponent<PlayerMovementSystemTag>(new PlayerMovementSystemTag{
                });
            }
        }
    }
}