using UnityEngine;

namespace Tami
{
    /// <summary>Một câu thoại. speaker: mayor, minji, ongba, player, hoặc để trống cho lời dẫn.</summary>
    [System.Serializable]
    public struct DialogueLine
    {
        public string speaker;
        [TextArea] public string text;

        public DialogueLine(string speaker, string text)
        {
            this.speaker = speaker;
            this.text = text;
        }
    }
}
