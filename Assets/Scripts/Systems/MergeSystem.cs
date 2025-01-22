using Components;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
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
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var merge = SystemAPI.GetSingletonRW<MergeSphereComponent>();

            state.Dependency = new SetMergeSphereFromJob
            {
                merge = merge
            }.Schedule(state.Dependency);
            state.Dependency = new SetMergeSphereToJob
            {
                merge = merge
            }.Schedule(state.Dependency);
            state.Dependency = new SphereMovesIntoSphereJob
            {
                merge = merge,
                deltaTime = SystemAPI.Time.fixedDeltaTime,
                localTransforms = SystemAPI.GetComponentLookup<LocalTransform>(),
                isCollisionWithSpheres = SystemAPI.GetComponentLookup<IsCollisionWithSphere>(),
                isMouseMoves = SystemAPI.GetComponentLookup<IsMouseMove>(),
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(IsMouseMove))]
        public partial struct SetMergeSphereFromJob : IJobEntity
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;

            public void Execute(Entity entity)
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

            public void Execute(Entity entity)
            {
                if (merge.ValueRW.from == entity) return;

                merge.ValueRW.to = entity;
            }
        }

        [BurstCompile]
        public struct SphereMovesIntoSphereJob : IJob
        {
            public ComponentLookup<LocalTransform> localTransforms;

            [ReadOnly] public float deltaTime;
            [ReadOnly] public ComponentLookup<IsCollisionWithSphere> isCollisionWithSpheres;
            [ReadOnly] public ComponentLookup<IsMouseMove> isMouseMoves;

            [NativeDisableUnsafePtrRestriction] public RefRW<MergeSphereComponent> merge;

            public void Execute()
            {
                if (merge.ValueRW.from == Entity.Null || merge.ValueRW.to == Entity.Null) return;

                if (!isCollisionWithSpheres.IsComponentEnabled(merge.ValueRW.from) ||
                    !isCollisionWithSpheres.IsComponentEnabled(merge.ValueRW.to))
                    return;

                if (isMouseMoves.IsComponentEnabled(merge.ValueRW.from) ||
                    isMouseMoves.IsComponentEnabled(merge.ValueRW.to))
                    return;

                float3 direction =
                    math.normalize(localTransforms[merge.ValueRW.to].Position -
                                   localTransforms[merge.ValueRW.from].Position);
                float distance = math.distance(localTransforms[merge.ValueRW.to].Position,
                    localTransforms[merge.ValueRW.from].Position);

                var transform = localTransforms[merge.ValueRW.from];

                if (distance > 5f * deltaTime)
                    transform.Position += direction * 5f * deltaTime;
                else
                    transform.Position = localTransforms[merge.ValueRW.to].Position;

                localTransforms[merge.ValueRW.from] = transform;
            }
        }
    }
}