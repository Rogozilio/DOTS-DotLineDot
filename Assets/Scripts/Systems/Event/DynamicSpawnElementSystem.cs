using Components;
using Components.DynamicBuffers;
using Components.Shared;
using Static;
using Systems.ECBSystems;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(BeforePhysicsSystemGroup), OrderFirst = true)]
    public partial struct DynamicSpawnElementSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<IsMouseMove>();
            state.RequireForUpdate<PullElementBuffer>();
            state.RequireForUpdate<LevelSettingComponent>();
            state.RequireForUpdate<EndBeforePhysicsEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndBeforePhysicsEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var spheresMoveMouse =
                SystemAPI.QueryBuilder()
                    .WithAll<IsMouseMove>()
                    .WithDisabled<IsBlockedSphere>()
                    .Build().ToEntityArray(Allocator.TempJob);
            state.Dependency = new SpawnElementJob
            {
                ecb = ecb,
                spheres = spheresMoveMouse,
                joints = SystemAPI.GetSingletonBuffer<PullJointBuffer>(),
                elements = SystemAPI.GetSingletonBuffer<PullElementBuffer>(),
                entityPull = SystemAPI.GetSingletonEntity<PullElementBuffer>(),
                levelSetting = SystemAPI.GetSingleton<LevelSettingComponent>(),
                transforms = SystemAPI.GetComponentLookup<LocalTransform>(true),
                indexes = SystemAPI.GetComponentLookup<IndexConnectComponent>(true),
            }.Schedule(state.Dependency);
            state.Dependency = spheresMoveMouse.Dispose(state.Dependency);
        }

        [BurstCompile]
        public partial struct SpawnElementJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public NativeArray<Entity> spheres;
            public DynamicBuffer<PullJointBuffer> joints;
            public DynamicBuffer<PullElementBuffer> elements;
            public Entity entityPull;
            public LevelSettingComponent levelSetting;
            [ReadOnly] public ComponentLookup<LocalTransform> transforms;
            [ReadOnly] public ComponentLookup<IndexConnectComponent> indexes;

            public void Execute(Entity entity, PhysicsConstrainedBodyPair bodyPair)
            {
                foreach (var sphere in spheres)
                {
                    if (bodyPair.EntityA != sphere && bodyPair.EntityB != sphere) return;

                    var distance = math.distance(transforms[bodyPair.EntityA].Position,
                        transforms[bodyPair.EntityB].Position);

                    if (distance > levelSetting.distanceSpawn)
                    {
                        var element = bodyPair.EntityA != sphere ? bodyPair.EntityA : bodyPair.EntityB;
                        StaticMethod.RemoveJoint(ecb, entityPull, entity);
                        var transform = transforms[sphere];
                        transform.Scale = transforms[levelSetting.prefabElement].Scale;
                        var newElement = StaticMethod.UseElement(ecb, elements, transform,
                            indexes[entity].value,
                            new IndexSharedComponent { value = levelSetting.indexShared }, "ElementNew");
                        StaticMethod.UseJoint(ecb, joints, element, newElement, levelSetting.distanceBetweenElements,
                            indexes[entity].value);
                        StaticMethod.UseJoint(ecb, joints, newElement, sphere, levelSetting.distanceBetweenElements,
                            indexes[entity].value);

                        ecb.AddComponent(newElement, new SkipFrameComponent { count = 5 });

                        //Debug.Log("123");
                    }
                }
            }
        }
    }
}