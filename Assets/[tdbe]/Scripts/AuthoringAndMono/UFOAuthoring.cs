using UnityEngine;
using Unity.Entities;

namespace GameWorld.NPCs
{
    public class UFOAuthoring : MonoBehaviour
    {
        public class UFOBaker : Baker<UFOAuthoring>
        {
            public override void Bake(UFOAuthoring authoring)
            {
                AddComponent<UFOComponent>(new UFOComponent{
                   
                });
            }
        }
    }

    
}