using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleTech;
using Harmony;
using PanicSystem.Components;
using PanicSystem.Patches;
using static PanicSystem.Logger;
using static PanicSystem.Components.Controller;
using static PanicSystem.PanicSystem;
using Random = UnityEngine.Random;

// ReSharper disable InconsistentNaming

namespace PanicSystem
{
    public static class Helpers
    {
        // need a global to check the value in another code path
        internal static float damageWithHeatDamage;

        // extension methods intended to make saving throws occur against realistic structure/armor maximums
        // since ME/CC adjust these, the HBS methods to check condition return inaccurately
        // so read out the stat and apply it, else if null return the original value
        // existing checks consider factor reduction as damage without this
        internal static float FactoredMaxStructureForLocation(this Mech mech, ChassisLocations location)
        {
            var maxStructureForLocation = mech.MaxStructureForLocation((int) location);
            var structureMultiplier = mech.StatCollection.GetStatistic("StructureMultiplier")?.Value<float>();
            if (structureMultiplier != null)
            {
                LogDebug($"StructureMultiplier Result = {maxStructureForLocation * structureMultiplier} ");
                return maxStructureForLocation *
                       mech.StatCollection.GetStatistic("StructureMultiplier").Value<float>();
            }

            LogDebug("StructureMultiplier was null");
            return maxStructureForLocation;
        }

        // also need to scale with ArmorFactor (100% more from reinforced wouldn't reflect in MaxArmorForLocation())
        internal static float FactoredMaxArmorForLocation(this Mech mech, ArmorLocation location)
        {
            var maxArmorForLocation = mech.MaxArmorForLocation((int) location);
            var armorMultiplier = mech.StatCollection.GetStatistic("ArmorMultiplier")?.Value<float>();
            if (armorMultiplier != null)
            {
                LogDebug($"ArmorMultiplier Result = {maxArmorForLocation * armorMultiplier} ");
                return maxArmorForLocation *
                       mech.StatCollection.GetStatistic("ArmorMultiplier").Value<float>();
            }

            LogDebug("ArmorMultiplier was null");
            return maxArmorForLocation;
        }

        // used in strings
        internal static float ActorHealth(AbstractActor actor) =>
            (actor.SummaryArmorCurrent + actor.SummaryStructureCurrent) /
            (actor.SummaryArmorMax + actor.SummaryStructureMax) * 100;

        // used in calculations
        internal static float PercentPilot(Pilot pilot) => 1 - (float) pilot.Injuries / pilot.Health;

        internal static float PercentRightTorso(Mech mech) =>
            (mech.RightTorsoStructure +
             mech.RightTorsoFrontArmor +
             mech.RightTorsoRearArmor) /
            (mech.FactoredMaxStructureForLocation(ChassisLocations.RightTorso) +
             mech.FactoredMaxArmorForLocation(ArmorLocation.RightTorso) +
             mech.FactoredMaxArmorForLocation(ArmorLocation.RightTorsoRear));

        internal static float PercentLeftTorso(Mech mech) =>
            (mech.LeftTorsoStructure +
             mech.LeftTorsoFrontArmor +
             mech.LeftTorsoRearArmor) /
            (mech.FactoredMaxStructureForLocation(ChassisLocations.LeftTorso) +
             mech.FactoredMaxArmorForLocation(ArmorLocation.LeftTorso) +
             mech.FactoredMaxArmorForLocation(ArmorLocation.LeftTorsoRear));

        internal static float PercentCenterTorso(Mech mech) =>
            (mech.CenterTorsoStructure +
             mech.CenterTorsoFrontArmor +
             mech.CenterTorsoRearArmor) /
            (mech.FactoredMaxStructureForLocation(ChassisLocations.CenterTorso) +
             mech.FactoredMaxArmorForLocation(ArmorLocation.CenterTorso) +
             mech.FactoredMaxArmorForLocation(ArmorLocation.CenterTorsoRear));

        internal static float PercentLeftLeg(Mech mech) =>
            (mech.LeftLegStructure + mech.LeftLegArmor) /
            (mech.FactoredMaxStructureForLocation(ChassisLocations.LeftLeg) +
             mech.FactoredMaxArmorForLocation(ArmorLocation.LeftLeg));

        internal static float PercentRightLeg(Mech mech) =>
            (mech.RightLegStructure + mech.RightLegArmor) /
            (mech.FactoredMaxStructureForLocation(ChassisLocations.RightLeg) +
             mech.FactoredMaxArmorForLocation(ArmorLocation.RightLeg));

        internal static float PercentHead(Mech mech) =>
            (mech.HeadStructure + mech.HeadArmor) /
            (mech.FactoredMaxStructureForLocation(ChassisLocations.Head) +
             mech.FactoredMaxArmorForLocation(ArmorLocation.Head));


        // applies combat modifiers to tracked mechs based on panic status
        public static void ApplyPanicDebuff(AbstractActor actor)
        {
            var index = GetActorIndex(actor);
            if (TrackedActors[index].Mech != actor.GUID)
            {
                LogDebug("Pilot and mech mismatch; no status to change");
                return;
            }

            // remove existing panic debuffs first
            int Uid() => Random.Range(1, int.MaxValue);
            var effectManager = UnityGameInstance.BattleTechGame.Combat.EffectManager;
            var effects = Traverse.Create(effectManager).Field("effects").GetValue<List<Effect>>();
            for (var i = 0; i < effects.Count; i++)
            {
                if (effects[i].id.StartsWith("PanicSystem") && Traverse.Create(effects[i]).Field("target").GetValue<object>() == actor)
                {
                    effectManager.CancelEffect(effects[i]);
                }
            }

            if (modSettings.VehiclesCanPanic &&
                actor is Vehicle)
            {
                LogReport($"{actor.DisplayName} condition worsened: Panicked");
                TrackedActors[index].PanicStatus = PanicStatus.Panicked;
                TrackedActors[index].PreventEjection = true;
                effectManager.CreateEffect(StatusEffect.PanickedToHit, "PanicSystemToHit", Uid(), actor, actor, new WeaponHitInfo(), 0);
                effectManager.CreateEffect(StatusEffect.PanickedToBeHit, "PanicSystemToBeHit", Uid(), actor, actor, new WeaponHitInfo(), 0);
            }
            else
            {
                switch (TrackedActors[index].PanicStatus)
                {
                    case PanicStatus.Confident:
                        LogReport($"{actor.DisplayName} condition worsened: Unsettled");
                        TrackedActors[index].PanicStatus = PanicStatus.Unsettled;
                        effectManager.CreateEffect(StatusEffect.UnsettledToHit, "PanicSystemToHit", Uid(), actor, actor, new WeaponHitInfo(), 0);
                        break;
                    case PanicStatus.Unsettled:
                        LogReport($"{actor.DisplayName} condition worsened: Stressed");
                        TrackedActors[index].PanicStatus = PanicStatus.Stressed;
                        effectManager.CreateEffect(StatusEffect.StressedToHit, "PanicSystemToHit", Uid(), actor, actor, new WeaponHitInfo(), 0);
                        effectManager.CreateEffect(StatusEffect.StressedToBeHit, "PanicSystemToBeHit", Uid(), actor, actor, new WeaponHitInfo(), 0);
                        break;
                    default:
                        LogReport($"{actor.DisplayName} condition worsened: Panicked");
                        TrackedActors[index].PanicStatus = PanicStatus.Panicked;
                        effectManager.CreateEffect(StatusEffect.PanickedToHit, "PanicSystemToHit", Uid(), actor, actor, new WeaponHitInfo(), 0);
                        effectManager.CreateEffect(StatusEffect.PanickedToBeHit, "PanicSystemToBeHit", Uid(), actor, actor, new WeaponHitInfo(), 0);
                        break;
                }
            }

            TrackedActors[index].PanicWorsenedRecently = true;
        }

        // check if panic roll is possible
        private static bool CanPanic(AbstractActor actor, AttackDirector.AttackSequence attackSequence)
        {
            if (actor == null || actor.IsDead || actor.IsFlaggedForDeath && actor.HasHandledDeath)
            {
                LogReport($"{attackSequence?.attacker?.DisplayName} incapacitated {actor?.DisplayName}");
                return false;
            }

            if (attackSequence == null ||
                actor.team.IsLocalPlayer && !modSettings.PlayersCanPanic ||
                !actor.team.IsLocalPlayer && !modSettings.EnemiesCanPanic)
            {
                return false;
            }

            return true;
        }

        // Returns a float to modify panic roll difficulty based on existing panic level
        internal static float GetPanicModifier(PanicStatus pilotStatus)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (pilotStatus)
            {
                case PanicStatus.Unsettled: return modSettings.UnsettledPanicFactor;
                case PanicStatus.Stressed: return modSettings.StressedPanicFactor;
                case PanicStatus.Panicked: return modSettings.PanickedPanicFactor;
                default: return 1f;
            }
        }


        internal static void DrawHeader()
        {
            LogReport(new string('-', 46));
            LogReport($"{"Factors",-20} | {"Change",10} | {"Total",10}");
            LogReport(new string('-', 46));
        }


        // method is called despite the setting, so it can be controlled in one place
        internal static void SaySpamFloatie(AbstractActor actor, string message)
        {
            if (!modSettings.FloatieSpam) return;
            actor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                new ShowActorInfoSequence(actor, message, FloatieMessage.MessageNature.Neutral, false)));
        }

        // bool controls whether to display as buff or debuff
        internal static void SayStatusFloatie(AbstractActor actor, bool buff)
        {
            var index = GetActorIndex(actor);

            var floatieString = $"{TrackedActors[index].PanicStatus.ToString()}";
            if (buff)
            {
                actor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                    new ShowActorInfoSequence(actor, floatieString, FloatieMessage.MessageNature.Inspiration, true)));
            }

            else
            {
                actor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                    new ShowActorInfoSequence(actor, floatieString, FloatieMessage.MessageNature.Debuff, true)));
            }
        }

        // true implies a panic condition was met
        public static bool ShouldPanic(AbstractActor actor, AttackDirector.AttackSequence attackSequence)
        {
            if (!CanPanic(actor, attackSequence))
            {
                return false;
            }

            return SufficientDamageWasDone(attackSequence);
        }

        public static bool ShouldSkipProcessing(AttackStackSequence __instance, MessageCenterMessage message)
        {
            var attackCompleteMessage = (AttackCompleteMessage) message;
            if (attackCompleteMessage == null || attackCompleteMessage.stackItemUID != __instance.SequenceGUID)
            {
                return true;
            }

            // can't do stuff with buildings
            if (!(__instance.directorSequences[0].chosenTarget is Vehicle) &&
                !(__instance.directorSequences[0].chosenTarget is Mech))
            {
                return true;
            }

            return __instance.directorSequences[0].chosenTarget?.GUID == null;
        }

        // returns true if enough damage was inflicted to trigger a panic save
        private static bool SufficientDamageWasDone(AttackDirector.AttackSequence attackSequence)
        {
            if (attackSequence == null)
            {
                return false;
            }

            var id = attackSequence.chosenTarget.GUID;
            if (!attackSequence.GetAttackDidDamage(id))
            {
                LogReport("No damage");
                return false;
            }

            var previousArmor = AttackStackSequence_OnAttackBegin_Patch.armorBeforeAttack;
            var previousStructure = AttackStackSequence_OnAttackBegin_Patch.structureBeforeAttack;
            var armorDamage = attackSequence.GetArmorDamageDealt(id);
            var structureDamage = attackSequence.GetStructureDamageDealt(id);
            var percentDamageDone = (attackSequence.GetArmorDamageDealt(id) + attackSequence.GetStructureDamageDealt(id)) / (previousArmor + previousStructure) * 100;
            damageWithHeatDamage = percentDamageDone + Mech_AddExternalHeat_Patch.heatDamage * modSettings.HeatDamageFactor;

            // have to check structure here AFTER armor, despite it being the priority, because we need to set the damageWithHeatDamage global
            LogReport($"Damage >>> A: {armorDamage:F3} S: {structureDamage:F3} ({modSettings.MinimumMechStructureDamageRequired}) ({percentDamageDone:F2}%) H: {Mech_AddExternalHeat_Patch.heatDamage}");
            if (attackSequence.chosenTarget is Mech &&
                structureDamage >= modSettings.MinimumMechStructureDamageRequired ||
                modSettings.VehiclesCanPanic &&
                attackSequence.chosenTarget is Vehicle &&
                structureDamage >= modSettings.MinimumVehicleStructureDamageRequired)
            {
                LogReport("Structure damage requires panic save");
                return true;
            }

            if (damageWithHeatDamage <= modSettings.MinimumDamagePercentageRequired)
            {
                LogReport("Not enough damage");
                Mech_AddExternalHeat_Patch.heatDamage = 0;
                return false;
            }

            LogReport("Total damage requires a panic save");
            return true;
        }

        internal static void SetupEjectPhrases(string modDir)
        {
            if (!modSettings.EnableEjectPhrases)
            {
                return;
            }

            if (!modSettings.EnableEjectPhrases) return;
            try
            {
                ejectPhraseList = File.ReadAllText(Path.Combine(modDir, "phrases.txt")).Split('\n').ToList();
            }
            catch (Exception ex)
            {
                LogReport("Error - problem loading phrases.txt but the setting is enabled");
                LogDebug(ex);
                // in case the file is missing but the setting is enabled
                modSettings.EnableEjectPhrases = false;
            }
        }

        // unused.  another option for handling vehicle ejection
        internal static int GetExposedSides(Vehicle vehicle)
        {
            var result = 0;
            if (vehicle.LeftSideArmor <= float.Epsilon)
            {
                result++;
            }

            if (vehicle.RightSideArmor <= float.Epsilon)
            {
                result++;
            }

            if (vehicle.FrontArmor <= float.Epsilon)
            {
                result++;
            }

            if (vehicle.RearArmor <= float.Epsilon)
            {
                result++;
            }

            // in case there is no turret? untested
            if (vehicle.MaxArmorForLocation(1) > float.Epsilon &&
                vehicle.TurretArmor <= float.Epsilon)
            {
                // TODO delete this log line when tested
                LogReport("Turret exposed");
                result++;
            }

            return result;
        }
    }
}
