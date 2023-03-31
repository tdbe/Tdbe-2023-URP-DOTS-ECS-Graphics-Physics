using UnityEngine;
using Unity.Entities;

namespace GameWorld.NPCs
{
    public class UFOAuthoring : MonoBehaviour
    {
        public float moveSpeed= 1;
        public float rotateSpeed= 0;
        public float maxChaseDist = 7;
        public float minChaseDist = 0.05f;
        public class UFOBaker : Baker<UFOAuthoring>
        {
            public override void Bake(UFOAuthoring authoring)
            {
                AddComponent<UFOComponent>(new UFOComponent{
                   moveSpeed = authoring.moveSpeed,
                   rotateSpeed = authoring.rotateSpeed,
                   maxChaseDist = authoring.maxChaseDist,
                   minChaseDist = authoring.minChaseDist
                });
            }
        }
    }

    
}