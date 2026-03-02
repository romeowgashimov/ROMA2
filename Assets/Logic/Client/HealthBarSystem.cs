using Logic.Common;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.Client
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct HealthBarSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<UIPrefabs>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Spawn health bars for entities that require them
            foreach ((LocalTransform transform, HealthBarOffset healthBarOffset,
                         MaxHealthPoints maxHealthPoints, Entity entity) in SystemAPI
                         .Query<LocalTransform, HealthBarOffset, MaxHealthPoints>()
                         .WithNone<HealthBarUIReference>()
                         .WithEntityAccess())
            {
                GameObject healthBarPrefab = SystemAPI.ManagedAPI.GetSingleton<UIPrefabs>().HealthBar;
                float3 spawnPosition = transform.Position + healthBarOffset.Value;
                GameObject newHealthBar = Object.Instantiate(healthBarPrefab, spawnPosition, Quaternion.identity);
                SetHealthBar(newHealthBar, maxHealthPoints.Value, maxHealthPoints.Value);
                ecb.AddComponent(entity, new HealthBarUIReference { Value = newHealthBar });
            }
            
            // Update positions and values of health bar
            foreach ((LocalTransform transform, HealthBarOffset healthBarOffset,
                         MaxHealthPoints maxHealthPoints, CurrentHealthPoints currentHealthPoints, 
                         HealthBarUIReference healthBarUIReference) in SystemAPI
                         .Query<LocalTransform, HealthBarOffset, MaxHealthPoints, CurrentHealthPoints, HealthBarUIReference>())
            {
                float3 healthBarPosition = transform.Position + healthBarOffset.Value;
                healthBarUIReference.Value.transform.position = healthBarPosition;
                SetHealthBar(healthBarUIReference.Value, currentHealthPoints.Value, maxHealthPoints.Value);
            }
            
            // Cleanup health bar once associated entity is destroyed
            foreach ((HealthBarUIReference healthBarUIReference, Entity entity) in SystemAPI
                         .Query<HealthBarUIReference>()
                         .WithNone<LocalTransform>()
                         .WithEntityAccess())
            {
                Object.Destroy(healthBarUIReference.Value);
                ecb.RemoveComponent<HealthBarUIReference>(entity);
            }
        }

        private void SetHealthBar(GameObject healthBarCanvasObject, int curPoints, int maxPoints)
        {
            Slider healthBarSlider = healthBarCanvasObject.GetComponentInChildren<Slider>();
            healthBarSlider.minValue = 0;
            healthBarSlider.maxValue = maxPoints;
            healthBarSlider.value = curPoints;
        }
    }
}