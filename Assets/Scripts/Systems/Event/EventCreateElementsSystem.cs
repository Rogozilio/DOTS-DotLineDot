using Baker;
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
            state.RequireForUpdate<MultiSphereComponent>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var levelSettings = SystemAPI.GetSingleton<MultiSphereComponent>();
            state.Dependency = new CreateElementsJob { ecb = ecb, levelSettings = levelSettings}.Schedule(state.Dependency);
        }

        private partial struct CreateElementsJob : IJobEntity
        {
            internal EntityCommandBuffer ecb;
            public MultiSphereComponent levelSettings;

            private void Execute(Entity entity, in ConnectSphere connectSphere)
            {
                for (byte i = 0; i < levelSettings.countElements; i++)
                {
                    var newElement = ecb.Instantiate(levelSettings.prefabElement);
                    ecb.SetName(newElement, "Element " + i);
                    ecb.SetComponent(newElement, new ElementComponent()
                    {
                        id = i
                    });
                }
            }
        }
    }
}