using ROMA2.Logic.Data;
using Unity.Entities;
using UnityEngine;

public class AttackCommandContainerAuthoring : MonoBehaviour
{
    public GameObject AttackCommand;

    private class AttackCommandContainerBaker : Baker<AttackCommandContainerAuthoring>
    {
        public override void Bake(AttackCommandContainerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent<AttackCommandContainerTag>(entity, new()
            {
                Value = GetEntity(authoring.AttackCommand, TransformUsageFlags.None)
            });
        }
    }
}