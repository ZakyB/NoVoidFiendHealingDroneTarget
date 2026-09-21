using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace NoVoidFiendHealingDroneTarget;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.zaky.novoidfiendhealingdronetarget";
    public const string PluginName = "No Void Fiend Drone Healing";
    public const string PluginVersion = "1.0.3";

    private void Awake()
    {
        IL.RoR2.CharacterAI.BaseAI.FindEnemyHurtBox += FilterHealingDroneSearchResults;

        On.RoR2.CharacterAI.BaseAI.EvaluateSingleSkillDriver += RejectVoidFiendEvaluation;

        On.EntityStates.Drone.DroneWeapon.StartHealBeam.IsHurt += ExcludeVoidFiendFromHealBeamSearch;

        Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private void OnDestroy()
    {
        IL.RoR2.CharacterAI.BaseAI.FindEnemyHurtBox -= FilterHealingDroneSearchResults;
        On.RoR2.CharacterAI.BaseAI.EvaluateSingleSkillDriver -= RejectVoidFiendEvaluation;
        On.EntityStates.Drone.DroneWeapon.StartHealBeam.IsHurt -= ExcludeVoidFiendFromHealBeamSearch;
    }

    private void FilterHealingDroneSearchResults(ILContext il)
    {
        var cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(
                MoveType.After,
                instruction => instruction.MatchCallOrCallvirt<BullseyeSearch>(nameof(BullseyeSearch.GetResults))))
        {
            Logger.LogError("Could not patch BaseAI.FindEnemyHurtBox; the game method may have changed.");
            return;
        }

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<IEnumerable<HurtBox>, BaseAI, IEnumerable<HurtBox>>>(
            static (results, ai) => IsHealingDrone(ai)
                ? results.Where(hurtBox => !IsVoidFiend(hurtBox?.healthComponent?.body))
                : results);
    }

    private static BaseAI.SkillDriverEvaluation? RejectVoidFiendEvaluation(
        On.RoR2.CharacterAI.BaseAI.orig_EvaluateSingleSkillDriver orig,
        BaseAI self,
        ref BaseAI.SkillDriverEvaluation currentSkillDriverEvaluation,
        AISkillDriver aiSkillDriver,
        float myHealthFraction)
    {
        BaseAI.SkillDriverEvaluation? evaluation = orig(
            self,
            ref currentSkillDriverEvaluation,
            aiSkillDriver,
            myHealthFraction);

        if (!evaluation.HasValue || !IsHealingDrone(self))
        {
            return evaluation;
        }

        GameObject? targetObject = evaluation.Value.target?.gameObject;
        CharacterBody? targetBody = targetObject?.GetComponent<CharacterBody>();

        return IsVoidFiend(targetBody) ? null : evaluation;
    }

    private static bool ExcludeVoidFiendFromHealBeamSearch(
        On.EntityStates.Drone.DroneWeapon.StartHealBeam.orig_IsHurt orig,
        HurtBox hurtBox)
    {
        return !IsVoidFiend(hurtBox?.healthComponent?.body) && orig(hurtBox);
    }

    private static bool IsHealingDrone(BaseAI? ai)
    {
        string? bodyName = ai?.body?.name;
        if (bodyName == null)
        {
            return false;
        }

        return bodyName.StartsWith("Drone2Body", StringComparison.Ordinal)
            || bodyName.StartsWith("EmergencyDroneBody", StringComparison.Ordinal)
            || bodyName.StartsWith("DTHealingDroneBody", StringComparison.Ordinal);
    }

    private static bool IsVoidFiend(CharacterBody? body)
    {
        return body != null && body.bodyIndex == BodyCatalog.FindBodyIndex("VoidSurvivorBody");
    }
}
