using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Tami.UIKit;

namespace Tami
{
    /// <summary>Hộp hội thoại cốt truyện (GDD mục 2).</summary>
    public partial class UIManager
    {
        readonly Queue<(DialogueLine[] lines, System.Action onDone)> dialogueQueue = new Queue<(DialogueLine[], System.Action)>();
        // Chiều cao hộp thoại: tối thiểu đủ cho chân dung, tối đa không che quá nửa màn hình.
        const float DialogueMinHeight = 300f;
        const float DialogueMaxHeight = 560f;

        GameObject dialogueRoot;
        RectTransform dialoguePanel;
        Image portrait;
        Text speakerText, bodyText;
        Image speakerPlate;
        DialogueLine[] currentLines;
        System.Action currentDone;
        int lineIndex;

        public bool DialogueOpen => dialogueRoot != null && dialogueRoot.activeSelf;

        public void ShowDialogue(DialogueLine[] lines, System.Action onDone = null)
        {
            if (lines == null || lines.Length == 0)
            {
                onDone?.Invoke();
                return;
            }
            dialogueQueue.Enqueue((lines, onDone));
            if (!DialogueOpen) NextDialogue();
        }

        void NextDialogue()
        {
            if (dialogueQueue.Count == 0)
            {
                if (dialogueRoot != null) dialogueRoot.SetActive(false);
                return;
            }
            var (lines, onDone) = dialogueQueue.Dequeue();
            currentLines = lines;
            currentDone = onDone;
            lineIndex = 0;
            EnsureDialogueUI();
            dialogueRoot.SetActive(true);
            dialogueRoot.transform.SetAsLastSibling();
            ShowLine();
        }

        void EnsureDialogueUI()
        {
            if (dialogueRoot != null) return;
            var rootRect = NewRect(overlayLayer, "Dialogue");
            Fill(rootRect);
            dialogueRoot = rootRect.gameObject;

            // Lớp chắn trong suốt: chạm đâu cũng sang câu tiếp, không làm tối màn hình.
            var blocker = Img(rootRect, null, new Color(0f, 0f, 0f, 0f), "Blocker");
            Fill(blocker.rectTransform);
            var next = blocker.gameObject.AddComponent<Button>();
            next.transition = Selectable.Transition.None;
            next.onClick.AddListener(AdvanceDialogue);

            // Khung 9-slice: góc bo và viền giữ nguyên, phần giữa giãn theo lượng chữ.
            var frame = Game.I.art.dialogBox;
            var panelImage = Img(rootRect, frame, frame != null ? Color.white : new Color(0.98f, 0.95f, 0.88f, 0.98f), "Panel");
            if (frame != null)
            {
                panelImage.type = Image.Type.Sliced;
                panelImage.preserveAspect = false;
                panelImage.pixelsPerUnitMultiplier = 1.6f;
            }
            var panel = panelImage.rectTransform;
            Stretch(panel, Vector2.zero, new Vector2(1f, 0f), new Vector2(24f, 24f), new Vector2(-24f, 24f + DialogueMinHeight));
            dialoguePanel = panel;
            var panelButton = panel.gameObject.AddComponent<Button>();
            panelButton.transition = Selectable.Transition.None;
            panelButton.onClick.AddListener(AdvanceDialogue);

            portrait = Img(panel, null, Color.white, "Portrait");
            portrait.raycastTarget = false;
            portrait.preserveAspect = true;
            Place(portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 10f), new Vector2(230f, 230f));

            // Bảng tên người nói: khung viền đậm, co giãn theo độ dài tên.
            if (NameSprite != null)
            {
                speakerPlate = Framed(Img(panel, null, Color.white, "NamePlate"), NameSprite);
                speakerPlate.raycastTarget = false;
            }
            speakerText = Label(panel, "", 44, Accent, TextAnchor.MiddleLeft);
            speakerText.fontStyle = FontStyle.Bold;
            Stretch(speakerText.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(280f, -90f), new Vector2(-200f, -26f));

            bodyText = Label(panel, "", 40, Ink, TextAnchor.UpperLeft);
            Stretch(bodyText.rectTransform, Vector2.zero, Vector2.one, new Vector2(280f, 60f), new Vector2(-36f, -96f));

            var hint = Label(panel, "Chạm để tiếp tục »", 28, Muted, TextAnchor.LowerRight);
            Stretch(hint.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 16f), new Vector2(-32f, 60f));

            var skip = Btn(panel, "Bỏ qua", Off, SkipDialogue, 30);
            Place((RectTransform)skip.transform, Vector2.one, Vector2.one, new Vector2(-24f, -20f), new Vector2(170f, 70f));
        }

        void ShowLine()
        {
            var line = currentLines[lineIndex];
            var (name, sprite) = Speaker(line.speaker);
            portrait.sprite = sprite;
            portrait.enabled = sprite != null;
            speakerText.text = name;
            bodyText.text = line.text;
            var bodyRect = bodyText.rectTransform;
            float left = sprite != null ? 280f : 40f;
            bodyRect.offsetMin = new Vector2(left, bodyRect.offsetMin.y);
            speakerText.rectTransform.offsetMin = new Vector2(left, speakerText.rectTransform.offsetMin.y);
            if (speakerPlate != null)
            {
                speakerPlate.enabled = !string.IsNullOrEmpty(name);
                float width = speakerText.preferredWidth + 56f;
                Place(speakerPlate.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(left - 28f, -18f), new Vector2(width, 80f));
            }
            FitDialogueToText();
        }

        /// <summary>Kéo hộp thoại cao/thấp theo số dòng của câu thoại (khung 9-slice nên góc không bị méo).</summary>
        void FitDialogueToText()
        {
            // Chiều rộng vùng chữ không đổi khi đổi chiều cao, nên tính trước được số dòng cần.
            var bodyRect = bodyText.rectTransform;
            float width = bodyRect.rect.width;
            if (width <= 0f) width = dialoguePanel.rect.width - bodyRect.offsetMin.x + bodyRect.offsetMax.x;
            var settings = bodyText.GetGenerationSettings(new Vector2(width, 0f));
            float textHeight = bodyText.cachedTextGeneratorForLayout.GetPreferredHeight(bodyText.text, settings) / bodyText.pixelsPerUnit;
            // Khoảng trên cho tên người nói (96) và dưới cho dòng "Chạm để tiếp tục" (60).
            float height = Mathf.Clamp(96f + textHeight + 64f, DialogueMinHeight, DialogueMaxHeight);
            dialoguePanel.offsetMax = new Vector2(dialoguePanel.offsetMax.x, dialoguePanel.offsetMin.y + height);
        }

        (string name, Sprite sprite) Speaker(string key)
        {
            var art = Game.I.art;
            switch (key)
            {
                case "mayor": return ("Bác Trưởng phố", art.portraitMayor);
                case "minji": return ("Chị Min-ji", art.portraitMinji);
                case "ongba": return ("Ông Ba", art.portraitOngBa);
                case "player": return ("Bạn", art.portraitPlayer);
                default: return ("", null);
            }
        }

        void AdvanceDialogue()
        {
            if (!DialogueOpen) return;
            lineIndex++;
            if (lineIndex < currentLines.Length)
            {
                ShowLine();
                return;
            }
            FinishCurrentDialogue();
        }

        void SkipDialogue()
        {
            if (!DialogueOpen) return;
            FinishCurrentDialogue();
        }

        void FinishCurrentDialogue()
        {
            var done = currentDone;
            currentDone = null;
            dialogueRoot.SetActive(false);
            done?.Invoke();
            if (!DialogueOpen) NextDialogue();
        }

        /// <summary>Đóng mọi hội thoại đang chờ, vẫn gọi các callback (dùng khi chạy thử).</summary>
        public void DismissAllDialogues()
        {
            int guard = 0;
            while ((DialogueOpen || dialogueQueue.Count > 0) && guard++ < 100)
            {
                if (DialogueOpen) FinishCurrentDialogue();
                else NextDialogue();
            }
        }

    }
}
