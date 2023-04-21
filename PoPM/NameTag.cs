using TMPro;
using UnityEngine;

namespace PoPM
{
    public class NameTag : MonoBehaviour
    {
        public new string name;
        public TextMeshProUGUI nameText;

        private void Start()
        {
            Plugin.Logger.LogInfo($"Adding NameTag to {name}");

            gameObject.AddComponent<TextMeshProUGUI>();
            nameText = GetComponent<TextMeshProUGUI>();
            nameText.text = name;
        }
    }
}