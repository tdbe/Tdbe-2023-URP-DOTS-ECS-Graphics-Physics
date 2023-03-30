using UnityEngine;
using Unity.Entities;

namespace GameWorld.Players
{
    public class PlayerSpawnerTagAuthoring : MonoBehaviour
    {

        public class AsteroidPrefabBaker : Baker<PlayerSpawnerTagAuthoring>
        {
            public override void Bake(PlayerSpawnerTagAuthoring authoring)
            {
                AddComponent<PlayerSpawnerTag>(new PlayerSpawnerTag{
                });
            }
        }
    }
}