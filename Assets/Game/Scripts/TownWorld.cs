using System;
using UnityEngine;
using UnityEngine.UI;

namespace MeadowQuest
{
    [DefaultExecutionOrder(50)]
    public sealed class TownWorld : MonoBehaviour
    {
        [Serializable]
        public class Passage
        {
            public Vector3Int cell, destination;
            public string label;
        }

        [Serializable]
        public class Resident
        {
            public Vector3Int cell;
            public string name;
            [TextArea]
            public string text;
            public bool heal;
            public bool shop;
            public TrainerNpc trainer;
        }

        public GridMover player;
        public BattleController battle;
        public Camera lens;
        public Text hint, body;
        public GameObject panel;
        public Passage[] passages;
        public Resident[] residents;
        public TownBuilding[] buildings = new TownBuilding[0];
        public TownRoom[] rooms = new TownRoom[0];
        public GridMap map;
        [HideInInspector]
        public int authoringVersion;
        [HideInInspector]
        public int gateVersion;
        [HideInInspector]
        public int entranceVersion;
        [HideInInspector]
        public int ridgeDoorVersion;
        public Vector3Int start = new Vector3Int(70, 22, 0);
        [HideInInspector]
        public int repairVersion;
        WorldArea[] areas;
        WorldArea AreaAt(Vector3 point)
        {
            foreach (var area in areas)
                if (area && area.Contains(point))
                    return area;
            return null;
        }

        bool talking;
        TrainerNpc pendingTrainer;
        int warpFrame;
        KeyboardMoveInput input;
        Resident hintResident;
        TownRoom hintRoom;
        bool hintValid, hintMeadow;
        TownRoom RoomAt(Vector3 point)
        {
            foreach (var room in rooms)
                if (room && room.Contains(point))
                    return room;
            return null;
        }

        void Start()
        {
            areas = FindObjectsByType<WorldArea>();
            if (!map)
                map = FindAnyObjectByType<GridMap>();
            input = player.GetComponent<KeyboardMoveInput>();
            var destination = battle.SavedPosition;
            player.Teleport(destination.HasValue && map.IsWalkable(destination.Value) ? destination.Value : start);
            panel.SetActive(false);
            player.TileReached += Reached;
        }

        void Reached(Vector3Int cell)
        {
            foreach (var building in buildings)
            {
                if (!building || !building.entrance || !building.returnPoint)
                    continue;
                TownRoom room = null;
                foreach (var candidate in rooms)
                    if (candidate && candidate.roomId == building.roomId)
                    {
                        room = candidate;
                        break;
                    }

                if (!room || !room.arrival || !room.exit)
                    continue;
                if (cell == map.CellAt(building.entrance.position))
                {
                    Warp(map.CellAt(room.arrival.position));
                    return;
                }

                if (cell == map.CellAt(room.exit.position))
                {
                    Warp(map.CellAt(building.returnPoint.position));
                    return;
                }
            }

            foreach (var passage in passages)
                if (cell == passage.cell)
                {
                    Warp(passage.destination);
                    return;
                }
        }

        public void ReturnToRecovery(string roomId)
        {
            var approach = GetComponent<TrainerApproach>();
            if (approach)
                approach.RestoreImmediately();
            foreach (var area in areas)
                if (area.recoveryRoom == roomId)
                {
                    player.Teleport(area.recoveryCell);
                    talking = false;
                    pendingTrainer = null;
                    warpFrame = 0;
                    hintValid = false;
                    panel.SetActive(false);
                    return;
                }

            TownRoom destination = null;
            foreach (var room in rooms)
                if (room && room.roomId == roomId)
                {
                    destination = room;
                    break;
                }

            if (!destination)
                foreach (var room in rooms)
                    if (room && room.roomId == "home")
                    {
                        destination = room;
                        break;
                    }

            var cell = destination && destination.arrival ? map.CellAt(destination.arrival.position) : start;
            if (!player.Teleport(cell))
            {
                bool moved = false;
                if (destination)
                    for (int y = 1; y < 9 && !moved; y++)
                        for (int x = 1; x < 12 && !moved; x++)
                            moved = player.Teleport(map.CellAt(destination.transform.position) + new Vector3Int(x, y, 0));
                if (!moved)
                    player.Teleport(start);
            }

            talking = false;
            pendingTrainer = null;
            warpFrame = 0;
            hintValid = false;
            panel.SetActive(false);
        }

        void Warp(Vector3Int destination)
        {
            if (player.Teleport(destination))
            {
                hintValid = false;
                player.MovementLocked = true;
                warpFrame = Time.frameCount;
            }
        }

        void Update()
        {
            if (!Application.isFocused)
                return;
            if (battle.IsActive)
            {
                if (hintValid)
                    hint.text = "";
                hintValid = false;
                return;
            }

            if (warpFrame > 0)
            {
                if (Time.frameCount > warpFrame)
                {
                    warpFrame = 0;
                    player.MovementLocked = false;
                }

                return;
            }

            if (talking)
            {
                if (pendingTrainer && battle.TrainerDefeated(pendingTrainer.id) && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    talking = false;
                    pendingTrainer = null;
                    panel.SetActive(false);
                    warpFrame = Time.frameCount;
                    return;
                }

                if (input.InteractPressed)
                {
                    talking = false;
                    panel.SetActive(false);
                    player.MovementLocked = false;
                    if (pendingTrainer)
                    {
                        var trainer = pendingTrainer;
                        pendingTrainer = null;
                        if (!battle.BeginTrainer(trainer))
                        {
                            var approach = GetComponent<TrainerApproach>();
                            if (approach)
                                approach.RestoreImmediately();
                        }
                    }
                }

                return;
            }

            if (player.MovementLocked)
                return;
            Resident near = null;
            var position = player.transform.position;
            var facingCell = map.CellAt(position) + new Vector3Int(player.Facing.x, player.Facing.y, 0);
            foreach (var resident in residents)
                if (resident.cell == facingCell && ((Vector2)position - new Vector2(resident.cell.x + .5f, resident.cell.y + .5f)).sqrMagnitude <= 1.1025f)
                {
                    near = resident;
                    break;
                }

            var currentRoom = RoomAt(position);
            bool meadow = position.x < 30;
            if (!hintValid || hintResident != near || hintRoom != currentRoom || hintMeadow != meadow)
            {
                string area = currentRoom ? currentRoom.displayName + "　｜　下中央のマットから街へ" : meadow ? "草原　｜　東の道から街へ" : "はじまりの街　｜　西の門：草原";
                var extraArea = AreaAt(position);
                if (extraArea)
                    area = extraArea.displayName;
                hint.text = near != null ? "E / Enter：" + near.name + "と話す" : area + "　WASD：移動　E / Enter：話す";
                hintResident = near;
                hintRoom = currentRoom;
                hintMeadow = meadow;
                hintValid = true;
            }

            if (near == null || !input.InteractPressed || player.IsMoving)
                return;
            if (near.shop)
            {
                var store = FindAnyObjectByType<ShopMenu>();
                if (store)
                    store.Open();
                return;
            }

            if (near.trainer)
            {
                OpenTrainerDialogue(near);
                return;
            }

            body.text = near.name + "\n\n" + (near.name == "街の案内人" ? battle.ReceiveCaptureSupplies() : near.heal ? battle.RecoverCompanion(currentRoom ? currentRoom.roomId : AreaAt(position) ? AreaAt(position).recoveryRoom : "home") : near.text) + "\n\nE / Enter：閉じる";
            panel.SetActive(true);
            talking = true;
            player.MovementLocked = true;
        }

        public void OpenTrainerDialogue(Resident resident)
        {
            var trainer = resident.trainer;
            bool defeated = battle.TrainerDefeated(trainer.id);
            bool available = battle.CanChallengeTrainer(trainer);
            pendingTrainer = available ? trainer : null;
            string text = !available ? trainer.LockedText : defeated ? trainer.RematchText : trainer.ChallengeText;
            body.text = trainer.displayName + "\n\n" + text + "\n\nE / Enter：" + (available ? "対戦する" : "閉じる") + (defeated && available ? "　Esc：やめる" : "");
            talking = true;
            player.MovementLocked = true;
            panel.SetActive(true);
        }

        void LateUpdate()
        {
            if (!lens || !player)
                return;
            var pos = player.transform.position;
            var room = RoomAt(pos);
            lens.orthographicSize = room ? 6.5f : 8;
            if (room)
                pos = room.Center;
            else
            {
                bool meadow = pos.x < 30;
                float halfWidth = lens.orthographicSize * lens.aspect;
                pos.x = ClampCamera(pos.x, meadow ? 0 : 40, meadow ? 25 : 104, halfWidth);
                pos.y = ClampCamera(pos.y, 0, meadow ? 17 : 44, lens.orthographicSize);
            }

            var extraArea = AreaAt(player.transform.position);
            if (extraArea)
            {
                lens.orthographicSize = 7;
                pos = player.transform.position;
                pos.x = ClampCamera(pos.x, extraArea.transform.position.x, extraArea.transform.position.x + extraArea.size.x, 7 * lens.aspect);
                pos.y = ClampCamera(pos.y, extraArea.transform.position.y, extraArea.transform.position.y + extraArea.size.y, 7);
            }

            lens.transform.position = new Vector3(pos.x, pos.y, -10);
        }

        static float ClampCamera(float value, float min, float max, float extent) => max - min < extent * 2 ? (min + max) / 2 : Mathf.Clamp(value, min + extent, max - extent);
        void OnDisable()
        {
            if (player && (talking || warpFrame > 0))
                player.MovementLocked = false;
            talking = false;
            warpFrame = 0;
            pendingTrainer = null;
            hintValid = false;
            if (panel)
                panel.SetActive(false);
        }

        void OnDestroy()
        {
            if (player)
                player.TileReached -= Reached;
        }
    }
}
