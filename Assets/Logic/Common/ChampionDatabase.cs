using System;
using System.Collections.Generic;
using UnityEngine;

namespace Logic.Common
{
    [Serializable]
    public struct HeroConfigEntry
    {
        public int Id;
        public GameObject Prefab;
    }

    [CreateAssetMenu(fileName = "ChampionDatabase", menuName = "ROMA2/Champion Database")]
    public class ChampionDatabase : ScriptableObject
    {
        public List<HeroConfigEntry> Configs;
    }
}