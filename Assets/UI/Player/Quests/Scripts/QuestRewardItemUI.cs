using TMPro;
using UnityEngine;

namespace UI.Player.Quests
{
    public class QuestRewardItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text descriptionLabel;

        private void Awake()
        {
            if (descriptionLabel == null)
                descriptionLabel = GetComponentInChildren<TMP_Text>(true);
        }

        public void SetText(string text)
        {
            if (descriptionLabel != null)
                descriptionLabel.text = text ?? string.Empty;
        }
    }
}
