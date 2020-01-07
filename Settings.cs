// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ConvertToConstant.Global
// ReSharper disable UnassignedField.Global

namespace PanicSystem
{
    public class Settings
    {
        public bool Debug;
        public bool CombatLog;
        public bool EnableEjectPhrases;
        public bool FloatieSpam;
        public float EjectPhraseChance;
        public bool ColorizeFloaties;
        public bool CountAsKills;

        // panic
        public bool PlayersCanPanic;
        public bool EnemiesCanPanic;
        public bool VehiclesCanPanic;
        public float MinimumDamagePercentageRequired;
        public float MinimumMechStructureDamageRequired;
        public float MinimumVehicleStructureDamageRequired;
        public bool OneChangePerTurn;
        public bool LosingLimbAlwaysPanics;
        public float UnsteadyModifier;
        public float PilotHealthMaxModifier;
        public float HeadMaxModifier;
        public float CenterTorsoMaxModifier;
        public float SideTorsoMaxModifier;
        public float LeggedMaxModifier;
        public float WeaponlessModifier;
        public float AloneModifier;
        public float UnsettledAimModifier;
        public float StressedAimModifier;
        public float StressedToHitModifier;
        public float PanickedAimModifier;
        public float PanickedToHitModifier;
        public float MedianResolve;
        public float VehicleResolveFactor;
        public float ResolveMaxModifier;
        public float DistractingModifier;
        public float OverheatedModifier;
        public float ShutdownModifier;
        public float PanicStatModifier;
        public float HeatDamageFactor;
        public float MechHealthForCrit;
        public float CritOver;
        public float UnsettledPanicFactor;
        public float StressedPanicFactor;
        public float PanickedPanicFactor;


        // Quirks
        public bool QuirksEnabled;
        public float BraveModifier;
        public float DependableModifier;

        // ejection
        public float MaxEjectChance;
        public float EjectChanceFactor;

        public float BaseEjectionResist;
        public float BaseVehicleEjectionResist;
        public float GutsEjectionResistPerPoint;
        public float TacticsEjectionResistPerPoint;
        public float VehicleGutAndTacticsFactor;
    }
}
