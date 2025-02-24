using Components;
using Components.DynamicBuffers;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
    public partial struct TriggerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
            state.Dependency = new TriggerEvent
            {
                spheres = SystemAPI.GetComponentLookup<SphereComponent>(true),
                finishes = SystemAPI.GetComponentLookup<FinishComponent>(),
                targetsGravity = SystemAPI.GetComponentLookup<TargetGravityComponent>(),
                localToWorlds = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                indexesBuffer = SystemAPI.GetBufferLookup<IndexConnectionBuffer>(true),
                indexesElement = SystemAPI.GetComponentLookup<IndexConnectComponent>(true),
                isCollisionWithSpheres = SystemAPI.GetComponentLookup<IsCollisionWithSphere>(),
            }.Schedule(simulationSingleton, state.Dependency);
        }

        [BurstCompile]
        struct TriggerEvent : ITriggerEventsJob
        {
            [ReadOnly] public ComponentLookup<SphereComponent> spheres;
            public ComponentLookup<FinishComponent> finishes;
            public ComponentLookup<TargetGravityComponent> targetsGravity;
            [ReadOnly] public ComponentLookup<LocalToWorld> localToWorlds;

            [ReadOnly] public BufferLookup<IndexConnectionBuffer> indexesBuffer;
            [ReadOnly] public ComponentLookup<IndexConnectComponent> indexesElement;

            public ComponentLookup<IsCollisionWithSphere> isCollisionWithSpheres;

            public void Execute(Unity.Physics.TriggerEvent triggerEvent)
            {
                CollisionSphereWithElement(triggerEvent);
                CollisionSphereWithSphere(triggerEvent);
                CollisionSphereWithFinish(triggerEvent);
            }

            private void CollisionSphereWithElement(Unity.Physics.TriggerEvent triggerEvent)
            {
                var sphere = Entity.Null;
                var element = Entity.Null;

                if (spheres.HasComponent(triggerEvent.EntityA))
                    sphere = triggerEvent.EntityA;
                if (spheres.HasComponent(triggerEvent.EntityB))
                    sphere = triggerEvent.EntityB;
                if (targetsGravity.HasComponent(triggerEvent.EntityA))
                    element = triggerEvent.EntityA;
                if (targetsGravity.HasComponent(triggerEvent.EntityB))
                    element = triggerEvent.EntityB;

                if (Entity.Null.Equals(sphere) || Entity.Null.Equals(element))
                    return;

                var isEqualsIndex = false;

                for (var i = 0; i < indexesBuffer[sphere].Length; i++)
                {
                    if (indexesBuffer[sphere][i].value == indexesElement[element].value)
                    {
                        isEqualsIndex = true;
                        break;
                    }
                }

                if (!isEqualsIndex) return;

                var newTarget = new TargetGravityComponent
                {
                    target = sphere, position = localToWorlds[sphere].Position,
                    distance = math.distance(localToWorlds[sphere].Position, localToWorlds[element].Position)
                };
                targetsGravity[element] = newTarget;
                targetsGravity.SetComponentEnabled(element, true);
            }

            private void CollisionSphereWithSphere(Unity.Physics.TriggerEvent triggerEvent)
            {
                var sphere1 = Entity.Null;
                var sphere2 = Entity.Null;

                if (spheres.HasComponent(triggerEvent.EntityA))
                    sphere1 = triggerEvent.EntityA;
                if (spheres.HasComponent(triggerEvent.EntityB))
                    sphere2 = triggerEvent.EntityB;

                if (Entity.Null.Equals(sphere1) || Entity.Null.Equals(sphere2))
                    return;

                isCollisionWithSpheres.SetComponentEnabled(sphere1, true);
                isCollisionWithSpheres.SetComponentEnabled(sphere2, true);
            }
            
            private void CollisionSphereWithFinish(Unity.Physics.TriggerEvent triggerEvent)
            {
                var sphere = Entity.Null;
                var finish = Entity.Null;

                if (spheres.HasComponent(triggerEvent.EntityA))
                    sphere = triggerEvent.EntityA;
                if (spheres.HasComponent(triggerEvent.EntityB))
                    sphere = triggerEvent.EntityB;
                if (finishes.HasComponent(triggerEvent.EntityA))
                    finish = triggerEvent.EntityA;
                if (finishes.HasComponent(triggerEvent.EntityB))
                    finish = triggerEvent.EntityB;

                if (Entity.Null.Equals(sphere) || Entity.Null.Equals(finish))
                    return;
                
                var finishComponent = finishes[finish];
                finishComponent.sphere = sphere;
                finishes[finish] = finishComponent;
            }
        }
    }
}