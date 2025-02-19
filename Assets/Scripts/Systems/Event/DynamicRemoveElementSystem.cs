using Components;
using Components.DynamicBuffers;
using Static;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    [UpdateBefore(typeof(GravityInSphereSystem))]
    public partial struct DynamicRemoveElementSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LevelSettingComponent>();
            state.RequireForUpdate<RemoveElementComponent>();
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var removeElement = SystemAPI.GetSingletonRW<RemoveElementComponent>();

            state.Dependency = new CheckDistanceBetweenSphereAndElementJob
            {
                removeElement = removeElement,
                levelSetting = SystemAPI.GetSingleton<LevelSettingComponent>()
            }.Schedule(state.Dependency);
            state.Dependency = new SetRemoveElementJob
            {
                removeElement = removeElement
            }.Schedule(state.Dependency);
            state.Dependency = new RemoveElementJob
            {
                ecb = ecb,
                removeElement = removeElement,
                joints = SystemAPI.GetSingletonBuffer<PullJointBuffer>(),
                elements = SystemAPI.GetSingletonBuffer<PullElementBuffer>(),
                levelSetting = SystemAPI.GetSingleton<LevelSettingComponent>(),
                transforms = SystemAPI.GetComponentLookup<LocalTransform>(true),
                indexes = SystemAPI.GetComponentLookup<IndexConnectComponent>(true),
            }.Schedule(state.Dependency);
            state.Dependency = new ClearRemoveElementComponentJob
            {
                removeElement = removeElement
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithNone(typeof(SkipFrameComponent))]
        public partial struct CheckDistanceBetweenSphereAndElementJob : IJobEntity
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<RemoveElementComponent> removeElement;
            public LevelSettingComponent levelSetting;

            public void Execute(Entity entity, TargetGravityComponent targetGravity)
            {
                if (targetGravity.distance < levelSetting.distanceRemove)
                {
                    removeElement.ValueRW.element = entity;
                }
            }
        }

        [BurstCompile]
        public partial struct SetRemoveElementJob : IJobEntity
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<RemoveElementComponent> removeElement;

            public void Execute(Entity entity, PhysicsConstrainedBodyPair bodyPair)
            {
                if (bodyPair.EntityA != removeElement.ValueRO.element &&
                    bodyPair.EntityB != removeElement.ValueRO.element) return;

                var connectedEntity = bodyPair.EntityA == removeElement.ValueRO.element
                    ? bodyPair.EntityB
                    : bodyPair.EntityA;

                if (removeElement.ValueRO.joint1 == Entity.Null)
                {
                    removeElement.ValueRW.joint1 = entity;
                    removeElement.ValueRW.connect1 = connectedEntity;
                }
                else if (removeElement.ValueRO.joint2 == Entity.Null)
                {
                    removeElement.ValueRW.joint2 = entity;
                    removeElement.ValueRW.connect2 = connectedEntity;
                }
            }
        }

        [BurstCompile]
        public struct RemoveElementJob : IJob
        {
            public EntityCommandBuffer ecb;
            [NativeDisableUnsafePtrRestriction] public RefRW<RemoveElementComponent> removeElement;
            public DynamicBuffer<PullJointBuffer> joints;
            public DynamicBuffer<PullElementBuffer> elements;
            public LevelSettingComponent levelSetting;
            [ReadOnly] public ComponentLookup<LocalTransform> transforms;
            [ReadOnly] public ComponentLookup<IndexConnectComponent> indexes;

            public void Execute()
            {
                if (removeElement.ValueRO.element == Entity.Null
                    || removeElement.ValueRO.joint1 == Entity.Null
                    || removeElement.ValueRO.joint2 == Entity.Null
                    || removeElement.ValueRO.connect1 == Entity.Null
                    || removeElement.ValueRO.connect2 == Entity.Null) return;

                StaticMethod.RemoveJoint(ecb, joints, removeElement.ValueRO.joint1);
                StaticMethod.RemoveJoint(ecb, joints, removeElement.ValueRO.joint2);
                StaticMethod.RemoveElement(ecb, elements, removeElement.ValueRO.element,
                    transforms[removeElement.ValueRO.element]);
                //TODO: maxDistance replace
                StaticMethod.SetJoint(ecb, joints, removeElement.ValueRO.connect1, removeElement.ValueRO.connect2,
                    levelSetting.distanceBetweenElements, indexes[removeElement.ValueRO.element].value);
                // Debug.LogWarning("Element " + removeElement.ValueRO.element +
                //                  " joint1 " + removeElement.ValueRO.joint1 +
                //                  " joint2 " + removeElement.ValueRO.joint2 +
                //                  " connect1 " + removeElement.ValueRO.connect1 +
                //                  " connect2 " + removeElement.ValueRO.connect2);
            }
        }

        [BurstCompile]
        public struct ClearRemoveElementComponentJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public RefRW<RemoveElementComponent> removeElement;

            public void Execute()
            {
                removeElement.ValueRW.element = Entity.Null;
                removeElement.ValueRW.joint1 = Entity.Null;
                removeElement.ValueRW.joint2 = Entity.Null;
                removeElement.ValueRW.connect1 = Entity.Null;
                removeElement.ValueRW.connect2 = Entity.Null;
            }
        }
    }
}