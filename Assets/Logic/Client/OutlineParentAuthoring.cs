using Unity.Entities;
using UnityEngine;

namespace Logic.Client
{
    public class OutlineParentAuthoring : MonoBehaviour
    {
        public GameObject OutlinePrefab;
        
        private class OutlineBaker : Baker<OutlineParentAuthoring>
        {
            public override void Bake(OutlineParentAuthoring parentAuthoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent<OutlineEntityContainer>(entity, new()
                {
                    Value = GetEntity(parentAuthoring.OutlinePrefab, TransformUsageFlags.Renderable)
                });
            }
        }
    }
}