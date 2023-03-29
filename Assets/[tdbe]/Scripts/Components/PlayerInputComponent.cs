using Unity.Entities;
using UnityEngine;
namespace GameWorld.Players
{
    public struct PlayerInputComponent : IComponentData
    {
        public struct InputPair{
            public KeyCode keyCode;
            public bool keyVal;
        }
        public InputPair Up;
        public InputPair Down;
        public InputPair Left;
        public InputPair Right;
        public InputPair Shoot;
        public InputPair Teleport;
        //public InputPair Boost;

    }
}
