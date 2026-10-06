using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeadowQuest
{
    [DefaultExecutionOrder(55)]
    public sealed class TrainerApproach : MonoBehaviour
    {
        public TownWorld town;
        TownWorld.Resident active;
        Vector3Int home, current;
        Vector3Int? reserved;
        float animationDistance;
        readonly List<Vector3Int> route = new List<Vector3Int>();
        public bool IsBusy => active != null;

        void Start()
        {
            town.battle.Finished += Finished;
            town.player.TileReached += OnStep;
        }

        void OnStep(Vector3Int cell)
        {
            Update();
        }

        void Update()
        {
            if (!isActiveAndEnabled || IsBusy || !town.battle.ProgressionReady || town.battle.IsActive || town.player.MovementLocked || town.player.IsMoving || !Application.isFocused)
                return;
            bool alive = false;
            foreach (var member in town.battle.Party)
                if (member.Data.currentHp > 0)
                {
                    alive = true;
                    break;
                }

            if (!alive)
                return;
            var target = town.map.CellAt(town.player.transform.position);
            foreach (var resident in town.residents)
            {
                var npc = resident.trainer;
                if (!npc || !npc.ApproachesChallenger || town.battle.TrainerDefeated(npc.id) || !town.battle.CanChallengeTrainer(npc))
                    continue;
                int distance = TrainerSight.Distance(resident.cell.x, resident.cell.y, npc.facing.x, npc.facing.y, target.x, target.y, npc.sightRange, (x, y) => town.map.IsWalkable(new Vector3Int(x, y, 0)));
                if (distance == 0)
                    continue;
                active = resident;
                home = current = resident.cell;
                animationDistance = 0;
                route.Clear();
                town.player.MovementLocked = true;
                StartCoroutine(Approach(distance));
                break;
            }
        }

        IEnumerator Approach(int distance)
        {
            var npc = active.trainer;
            if (npc.alert)
                npc.alert.gameObject.SetActive(true);
            yield return new WaitForSeconds(.45f);
            if (npc.alert)
                npc.alert.gameObject.SetActive(false);
            for (int i = 1; i < distance; i++)
            {
                var next = home + new Vector3Int(npc.facing.x * i, npc.facing.y * i, 0);
                if (!town.map.IsWalkable(next))
                {
                    RestoreImmediately();
                    town.player.MovementLocked = false;
                    yield break;
                }

                route.Add(current);
                yield return Move(next);
            }

            town.OpenTrainerDialogue(active);
        }

        IEnumerator Move(Vector3Int next)
        {
            reserved = next;
            town.map.AddOccupiedCell(next);
            var renderer = active.trainer.GetComponent<SpriteRenderer>();
            Vector3 destination = town.map.CenterOf(next);
            var direction = new Vector2Int(next.x - current.x, next.y - current.y);
            while ((active.trainer.transform.position - destination).sqrMagnitude > .00001f)
            {
                Vector3 before = active.trainer.transform.position;
                active.trainer.transform.position = Vector3.MoveTowards(before, destination, 4 * Time.deltaTime);
                animationDistance += Vector3.Distance(before, active.trainer.transform.position);
                active.trainer.Pose(direction, animationDistance);
                renderer.sortingOrder = 1000 - Mathf.RoundToInt(active.trainer.transform.position.y * 2);
                yield return null;
            }

            town.map.RemoveOccupiedCell(current);
            current = next;
            reserved = null;
            active.cell = current;
            active.trainer.Pose(direction);
        }

        public void Finished()
        {
            if (IsBusy)
                StartCoroutine(ReturnHome());
        }

        IEnumerator ReturnHome()
        {
            animationDistance = 0;
            town.player.MovementLocked = true;
            for (int i = route.Count - 1; i >= 0; i--)
                yield return Move(route[i]);
            RestoreImmediately();
        // BattleController releases movement only after this return completes.
        }

        public void RestoreImmediately()
        {
            if (!IsBusy)
                return;
            StopAllCoroutines();
            if (reserved.HasValue)
                town.map.RemoveOccupiedCell(reserved.Value);
            reserved = null;
            town.map.RemoveOccupiedCell(current);
            town.map.AddOccupiedCell(home);
            active.cell = home;
            active.trainer.transform.position = town.map.CenterOf(home);
            active.trainer.GetComponent<SpriteRenderer>().sortingOrder = 1000 - Mathf.RoundToInt(active.trainer.transform.position.y * 2);
            active.trainer.Pose(active.trainer.facing);
            if (active.trainer.alert)
                active.trainer.alert.gameObject.SetActive(false);
            active = null;
            route.Clear();
        }

        void OnDisable()
        {
            RestoreImmediately();
        }

        void OnDestroy()
        {
            if (town && town.battle)
                town.battle.Finished -= Finished;
            if (town && town.player)
                town.player.TileReached -= OnStep;
        }
    }
}
