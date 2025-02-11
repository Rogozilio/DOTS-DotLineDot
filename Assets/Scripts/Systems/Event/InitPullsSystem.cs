using Aspects;
using Components.DynamicBuffers;
using Static;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct InitPullsSystem : ISystem
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

            state.Dependency = new CreatePullsJob
            {
                ecb = ecb
            }.Schedule(state.Dependency);

            state.Enabled = false;
        }

        [BurstCompile]
        public partial struct CreatePullsJob : IJobEntity
        {
            public EntityCommandBuffer ecb;

            public void Execute(LevelSettingAspect levelSettingAspect, in LevelSettingComponent levelSetting)
            {
                for (var i = 0; i < levelSetting.countSphere; i++)
                {
                    var newSphere = ecb.Instantiate(levelSetting.prefabSphere);
                    levelSettingAspect.AddSphereInPull(ecb, newSphere, i, true);//Pull sphere
                }

                for (var i = 0; i < levelSetting.countElement; i++)
                {
                    var newElement = ecb.Instantiate(levelSetting.prefabElement);
                    levelSettingAspect.AddElementInPull(ecb, newElement, true); //Pull element

                    var newJoint = StaticMethod.CreateJoint(ecb, Entity.Null, Entity.Null, 0, -1);
                    levelSettingAspect.AddJointInPull(ecb, newJoint, true); //Pull joint
                }
            }
        }
    }
}