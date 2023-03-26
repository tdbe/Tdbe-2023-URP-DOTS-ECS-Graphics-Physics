using Unity.Entities;

namespace GameWorld.NPCs
{
    // we could have some sort of inheritance or generics here e.g. across ufo, asteroid, powerup spawning.
    // but conceptually spealking these are 3 categories of things that normally shouldn't have common links.
    public struct UFOSpawnComponent : IComponentData 
    {
        public uint maxNumber; 
        public float zRange;
        public float decorativeRandomScaleBump;
        public uint inGameSpawnRate_ms;// TODO: if I decide to change this gradually, maybe move to own separate component
    }
}
