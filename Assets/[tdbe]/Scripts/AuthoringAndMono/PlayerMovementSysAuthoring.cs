using UnityEngine;
using Unity.Entities;

namespace GameWorld.Players
{
    public class PlayerMovementAuthoring : MonoBehaviour
    {

        public class AsteroidPrefabBaker : Baker<PlayerMovementAuthoring>
        {
            public override void Bake(PlayerMovementAuthoring authoring)
            {
                AddComponent<PlayerMovementSystemTag>(new PlayerMovementSystemTag{
                });
            }
        }
    }
}