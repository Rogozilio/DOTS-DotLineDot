using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
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
                merges = SystemAPI.GetComponentLookup<MergeComponent>(),
            }.Schedule(simulationSingleton, state.Dependency);
        }

        [BurstCompile]
        struct TriggerEvent : ITriggerEventsJob
        {
            [ReadOnly] public ComponentLookup<SphereComponent> spheres;
            public ComponentLookup<FinishComponent> finishes;
            public ComponentLookup<MergeComponent> merges;

            public void Execute(Unity.Physics.TriggerEvent triggerEvent)
            {
                CollisionSphereWithSphere(triggerEvent);
                CollisionSphereWithFinish(triggerEvent);
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

                if (!merges.IsComponentEnabled(sphere1))
                {
                    merges[sphere1] = new MergeComponent { target = sphere2 };
                }

                if (!merges.IsComponentEnabled(sphere2))
                {
                    merges[sphere2] = new MergeComponent { target = sphere1 };
                }
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

                finishes[finish] = new FinishComponent { sphere = sphere };
            }
        }
    }
}