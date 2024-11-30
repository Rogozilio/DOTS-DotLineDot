using Components;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace Systems
{
    public partial struct EventConnectSphereElementsSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var elements = SystemAPI.QueryBuilder().WithAll<ElementComponent>().Build();
            state.Dependency = new CreateConnectElementsJob
                { ecb = ecb, elements = elements.ToEntityArray(Allocator.TempJob) }.Schedule(state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private partial struct CreateConnectElementsJob : IJobEntity
        {
            internal EntityCommandBuffer ecb;
            public NativeArray<Entity> elements;

            private void Execute(Entity entity, in TagInitConnectElements tag)
            {
                for (var i = 0; i < elements.Length - 1; i++)
                {
                    var newEntity = ecb.CreateEntity();

                    var bodyPair = new PhysicsConstrainedBodyPair(elements[i], elements[i + 1], false);
                    var limitedDistance =
                        PhysicsJoint.CreateLimitedDistance(float3.zero, float3.zero, new Math.FloatRange(0, 0.3f));

                    ecb.SetName(newEntity, "JointElement");
                    ecb.AddComponent(newEntity, bodyPair);
                    ecb.AddComponent(newEntity, limitedDistance);
                    ecb.AddSharedComponent(newEntity, new PhysicsWorldIndex());
                }
                
                ecb.RemoveComponent<TagInitConnectElements>(entity);
            }
        }
    }
}