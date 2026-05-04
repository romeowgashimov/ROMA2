using System;
using System.Collections.Generic;
using UnityEngine;

namespace Logic.Common
{
    [Serializable]
    public struct ChampionConfigEntry
    {
        public int Id;
        public GameObject Prefab;
    }

    [CreateAssetMenu(fileName = "ChampionDatabase", menuName = "ROMA2/Champion Database")]
    public class ChampionDatabase : ScriptableObject
    {
        public List<ChampionConfigEntry> Configs;
    }
}