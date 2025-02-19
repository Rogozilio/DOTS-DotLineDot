using System.Linq;
using Aspects;
using Components;
using Components.DynamicBuffers;
using Components.Shared;
using Static;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
    [UpdateAfter(typeof(MoveMouseSphereSystem))]
    public partial struct MergeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MergeSphereComponent>();
            state.RequireForUpdate<LevelSettingComponent>();
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var merge = SystemAPI.GetSingletonRW<MergeSphereComponent>();
            var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var ecbParallel = ecb.AsParallelWriter();

            var bufferIndexConnectForRemove = SystemAPI.GetSingletonBuffer<IndexConnectionForRemoveBuffer>();
            var jointBuffer = SystemAPI.GetSingletonBuffer<PullJointBuffer>();

            bufferIndexConnectForRemove.Clear();

            state.Dependency = new SetMergeSphereFromJob
            {
                merge = merge
            }.Schedule(state.Dependency);
            state.Dependency = new SetMergeSphereToJob
            {
                merge = merge
            }.Schedule(state.Dependency);
            state.Dependency = new SphereMoveIntoSphereJob
            {
                merge = merge,
                deltaTime = SystemAPI.Time.fixedDeltaTime,
                localTransforms = SystemAPI.GetComponentLookup<LocalTransform>(),
                isCollisionWithSpheres = SystemAPI.GetComponentLookup<IsCollisionWithSphere>(true),
                isMouseMoves = SystemAPI.GetComponentLookup<IsMouseMove>(true),
            }.Schedule(state.Dependency);
            if (!merge.ValueRO.launchLastStageMerge) return;
            state.Dependency = new SetIndexBetweenFromAndToForMergeSphereJob
            {
                merge = merge,
                buffers = SystemAPI.GetBufferLookup<IndexConnectionBuffer>(true),
                bufferIndexForRemove = bufferIndexConnectForRemove
            }.Schedule(state.Dependency);
            state.Dependency = new RemoveElementBetweenSphereJob
            {
                ecb = ecbParallel,
                elementBuffer = SystemAPI.GetSingletonBuffer<PullElementBuffer>(),
                bufferIndexForRemove = bufferIndexConnectForRemove
            }.Schedule(state.Dependency);
            state.Dependency = new RemoveJointBetweenSphereJob
            {
                ecb = ecbParallel,
                jointBuffer = jointBuffer,
                bufferIndexForRemove = bufferIndexConnectForRemove
            }.Schedule(state.Dependency);
            state.Dependency = new RemoveIndexConnectInSphereJob
            {
                bufferIndexForRemove = bufferIndexConnectForRemove
            }.Schedule(state.Dependency);
            state.Dependency = new ReconnectJointJob
            {
                merge = merge,
                ecb = ecb,
                levelSetting = SystemAPI.GetSingleton<LevelSettingComponent>(),
                buffers = SystemAPI.GetBufferLookup<IndexConnectionBuffer>(),
                bufferIndexForRemove = bufferIndexConnectForRemove,
                joints = jointBuffer
            }.Schedule(state.Dependency);
            state.Dependency = new RemoveSphereJob
            {
                merge = merge,
                ecb = ecb
            }.Schedule(state.Dependency);

            var oldIndex = state.EntityManager.GetSharedComponent<IndexSharedComponent>(merge.ValueRO.to).value;
            var newIndex = state.EntityManager.GetSharedComponent<IndexSharedComponent>(merge.ValueRO.from).value;

            if (oldIndex != newIndex)
            {
                var spheres = SystemAPI.GetComponentLookup<SphereComponent>(true);

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
                    sumCountElements = spheres[merge.ValueRO.from].countElements +
                                       spheres[merge.ValueRO.to].countElements
                }.Schedule(state.Dependency);
            }

            state.Dependency = new ClearMergeComponentJob
            {
                merge = merge
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(IsMouseMove))]
        public partial struct SetMergeSphereFromJob : IJobEntity
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;

            public void Execute(Entity entity, in SphereComponent sphere)
            {
                merge.ValueRW.from = entity;
            }
        }

        [BurstCompile]
        [WithAll(typeof(IsCollisionWithSphere))]
        [WithNone(typeof(IsMouseMove))]
        public partial struct SetMergeSphereToJob : IJobEntity
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;

            public void Execute(Entity entity, in SphereComponent sphere)
            {
                if (merge.ValueRO.from == entity) return;

                merge.ValueRW.to = entity;
            }
        }

        [BurstCompile]
        public struct SphereMoveIntoSphereJob : IJob
        {
            public ComponentLookup<LocalTransform> localTransforms;

            [ReadOnly] public float deltaTime;
            [ReadOnly] public ComponentLookup<IsCollisionWithSphere> isCollisionWithSpheres;
            [ReadOnly] public ComponentLookup<IsMouseMove> isMouseMoves;

            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;

            public void Execute()
            {
                if (merge.ValueRO.from == Entity.Null || merge.ValueRO.to == Entity.Null) return;

                if (!isCollisionWithSpheres.IsComponentEnabled(merge.ValueRO.from) ||
                    !isCollisionWithSpheres.IsComponentEnabled(merge.ValueRO.to))
                    return;

                if (isMouseMoves.IsComponentEnabled(merge.ValueRO.from) ||
                    isMouseMoves.IsComponentEnabled(merge.ValueRO.to))
                    return;

                float3 direction =
                    math.normalizesafe(localTransforms[merge.ValueRO.to].Position -
                                       localTransforms[merge.ValueRO.from].Position);
                float distance = math.distance(localTransforms[merge.ValueRO.to].Position,
                    localTransforms[merge.ValueRO.from].Position);

                var transform = localTransforms[merge.ValueRO.from];

                if (distance > 5f * deltaTime)
                    transform.Position += direction * 5f * deltaTime;
                else
                {
                    transform.Position = localTransforms[merge.ValueRO.to].Position;
                    merge.ValueRW.launchLastStageMerge = true;
                }

                localTransforms[merge.ValueRO.from] = transform;
            }
        }

        [BurstCompile]
        public struct SetIndexBetweenFromAndToForMergeSphereJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;
            [ReadOnly] public BufferLookup<IndexConnectionBuffer> buffers;
            public DynamicBuffer<IndexConnectionForRemoveBuffer> bufferIndexForRemove;

            public void Execute()
            {
                if (!buffers.EntityExists(merge.ValueRO.from) || !buffers.EntityExists(merge.ValueRO.to)) return;

                foreach (var indexFrom in buffers[merge.ValueRO.from])
                {
                    foreach (var indexTo in buffers[merge.ValueRO.to])
                    {
                        if (indexFrom.value != indexTo.value) continue;

                        bufferIndexForRemove.Add(new IndexConnectionForRemoveBuffer { value = indexFrom.value });
                    }
                }
            }
        }

        [BurstCompile]
        [WithNone(typeof(PhysicsConstrainedBodyPair))]
        public partial struct RemoveElementBetweenSphereJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;
            [ReadOnly] public DynamicBuffer<IndexConnectionForRemoveBuffer> bufferIndexForRemove;
            public DynamicBuffer<PullElementBuffer> elementBuffer;

            public void Execute(Entity entity, [EntityIndexInQuery] int sortKey, in LocalTransform transform,
                in IndexConnectComponent indexConnect)
            {
                foreach (var index in bufferIndexForRemove)
                {
                    if (indexConnect.value != index.value) continue;

                    StaticMethod.RemoveElement(ecb, sortKey, elementBuffer, entity, transform);
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(PhysicsConstrainedBodyPair))]
        public partial struct RemoveJointBetweenSphereJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;
            [ReadOnly] public DynamicBuffer<IndexConnectionForRemoveBuffer> bufferIndexForRemove;
            public DynamicBuffer<PullJointBuffer> jointBuffer;

            public void Execute(Entity entity, [EntityIndexInQuery] int sortKey, in IndexConnectComponent indexConnect)
            {
                foreach (var index in bufferIndexForRemove)
                {
                    if (indexConnect.value != index.value) continue;

                    StaticMethod.RemoveJoint(ecb, sortKey, jointBuffer, entity);
                }
            }
        }

        [BurstCompile]
        public partial struct RemoveIndexConnectInSphereJob : IJobEntity
        {
            [ReadOnly] public DynamicBuffer<IndexConnectionForRemoveBuffer> bufferIndexForRemove;

            public void Execute(ref DynamicBuffer<IndexConnectionBuffer> buffer)
            {
                for (var i = 0; i < buffer.Length; i++)
                {
                    foreach (var index in bufferIndexForRemove)
                    {
                        if (buffer[i].value == index.value)
                            buffer.RemoveAt(i);
                    }
                }
            }
        }

        [BurstCompile]
        public partial struct ReconnectJointJob : IJobEntity
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;

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

                if (merge.ValueRO.from != bodyPair.EntityA && merge.ValueRO.from != bodyPair.EntityB) return;

                var element = merge.ValueRO.from != bodyPair.EntityA ? bodyPair.EntityA : bodyPair.EntityB;

                ecb.DestroyEntity(entity);

                StaticMethod.SetJoint(ecb, joints,merge.ValueRO.to, element, levelSetting.distanceBetweenElements,
                    indexConnect.value);

                buffers[merge.ValueRO.to].Add(new IndexConnectionBuffer { value = indexConnect.value });
            }
        }

        [BurstCompile]
        public partial struct RemoveSphereJob : IJobEntity
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;

            public EntityCommandBuffer ecb;

            public void Execute(LevelSettingAspect levelSettingAspect)
            {
                levelSettingAspect.AddSphereInPull(ecb, merge.ValueRO.from);
            }
        }

        [BurstCompile]
        public partial struct ChangeIndexSharedSphereJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            [ReadOnly] public int oldIndex;
            [ReadOnly] public int newIndex;

            public void Execute(Entity entity, in IndexSharedComponent index)
            {
                if (index.value != oldIndex) return;

                ecb.SetSharedComponent(entity, new IndexSharedComponent { value = newIndex });
            }
        }

        [BurstCompile]
        public partial struct ChangeLimitElementsInSphereJob : IJobEntity
        {
            [ReadOnly] public int oldIndex;
            [ReadOnly] public int newIndex;
            [ReadOnly] public int sumCountElements;

            public void Execute(ref SphereComponent sphere, in IndexSharedComponent index)
            {
                if (index.value != oldIndex && index.value != newIndex) return;

                sphere.countElements = sumCountElements;
            }
        }

        [BurstCompile]
        public partial struct ClearMergeComponentJob : IJobEntity
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;

            public void Execute()
            {
                merge.ValueRW.from = Entity.Null;
                merge.ValueRW.launchLastStageMerge = false;
            }
        }
    }
}