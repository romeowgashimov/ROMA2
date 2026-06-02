using ROMA2.Logic.Data;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Bakers
{
    public class AbilityMoveSpeedAuthoring : MonoBehaviour
    {
        public float MoveSpeed;
        
        public class AbilityMoveSpeedBaker : Baker<AbilityMoveSpeedAuthoring>
        {
            public override void Bake(AbilityMoveSpeedAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new AbilityMoveSpeed { Value = authoring.MoveSpeed });
            }
        }
    }
}