using Unity.Entities;
using UnityEngine;

namespace Logic.Common
{
    public class NpcContainerAuthoring : MonoBehaviour
    {
        private class NpcContainerBaker : Baker<NpcContainerAuthoring>
        {
            public override void Bake(NpcContainerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent<NpcsContainer>(entity);
                AddBuffer<RedNpcBufferElement>(entity);
                AddBuffer<BlueNpcBufferElement>(entity);
            }
        }
    }
}