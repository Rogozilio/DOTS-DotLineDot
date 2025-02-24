using System.Collections.Generic;
using Components;
using Components.DynamicBuffers;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEditor;
using UnityEngine;

namespace Baker
{
    public class SceneManagerAuthoring : MonoBehaviour
    {
        public SceneAsset mainScene;
        public List<SceneAsset> scenes;

        private class ListScenesAuthoringBaker : Baker<SceneManagerAuthoring>
        {
            public override void Bake(SceneManagerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var data = new SceneDataComponent
                {
                    indexBuffer = 0,
                    sceneReference = new EntitySceneReference(authoring.mainScene)
                };
                AddComponent(entity, new CurrentSceneComponent
                {
                    data = data
                });
                AddComponent(entity, new LoadLevelComponent
                {
                    data = data
                });
                AddBuffer<SceneBuffer>(entity);
                var buffer = SetBuffer<SceneBuffer>(entity);
                for (var i = 0; i < authoring.scenes.Count; i++)
                {
                    buffer.Add(new SceneBuffer
                    {
                        value = new SceneDataComponent
                        {
                            indexBuffer = i,
                            sceneReference = new EntitySceneReference(authoring.scenes[i])
                        }
                    });
                }
            }
        }
    }
}