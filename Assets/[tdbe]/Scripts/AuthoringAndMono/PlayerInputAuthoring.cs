using UnityEngine;
using Unity.Entities;

namespace GameWorld.Players
{
    public class PlayerInputAuthoring : MonoBehaviour
    {
        public class PlayerBaker : Baker<PlayerInputAuthoring>
        {
            public override void Bake(PlayerInputAuthoring authoring)
            {
                AddComponent<PlayerInputComponent>(new PlayerInputComponent{
                   
                });
            }
        }
    }

    
}