using Components;
using Static;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(EventCreateElementsSystem))]
    public partial struct EventCreateJointElementsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var elements = SystemAPI.QueryBuilder().WithAll<ElementComponent>().Build();
            var entityElements = elements.ToEntityArray(Allocator.TempJob);
            
            if(entityElements.Length <= 0) return;
            
            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var maxDistanceRange = SystemAPI.GetComponent<LocalTransform>(entityElements[0]).Scale / 1.5f;
            state.Dependency = new CreateConnectElementsJob
                { ecb = ecb, elements = entityElements, maxDistanceRange = maxDistanceRange}.Schedule(state.Dependency);
        }

        private partial struct CreateConnectElementsJob : IJobEntity
        {
            internal EntityCommandBuffer ecb;
            public NativeArray<Entity> elements;
            public float maxDistanceRange;

            private void Execute(Entity entity, in MultiSphereComponent multiSphereComponent,
                in TagInitConnectElements tag)
            {
                StaticMethod.CreateJoint(ecb, multiSphereComponent.startSphere, elements[0], maxDistanceRange);

                for (var i = 0; i < elements.Length - 1; i++)
                {
                    StaticMethod.CreateJoint(ecb, elements[i], elements[i + 1], maxDistanceRange);
                }

                StaticMethod.CreateJoint(ecb, elements[^1], multiSphereComponent.endSphere, maxDistanceRange);

                ecb.RemoveComponent<TagInitConnectElements>(entity);
            }
        }

      
    }
}