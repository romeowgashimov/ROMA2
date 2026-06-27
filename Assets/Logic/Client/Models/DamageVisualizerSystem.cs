using ROMA2.Logic.Client.Controllers;
using ROMA2.Logic.Client.Data;
using ROMA2.Logic.Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace ROMA2.Logic.Client.Models
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct DamageVisualizerSystem : ISystem
    {
        private static readonly float3 DAMAGE_WORLD_OFFSET = new(1.65f, -0.15f, -0.5f);

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<UIPrefabs>();
            state.RequireForUpdate<MainCamera>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            GameObject visualizerPrefab = SystemAPI.ManagedAPI.GetSingleton<UIPrefabs>().Visualizer;
            Camera mainCamera = SystemAPI.ManagedAPI.GetSingleton<MainCamera>().Value;
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (DynamicBuffer<CachedDamageElement> processedDamages in SystemAPI
                        .Query<DynamicBuffer<CachedDamageElement>>()
                        .WithAll<GhostOwnerIsLocal>()
                        .WithNone<DestroyEntityTag>())
            {
                foreach(CachedDamageElement element in processedDamages)
                {
                    Entity receiver = element.Receiver;
                    if (SystemAPI.HasComponent<DestroyEntityTag>(receiver) 
                        || !SystemAPI.HasComponent<LocalTransform>(receiver)) continue;

                    if (!SystemAPI.ManagedAPI.HasComponent<DamageVisualizerUIReference>(receiver))
                    {
                        GameObject newVisualizer = Object.Instantiate(visualizerPrefab);
                        UpdateVisualizerPosition(newVisualizer, SystemAPI
                            .GetComponent<LocalTransform>(receiver).Position, mainCamera);
                        CachedDamage cached = new()
                        {
                            PhysicalDamage = element.PhysicalDamage,
                            MagicalDamage = element.MagicalDamage,
                            TrueDamage = element.TrueDamage,
                            LifeTime = 1f
                        };
                        SetDamageVisualizer(newVisualizer, cached);
                        ecb.AddComponent(receiver, new DamageVisualizerUIReference { Value = newVisualizer });
                        ecb.AddComponent(receiver, cached);
                    }
                    else
                    {
                        CachedDamage cached = SystemAPI.GetComponent<CachedDamage>(receiver);
                        GameObject currVisualizer = SystemAPI.ManagedAPI
                            .GetComponent<DamageVisualizerUIReference>(receiver).Value;

                        cached.PhysicalDamage += element.PhysicalDamage;
                        cached.MagicalDamage += element.MagicalDamage;
                        cached.TrueDamage += element.TrueDamage;
                        cached.LifeTime = 1f;

                        SetDamageVisualizer(currVisualizer, cached);
                        ecb.AddComponent(receiver, cached);
                    }
                }

                // Если хотим ещё что-нибудь сделать с UI уроном, 
                // то нужно сделать отдельную систему очистки кешированного урона
                processedDamages.Clear();
            }

            foreach ((LocalTransform transform, DamageVisualizerUIReference visualizer, 
                RefRW<CachedDamage> cached, Entity entity) in SystemAPI
                    .Query<LocalTransform, DamageVisualizerUIReference, RefRW<CachedDamage>>()
                    .WithNone<DestroyEntityTag>()
                    .WithEntityAccess())
            {
                UpdateVisualizerPosition(visualizer.Value, transform.Position, mainCamera);
                cached.ValueRW.LifeTime -= deltaTime;

                if (cached.ValueRO.LifeTime <= 0)
                {
                    Object.Destroy(visualizer.Value);
                    ecb.RemoveComponent<DamageVisualizerUIReference>(entity);
                    ecb.RemoveComponent<CachedDamage>(entity);
                }
            }

            foreach ((DamageVisualizerUIReference UIReference, Entity entity) in SystemAPI
                    .Query<DamageVisualizerUIReference>()
                    .WithNone<LocalTransform>()
                    .WithEntityAccess())
            {
                Object.Destroy(UIReference.Value);
                ecb.RemoveComponent<DamageVisualizerUIReference>(entity);
            }
        }

        private void SetDamageVisualizer(GameObject visualizerCanvas, CachedDamage element)
        {
            DamageVisualizer visualizer = visualizerCanvas.GetComponent<DamageVisualizer>();
            if (element.PhysicalDamage > 0)
                visualizer.ChangeDamage(DamageType.Physical, element.PhysicalDamage);
            if (element.MagicalDamage > 0)
                visualizer.ChangeDamage(DamageType.Magical, element.MagicalDamage);
            if (element.TrueDamage > 0)
                visualizer.ChangeDamage(DamageType.True, element.TrueDamage);
        }

        private void UpdateVisualizerPosition(GameObject visualizerCanvas, float3 worldPosition, Camera camera)
        {
            if (visualizerCanvas == null) return;

            Vector3 targetWorldPos = (Vector3)(worldPosition + DAMAGE_WORLD_OFFSET);
            visualizerCanvas.transform.position = targetWorldPos;

            if (camera != null)
                visualizerCanvas.transform.rotation = camera.transform.rotation;
        }
    }
}