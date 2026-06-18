using ROMA2.Logic.Client.Data;
using ROMA2.Logic.Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ROMA2.Logic.Client.Models
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct PortraitCameraSystem : ISystem
    {
        private const float DISTANCE_OFFSET = 1.5f; // Расстояние перед персонажем
        private const float SIDE_OFFSET = 1.0f;     // Смещение вправо от персонажа
        private const float HEIGHT_OFFSET = 0.6f;   // Высота камеры (на уровне лица/груди)
        
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<OwnerChampTag>(out Entity playerEntity)) return;
            if (!SystemAPI.TryGetSingletonEntity<PortraitCameraTag>(out Entity config)) return;
            Camera portraitCamera = SystemAPI.ManagedAPI.GetComponent<PortraitCamera>(config).Value;

            LocalToWorld playerTransform = SystemAPI.GetComponent<LocalToWorld>(playerEntity);
            float3 playerPos = playerTransform.Position;
            float3 playerForward = playerTransform.Forward;
            float3 playerUp = playerTransform.Up;
            float3 playerRight = playerTransform.Right;

            // Вычисляем целевую позицию камеры
            float3 targetCameraPosition = playerPos 
                                          + playerForward * DISTANCE_OFFSET
                                          + playerRight * SIDE_OFFSET
                                          + playerUp * HEIGHT_OFFSET;

            // Расчет направления "взгляда" камеры на персонажа.
            // Направляем камеру в центр персонажа (или чуть выше, прибавив playerUp * heightOffset)
            float3 lookTarget = playerPos + playerUp * HEIGHT_OFFSET;
            float3 lookDirection = math.normalize(lookTarget - targetCameraPosition);
            
            Quaternion targetCameraRotation = Quaternion.LookRotation(lookDirection, playerUp);
            
            portraitCamera.transform.position = targetCameraPosition;
            portraitCamera.transform.rotation = targetCameraRotation;
        }
    }
}