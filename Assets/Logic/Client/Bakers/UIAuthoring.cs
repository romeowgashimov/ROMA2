using ROMA2.Logic.Client.Data;
using Unity.Entities;
using UnityEngine;

namespace ROMA2.Logic.Client.Bakers
{
    public class UIAuthoring : MonoBehaviour
    {
        private class UIBaker : Baker<UIAuthoring>
        {
            public override void Bake(UIAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<UpdatedHP4UI>(entity);
                AddComponent<UpdatedMana4UI>(entity);
                AddComponent<UpdatedChars>(entity);
                // Нужен для отобржанеия урона
                AddBuffer<CachedDamageElement>(entity);
            }
        }
    }
}