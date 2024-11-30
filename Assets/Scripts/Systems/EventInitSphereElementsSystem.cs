using Components;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace Systems
{
    public partial struct EventInitSphereElementsSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            new CreateElementsJob { ecb = ecb}.Schedule();
            
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private partial struct CreateElementsJob : IJobEntity
        {
            internal EntityCommandBuffer ecb;

            private void Execute(Entity e, in MultiSphereComponent sphere, in TagInitMultiSphere tag)
            {
                for (byte i = 0; i < sphere.countElements; i++)
                {
                    var newElement = ecb.Instantiate(sphere.prefabElement);
                    ecb.SetName(newElement, "Element " + i);
                    ecb.AddComponent(newElement, new ElementComponent()
                    {
                        id = i
                    });
                }
                
                ecb.RemoveComponent<TagInitMultiSphere>(e);
                ecb.AddComponent<TagInitConnectElements>(e);
            }
        }
    }
}