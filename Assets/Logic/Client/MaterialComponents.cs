using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Logic.Client
{
    [MaterialProperty("_OutlineWidth")]
    public struct OutlineWidth : IComponentData
    {
        public float Value;
    }

    [MaterialProperty("_Color")]
    public struct OutlineColor : IComponentData
    {
        public Color Value;
    }

    public struct OutlineEntityContainer : IComponentData
    {
        public Entity Value;
    }

    public struct LastOutlinedEntity : IComponentData
    {
        public Entity Value;
    }
}