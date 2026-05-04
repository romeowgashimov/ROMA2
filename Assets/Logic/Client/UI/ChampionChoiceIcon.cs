using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Logic.Client.UI
{
    public class ChampionChoiceIcon : MonoBehaviour, IPointerClickHandler
    {
        public Action OnChosen;

        public void OnPointerClick(PointerEventData eventData) => OnChosen?.Invoke();

        public void RemoveAllListeners() => OnChosen = null;
    }
}