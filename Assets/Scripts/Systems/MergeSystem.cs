using Components;
using Components.DynamicBuffers;
using Components.Shared;
using Static;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Utilities;

namespace Systems
{
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public partial struct MergeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PullJointBuffer>();
            state.RequireForUpdate<LevelSettingComponent>();
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var ecbParallel = ecb.AsParallelWriter();

            var bufferIndexConnectForRemove = SystemAPI.GetSingletonBuffer<IndexConnectionForRemoveBuffer>();
            var jointBuffer = SystemAPI.GetSingletonBuffer<PullJointBuffer>();
            var entityPull = SystemAPI.GetSingletonEntity<PullJointBuffer>();

            bufferIndexConnectForRemove.Clear();

            var entitiesForMerge = new NativeArray<Entity>(2, Allocator.TempJob);
            var oldIndex = new NativeReference<int>(Allocator.TempJob);
            var newIndex = new NativeReference<int>(Allocator.TempJob);
            var countElements = new NativeReference<int>(Allocator.TempJob);

            state.Dependency = new ActivateMergeComponentJob().Schedule(state.Dependency);
            state.Dependency = new MergeSystemJob
            {
                ecb = ecb,
                localTransforms = SystemAPI.GetComponentLookup<LocalTransform>(),
                buffers = SystemAPI.GetBufferLookup<IndexConnectionBuffer>(),
                bufferIndexForRemove = bufferIndexConnectForRemove,
                entitiesForMerge = entitiesForMerge
            }.Schedule(state.Dependency);
            state.Dependency = new RemoveElementBetweenSphereJob
            {
                ecb = ecbParallel,
                entityPull = entityPull,
                bufferIndexForRemove = bufferIndexConnectForRemove
            }.Schedule(state.Dependency);
            state.Dependency = new RemoveJointBetweenSphereJob
            {
                ecb = ecbParallel,
                entityPull = entityPull,
                bufferIndexForRemove = bufferIndexConnectForRemove
            }.Schedule(state.Dependency);
            if (SystemAPI.TryGetSingleton(out ReconnectSphereComponent reconnectSphere))
            {
                state.Dependency = new ReconnectSphereJob
                {
                    reconnectSphere = reconnectSphere,
                    ecb = ecb,
                    levelSetting = SystemAPI.GetSingleton<LevelSettingComponent>(),
                    buffers = SystemAPI.GetBufferLookup<IndexConnectionBuffer>(),
                    bufferIndexForRemove = bufferIndexConnectForRemove,
                    joints = jointBuffer
                }.Schedule(state.Dependency);
                state.Dependency = new RemoveAndClearSphereJob
                {
                    ecb = ecb,
                    entityPull = entityPull,
                    lengthBuffer = SystemAPI.GetSingletonBuffer<PullSphereBuffer>().Length
                }.Schedule(state.Dependency);
                state.Dependency = new RemoveReconnectSphereComponentJob
                {
                    ecb = ecb
                }.Schedule(state.Dependency);
            }

            state.Dependency = new SetIndexSharedForMergeJob
            {
                entitiesForMerge = entitiesForMerge,
                oldIndex = oldIndex,
                newIndex = newIndex
            }.Schedule(state.Dependency);
            state.Dependency = new CalculateSumCountElementSphereJob
            {
                entitiesForMerge = entitiesForMerge,
                countElements = countElements,
                spheres = SystemAPI.GetComponentLookup<SphereComponent>(true)
            }.Schedule(state.Dependency);
            state.Dependency = new ChangeIndexSharedSphereJob
            {
                ecb = ecb,
                oldIndex = oldIndex,
                newIndex = newIndex
            }.Schedule(state.Dependency);
            state.Dependency = new ChangeLimitElementsInSphereJob
            {
                oldIndex = oldIndex,
                newIndex = newIndex,
                countElements = countElements
            }.Schedule(state.Dependency);
            state.Dependency = new SetIndexSharedInLevelSettingJob
            {
                newIndex = newIndex
            }.Schedule(state.Dependency);

            entitiesForMerge.Dispose(state.Dependency);
            oldIndex.Dispose(state.Dependency);
            newIndex.Dispose(state.Dependency);
            countElements.Dispose(state.Dependency);
        }

        [BurstCompile]
        [WithDisabled(typeof(IsMouseMove))]
        [WithDisabled(typeof(MergeComponent))]
        public partial struct ActivateMergeComponentJob : IJobEntity
        {
            public void Execute(in IsMouseMove isMouseMove, ref MergeComponent merge,
                EnabledRefRW<MergeComponent> enableMerge)
            {
                if (merge.target == Entity.Null || !isMouseMove.isLastMove) return;

                enableMerge.ValueRW = true;
            }
        }

        [BurstCompile]
        [WithDisabled(typeof(IsMouseMove))]
        public partial struct MergeSystemJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public ComponentLookup<LocalTransform> localTransforms;
            public BufferLookup<IndexConnectionBuffer> buffers;
            public DynamicBuffer<IndexConnectionForRemoveBuffer> bufferIndexForRemove;
            public NativeArray<Entity> entitiesForMerge;

            public void Execute(Entity entity, in MergeComponent merge)
            {
                float3 direction =
                    math.normalizesafe(localTransforms[merge.target].Position - localTransforms[entity].Position);

                float distance = math.distance(localTransforms[merge.target].Position,
                    localTransforms[entity].Position);

                var transform = localTransforms[entity];
                if (distance > 0.05f)
                    transform.Position += direction * 0.08f;
                else
                {
                    transform.Position = localTransforms[merge.target].Position;

                    entitiesForMerge[0] = entity;
                    entitiesForMerge[1] = merge.target;

                    foreach (var indexFrom in buffers[entity])
                    {
                        foreach (var indexTo in buffers[merge.target])
                        {
                            if (indexFrom.value != indexTo.value) continue;

                            bufferIndexForRemove.Add(new IndexConnectionForRemoveBuffer { value = indexFrom.value });

                            for (var i = 0; i < buffers[entity].Length; i++)
                            {
                                if (buffers[entity][i].value != indexTo.value) continue;

                                buffers[entity].RemoveAt(i);
                                buffers[merge.target].RemoveAt(i);
                            }
                        }
                    }

                    ecb.AddComponent(entity, new ReconnectSphereComponent { from = entity, to = merge.target });
                }

                localTransforms[entity] = transform;
            }
        }

        [BurstCompile]
        [WithNone(typeof(PhysicsConstrainedBodyPair))]
        public partial struct RemoveElementBetweenSphereJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;
            [ReadOnly] public DynamicBuffer<IndexConnectionForRemoveBuffer> bufferIndexForRemove;
            [ReadOnly] public Entity entityPull;

            public void Execute(Entity entity, [EntityIndexInQuery] int sortKey, in LocalTransform transform,
                in IndexConnectComponent indexConnect)
            {
                foreach (var index in bufferIndexForRemove)
                {
                    if (indexConnect.value != index.value) continue;

                    StaticMethod.RemoveElement(ecb, sortKey, entityPull, entity, transform);
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(PhysicsConstrainedBodyPair))]
        public partial struct RemoveJointBetweenSphereJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;
            [ReadOnly] public DynamicBuffer<IndexConnectionForRemoveBuffer> bufferIndexForRemove;
            [ReadOnly] public Entity entityPull;

            public void Execute(Entity entity, [EntityIndexInQuery] int sortKey, in IndexConnectComponent indexConnect)
            {
                foreach (var index in bufferIndexForRemove)
                {
                    if (indexConnect.value != index.value) continue;

                    StaticMethod.RemoveJoint(ecb, sortKey, entityPull, entity);
                }
            }
        }

        [BurstCompile]
        public partial struct ReconnectSphereJob : IJobEntity
        {
            public ReconnectSphereComponent reconnectSphere;
            public EntityCommandBuffer ecb;
            public BufferLookup<IndexConnectionBuffer> buffers;
            public LevelSettingComponent levelSetting;
            public DynamicBuffer<PullJointBuffer> joints;
            [ReadOnly] public DynamicBuffer<IndexConnectionForRemoveBuffer> bufferIndexForRemove;

            public void Execute(Entity entity, in PhysicsConstrainedBodyPair bodyPair,
                in IndexConnectComponent indexConnect)
            {
                foreach (var index in bufferIndexForRemove)
                {
                    if (index.value == indexConnect.value) return;
                }

                if (reconnectSphere.from != bodyPair.EntityA && reconnectSphere.from != bodyPair.EntityB) return;

                var element = reconnectSphere.from != bodyPair.EntityA ? bodyPair.EntityA : bodyPair.EntityB;

                ecb.DestroyEntity(entity);

                StaticMethod.UseJoint(ecb, joints, reconnectSphere.to, element, levelSetting.distanceBetweenElements,
                    indexConnect.value);

                buffers[reconnectSphere.to].Add(new IndexConnectionBuffer { value = indexConnect.value });
            }
        }

        [BurstCompile]
        [WithAll(typeof(ReconnectSphereComponent))]
        [WithPresent(typeof(IsMouseMove))]
        public partial struct RemoveAndClearSphereJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            [ReadOnly] public Entity entityPull;
            [ReadOnly] public int lengthBuffer;

            public void Execute(Entity entity, ref LocalTransform transform, ref MergeComponent merge,
                ref IsMouseMove isMouseMove, EnabledRefRW<MergeComponent> enableMerge)
            {
                enableMerge.ValueRW = false;
                isMouseMove.isLastMove = false;
                merge.target = Entity.Null;
                transform.Position = TransformUtility.DefaultPositionSphere(lengthBuffer);
                StaticMethod.RemoveSphere(ecb, entityPull, entity, transform);
            }
        }

        [BurstCompile]
        [WithAll(typeof(ReconnectSphereComponent))]
        public partial struct RemoveReconnectSphereComponentJob : IJobEntity
        {
            public EntityCommandBuffer ecb;

            public void Execute(Entity entity)
            {
                ecb.RemoveComponent<ReconnectSphereComponent>(entity);
            }
        }

        [BurstCompile]
        [WithAll(typeof(SphereComponent))]
        public partial struct SetIndexSharedForMergeJob : IJobEntity
        {
            public NativeArray<Entity> entitiesForMerge;
            public NativeReference<int> oldIndex;
            public NativeReference<int> newIndex;

            public void Execute(Entity entity, in IndexSharedComponent indexShared)
            {
                if (entitiesForMerge[0] == entity)
                    oldIndex.Value = indexShared.value;
                else if (entitiesForMerge[1] == entity)
                    newIndex.Value = indexShared.value;
            }
        }

        [BurstCompile]
        public partial struct ChangeIndexSharedSphereJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public NativeReference<int> oldIndex;
            public NativeReference<int> newIndex;

            public void Execute(Entity entity, in IndexSharedComponent index)
            {
                if (oldIndex.Value == newIndex.Value) return;
                if (index.value != oldIndex.Value) return;

                ecb.SetSharedComponent(entity, new IndexSharedComponent { value = newIndex.Value });
            }
        }

        [BurstCompile]
        public partial struct SetIndexSharedInLevelSettingJob : IJobEntity
        {
            public NativeReference<int> newIndex;

            public void Execute(ref LevelSettingComponent levelSetting)
            {
                levelSetting.indexShared = (byte)newIndex.Value;
            }
        }

        [BurstCompile]
        public struct CalculateSumCountElementSphereJob : IJob
        {
            public NativeArray<Entity> entitiesForMerge;
            public NativeReference<int> countElements;
            [ReadOnly] public ComponentLookup<SphereComponent> spheres;

            public void Execute()
            {
                if (entitiesForMerge[0] == Entity.Null || entitiesForMerge[1] == Entity.Null) return;

                countElements.Value = spheres[entitiesForMerge[0]].countElements +
                                      spheres[entitiesForMerge[1]].countElements;
            }
        }

        [BurstCompile]
        public partial struct ChangeLimitElementsInSphereJob : IJobEntity
        {
            public NativeReference<int> oldIndex;
            public NativeReference<int> newIndex;
            [ReadOnly] public NativeReference<int> countElements;

            public void Execute(ref SphereComponent sphere, in IndexSharedComponent index)
            {
                if (oldIndex.Value == newIndex.Value) return;
                if (index.value != oldIndex.Value && index.value != newIndex.Value) return;

                sphere.countElements = countElements.Value;
            }
        }
    }
}