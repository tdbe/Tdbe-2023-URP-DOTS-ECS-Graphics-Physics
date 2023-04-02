# Tdbe-2023-DOTS-ECS-Graphics-Physics
Tdbe-2023-DOTS-ECS-Graphics-Physics

2022.2.6f1

.'s 1.0 sandbox
- player, game system, states, random/spawners, variable rates, threads, aspects, collisions, dynamic bounds, warping, pickups with visuals, rocks, ufos+ai, shooting and health / dying.


A fairly wide scope of ECS DAO usage, generic and specific setups, for a full game core loop; all approaches performant and threaded by default. There are still a few details done in a hurry, marked with "// TODO:" or "// NOTE". Everything is described in code.


The project is set up to be visible via the Hierarchy what is going on and roughly in what order, using prefabs and components with mono authorings.


Play:
- Control ship with the input data shown in the Player prefab. By default: arrow keys to move (physics like a hovercraft), right Ctrl to shoot, right Shift to teleport. Touch the pickups to equip them.
- To easier test, you have 1000 health. You get damaged by 1, every physics tick, by every damage component that is touching you.
Anything that dies disappears, no animations or menus for now.


Some points of interest:
- Everything is physics based.
- I made what I think is a cool multithreaded RandomnessComponent using nativeArray, persistent state, and local plus per-game seeds.
- simple but reusable Random Spawner aspect, also used in targeted spawning of child rocks.
- resizeable window / teleport bounds
- equipped pickups are visible on you and have modding behaviour to you or to what you shoot (e.g. go through objects).
- tweakable health and time to live on *everything that moves* including rocks.
- tweakable damage dealing from everything that moves.
- randomized PCG for variable rate update groups, randomized and/or binary sizes as well, for enemies and rocks.
- enemy AI follows you through portals (picks shortest path including portals).
- Quickly made a dumb but cleverly versatile offsetted outline shadergraph shader that I quickly built all my assets from "CSG style". 


Philosophy:
- performant (threaded, bursted, instanced, masked) by default, not "well this won't hurt so much".
- main system can control states of other systems, other systems control their own state and do their one job. (yes there can be sub-branches).
- reuse components, systems, threads, and aspects, unless doing so becomes confusing project-management wise or future-gamedev wise.
- at the same time, don't split up code that you don't need accessed from anywhere else yet. E.g. you can use "{ }" to separate out blocks without making actual functions that someone else won't know how to use, etc.
- track memory limits, pay attention to what / when you're increasing or destroying; maybe destroy everything in one system at a controlled time.
- always think about the limits; e.g. is it bad if you wipe out all enemies on the screen at the same time?
- use state machines; approaches are described in code (e.g. in GameSystem).
- maybe break up large components if there is some small part you're writing to a lot.


Some annoying quirks I found:
- Cross-scene communication techniques in ECS are: *\*crickets\** ..just use statics or somehtin..?
- Oh what's that you just wanted to quickly access some main Camera data, from your entity subscene? 🙃
- Some things you don't expect, don't get authored from mono. For example: isKinematic, isTrigger, physics layers.
- Rigidbody freeze position and rotation does NOT have a solution from Unity in ECS. Yeah there's the external JAC shit but it's not the same behaviour, it's restricting and sometimes unreliable AF joint authoring components.
- Yes you knew about the renderer and TransformSystemGroup when spawning, but ECS fixed step simulation will also process some collider of an entity at 0,0,0 if you don't use the right command buffer stage. And yeah I know this is per design.
- NonUniformScale component (to be replaced with PostTransformMatrix) is not disabled but actually absent by default, and can be requested / added.
- Getting collision hit points. I get it, but cumbersome UX...


![image](https://user-images.githubusercontent.com/1399607/229301717-71ba254b-e5c5-44f9-be70-14a46b998b42.png)
![image](https://user-images.githubusercontent.com/1399607/228077452-9fc860c3-e4eb-4a14-a27d-3230db34fdf4.png)
![image](https://user-images.githubusercontent.com/1399607/228080576-c4664bf1-46d0-47a9-adca-17458bbd6c09.png)
