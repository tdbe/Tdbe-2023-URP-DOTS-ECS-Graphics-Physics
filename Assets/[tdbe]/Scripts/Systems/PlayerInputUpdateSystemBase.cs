using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;


namespace GameWorld.Players
{
    [UpdateAfter(typeof(GameSystem))]

    public partial class PlayerInputUpdateSystemBase : SystemBase
    {
 
        protected override void OnCreate()
        {
            // at least one player in the scene
            RequireForUpdate<PlayerInputComponent>();
        }

  
        protected override void OnDestroy()
        {
        }


        protected override void OnUpdate()
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);
            
            // TODO: T_T I don't know if there's a better way right now to get (old) input into ECS.
            // At least this way I have a command buffer. The idiomatic foreach afaics can't grab entities.
            Entities.ForEach((in PlayerComponent plComp, in Entity ent)=>{
                var plInpComp = SystemAPI.GetComponent<PlayerInputComponent>(ent);
                ecb.SetComponent<PlayerInputComponent>(ent, new PlayerInputComponent{
                    Up = new PlayerInputComponent.InputPair{
                        keyCode = plInpComp.Up.keyCode,
                        keyVal = Input.GetKey(plInpComp.Up.keyCode)},
                    Down = new PlayerInputComponent.InputPair{
                        keyCode = plInpComp.Down.keyCode,
                        keyVal = Input.GetKey(plInpComp.Down.keyCode)},
                    Left = new PlayerInputComponent.InputPair{
                        keyCode = plInpComp.Left.keyCode,
                        keyVal = Input.GetKey(plInpComp.Left.keyCode)},
                    Right = new PlayerInputComponent.InputPair{
                        keyCode = plInpComp.Right.keyCode,
                        keyVal = Input.GetKey(plInpComp.Right.keyCode)},
                    Shoot = new PlayerInputComponent.InputPair{
                        keyCode = plInpComp.Shoot.keyCode,
                        keyVal = Input.GetKeyUp(plInpComp.Shoot.keyCode)},
                    Teleport = new PlayerInputComponent.InputPair{
                        keyCode = plInpComp.Teleport.keyCode,
                        keyVal = Input.GetKeyUp(plInpComp.Teleport.keyCode)},
                });
            }).Run();
        }
    }
    
}
