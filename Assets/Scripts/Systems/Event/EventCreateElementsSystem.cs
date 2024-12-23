using Components;
using Tags;
using Unity.Burst;
using Unity.Entities;

namespace Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EventCreateElementsSystem : ISystem
    {
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged); 
            state.Dependency = new CreateElementsJob { ecb = ecb}.Schedule(state.Dependency);
        }

        private partial struct CreateElementsJob : IJobEntity
        {
            internal EntityCommandBuffer ecb;

            private void Execute(Entity e, in MultiSphereComponent sphere, in TagInitMultiSphere tag)
            {
                for (byte i = 0; i < sphere.countElements; i++)
                {
                    var newElement = ecb.Instantiate(sphere.prefabElement);
                    ecb.SetName(newElement, "Element " + i);
                    ecb.AddComponent(newElement, new ElementComponent()
                    {
                        id = i
                    });
                    ecb.AddComponent<TargetGravityComponent>(newElement);
                    ecb.SetComponentEnabled<TargetGravityComponent>(newElement,false);
                }
                
                ecb.RemoveComponent<TagInitMultiSphere>(e);
                ecb.AddComponent<TagInitConnectElements>(e);
            }
        }
    }
}