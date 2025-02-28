using Aspects;
using Components.DynamicBuffers;
using Static;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Utilities;

namespace Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct InitPullsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PullSphereBuffer>();
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
                ecb = ecb,
                entityBuffer = SystemAPI.GetSingletonEntity<PullSphereBuffer>(),
                transforms = SystemAPI.GetComponentLookup<LocalTransform>(true)
            }.Schedule(state.Dependency);

            state.Enabled = false;
        }

        [BurstCompile]
        public partial struct CreatePullsJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public Entity entityBuffer;
            [ReadOnly] public ComponentLookup<LocalTransform> transforms;

            public void Execute(LevelSettingAspect levelSettingAspect)
            {
                var transformElement = transforms[levelSettingAspect.level.prefabElement];
                transformElement.Position = TransformUtility.DefaultPositionElement();

                for (var i = 0; i < levelSettingAspect.level.countSphere; i++)
                {
                    var transform = new LocalTransform()
                    {
                        Position = TransformUtility.DefaultPositionSphere(i),
                        Rotation = quaternion.identity,
                        Scale = 1f
                    };
                    StaticMethod.InitSphere(ecb, entityBuffer, levelSettingAspect.level.prefabSphere, transform);
                }

                for (var i = 0; i < levelSettingAspect.level.countElement; i++)
                {
                    StaticMethod.InitElement(ecb, entityBuffer, levelSettingAspect.level.prefabElement,
                        transformElement);

                    StaticMethod.InitJoint(ecb, entityBuffer);
                }
            }
        }
    }
}