using Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Static
{
    public static class StaticMethod
    {
        public static void CreateJoint(EntityCommandBuffer ecb, Entity a, Entity b, float maxDistanceRange, byte indexConnection, string name = "JointElement")
        {
            var newEntity = ecb.CreateEntity();

            var bodyPair = new PhysicsConstrainedBodyPair(a, b, false);
            var limitedDistance =
                PhysicsJoint.CreateLimitedDistance(float3.zero, float3.zero, new Math.FloatRange(0, maxDistanceRange));

            ecb.SetName(newEntity, name);
            ecb.AddComponent(newEntity, bodyPair);
            ecb.AddComponent(newEntity, limitedDistance); ecb.AddComponent(newEntity, new IndexConnectComponent{value = indexConnection});
            ecb.AddSharedComponent(newEntity, new PhysicsWorldIndex());
        }
        
        public static Entity CreateJoint(EntityCommandBuffer.ParallelWriter ecb, int sortKey, Entity a, Entity b, float maxDistanceRange, byte indexConnection, string name = "JointElement")
        {
            var newEntity = ecb.CreateEntity(sortKey);

            var bodyPair = new PhysicsConstrainedBodyPair(a, b, false);
            var limitedDistance =
                PhysicsJoint.CreateLimitedDistance(float3.zero, float3.zero, new Math.FloatRange(0, maxDistanceRange));

            ecb.SetName(sortKey, newEntity, name);
            ecb.AddComponent(sortKey, newEntity, bodyPair);
            ecb.AddComponent(sortKey, newEntity, limitedDistance);
            ecb.AddComponent(sortKey, newEntity, new IndexConnectComponent{value = indexConnection});
            ecb.AddSharedComponent(sortKey, newEntity, new PhysicsWorldIndex());

            return newEntity;
        }
    }
}