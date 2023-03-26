using UnityEngine;
using Unity.Entities;

namespace GameWorld.Asteroid
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public class PlayerBaker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                AddComponent<PlayerComponent>(new PlayerComponent{
                   
                });
            }
        }
    }

    
}