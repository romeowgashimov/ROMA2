using Unity.Entities;
using UnityEngine;

namespace Logic.Common
{
    public class AbilityMoveSpeedAuthoring : MonoBehaviour
    {
        public float MoveSpeed;
        
        public partial class AbilityMoveSpeedBaker : Baker<AbilityMoveSpeedAuthoring>
        {
            public override void Bake(AbilityMoveSpeedAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new AbilityMoveSpeed { Value = authoring.MoveSpeed });
            }
        }
    }
}