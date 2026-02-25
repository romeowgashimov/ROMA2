using Assets.Logic.Client;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Logic.Common
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial struct AimSkillShotSystem : ISystem
    {
        private CollisionFilter _collisionFilter;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MainCameraTag>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            _collisionFilter = new()
            {
                BelongsTo = 1 << 5, //Raycasts
                CollidesWith = 1 << 0 //GroundPlane
            };
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach ((RefRW<AimInput> aimInput, RefRW<LocalTransform> localTransform) in SystemAPI
                         .Query<RefRW<AimInput>, RefRW<LocalTransform>>()
                         .WithAll<AimSkillShotTag, OwnerChampTag>())
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
                    direction.y = localTransform.ValueRO.Position.y;
                    direction = math.normalize(direction);
                    aimInput.ValueRW.Value = direction;
                }
            }
        }
    }
}