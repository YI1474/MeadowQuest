using UnityEngine;
using UnityEngine.Tilemaps;

namespace MeadowQuest
{
    public sealed class GrassEncounter : MonoBehaviour
    {
        public EncounterHabitat habitat;
        [SerializeField]
        GridMover player;
        [SerializeField]
        Tilemap grass;
        [SerializeField]
        BattleController battle;
        [SerializeField]
        MonsterDefinition enemy;
        [SerializeField]
        MonsterDefinition[] enemyPool = new MonsterDefinition[0];
        public void SetPool(MonsterDefinition[] monsters)
        {
            enemyPool = monsters;
        }

        [SerializeField, Range(0, 1)]
        float chancePerStep = .22f;
        [SerializeField, Min(1)]
        int guaranteedAfter = 8;
        int grassSteps, safeSteps;
        public void Configure(GridMover mover, Tilemap area, BattleController controller, MonsterDefinition monster)
        {
            player = mover;
            grass = area;
            battle = controller;
            enemy = monster;
        }

        void OnEnable()
        {
            if (player)
                player.TileReached += OnStep;
            if (battle)
                battle.Finished += OnFinished;
        }

        void OnDisable()
        {
            if (player)
                player.TileReached -= OnStep;
            if (battle)
                battle.Finished -= OnFinished;
        }

        void OnFinished()
        {
            safeSteps = 3;
            grassSteps = 0;
        }

        void OnStep(Vector3Int cell)
        {
            if (battle.IsActive || player.MovementLocked)
                return;
            if (safeSteps > 0)
            {
                safeSteps--;
                return;
            }

            if (!grass.HasTile(cell))
            {
                grassSteps = 0;
                return;
            }

            grassSteps++;
            if (grassSteps >= guaranteedAfter || Random.value < chancePerStep)
                if (battle.Begin(enemyPool != null && enemyPool.Length > 0 ? enemyPool[Random.Range(0, enemyPool.Length)] : enemy))
                    grassSteps = 0;
        }
    }
}
