using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Logic.Common
{
    public class GridAuthoring : MonoBehaviour
    {
        public int2 GridSize = new(100, 100);

        private class GridBaker : Baker<GridAuthoring>
        {
            public override void Bake(GridAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent<GridTag>(entity);
                AddComponent<GridSize>(entity,  new() { Value = authoring.GridSize });
            }
        }
    }
}