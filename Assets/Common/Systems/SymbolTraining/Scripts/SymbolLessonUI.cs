using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Common.Systems.SymbolTraining
{
    public class SymbolLessonUI : MonoBehaviour
    {
        [SerializeField] private GameObject lessonPanel;
        [SerializeField] private RawImage referenceImage;
        [SerializeField] private TMP_Text successCounterText;
        [SerializeField] private string successFormat = "Successfully drawn symbols: {0}/{1}";
        [SerializeField] private bool clearOnLessonEnd = true;
        [SerializeField] private bool matchSpriteNativeSize = true;

        private SymbolLesson activeLesson;

        private void Awake()
        {
            if (lessonPanel == null)
                lessonPanel = gameObject;

            SetLessonPanelActive(false);
        }

        public void ShowLesson(SymbolLesson lesson, int successfulAttempts)
        {
            activeLesson = lesson;

            if (lesson == null)
            {
                ClearUI();
                return;
            }

            SetLessonPanelActive(true);
            ApplyReferenceSprite(lesson.ReferenceSprite);
            UpdateSuccessCounter(successfulAttempts);
        }

        public void UpdateProgress(int successfulAttempts)
        {
            if (activeLesson == null)
                return;

            UpdateSuccessCounter(successfulAttempts);
        }

        public void EndLesson()
        {
            if (clearOnLessonEnd)
            {
                ClearUI();
            }
            else
            {
                activeLesson = null;
                SetLessonPanelActive(false);
            }
        }

        private void ApplyReferenceSprite(Sprite sprite)
        {
            if (referenceImage == null)
                return;

            if (sprite != null)
            {
                referenceImage.texture = sprite.texture;
                referenceImage.enabled = true;

                if (matchSpriteNativeSize && referenceImage.rectTransform != null)
                    referenceImage.rectTransform.sizeDelta = sprite.rect.size;

                Rect spriteRect = sprite.rect;
                Rect uvRect = new Rect(
                    spriteRect.x / sprite.texture.width,
                    spriteRect.y / sprite.texture.height,
                    spriteRect.width / sprite.texture.width,
                    spriteRect.height / sprite.texture.height);
                referenceImage.uvRect = uvRect;
            }
            else
            {
                referenceImage.texture = null;
                referenceImage.enabled = false;
                referenceImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            }
        }

        private void UpdateSuccessCounter(int successfulAttempts)
        {
            if (successCounterText == null)
                return;

            if (activeLesson == null)
            {
                successCounterText.text = string.Empty;
                return;
            }

            int required = Mathf.Max(activeLesson.RequiredSuccessfulAttempts, 1);
            int clampedSuccess = Mathf.Clamp(successfulAttempts, 0, required);
            successCounterText.text = string.Format(successFormat, clampedSuccess, required);
        }

        private void ClearUI()
        {
            activeLesson = null;
            ApplyReferenceSprite(null);

            if (successCounterText != null)
                successCounterText.text = string.Empty;

            SetLessonPanelActive(false);
        }

        private void SetLessonPanelActive(bool isActive)
        {
            if (lessonPanel == null)
                return;

            if (lessonPanel.activeSelf != isActive)
                lessonPanel.SetActive(isActive);
        }
    }
}
