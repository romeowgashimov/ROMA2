using ROMA2.Logic.Navigation;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ROMA2.Logic.Common.Bakers
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