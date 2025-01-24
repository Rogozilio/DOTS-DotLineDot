using Aspects;
using Components;
using Components.DynamicBuffers;
using Static;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
    [UpdateBefore(typeof(GravityInSphereSystem))]
    [UpdateAfter(typeof(MoveMouseSphereSystem))]
    [DisableAutoCreation]
    public partial struct BlockElementsInSphere : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BlockElementBuffer>();
            state.RequireForUpdate<LevelSettingComponent>();
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var buffer = SystemAPI.GetSingletonBuffer<BlockElementBuffer>();

            var ecbSingalton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingalton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var count = SystemAPI.QueryBuilder().WithDisabled<TargetGravityComponent>()
                .WithNone<TagDisabledTargetGravity>().Build().CalculateEntityCount();
            Debug.Log(count);
            if (count >= 10 && buffer.IsEmpty)
            {
                Debug.Log("Block");
                state.Dependency = new CreateJointForDisabledTargetGravity
                {
                    ecb = ecb,
                    bufferEntity = SystemAPI.GetSingletonEntity<BlockElementBuffer>(),
                    levelSetting = SystemAPI.GetSingleton<LevelSettingComponent>()
                }.Schedule(state.Dependency);
            }
            else if (count < 10 && !buffer.IsEmpty)
            {
                Debug.Log("Unblock");
                for (var i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i].element != Entity.Null)
                        ecb.DestroyEntity(0, buffer[i].element);
                }
                buffer.Clear();

                state.Dependency = new RemoveJointForDisabledTargetGravity
                {
                    ecb = ecb,
                }.Schedule(state.Dependency);
            }
        }

        [BurstCompile]
        private partial struct CreateJointForDisabledTargetGravity : IJobEntity
        {
            internal EntityCommandBuffer.ParallelWriter ecb;
            public Entity bufferEntity;
            [ReadOnly] public LevelSettingComponent levelSetting;

            private void Execute(Entity entity, [ChunkIndexInQuery] int sortKey, ElementAspect element)
            {
                var e = StaticMethod.CreateJoint(ecb, sortKey, entity, element.TargetGravity.target,
                    element.DistanceToTargetGravity, levelSetting.indexConnection,"JointNew");
                ecb.AppendToBuffer(sortKey, bufferEntity, new BlockElementBuffer { element = e });
                ecb.AddComponent<TagDisabledTargetGravity>(sortKey, entity);
            }
        }

        [BurstCompile]
        [WithAll(typeof(TagDisabledTargetGravity))]
        private partial struct RemoveJointForDisabledTargetGravity : IJobEntity
        {
            internal EntityCommandBuffer.ParallelWriter ecb;

            private void Execute(Entity entity, [ChunkIndexInQuery] int sortKey)
            {
                ecb.RemoveComponent<TagDisabledTargetGravity>(sortKey, entity);
            }
        }
    }
}