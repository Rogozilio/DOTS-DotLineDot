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
                isMouseClicked = Input.GetMouseButtonDown(0),
                isMouseUp = Input.GetMouseButtonUp(0),
                isMouseDown = Input.GetMouseButton(0),
                mousePosition = Input.mousePosition,
                ray = _camera.ScreenPointToRay(Input.mousePosition)
            });
        }
    }
}