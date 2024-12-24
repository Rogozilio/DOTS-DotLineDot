using System;
using Components;
using Unity.Entities;
using UnityEngine;

namespace Systems
{
    public partial class InputSystem : MonoBehaviour
    {
        private EntityManager _entityManager;

        private Camera _camera;

        private void Awake()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _camera = Camera.main;
        }

        private void Update()
        {
            var query = _entityManager.CreateEntityQuery(typeof(InputDataComponent));
            
            if(query.IsEmpty) return;
            
            var entity = query.GetSingletonEntity();
            _entityManager.SetComponentData(entity, new InputDataComponent()
            {
                isLeftMouseClicked = Input.GetMouseButtonDown(0),
                isLeftMouseUp = Input.GetMouseButtonUp(0),
                isLeftMouseDown = Input.GetMouseButton(0),
                isRightMouseClicked = Input.GetMouseButtonDown(1),
                isRightMouseUp = Input.GetMouseButtonUp(1),
                isRightMouseDown = Input.GetMouseButton(1),
                ray = _camera.ScreenPointToRay(Input.mousePosition)
            });
        }
    }
}