﻿namespace HouseRules.Essentials.Rules
{
    using Boardgame;
    using DataKeys;
    using HarmonyLib;
    using HouseRules.Types;

    public sealed class EnemyHealthScaledRule : Rule, IConfigWritable<float>, IPatchable, IMultiplayerSafe
    {
        public override string Description => "Enemy health is scaled";

        private static float _globalMultiplier;
        private static bool _isActivated;

        private readonly float _multiplier;

        public EnemyHealthScaledRule(float multiplier)
        {
            _multiplier = multiplier;
        }

        public float GetConfigObject() => _multiplier;

        protected override void OnActivate(GameContext gameContext)
        {
            _globalMultiplier = _multiplier;
            _isActivated = true;
        }

        protected override void OnDeactivate(GameContext gameContext) => _isActivated = false;

        private static void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.PropertyGetter(typeof(PieceConfig), "StartHealth"),
                postfix: new HarmonyMethod(
                    typeof(EnemyHealthScaledRule),
                    nameof(PieceConfig_StartHealth_Postfix)));
        }

        private static void PieceConfig_StartHealth_Postfix(PieceConfig __instance, ref int __result)
        {
            if (!_isActivated)
            {
                return;
            }

            if (!__instance.HasPieceType(PieceType.Enemy))
            {
                return;
            }

            __result = (int)(__result * _globalMultiplier);
        }
    }
}
