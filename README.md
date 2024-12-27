# Tdbe-2023-DOTS-ECS-Graphics-Physics
An Asteroids style game with health, powerups, ai, warp, teleportation, and local co-op.

Quick gameplay video: 

https://github.com/tdbe/Tdbe-2023-URP-DOTS-ECS-Graphics-Physics/assets/1399607/75d561f0-0448-4c98-8ef1-81648691fd82

![image](https://user-images.githubusercontent.com/1399607/229624241-bfa26a77-4a56-41a4-a14a-e5c4d359378e.png)

v 2022.2.6f1

## DOTs ECS 1.0 sandbox

- everything is unmanaged, bursted, and (multi)threaded by default (except the legacy input)
- player, game system, states, random/spawners, variable rates, threads, aspects, dynamic buffers & nativearray components, collisions, dynamic bounds, warping, pickups with visuals, rocks, ufos+ai, shooting and health / dying.

![image](https://user-images.githubusercontent.com/1399607/229301717-71ba254b-e5c5-44f9-be70-14a46b998b42.png)

- 5-8 ms on main thread, and 140-190FPS, with 500k-1m triangles

![stats1](https://user-images.githubusercontent.com/1399607/230787051-743b08a1-a4f0-4d21-baec-015b44767a75.PNG)

- Diagram of the ECS layout: https://miro.com/app/board/uXjVMWg58OI=/?share_link_id=616428552594

![image](https://user-images.githubusercontent.com/1399607/230787618-f4b31c5c-07e2-499c-8e7b-64f87e1818b9.png)

## Project

Fairly wide scope of ECS DOD / DOT usage, generic and specific setups, for a full game core loop. There are still a few gameplay details done in a hurry, marked with "// TODO:" or "// NOTE".


The project is set up to be visible via the Hierarchy what is going on and roughly in what order, and using prefabs and components with mono authorings, and inspector notes. Most things & rationales are also described in comments in code.

Assets\[tdbe]\Scenes\GameScene_01 <-- scene to play

### Play:
- Control ship with the chosen input data keys in the corresponding Player prefab. By default: 
  - Player_1: arrow keys to move (physics like a hovercraft), right Ctrl to shoot, right Shift to teleport. Touch the pickups to equip them.
  - Player_2: WASD to move, Space to shoot, leftShift to teleport.
- To easier test, you have 1000 health. You get damaged by 1, every physics tick, by every damage component that is touching you.
Anything that dies disappears, no animations, but there is health GUI.


### Some points of interest:
- everything is physics based.
- I made what I think is a cool multithreaded RandomnessComponent using nativeArray, persistent state, and local plus per-game seeds.
- simple but reusable Random Spawner aspect, also reused in targeted spawning of child rocks and player teleportation.
- resizeable window / teleport bounds
- equipped pickups are visible on you and have modding behaviour to your health or to your shooting (e.g. go through objects).
- tweakable health and time to live on *everything that moves* including rocks.
- tweakable damage dealing from everything that moves.
- randomized PCG for variable rate update groups, randomized (and/or binary) sizes as well, for enemies and rocks.
- enemy AI follows closest player, even through portals (picks shortest path to nearest player, including portals).
- Quickly made a dumb but cleverly versatile offsetted outline shadergraph shader that I quickly built all my assets from "CSG style". 


### Philosophy:
- performant (threaded, bursted, instanced, masked) by default, not "well this won't hurt so much".
- main system can update states of other systems, other systems control their own state and do their one job. (and there can be sub-branching).
- a system changes the component state a thing, and then another system takes over. E.g. no scripting of events chains on spawn or calling systems etc.
- reuse components, systems, threads, and aspects, unless doing so becomes confusing project-management wise or future-gamedev wise. #ProgrammerUX is real.
- at the same time don't preemptively expose code that you don't need anywhere else yet. E.g. you can even use "{ }" blocks locally, in some large main function (yeah I know usually one function does one thing). Don't end up with confusing directionless fragments for someone else to hunt down and wonder when to use, etc.
- use state machines, state graphs; some approaches are described in code (e.g. in GameSystem). Before starting a big project, create a state / decision transition visualizer that your grandma would understand.
- break up large / often edited parts of components for iteration & cache coherency, have a look at the chunk buffers.
- track game's memory limits. Pay attention to when anything (should be) increased / destroyed and queue them only in specialized systems at safe times.
- track all the other bandwidths / stress points :) (threads, hardware, non-ecs-ties); e.g. what happens if you wipe out all enemies on the screen at the same time?
- make philosophy clear at a glance: hierarchy object naming and structure, inspector notes, code descriptions of intention or the point etc.
- In ECS anything can be represented as just an efficient database query. So the difficulty, the limits & wisdom, are about how you store, define, equip, and see this query as a state or concept, in a production-friendly sane way.


### Some annoying quirks I found:
- At this time cross-scene communication techniques in unity ECS are: *\*crickets\** ..just use statics or somehtin..?
- Oh what's that, you just wanted to quickly access some main Camera data, from your entity subscene? 🙃
- Yo what's up with Variable Rate Update Groups insta-updating on rate change and not next tick!?
- Some things you wouldn't expect, don't get authored from mono. For example: isKinematic, isTrigger, physics layers.
- Rigidbody freeze position and rotation do NOT have a solution from Unity in ECS. Yeah there's the external JAC shit but it's not the same behaviour, it's restricting and sometimes physics-unreliable AF joint authoring components.
- Yes you knew about the renderer and TransformSystemGroup when spawning, but did you know/remember the ECS Fixed Step Physics simulation will also process entity colliders at 0,0,0 if you don't use the right command buffer stage.
- NonUniformScale/PostTransformScale component (to be replaced with PostTransformMatrix) is not disabled but actually absent by default, and can be requested / added.
- Getting collision hit points. I get it, but very cumbersome UX...


![image](https://user-images.githubusercontent.com/1399607/228077452-9fc860c3-e4eb-4a14-a27d-3230db34fdf4.png)
