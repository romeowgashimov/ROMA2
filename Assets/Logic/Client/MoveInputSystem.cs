using Assets.Logic.Client;
using Logic.Common;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Logic.Client
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial class MoveInputSystem : SystemBase
    {
        private MobaInputActions _inputActions;
        private CollisionFilter _selectionFilter;
        private EntityQuery _pendingNetworkIdQuery;
        
        protected override void OnCreate()
        {
            _inputActions = new();
            _selectionFilter = new CollisionFilter
            {
              BelongsTo = 1 << 5, //Raycasts
              CollidesWith = 1 << 0  //GroundPlane
            };
        }

        protected override void OnStartRunning()
        {
            _inputActions.Enable();
            _inputActions.GameplayMap.SelectMovePosition.performed += OnSelectMovePosition;
        }

        protected override void OnStopRunning()
        {
            _inputActions.GameplayMap.SelectMovePosition.performed -= OnSelectMovePosition;
            _inputActions.Disable();
        }

        protected override void OnUpdate()
        {
            
        }

        private void OnSelectMovePosition(InputAction.CallbackContext obj)
        {
            CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            Entity cameraContainer = SystemAPI.GetSingletonEntity<MainCameraTag>();
            Camera camera = EntityManager.GetComponentData<MainCamera>(cameraContainer).Value;
            
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 100f;
            Vector3 worldPos = camera.ScreenToWorldPoint(mousePos);

            RaycastInput selectionInput = new()
            {
                Start = camera.transform.position,
                End = worldPos,
                Filter = _selectionFilter
            };

            if(collisionWorld.CastRay(selectionInput, out Unity.Physics.RaycastHit closestHit))
            {
                Entity owner = SystemAPI.GetSingletonEntity<OwnerChampTag>();
                bool flag = EntityManager.GetComponentData<InputMoveTargetPosition>(owner).Flag;
                EntityManager.SetComponentData(owner, new InputMoveTargetPosition
                {
                    Value = closestHit.Position,
                    Flag = !flag
                });
            }
        }
    }
}