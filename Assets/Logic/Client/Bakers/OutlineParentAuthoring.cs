using ROMA2.Logic.Client.Data;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Client.Bakers
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