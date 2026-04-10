using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Logic.Common
{
    public class ChampionChoiceIcon : MonoBehaviour, IPointerClickHandler
    {
        public Action OnChosen;

        public void OnPointerClick(PointerEventData eventData) => OnChosen?.Invoke();

        public void RemoveAllListeners() => OnChosen = null;
    }
}