using Components;
using Components.DynamicBuffers;
using Tags;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EventCreateSphereSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TagCreateSphere>();
            state.RequireForUpdate<MultiSphereComponent>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new CreateSphereJob
            {
                ecb = ecb,
                levelSettings = SystemAPI.GetSingleton<MultiSphereComponent>(),
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(TagCreateSphere))]
        public partial struct CreateSphereJob : IJobEntity
        {
            internal EntityCommandBuffer ecb;
            public MultiSphereComponent levelSettings;

            private void Execute(Entity entity, in LocalTransform localTransform)
            {
                var newSphere = ecb.Instantiate(levelSettings.prefabSphere);
                ecb.SetComponent(newSphere, localTransform);
                ecb.AddComponent(entity, new ConnectSphere { target = newSphere });
                ecb.SetComponentEnabled<IsMouseMove>(newSphere, true);

                var elementBuffer = new IndexConnectionBuffer() { value = levelSettings.indexConnection };
                ecb.AppendToBuffer(entity, elementBuffer);
                ecb.AppendToBuffer(newSphere, elementBuffer);

                ecb.RemoveComponent<TagCreateSphere>(entity);
            }
        }
    }
}