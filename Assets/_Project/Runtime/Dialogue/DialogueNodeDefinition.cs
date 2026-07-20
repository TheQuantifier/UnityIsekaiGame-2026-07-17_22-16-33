using System.Collections.Generic;
using UnityEngine;

namespace UnityIsekaiGame.Dialogue
{
    [CreateAssetMenu(fileName = "DialogueNode", menuName = "Unity Isekai/Dialogue/Dialogue Node")]
    public sealed class DialogueNodeDefinition : ScriptableObject
    {
        [SerializeField] private string speakerName = "Speaker";
        [SerializeField, TextArea(2, 6)] private string dialogueText;
        [SerializeField] private Sprite portrait;
        [SerializeField] private DialogueNodeDefinition[] nextNodes;
        [SerializeField] private DialogueChoice[] choices;
        [SerializeField] private bool endsConversation;
        [SerializeField] private bool canCancel = true;

        public string SpeakerName => string.IsNullOrWhiteSpace(speakerName) ? "Speaker" : speakerName;
        public string DialogueText => dialogueText;
        public Sprite Portrait => portrait;
        public IReadOnlyList<DialogueNodeDefinition> NextNodes => nextNodes ?? System.Array.Empty<DialogueNodeDefinition>();
        public IReadOnlyList<DialogueChoice> Choices => choices ?? System.Array.Empty<DialogueChoice>();
        public bool EndsConversation => endsConversation;
        public bool CanCancel => canCancel;
        public bool HasChoices => Choices.Count > 0;
    }
}
