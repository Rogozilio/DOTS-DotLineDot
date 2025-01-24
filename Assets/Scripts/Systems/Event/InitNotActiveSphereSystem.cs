using Aspects;
using Components.DynamicBuffers;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct InitNotActiveSphereSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LevelSettingComponent>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new InitNotActiveSphereJob
            {
                ecb = ecb
            }.Schedule(state.Dependency);

            state.Enabled = false;
        }

        [BurstCompile]
        public partial struct InitNotActiveSphereJob : IJobEntity
        {
            internal EntityCommandBuffer ecb;

            public void Execute(LevelSettingAspect levelSettingAspect, in LevelSettingComponent levelSetting)
            {
                for (var i = 0; i < levelSetting.countSphere; i++)
                {
                    var newSphere = ecb.Instantiate(levelSetting.prefabSphere);
                    levelSettingAspect.AddSphereInBuffer(ecb, newSphere, i);
                }
            }
        }
    }
}