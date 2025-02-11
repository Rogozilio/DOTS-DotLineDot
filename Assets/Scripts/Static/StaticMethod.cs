using Components;
using Components.DynamicBuffers;
using Components.Shared;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Static
{
    public static class StaticMethod
    {
        public static Entity CreateJoint(EntityCommandBuffer ecb, Entity a, Entity b, float maxDistanceRange,
            int indexConnection, string name = "JointElement")
        {
            var newEntity = ecb.CreateEntity();

            var bodyPair = new PhysicsConstrainedBodyPair(a, b, false);
            var limitedDistance =
                PhysicsJoint.CreateLimitedDistance(float3.zero, float3.zero, new Math.FloatRange(0, maxDistanceRange));

            ecb.SetName(newEntity, name);
            ecb.AddComponent(newEntity, bodyPair);
            ecb.AddComponent(newEntity, limitedDistance);
            ecb.AddComponent(newEntity, new IndexConnectComponent { value = indexConnection });
            ecb.AddSharedComponent(newEntity, new PhysicsWorldIndex());

            return newEntity;
        }

        public static Entity CreateJoint(EntityCommandBuffer.ParallelWriter ecb, int sortKey, Entity a, Entity b,
            float maxDistanceRange, int indexConnection, string name = "JointElement", bool enableCollision = false)
        {
            var newEntity = ecb.CreateEntity(sortKey);

            var bodyPair = new PhysicsConstrainedBodyPair(a, b, enableCollision);
            var limitedDistance =
                PhysicsJoint.CreateLimitedDistance(float3.zero, float3.zero, new Math.FloatRange(0, maxDistanceRange));

            ecb.SetName(sortKey, newEntity, name);
            ecb.AddComponent(sortKey, newEntity, bodyPair);
            ecb.AddComponent(sortKey, newEntity, limitedDistance);
            ecb.AddComponent(sortKey, newEntity, new IndexConnectComponent { value = indexConnection });
            ecb.AddSharedComponent(sortKey, newEntity, new PhysicsWorldIndex());

            return newEntity;
        }

        public static void SetJoint(EntityCommandBuffer ecb, DynamicBuffer<PullJointBuffer> joints, Entity a, Entity b,
            float maxDistanceRange,
            int indexConnection, string name = "JointElement")
        {
            var newEntity = joints[^1].value;

            var bodyPair = new PhysicsConstrainedBodyPair(a, b, false);
            var limitedDistance =
                PhysicsJoint.CreateLimitedDistance(float3.zero, float3.zero, new Math.FloatRange(0, maxDistanceRange));

            ecb.SetName(newEntity, name);
            ecb.SetComponent(newEntity, bodyPair);
            ecb.SetComponent(newEntity, limitedDistance);
            ecb.SetComponent(newEntity, new IndexConnectComponent { value = indexConnection });
            ecb.SetSharedComponent(newEntity, new PhysicsWorldIndex());

            joints.RemoveAt(joints.Length != 0 ? joints.Length - 1 : 0);
        }

        public static void RemoveJoint(EntityCommandBuffer ecb, DynamicBuffer<PullJointBuffer> joints, Entity entity,
            string name = "JointElement")
        {
            joints.Add(new PullJointBuffer { value = entity });

            var bodyPair = new PhysicsConstrainedBodyPair(Entity.Null, Entity.Null, false);
            var limitedDistance =
                PhysicsJoint.CreateLimitedDistance(float3.zero, float3.zero, new Math.FloatRange(0, 0));

            ecb.SetName(entity, name);
            ecb.SetComponent(entity, bodyPair);
            ecb.SetComponent(entity, limitedDistance);
            ecb.SetComponent(entity, new IndexConnectComponent { value = -1 });
            ecb.SetSharedComponent(entity, new PhysicsWorldIndex());
        }
        
        public static void RemoveJoint(EntityCommandBuffer.ParallelWriter ecb, int sortKey, DynamicBuffer<PullJointBuffer> joints, Entity entity,
            string name = "JointElement")
        {
            joints.Add(new PullJointBuffer { value = entity });

            var bodyPair = new PhysicsConstrainedBodyPair(Entity.Null, Entity.Null, false);
            var limitedDistance =
                PhysicsJoint.CreateLimitedDistance(float3.zero, float3.zero, new Math.FloatRange(0, 0));

            ecb.SetName(sortKey, entity, name);
            ecb.SetComponent(sortKey, entity, bodyPair);
            ecb.SetComponent(sortKey, entity, limitedDistance);
            ecb.SetComponent(sortKey, entity, new IndexConnectComponent { value = -1 });
            ecb.SetSharedComponent(sortKey, entity, new PhysicsWorldIndex());
        }

        public static Entity CreateElement(EntityCommandBuffer ecb, DynamicBuffer<PullElementBuffer> elementBuffers,
            LocalTransform transform, int
                indexConnection, IndexSharedComponent index, string name = "Element")
        {
            var newElement = elementBuffers[^1].value;
            ecb.SetName(newElement, name);
            ecb.SetComponent(newElement, new IndexConnectComponent()
            {
                value = indexConnection
            });
            ecb.SetComponent(newElement, transform);
            ecb.SetComponentEnabled<TargetGravityComponent>(newElement, true);
            ecb.SetSharedComponent(newElement, new IndexSharedComponent { value = index.value });
            elementBuffers.RemoveAt(elementBuffers.Length - 1);
            return newElement;
        }

        public static void RemoveElement(EntityCommandBuffer ecb, DynamicBuffer<PullElementBuffer> elements,
            Entity entity, LocalTransform transform, string name = "Element")
        {
            elements.Add(new PullElementBuffer { value = entity });

            ecb.SetName(entity, name);
            transform.Position = new float3(0, -15, 0);
            ecb.SetComponent(entity, transform);
            ecb.SetComponent(entity, new IndexConnectComponent { value = -1 });
            ecb.SetSharedComponent(entity, new IndexSharedComponent() { value = -1 });
        }

        public static void RemoveElement(EntityCommandBuffer.ParallelWriter ecb, int sortKey,
            DynamicBuffer<PullElementBuffer> elements,
            Entity entity, LocalTransform transform, string name = "Element")
        {
            elements.Add(new PullElementBuffer { value = entity });

            ecb.SetName(sortKey, entity, name);
            transform.Position = new float3(0, -15, 0);
            ecb.SetComponent(sortKey, entity, transform);
            ecb.SetComponent(sortKey, entity, new IndexConnectComponent { value = -1 });
            ecb.SetSharedComponent(sortKey, entity, new IndexSharedComponent() { value = -1 });
        }
    }
}