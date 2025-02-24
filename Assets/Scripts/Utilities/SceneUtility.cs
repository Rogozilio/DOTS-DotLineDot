using Components;
using Components.DynamicBuffers;
using Unity.Entities;
using Unity.Entities.Serialization;

namespace Utilities
{
    public static class SceneUtility
    {
        public static SceneDataComponent NextLevel(DynamicBuffer<SceneBuffer> sceneBuffer,
            SceneDataComponent currentScene)
        {
            return currentScene.indexBuffer + 1 >= sceneBuffer.Length
                ? currentScene
                : sceneBuffer[currentScene.indexBuffer + 1].value;
        }
        
        public static SceneDataComponent PrevLevel(DynamicBuffer<SceneBuffer> sceneBuffer,
            SceneDataComponent currentScene)
        {
            return currentScene.indexBuffer - 1 < 0
                ? currentScene
                : sceneBuffer[currentScene.indexBuffer - 1].value;
        }
    }
}