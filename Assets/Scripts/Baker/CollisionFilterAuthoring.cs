using System.Collections.Generic;
using System.Linq;
using Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;
using UnityEngine.Serialization;

namespace Baker
{
    public class CollisionFilterAuthoring : MonoBehaviour
    {
        [Header("Raycast Floor")]
        public PhysicsCategoryTags FloorBelongsTo;
        public PhysicsCategoryTags FloorCollidesWith;
        [Space]
        [Header("Raycast Sphere")]
        public PhysicsCategoryTags SphereBelongsTo;
        public PhysicsCategoryTags SphereCollidesWith;

        private class CollisionFilterAuthoringBaker : Baker<CollisionFilterAuthoring>
        {
            public override void Bake(CollisionFilterAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CollisionFilterComponent()
                {
                    collisionFilterFloor = new CollisionFilter()
                    {
                        BelongsTo = authoring.FloorBelongsTo.Value,
                        CollidesWith = authoring.FloorCollidesWith.Value
                    },
                    collisionFilterSphere = new CollisionFilter()
                    {
                        BelongsTo = authoring.SphereBelongsTo.Value,
                        CollidesWith = authoring.SphereCollidesWith.Value
                    }
                });
            }
        }
    }
}