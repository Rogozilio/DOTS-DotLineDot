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
            
            state.Dependency = new CreateJointElementsJob
            {
                ecb = ecb,
                levelSettings = SystemAPI.GetSingleton<LevelSettingComponent>(),
                entityLevelSettings = SystemAPI.GetSingletonEntity<LevelSettingComponent>(),
                jointBuffer = SystemAPI.GetSingletonBuffer<PullJointBuffer>(),
            }.Schedule(state.Dependency);
            state.Dependency = new IncrementIndexConnectionJob().Schedule(state.Dependency);
        }

        [BurstCompile]
        private partial struct CreateJointElementsJob : IJobEntity
        {
            internal EntityCommandBuffer ecb;
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
            private void Execute(ref LevelSettingComponent levelSettings)
            {
                levelSettings.indexConnection++;
            }
        }
    }
}