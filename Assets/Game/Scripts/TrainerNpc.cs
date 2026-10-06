using UnityEngine;

namespace MeadowQuest
{
    public sealed class TrainerNpc : MonoBehaviour
    {
        public string id, displayName;
        public Vector2Int facing = Vector2Int.down;
        [Range(1, 8)]
        public int sightRange = 4;
        public bool sightEnabled;
        public bool IsMaster => id == AdventureProgress.FinalTrainer;
        public bool ApproachesChallenger => sightEnabled && !IsMaster;
        public string ChallengeText => IsMaster ? "よくぞここまで来た。\n仲間と磨いた力、その絆を私に示してみよ。" : challenge;
        public string RematchText => IsMaster ? "再び来たか、挑戦者よ。\nさらなる高みへ至ったか、この私が見届けよう。\n（再戦の賞金は初回の半額です）" : "もう一度勝負する？ 賞金は初回の半額だよ。";
        public string LockedText => IsMaster ? "焦ることはない。まずはレンとの試練を越えよ。\nその先で、おぬしの挑戦を待っている。" : "まずは試練場のレンに勝っておいで。";

        public TextMesh alert;
        public Sprite[] walkFrames = new Sprite[0];
        public void Pose(Vector2Int direction, float distance = 0)
        {
            int row = direction.y < 0 ? 0 : direction.y > 0 ? 2 : direction.x < 0 ? 3 : 1;
            int phase = (int)(distance * 2) % 4;
            int frame = distance <= 0 ? 1 : phase == 1 ? 0 : phase == 3 ? 2 : 1;
            if (walkFrames.Length > row * 3 + frame && walkFrames[row * 3 + frame])
                GetComponent<SpriteRenderer>().sprite = walkFrames[row * 3 + frame];
        }

        public MonsterDefinition monster;
        [System.Serializable]
        public sealed class TeamMember
        {
            public MonsterDefinition monster;
            [Range(1, 100)]
            public int level = 10;
        }

        public TeamMember[] team = new TeamMember[0];
        [Min(0)]
        public int prizeMoney = 300;
        [Range(0, 100)]
        public int levelOverride;
        [TextArea]
        public string challenge = "一緒に腕試しをしよう！";
        [TextArea]
        public string afterVictory = "いい勝負だったね。また街で会おう！";
    }
}
