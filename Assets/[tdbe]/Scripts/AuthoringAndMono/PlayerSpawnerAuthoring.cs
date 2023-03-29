using UnityEngine;
using Unity.Entities;

namespace GameWorld.Players
{
    public class PlayerSpawnerAuthoring : MonoBehaviour
    {

        public class AsteroidPrefabBaker : Baker<PlayerSpawnerAuthoring>
        {
            public override void Bake(PlayerSpawnerAuthoring authoring)
            {
                AddComponent<PlayerSpawnerTag>(new PlayerSpawnerTag{
                });
            }
        }
    }
}