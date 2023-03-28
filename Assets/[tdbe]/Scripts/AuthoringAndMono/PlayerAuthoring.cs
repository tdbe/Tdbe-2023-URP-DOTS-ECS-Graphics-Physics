using UnityEngine;
using Unity.Entities;

namespace GameWorld.Players
{
    public class PlayerAuthoring : MonoBehaviour
    {
        [Header("Stores when player was spawned. For score etc.")]
        public double spawnTime;
        public class PlayerBaker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                AddComponent<PlayerComponent>(new PlayerComponent{
                   spawnTime = authoring.spawnTime
                });
            }
        }
    }

    
}