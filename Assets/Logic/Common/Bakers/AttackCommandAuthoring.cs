using ROMA2.Logic.Data;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Common.Combat
{
    public class AttackCommandAuthoring : MonoBehaviour
    {
        private class AttackCommandBaker : Baker<AttackCommandAuthoring>
        {
            public override void Bake(AttackCommandAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent<AttackCommand>(entity);
    
                AddComponent<NewAttackCommand>(entity);

                AddComponent<ProcessedAttackCommand>(entity);
                SetComponentEnabled<ProcessedAttackCommand>(entity, false); 
    
                AddComponent<BasicAttackCommand>(entity);
                SetComponentEnabled<BasicAttackCommand>(entity, true); 
            }
        }
    }
}