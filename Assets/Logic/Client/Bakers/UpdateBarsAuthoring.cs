using ROMA2.Logic.Client.Data;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Client.Bakers
{
    public class UpdateBarsAuthoring : MonoBehaviour
    {
        private class UpdateBarsAuthoringBaker : Baker<UpdateBarsAuthoring>
        {
            public override void Bake(UpdateBarsAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<UpdatedHP4UI>(entity);
                AddComponent<UpdatedMana4UI>(entity);
            }
        }
    }
}