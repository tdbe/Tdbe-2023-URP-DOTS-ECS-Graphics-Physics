using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace GameWorld.NPCs
{
    public class NPCSpawnerStateAuthoring : MonoBehaviour
    {
       
        public NPCSpawnerStateComponent.State state = NPCSpawnerStateComponent.State.Inactive;


        public class NPCSpawnerStateBaker : Baker<NPCSpawnerStateAuthoring>
        {
            public override void Bake(NPCSpawnerStateAuthoring authoring)
            {
                AddComponent<NPCSpawnerStateComponent>(new NPCSpawnerStateComponent{
                    state = authoring.state
                });
            }
        }
    }

    
}