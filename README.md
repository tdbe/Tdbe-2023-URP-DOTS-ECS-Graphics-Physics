# Tdbe-2023-DOTS-ECS-Graphics-Physics
Quick gameplay video: https://www.deferredreality.com/images/tdbe_ecs10_quantum_asteroids_01.webm

![image](https://user-images.githubusercontent.com/1399607/229624241-bfa26a77-4a56-41a4-a14a-e5c4d359378e.png)

v 2022.2.6f1

## .'s 1.0 sandbox

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
- a system changes the component state of something, and then another system takes over. E.g. no scripting of events chains on spawn etc.
- reuse components, systems, threads, and aspects, unless doing so becomes confusing project-management wise or future-gamedev wise.
- at the same time, don't split up code that you don't need accessed from anywhere else yet. E.g. you can use "{ }" to separate out blocks locally, without actually moving them out. So you don't end up with confusing modules that someone else won't know when to use, etc.
- track memory limits, pay attention to what / when you're increasing or destroying; maybe destroy everything in one system at a controlled time.
- think about all the limits; e.g. is it bad if you wipe out all enemies on the screen at the same time?
- use state machines; approaches are described in code (e.g. in GameSystem).
- maybe break up large components if there is some small part you're writing to a lot.
- make things clear at a glance: hierarchy objects, inspector notes, code descriptions of your ideas etc.
- In ECS anything can be represented as just an efficient database query. So the limits & wisdom are about how you save, define, equip and see this query as a state in a production friendly way.


### Some annoying quirks I found:
- Cross-scene communication techniques in ECS are: *\*crickets\** ..just use statics or somehtin..?
- Oh what's that, you just wanted to quickly access some main Camera data, from your entity subscene? 🙃
- Yo what's up with Variable Rate Update Groups - insta-updating on rate change? It's an interval, not a sometimes-interval..!
- Some things you don't expect, don't get authored from mono. For example: isKinematic, isTrigger, physics layers.
- Rigidbody freeze position and rotation does NOT have a solution from Unity in ECS. Yeah there's the external JAC shit but it's not the same behaviour, it's restricting and sometimes physics-unreliable AF joint authoring components.
- Yes you knew about the renderer and TransformSystemGroup when spawning, but ECS fixed step physics simulation will also process some collider at 0,0,0 of an entity if you don't use the right command buffer stage. And yeah I know this is per design.
- NonUniformScale/PostTransformScale component (to be replaced with PostTransformMatrix) is not disabled but actually absent by default, and can be requested / added.
- Getting collision hit points. I get it, but cumbersome UX...


![image](https://user-images.githubusercontent.com/1399607/228077452-9fc860c3-e4eb-4a14-a27d-3230db34fdf4.png)
