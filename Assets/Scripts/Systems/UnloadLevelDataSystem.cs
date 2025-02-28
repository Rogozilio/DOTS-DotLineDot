using Components;
using Components.DynamicBuffers;
using Components.Shared;
using Static;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Utilities;

namespace Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(SwitchSceneSystem))]
    public partial struct UnloadLevelDataSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PullSphereBuffer>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var entityPull = SystemAPI.GetSingletonEntity<PullSphereBuffer>();

            var countDisableFinish =
                SystemAPI.QueryBuilder().WithAll<LoadLevelComponent>().Build().CalculateEntityCount();

            if(countDisableFinish == 0) return;
           
            state.Dependency = new RemoveActiveJointJob
            {
                ecb = ecb,
                entityPull = entityPull
            }.Schedule(state.Dependency);
            state.Dependency = new RemoveActiveElementJob
            {
                ecb = ecb,
                entityPull = entityPull
            }.Schedule(state.Dependency);
            state.Dependency = new RemoveActiveSphereJob
            {
                ecb = ecb,
                entityPull = entityPull,
                lenghtBuffer = SystemAPI.GetSingletonBuffer<PullSphereBuffer>().Length
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(PhysicsConstrainedBodyPair))]
        public partial struct RemoveActiveJointJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            [ReadOnly] public Entity entityPull;

            public void Execute(Entity entity, in IndexConnectComponent index)
            {
                if (index.value < 0) return;

                StaticMethod.RemoveJoint(ecb, entityPull, entity);
            }
        }

        [BurstCompile]
        [WithNone(typeof(PhysicsConstrainedBodyPair))]
        public partial struct RemoveActiveElementJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            [ReadOnly] public Entity entityPull;

            public void Execute(Entity entity, in IndexConnectComponent index, in LocalTransform transform)
            {
                if (index.value < 0) return;

                var newTransform = transform;
                newTransform.Position = TransformUtility.DefaultPositionElement();
                StaticMethod.RemoveElement(ecb, entityPull, entity, newTransform);
            }
        }

        [BurstCompile]
        [WithAll(typeof(SphereComponent))]
        public partial struct RemoveActiveSphereJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            [ReadOnly] public Entity entityPull;
            public int lenghtBuffer;

            public void Execute(Entity entity, in IndexSharedComponent index, in LocalTransform transform)
            {
                if (index.value < 0) return;

                var newTransform = transform;
                newTransform.Position = TransformUtility.DefaultPositionSphere(lenghtBuffer++);
                
                StaticMethod.RemoveSphere(ecb, entityPull, entity, newTransform);
            }
        }
    }
}