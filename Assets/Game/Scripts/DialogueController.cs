using UnityEngine;
using UnityEngine.UI;
namespace MeadowQuest
{
    public sealed class DialogueController : MonoBehaviour
    {
        [SerializeField] GridMover player;
        [SerializeField] KeyboardMoveInput input;
        [SerializeField] Transform npc;
        [SerializeField] DialogueData dialogue;
        [SerializeField] GameObject panel;
        [SerializeField] Text body;
        [SerializeField] Text hint;
        int line;
        bool talking;
        string[] activeLines = System.Array.Empty<string>();
        public void Configure(GridMover mover,Transform character,DialogueData data,GameObject box,Text text,Text prompt)
        { player=mover; input=mover.GetComponent<KeyboardMoveInput>(); npc=character; dialogue=data; panel=box; body=text; hint=prompt; }
        void Start() { panel.SetActive(false); }
        void Update()
        {
            if(!player || !input || !npc || !dialogue) return;
            if(player.MovementLocked && !talking) return;
            Vector2 delta=npc.position-player.transform.position;
            bool nearby=delta.sqrMagnitude<=1.1f;
            hint.text=talking ? (line == activeLines.Length-1 ? "E / Enter：閉じる" : "E / Enter：次へ") : nearby ? "E / Enter：案内人と話す" : "WASD / 矢印キー：移動　｜　右側の案内人に話しかけよう";
            if(!Application.isFocused || !input.InteractPressed) return;
            if(talking)
            {
                line++;
                if(line>=activeLines.Length) Close(); else ShowLine();
            }
            else if(nearby && !player.IsMoving && dialogue.lines!=null && dialogue.lines.Length>0)
            {
                activeLines=System.Array.FindAll(dialogue.lines,text=>!string.IsNullOrWhiteSpace(text));
                if(activeLines.Length==0) return;
                talking=true; line=0; player.MovementLocked=true; panel.SetActive(true); ShowLine();
            }
        }
        void ShowLine() { body.text=dialogue.speaker+"\n\n"+activeLines[line]+"\n\n("+(line+1)+" / "+activeLines.Length+")"; }
        void Close() { talking=false; panel.SetActive(false); player.MovementLocked=false; }
        void OnDisable() { if(player) player.MovementLocked=false; if(panel) panel.SetActive(false); talking=false; }
    }
}

