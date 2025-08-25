using System.Collections.Generic;
using Model.Runtime.Projectiles;
using UnityEngine;

namespace UnitBrains.Player
{
    public class SecondUnitBrain : DefaultPlayerUnitBrain
    {
        public override string TargetUnitName => "Cobra Commando";

        private const float OverheatTemperature = 3f;
        private const float OverheatCooldown = 2f;
        private float _temperature = 0f;
        private float _cooldownTime = 0f;
        private bool _overheated;
        private List<Vector2Int> unreachableTargets = new List<Vector2Int>();

        protected override void GenerateProjectiles(Vector2Int forTarget, List<BaseProjectile> intoList)
        {
            int currentTemp = GetTemperature();
            if (currentTemp >= (int)OverheatTemperature)
            {
                return;
            }

            IncreaseTemperature();

            for (int i = 0; i < currentTemp; i++)
            {
                var projectile = CreateProjectile(forTarget);
                AddProjectileToList(projectile, intoList);
            }
        }

        public override Vector2Int GetNextStep()
        {
            Vector2Int currentPosition = GetCurrentPosition();

            if (targets == null || targets.Count == 0)
            {
                var enemyBaseId = IsPlayerUnitBrain ? RuntimeModel.RoMap.Bases.PlayerId : RuntimeModel.RoMap.Bases.BotId;
                var enemyBasePosition = RuntimeModel.RoMap.Bases.GetBasePositionById(enemyBaseId);
                return CalcNextStepTowards(currentPosition, enemyBasePosition);
            }

            Vector2Int targetPos = targets[0];

            if (IsInAttackRange(currentPosition, targetPos))
            {
                return currentPosition;
            }
            else
            {
                return CalcNextStepTowards(currentPosition, targetPos);
            }
        }

        protected override List<Vector2Int> SelectTargets()
        {
            var allTargets = GetAllTargets();

            if (allTargets == null || allTargets.Count == 0)
            {
                targets = new List<Vector2Int>();
                var enemyBaseId = IsPlayerUnitBrain ? RuntimeModel.RoMap.Bases.PlayerId : RuntimeModel.RoMap.Bases.BotId;
                var enemyBasePos = RuntimeModel.RoMap.Bases.GetBasePositionById(enemyBaseId);
                targets.Add(enemyBasePos);
                unreachableTargets.Clear();
                return targets;
            }

            targets.Clear();
            unreachableTargets.Clear();

            Vector2Int ownBaseId = IsPlayerUnitBrain ? RuntimeModel.RoMap.Bases.PlayerId : RuntimeModel.RoMap.Bases.BotId;
            float minDistanceToOwnBase = float.MaxValue;
            Vector2Int mostDangerousTarget = default;

            foreach (var target in allTargets)
            {
                float distanceToOwnBase = DistanceToOwnBase(target);

                if (IsTargetInRange(target))
                {
                    targets.Add(target);
                }
                else
                {
                    if (distanceToOwnBase < minDistanceToOwnBase)
                    {
                        minDistanceToOwnBase = distanceToOwnBase;
                        mostDangerousTarget = target;
                    }
                }
            }


            if (targets.Count == 0 && mostDangerousTarget != default)
            {
                unreachableTargets.Add(mostDangerousTarget);
                targets.Add(mostDangerousTarget);
            }

            return targets;
        }

        public override void Update(float deltaTime, float time)
        {
            if (_overheated)
            {
                _cooldownTime += Time.deltaTime;
                float t = _cooldownTime / OverheatCooldown;
                _temperature = Mathf.Lerp(OverheatTemperature, 0, t);
                if (t >= 1)
                {
                    _cooldownTime = 0;
                    _overheated = false;
                    _temperature = 0f;
                }
            }
        }

        private int GetTemperature()
        {
            if (_overheated) return (int)OverheatTemperature;
            else return Mathf.CeilToInt(_temperature);
        }

        private void IncreaseTemperature()
        {
            _temperature += 1f;
            if (_temperature >= OverheatTemperature) _overheated = true;
        }

        private Vector2Int GetCurrentPosition()
        {
            return new Vector2Int(0, 0);
        }

        private bool IsInAttackRange(Vector2Int from, Vector2Int to)
        {
            float attackRange = 1.5f;
            return Vector2.Distance(from, to) <= attackRange;
        }

        private Vector2Int CalcNextStepTowards(Vector2Int from, Vector2Int to)
        {
            Vector2 direction = (to - from).normalized;
            return from + new Vector2Int(Mathf.RoundToInt(direction.x), Mathf.RoundToInt(direction.y));
        }

        private bool IsTargetInRange(Vector2Int target)
        {
            Vector2Int currentPos = GetCurrentPosition();
            return IsInAttackRange(currentPos, target);
        }
    }
}