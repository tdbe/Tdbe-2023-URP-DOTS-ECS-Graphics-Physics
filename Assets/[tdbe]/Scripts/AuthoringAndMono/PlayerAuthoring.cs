using UnityEngine;
using Unity.Entities;
using Unity.Physics;

namespace GameWorld.Players
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public float moveSpeed= 5;
        public float rotateSpeed= 1;

        public class PlayerBaker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                AddComponent<PlayerComponent>(new PlayerComponent{
                   moveSpeed = authoring.moveSpeed,
                   rotateSpeed = authoring.rotateSpeed
                });
                
            }
        }
    }

    
}