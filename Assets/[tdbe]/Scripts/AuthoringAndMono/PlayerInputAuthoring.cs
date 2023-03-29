using UnityEngine;
using Unity.Entities;

namespace GameWorld.Players
{
    public class PlayerInputAuthoring : MonoBehaviour
    {
        public KeyCode Up;
        public KeyCode Down;
        public KeyCode Left;
        public KeyCode Right;
        public KeyCode Shoot;
        public KeyCode Teleport;
        public class PlayerBaker : Baker<PlayerInputAuthoring>
        {
            public override void Bake(PlayerInputAuthoring authoring)
            {
                AddComponent<PlayerInputComponent>(new PlayerInputComponent{
                   Up = new PlayerInputComponent.InputPair{ keyCode = authoring.Up},
                   Down = new PlayerInputComponent.InputPair{ keyCode = authoring.Down},
                   Left = new PlayerInputComponent.InputPair{ keyCode = authoring.Left},
                   Right = new PlayerInputComponent.InputPair{ keyCode = authoring.Right},
                   Shoot = new PlayerInputComponent.InputPair{ keyCode = authoring.Shoot},
                   Teleport = new PlayerInputComponent.InputPair{ keyCode = authoring.Teleport}
                });
            }
        }
    }

    
}