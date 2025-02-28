using Components;
using Components.DynamicBuffers;
using Components.Shared;
using Tags;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Static
{
    public static class StaticMethod
    {
        #region Sphere

        public static void InitSphere(EntityCommandBuffer ecb, Entity buffer, Entity prefab, LocalTransform transform,
            string name = "SphereInPull")
        {
            var entity = ecb.Instantiate(prefab);

            RemoveSphere(ecb, buffer, entity, transform, name);
        }

        public static void RemoveSphere(EntityCommandBuffer ecb, Entity buffer, Entity entity, LocalTransform transform,
            string name = "SphereInPull")
        {
            ecb.SetName(entity, name);

            ecb.SetComponent(entity, transform);
            ecb.SetSharedComponent(entity, new IndexSharedComponent { value = -1 });
            ecb.SetBuffer<IndexConnectionBuffer>(entity); //Clear buffer
            ecb.RemoveComponent<BlockMoveSphereTag>(entity);

            ecb.AppendToBuffer(buffer, new PullSphereBuffer { value = entity });
        }

        public static Entity UseSphere(EntityCommandBuffer ecb, DynamicBuffer<PullSphereBuffer> sphereBuffers,
            LocalTransform transform, int indexShared, int countElements, string name = "Sphere")
        {
            var sphere = sphereBuffers[^1].value;
            ecb.SetName(sphere, name);
            ecb.SetComponent(sphere, transform);
            ecb.SetComponent(sphere, new SphereComponent { countElements = countElements });
            ecb.SetSharedComponent(sphere, new IndexSharedComponent { value = indexShared });
            sphereBuffers.RemoveAt(sphereBuffers.Length - 1);
            return sphere;
        }

        #endregion

        #region Element

        public static void InitElement(EntityCommandBuffer ecb, Entity buffer, Entity prefab, LocalTransform transform,
            string name = "ElementInPull")
        {
            var entity = ecb.Instantiate(prefab);

            RemoveElement(ecb, buffer, entity, transform, name);
        }

        public static void RemoveElement(EntityCommandBuffer ecb, Entity buffer,
            Entity entity, LocalTransform transform, string name = "ElementInPull")
        {
            ecb.SetName(entity, name);
            transform.Position = new float3(0, -15, 0);
            ecb.SetComponent(entity, transform);
            ecb.SetComponent(entity, new PhysicsVelocity()); //Clear PhysicsVelocity
            ecb.SetComponent(entity, new TargetGravityComponent()); //Clear TargetGravityComponent
            ecb.SetComponent(entity, new IndexConnectComponent { value = -1 });
            ecb.SetSharedComponent(entity, new IndexSharedComponent { value = -1 });
            ecb.AppendToBuffer(buffer, new PullElementBuffer() { value = entity });
        }

        public static void RemoveElement(EntityCommandBuffer.ParallelWriter ecb, int sortKey,
            Entity buffer, Entity entity, LocalTransform transform, string name = "ElementInPull")
        {
            ecb.SetName(sortKey, entity, name);
            transform.Position = new float3(0, -15, 0);
            ecb.SetComponent(sortKey, entity, transform);
            ecb.SetComponent(sortKey, entity, new PhysicsVelocity()); //Clear PhysicsVelocity
            ecb.SetComponent(sortKey, entity, new TargetGravityComponent()); //Clear TargetGravityComponent
            ecb.SetComponent(sortKey, entity, new IndexConnectComponent { value = -1 });
            ecb.SetSharedComponent(sortKey, entity, new IndexSharedComponent { value = -1 });
            ecb.AppendToBuffer(sortKey, buffer, new PullElementBuffer() { value = entity });
        }

        public static Entity UseElement(EntityCommandBuffer ecb, DynamicBuffer<PullElementBuffer> elementBuffers,
            LocalTransform transform, int indexConnection, IndexSharedComponent index, string name = "Element")
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

        #endregion

        #region Joint

        public static void InitJoint(EntityCommandBuffer ecb, Entity buffer, string name = "JointInPull")
        {
            var entity = ecb.CreateEntity();

            var bodyPair = new PhysicsConstrainedBodyPair(Entity.Null, Entity.Null, false);
            var limitedDistance =
                PhysicsJoint.CreateLimitedDistance(float3.zero, float3.zero, new Math.FloatRange(0, 0));

            ecb.SetName(entity, name);
            ecb.AddComponent(entity, bodyPair);
            ecb.AddComponent(entity, limitedDistance);
            ecb.AddComponent(entity, new IndexConnectComponent { value = -1 });
            ecb.AddSharedComponent(entity, new PhysicsWorldIndex());

            ecb.AppendToBuffer(buffer, new PullJointBuffer() { value = entity });
        }

        public static void InitJoint(EntityCommandBuffer.ParallelWriter ecb, int sortKey, Entity buffer,
            string name = "JointInPull")
        {
            var entity = ecb.CreateEntity(sortKey);

            var bodyPair = new PhysicsConstrainedBodyPair(Entity.Null, Entity.Null, false);
            var limitedDistance =
                PhysicsJoint.CreateLimitedDistance(float3.zero, float3.zero, new Math.FloatRange(0, 0));

            ecb.SetName(sortKey, entity, name);
            ecb.AddComponent(sortKey, entity, bodyPair);
            ecb.AddComponent(sortKey, entity, limitedDistance);
            ecb.AddComponent(sortKey, entity, new IndexConnectComponent { value = -1 });
            ecb.AddSharedComponent(sortKey, entity, new PhysicsWorldIndex());

            ecb.AppendToBuffer(sortKey, buffer, new PullJointBuffer() { value = entity });
        }

        public static void RemoveJoint(EntityCommandBuffer ecb, Entity buffer, Entity entity,
            string name = "JointInPull")
        {
            var bodyPair = new PhysicsConstrainedBodyPair(Entity.Null, Entity.Null, false);
            var limitedDistance =
                PhysicsJoint.CreateLimitedDistance(float3.zero, float3.zero, new Math.FloatRange(0, 0));

            ecb.SetName(entity, name);
            ecb.SetComponent(entity, bodyPair);
            ecb.SetComponent(entity, limitedDistance);
            ecb.SetComponent(entity, new IndexConnectComponent { value = -1 });
            ecb.SetSharedComponent(entity, new PhysicsWorldIndex());

            ecb.AppendToBuffer(buffer, new PullJointBuffer() { value = entity });
        }

        public static void RemoveJoint(EntityCommandBuffer.ParallelWriter ecb, int sortKey,
            Entity buffer, Entity entity, string name = "JointInPull")
        {
            var bodyPair = new PhysicsConstrainedBodyPair(Entity.Null, Entity.Null, false);
            var limitedDistance =
                PhysicsJoint.CreateLimitedDistance(float3.zero, float3.zero, new Math.FloatRange(0, 0));

            ecb.SetName(sortKey, entity, name);
            ecb.SetComponent(sortKey, entity, bodyPair);
            ecb.SetComponent(sortKey, entity, limitedDistance);
            ecb.SetComponent(sortKey, entity, new IndexConnectComponent { value = -1 });
            ecb.SetSharedComponent(sortKey, entity, new PhysicsWorldIndex());

            ecb.AppendToBuffer(sortKey, buffer, new PullJointBuffer() { value = entity });
        }

        public static void UseJoint(EntityCommandBuffer ecb, DynamicBuffer<PullJointBuffer> joints, Entity a, Entity b,
            float maxDistanceRange, int indexConnection, string name = "JointElement")
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

        public static void UseJoint(EntityCommandBuffer.ParallelWriter ecb, int sortKey,
            DynamicBuffer<PullJointBuffer> joints, Entity a, Entity b,
            float maxDistanceRange,
            int indexConnection, string name = "JointElement")
        {
            var newEntity = joints[^1].value;

            var bodyPair = new PhysicsConstrainedBodyPair(a, b, false);
            var limitedDistance =
                PhysicsJoint.CreateLimitedDistance(float3.zero, float3.zero, new Math.FloatRange(0, maxDistanceRange));

            ecb.SetName(sortKey, newEntity, name);
            ecb.SetComponent(sortKey, newEntity, bodyPair);
            ecb.SetComponent(sortKey, newEntity, limitedDistance);
            ecb.SetComponent(sortKey, newEntity, new IndexConnectComponent { value = indexConnection });
            ecb.SetSharedComponent(sortKey, newEntity, new PhysicsWorldIndex());

            joints.RemoveAt(joints.Length != 0 ? joints.Length - 1 : 0);
        }

        #endregion
    }
}