using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace World.Asteroid
{
    public class GameSystemStateAuthoring : MonoBehaviour
    {
       
        public GameSystemStateComponent.State state = GameSystemStateComponent.State.Inactive;


        public class GameSystemStateBaker : Baker<GameSystemStateAuthoring>
        {
            public override void Bake(GameSystemStateAuthoring authoring)
            {
                AddComponent<GameSystemStateComponent>(new GameSystemStateComponent{
                    state = authoring.state
                });
            }
        }
    }

    
}