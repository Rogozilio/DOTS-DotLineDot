using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace Components
{
    public struct CollisionFilterComponent : IComponentData
    {
        public CollisionFilter collisionFilterFloor;
        public CollisionFilter collisionFilterSphere;
    }
}