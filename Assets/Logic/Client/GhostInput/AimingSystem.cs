using Assets.Logic.Client;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Logic.Common.Systems
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial struct AimingSystem : ISystem
    {
        private CollisionFilter _collisionFilter;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<MainCameraTag>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            _collisionFilter = new()
            {
                BelongsTo = 1 << 5, //Raycasts
                CollidesWith = 1 << 0 /* GroundPlane */ | 1 << 1 | 1 << 2 | 1 << 4 
            };
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach ((RefRO<LocalTransform> localTransform, RefRW<AimInput> aimInput, 
                         RefRW<SelectedEntity> selectedEntity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRW<AimInput>, RefRW<SelectedEntity>>()
                         .WithAll<AimingTag, OwnerChampTag>())
            {
                CollisionWorld collisionFilter = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
                Entity cameraEntity = SystemAPI.GetSingletonEntity<MainCameraTag>();
                Camera mainCamera = state.EntityManager.GetComponentObject<MainCamera>(cameraEntity).Value;

                Vector3 mousePosition = Input.mousePosition;
                mousePosition.z = 1000f;
                Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);

                RaycastInput selectionInput = new()
                {
                    Start = mainCamera.transform.position,
                    End = worldPosition,
                    Filter = _collisionFilter
                };
                
                if(collisionFilter.CastRay(selectionInput, out Unity.Physics.RaycastHit closestHit))
                {
                    Vector3 direction = closestHit.Position - localTransform.ValueRO.Position;
                    direction.y = 1;
                    direction = math.normalize(direction);
                    aimInput.ValueRW.Value = direction;

                    Entity hitEntity = closestHit.Entity;
                    selectedEntity.ValueRW.Value = hitEntity != Entity.Null 
                        ? hitEntity 
                        : Entity.Null;
                }
            }
        }
    }
}