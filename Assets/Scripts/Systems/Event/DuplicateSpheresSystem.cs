using Aspects;
using Baker;
using Components;
using Components.DynamicBuffers;
using Components.Shared;
using Static;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct DuplicateSpheresSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ConnectSphere>();
            state.RequireForUpdate<LevelSettingComponent>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var entityLevelSettings = SystemAPI.GetSingletonEntity<LevelSettingComponent>();
            
            state.Dependency = new CreateJointElementsJob
            {
                ecb = ecb,
                jointBuffer = SystemAPI.GetSingletonBuffer<PullJointBuffer>(),
                levelSettings = SystemAPI.GetSingleton<LevelSettingComponent>(),
                entityLevelSettings = entityLevelSettings,
            }.Schedule(state.Dependency);
            state.Dependency = new IncrementIndexConnectionJob
            {
                ecb = ecb,
                entityLevelSettings = entityLevelSettings
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private partial struct CreateJointElementsJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public DynamicBuffer<PullJointBuffer> jointBuffer;
            public LevelSettingComponent levelSettings;
            public Entity entityLevelSettings;

            private void Execute(Entity entity, in ConnectSphere connectSphere, in IndexSharedComponent indexShared)
            {
                StaticMethod.SetJoint(ecb, jointBuffer, entity, connectSphere.target,
                    levelSettings.distanceBetweenElements, levelSettings.indexConnection);
                levelSettings.indexShared = (byte)indexShared.value;
                ecb.SetComponent(entityLevelSettings, levelSettings);
                ecb.RemoveComponent<ConnectSphere>(entity);
            }
        }

        private partial struct IncrementIndexConnectionJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public Entity entityLevelSettings;
            private void Execute(LevelSettingAspect levelSettingsAspect)
            {
                levelSettingsAspect.IncrementIndexConnect(ecb, entityLevelSettings);
            }
        }
    }
}