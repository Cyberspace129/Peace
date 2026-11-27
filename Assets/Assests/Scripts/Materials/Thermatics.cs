using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParticleChemistry
{

/// <summary>
/// Thermatics is a ScriptableObject database for thermal material behaviour.
///
/// It is designed to work with the material catalogue in
/// ParticleChemistry.Material. A thermal profile uses the material's stable
/// ID (for example, "quartz" or "fresh-leaf") and adds thermal information
/// that is not stored in the general material definition.
///
/// The asset stores reusable definitions. ThermalSample is a runtime object
/// representing one actual quantity of a material with its own mass,
/// temperature, phase and thermal history.
/// </summary>
[CreateAssetMenu(
    fileName = "Thermatics",
    menuName = "Particle Physics/Thermatics")]
public class Thermatics : ScriptableObject
{
    private const float GasConstantJPerMolK = 8.314462618f;
    private const float AbsoluteZeroCelsius = -273.15f;
    private const float MinimumPositiveValue = 0.000001f;

    // ============================================================
    // ENUMERATIONS
    // ============================================================

    public enum ThermalPhase
    {
        Unknown,
        Solid,
        SolidLiquidMixture,
        Liquid,
        Gas,
        Plasma
    }

    // ============================================================
    // TEMPERATURE-DEPENDENT PROPERTY CURVES
    // ============================================================

    /// <summary>
    /// AnimationCurve values are dimensionless multipliers, not the
    /// underlying physical property. For example, a value of 1.1 means
    /// 110% of the profile's base specific heat at that temperature.
    /// X values are temperatures in degrees Celsius.
    /// </summary>
    [Serializable]
    public class TemperatureMultipliers
    {
        [Tooltip("Multiplier for specific heat capacity versus temperature. Y=1 means the base value.")]
        [SerializeField] private AnimationCurve specificHeat = CreateDefaultCurve();

        [Tooltip("Multiplier for thermal conductivity versus temperature. Y=1 means the base value.")]
        [SerializeField] private AnimationCurve conductivity = CreateDefaultCurve();

        [Tooltip("Multiplier for the linear thermal expansion coefficient versus temperature. Y=1 means the base value.")]
        [SerializeField] private AnimationCurve expansion = CreateDefaultCurve();

        [Tooltip("Multiplier for density versus temperature. Y=1 means the reference density.")]
        [SerializeField] private AnimationCurve density = CreateDefaultCurve();

        [Tooltip("Multiplier for strength versus temperature. Y=1 means full reference strength.")]
        [SerializeField] private AnimationCurve strength = CreateDefaultCurve();

        public AnimationCurve SpecificHeat => specificHeat;
        public AnimationCurve Conductivity => conductivity;
        public AnimationCurve Expansion => expansion;
        public AnimationCurve Density => density;
        public AnimationCurve Strength => strength;

        public TemperatureMultipliers()
        {
            specificHeat = CreateDefaultCurve();
            conductivity = CreateDefaultCurve();
            expansion = CreateDefaultCurve();
            density = CreateDefaultCurve();
            strength = CreateDefaultCurve();
        }

        public void SetCurves(
            AnimationCurve specificHeatMultiplier,
            AnimationCurve conductivityMultiplier,
            AnimationCurve expansionMultiplier,
            AnimationCurve densityMultiplier,
            AnimationCurve strengthMultiplier)
        {
            specificHeat = specificHeatMultiplier ?? CreateDefaultCurve();
            conductivity = conductivityMultiplier ?? CreateDefaultCurve();
            expansion = expansionMultiplier ?? CreateDefaultCurve();
            density = densityMultiplier ?? CreateDefaultCurve();
            strength = strengthMultiplier ?? CreateDefaultCurve();
        }

        private static AnimationCurve CreateDefaultCurve()
        {
            return new AnimationCurve(
                new Keyframe(AbsoluteZeroCelsius, 1f),
                new Keyframe(3000f, 1f));
        }
    }

    // ============================================================
    // COMBUSTION DEFINITION
    // ============================================================

    /// <summary>
    /// Describes a simplified combustion model for a material.
    ///
    /// The Arrhenius parameters control the temperature-dependent reaction
    /// rate. oxygenRequiredKgPerKgFuel is the required oxygen mass per
    /// kilogram of fuel consumed. charYieldFraction is the fraction of the
    /// consumed fuel retained in the sample as solid char.
    /// </summary>
    [Serializable]
    public class CombustionDefinition
    {
        [SerializeField] private bool combustible;
        [SerializeField] private float ignitionTemperatureC = -1f;
        [SerializeField] private float heatOfCombustionJPerKg = -1f;
        [SerializeField] private float oxygenRequiredKgPerKgFuel = -1f;
        [SerializeField] private float preExponentialFactorPerSecond = 100000f;
        [SerializeField] private float activationEnergyJPerMol = 80000f;
        [Range(0f, 1f)]
        [SerializeField] private float combustionEfficiency = 0.8f;
        [Range(0f, 1f)]
        [SerializeField] private float charYieldFraction = 0.1f;

        public bool Combustible => combustible;
        public float IgnitionTemperatureC => ignitionTemperatureC;
        public float HeatOfCombustionJPerKg => heatOfCombustionJPerKg;
        public float OxygenRequiredKgPerKgFuel => oxygenRequiredKgPerKgFuel;
        public float PreExponentialFactorPerSecond => preExponentialFactorPerSecond;
        public float ActivationEnergyJPerMol => activationEnergyJPerMol;
        public float CombustionEfficiency => combustionEfficiency;
        public float CharYieldFraction => charYieldFraction;

        public CombustionDefinition()
        {
        }

        public CombustionDefinition(
            bool combustible,
            float ignitionTemperatureC,
            float heatOfCombustionJPerKg,
            float oxygenRequiredKgPerKgFuel,
            float preExponentialFactorPerSecond = 100000f,
            float activationEnergyJPerMol = 80000f,
            float combustionEfficiency = 0.8f,
            float charYieldFraction = 0.1f)
        {
            this.combustible = combustible;
            this.ignitionTemperatureC = ignitionTemperatureC;
            this.heatOfCombustionJPerKg = heatOfCombustionJPerKg;
            this.oxygenRequiredKgPerKgFuel = oxygenRequiredKgPerKgFuel;
            this.preExponentialFactorPerSecond = preExponentialFactorPerSecond;
            this.activationEnergyJPerMol = activationEnergyJPerMol;
            this.combustionEfficiency = Mathf.Clamp01(combustionEfficiency);
            this.charYieldFraction = Mathf.Clamp01(charYieldFraction);
        }
    }

    // ============================================================
    // THERMAL DEGRADATION DEFINITION
    // ============================================================

    /// <summary>
    /// Describes heat-driven chemical or structural degradation.
    /// This is separate from combustion: a material can degrade without
    /// burning, and a material can burn without this degradation model
    /// being enabled.
    /// </summary>
    [Serializable]
    public class ThermalDegradationDefinition
    {
        [SerializeField] private bool enabled;
        [SerializeField] private float onsetTemperatureC = -1f;
        [SerializeField] private float preExponentialFactorPerSecond = 10000f;
        [SerializeField] private float activationEnergyJPerMol = 100000f;
        [Range(0f, 1f)]
        [SerializeField] private float maximumMassLossFraction = 0.5f;
        [Range(0f, 1f)]
        [SerializeField] private float maximumStrengthLossFraction = 1f;

        public bool Enabled => enabled;
        public float OnsetTemperatureC => onsetTemperatureC;
        public float PreExponentialFactorPerSecond => preExponentialFactorPerSecond;
        public float ActivationEnergyJPerMol => activationEnergyJPerMol;
        public float MaximumMassLossFraction => maximumMassLossFraction;
        public float MaximumStrengthLossFraction => maximumStrengthLossFraction;

        public ThermalDegradationDefinition()
        {
        }

        public ThermalDegradationDefinition(
            bool enabled,
            float onsetTemperatureC,
            float preExponentialFactorPerSecond,
            float activationEnergyJPerMol,
            float maximumMassLossFraction,
            float maximumStrengthLossFraction = 1f)
        {
            this.enabled = enabled;
            this.onsetTemperatureC = onsetTemperatureC;
            this.preExponentialFactorPerSecond = preExponentialFactorPerSecond;
            this.activationEnergyJPerMol = activationEnergyJPerMol;
            this.maximumMassLossFraction = Mathf.Clamp01(maximumMassLossFraction);
            this.maximumStrengthLossFraction = Mathf.Clamp01(maximumStrengthLossFraction);
        }
    }

    // ============================================================
    // THERMAL MATERIAL PROFILE
    // ============================================================

    /// <summary>
    /// Adds thermal behaviour to one entry in the Material database.
    /// The materialId must match Material.MaterialDefinition.Id.
    ///
    /// Values with -1 are treated as unknown or not supplied. They are not
    /// silently interpreted as zero.
    /// </summary>
    [Serializable]
    public class ThermalMaterialProfile
    {
        [Header("Material Link")]
        [SerializeField] private string materialId;
        [SerializeField] private string displayName;
        [SerializeField] private ThermalPhase referencePhase = ThermalPhase.Solid;
        [SerializeField] private float referenceTemperatureC = 20f;

        [Header("Base Thermal Properties")]
        [Tooltip("Specific heat capacity in J/(kg·K). Use -1 when unknown.")]
        [SerializeField] private float specificHeatJPerKgK = -1f;

        [Tooltip("Thermal conductivity in W/(m·K). Use -1 when unknown.")]
        [SerializeField] private float conductivityWPerMK = -1f;

        [Tooltip("Linear thermal expansion coefficient in 1/K. Use -1 when unknown.")]
        [SerializeField] private float linearExpansionCoefficientPerK = -1f;

        [Tooltip("Reference density in kg/m³. Usually copied from the Material database.")]
        [SerializeField] private float referenceDensityKgPerM3 = -1f;

        [Header("Melting and Freezing")]
        [Tooltip("Enable this when the material has a defined melting point. This separate flag allows negative Celsius values.")]
        [SerializeField] private bool hasMeltingPoint;

        [Tooltip("Melting temperature in °C.")]
        [SerializeField] private float meltingPointC;

        [Tooltip("Enable this to provide a freezing point different from the melting point. Leave disabled to reuse the melting point.")]
        [SerializeField] private bool hasFreezingPoint;

        [Tooltip("Freezing temperature in °C. Negative values are valid.")]
        [SerializeField] private float freezingPointC;

        [Tooltip("Latent heat of fusion in J/kg. Use -1 when unknown. Zero means an instantaneous simplified transition.")]
        [SerializeField] private float latentHeatOfFusionJPerKg = -1f;

        [Header("Temperature-Dependent Behaviour")]
        [SerializeField] private TemperatureMultipliers temperatureMultipliers =
            new TemperatureMultipliers();

        [Header("Combustion")]
        [SerializeField] private CombustionDefinition combustion =
            new CombustionDefinition();

        [Header("Thermal Degradation")]
        [SerializeField] private ThermalDegradationDefinition degradation =
            new ThermalDegradationDefinition();

        [TextArea(2, 5)]
        [SerializeField] private string notes;

        public string MaterialId => materialId;
        public string DisplayName => displayName;
        public ThermalPhase ReferencePhase => referencePhase;
        public float ReferenceTemperatureC => referenceTemperatureC;
        public float SpecificHeatJPerKgK => specificHeatJPerKgK;
        public float ConductivityWPerMK => conductivityWPerMK;
        public float LinearExpansionCoefficientPerK => linearExpansionCoefficientPerK;
        public float ReferenceDensityKgPerM3 => referenceDensityKgPerM3;
        public bool HasMeltingPoint => hasMeltingPoint;
        public float MeltingPointC => meltingPointC;
        public bool HasFreezingPoint => hasFreezingPoint || hasMeltingPoint;
        public float FreezingPointC => hasFreezingPoint ? freezingPointC : meltingPointC;
        public float LatentHeatOfFusionJPerKg => latentHeatOfFusionJPerKg;
        public TemperatureMultipliers TemperatureCurves => temperatureMultipliers;
        public CombustionDefinition Combustion => combustion;
        public ThermalDegradationDefinition Degradation => degradation;
        public string Notes => notes;

        public ThermalMaterialProfile()
        {
            temperatureMultipliers = new TemperatureMultipliers();
            combustion = new CombustionDefinition();
            degradation = new ThermalDegradationDefinition();
        }

        public ThermalMaterialProfile(
            string materialId,
            string displayName,
            float specificHeatJPerKgK,
            float conductivityWPerMK,
            float linearExpansionCoefficientPerK,
            float referenceDensityKgPerM3,
            float meltingPointC,
            float freezingPointC = -1f,
            float latentHeatOfFusionJPerKg = -1f,
            ThermalPhase referencePhase = ThermalPhase.Solid,
            string notes = "",
            bool hasMeltingPoint = false,
            bool hasFreezingPoint = false)
        {
            this.materialId = materialId;
            this.displayName = displayName;
            this.specificHeatJPerKgK = specificHeatJPerKgK;
            this.conductivityWPerMK = conductivityWPerMK;
            this.linearExpansionCoefficientPerK = linearExpansionCoefficientPerK;
            this.referenceDensityKgPerM3 = referenceDensityKgPerM3;
            this.meltingPointC = meltingPointC;
            this.freezingPointC = freezingPointC;
            this.hasMeltingPoint = hasMeltingPoint;
            this.hasFreezingPoint = hasFreezingPoint;
            this.latentHeatOfFusionJPerKg = latentHeatOfFusionJPerKg;
            this.referencePhase = referencePhase;
            this.notes = notes;
            temperatureMultipliers = new TemperatureMultipliers();
            combustion = new CombustionDefinition();
            degradation = new ThermalDegradationDefinition();
        }

        public void SetCombustion(CombustionDefinition definition)
        {
            combustion = definition ?? new CombustionDefinition();
        }

        public void SetDegradation(ThermalDegradationDefinition definition)
        {
            degradation = definition ?? new ThermalDegradationDefinition();
        }

        public void SetTemperatureCurves(TemperatureMultipliers curves)
        {
            temperatureMultipliers = curves ?? new TemperatureMultipliers();
        }

        public void SetMeltingAndFreezing(
            bool hasMeltingPoint,
            float meltingTemperatureC,
            bool hasFreezingPoint,
            float freezingTemperatureC,
            float latentHeatOfFusionJPerKg)
        {
            this.hasMeltingPoint = hasMeltingPoint;
            this.meltingPointC = meltingTemperatureC;
            this.hasFreezingPoint = hasFreezingPoint;
            this.freezingPointC = freezingTemperatureC;
            this.latentHeatOfFusionJPerKg = latentHeatOfFusionJPerKg;
        }
    }

    // ============================================================
    // RUNTIME MATERIAL SAMPLE
    // ============================================================

    /// <summary>
    /// One actual sample of a material. Unlike a profile, this class stores
    /// changing simulation state and should normally be created at runtime.
    /// </summary>
    [Serializable]
    public class ThermalSample
    {
        private readonly ThermalMaterialProfile profile;
        private readonly float initialMassKg;

        private float massKg;
        private float temperatureC;
        private ThermalPhase phase;
        private float liquidFraction;
        private float remainingFuelMassKg;
        private float charMassKg;
        private float thermalDamageFraction;
        private float massLostKg;

        public ThermalMaterialProfile Profile => profile;
        public float InitialMassKg => initialMassKg;
        public float MassKg => massKg;
        public float TemperatureC => temperatureC;
        public ThermalPhase Phase => phase;
        public float LiquidFraction => liquidFraction;
        public float RemainingFuelMassKg => remainingFuelMassKg;
        public float CharMassKg => charMassKg;
        public float ThermalDamageFraction => thermalDamageFraction;
        public float MassLostKg => massLostKg;

        internal ThermalSample(
            ThermalMaterialProfile profile,
            float massKg,
            float temperatureC,
            ThermalPhase phase,
            float initialCombustibleMassKg = -1f)
        {
            this.profile = profile;
            this.initialMassKg = massKg;
            this.massKg = massKg;
            this.temperatureC = temperatureC;
            this.phase = phase;

            liquidFraction = phase == ThermalPhase.Liquid ? 1f : 0f;
            // Moisture and other non-fuel constituents must not automatically
            // become combustible simply because they are part of the sample mass.
            float fuelMass = initialCombustibleMassKg < 0f
                ? massKg
                : Mathf.Clamp(initialCombustibleMassKg, 0f, massKg);
            remainingFuelMassKg = profile.Combustion.Combustible ? fuelMass : 0f;
        }

        internal void SetTemperature(float value)
        {
            temperatureC = value;
        }

        internal void SetPhase(ThermalPhase value)
        {
            phase = value;
            if (value == ThermalPhase.Solid)
                liquidFraction = 0f;
            else if (value == ThermalPhase.Liquid)
                liquidFraction = 1f;
        }

        internal void SetLiquidFraction(float value)
        {
            liquidFraction = Mathf.Clamp01(value);

            if (liquidFraction <= 0f)
                phase = ThermalPhase.Solid;
            else if (liquidFraction >= 1f)
                phase = ThermalPhase.Liquid;
            else
                phase = ThermalPhase.SolidLiquidMixture;
        }

        internal void AddChar(float amountKg)
        {
            charMassKg += Mathf.Max(0f, amountKg);
        }

        internal void AddExternalMass(float amountKg)
        {
            massKg += Mathf.Max(0f, amountKg);
        }

        internal void ConsumeFuel(float amountKg, float charYieldFraction)
        {
            float consumed = Mathf.Clamp(amountKg, 0f, remainingFuelMassKg);
            remainingFuelMassKg -= consumed;

            float charMass = consumed * Mathf.Clamp01(charYieldFraction);
            float massLeavingSample = consumed - charMass;

            charMassKg += charMass;
            massKg = Mathf.Max(0f, massKg - massLeavingSample);
            massLostKg += massLeavingSample;
        }

        internal void ApplyDegradation(float damageIncrease, float massLossKg)
        {
            thermalDamageFraction = Mathf.Clamp01(
                thermalDamageFraction + Mathf.Max(0f, damageIncrease));

            float loss = Mathf.Clamp(massLossKg, 0f, massKg);
            massKg -= loss;
            massLostKg += loss;
        }
    }

    // ============================================================
    // RESULT TYPES
    // ============================================================

    public class ThermalEnergyResult
    {
        public bool IsValid { get; internal set; }
        public float InputEnergyJ { get; internal set; }
        public float RemainingEnergyJ { get; internal set; }
        public float StartingTemperatureC { get; internal set; }
        public float FinalTemperatureC { get; internal set; }
        public float LatentEnergyUsedJ { get; internal set; }
        public bool PhaseChanged { get; internal set; }

        public float EnergyAbsorbedJ => InputEnergyJ - RemainingEnergyJ;
    }

    public class CombustionResult
    {
        public bool Ignited { get; internal set; }
        public float FuelConsumedKg { get; internal set; }
        public float OxygenConsumedKg { get; internal set; }
        public float CharProducedKg { get; internal set; }
        public float HeatReleasedJ { get; internal set; }
        public float ReactionRatePerSecond { get; internal set; }
    }

    public class DegradationResult
    {
        public bool DegradationActive { get; internal set; }
        public float DamageIncrease { get; internal set; }
        public float TotalDamageFraction { get; internal set; }
        public float MassLostKg { get; internal set; }
        public float ReactionRatePerSecond { get; internal set; }
    }

    // ============================================================
    // DATABASE REFERENCES AND SERIALIZED PROFILES
    // ============================================================

    [Header("Material Database")]
    [Tooltip("Assign the PeriodicTableAndMaterials asset. Its material IDs are used to link thermal profiles.")]
    [SerializeField] private ParticleChemistry.Material materialDatabase;

    [Header("Thermal Profiles")]
    [SerializeField] private List<ThermalMaterialProfile> profiles =
        new List<ThermalMaterialProfile>();

    public ParticleChemistry.Material MaterialDatabase => materialDatabase;
    public IReadOnlyList<ThermalMaterialProfile> Profiles => profiles;

    // ============================================================
    // UNITY INITIALIZATION AND PROFILE GENERATION
    // ============================================================

    private void OnEnable()
    {
        if (profiles == null)
            profiles = new List<ThermalMaterialProfile>();

        // Create basic profiles only when the asset has none. This copies
        // density, specific heat, conductivity, and melting point from the
        // material database. More specialised values remain editable.
        if (profiles.Count == 0 && materialDatabase != null)
        {
            AddMissingProfilesFromMaterialDatabase();
            MarkAssetDirty();
        }
    }

    /// <summary>
    /// Adds a basic thermal profile for every material that does not already
    /// have one. Existing profiles and their custom thermal data are preserved.
    /// </summary>
    [ContextMenu("Add Missing Profiles From Material Database")]
    public void AddMissingProfilesFromMaterialDatabase()
    {
        if (materialDatabase == null)
            throw new InvalidOperationException(
                "Assign a ParticleChemistry.Material database before generating thermal profiles.");

        if (profiles == null)
            profiles = new List<ThermalMaterialProfile>();

        foreach (ParticleChemistry.Material.MaterialDefinition material
            in materialDatabase.Materials)
        {
            if (material == null || HasProfile(material.Id))
                continue;

            profiles.Add(CreateProfileFromMaterial(material));
        }

        MarkAssetDirty();
    }

    /// <summary>
    /// Imports missing thermal profiles from an explicitly supplied Material database.
    /// This overload lets the central Material asset coordinate the subsystems without
    /// requiring circular asset references between every subsystem and the material database.
    /// Existing profiles are preserved.
    /// </summary>
    public void AddMissingProfilesFromMaterialDatabase(ParticleChemistry.Material database)
    {
        if (database == null)
            throw new ArgumentNullException(nameof(database));

        if (profiles == null)
            profiles = new List<ThermalMaterialProfile>();

        int added = 0;
        foreach (ParticleChemistry.Material.MaterialDefinition material in database.Materials)
        {
            if (material == null || string.IsNullOrWhiteSpace(material.Id) || HasProfile(material.Id))
                continue;

            profiles.Add(CreateProfileFromMaterial(material));
            added++;
        }

        if (added > 0)
            MarkAssetDirty();
    }

    /// <summary>
    /// Rebuilds all thermal profiles from the Material database.
    /// WARNING: this overwrites custom thermal profile settings.
    /// </summary>
    [ContextMenu("Rebuild All Thermal Profiles From Material Database")]
    public void RebuildAllProfilesFromMaterialDatabase()
    {
        if (materialDatabase == null)
            throw new InvalidOperationException(
                "Assign a ParticleChemistry.Material database before rebuilding thermal profiles.");

        profiles = new List<ThermalMaterialProfile>();

        foreach (ParticleChemistry.Material.MaterialDefinition material
            in materialDatabase.Materials)
        {
            if (material != null)
                profiles.Add(CreateProfileFromMaterial(material));
        }

        MarkAssetDirty();
    }

    private ThermalMaterialProfile CreateProfileFromMaterial(
        ParticleChemistry.Material.MaterialDefinition material)
    {
        ThermalPhase phase = ConvertPhase(material.Phase);

        return new ThermalMaterialProfile(
            material.Id,
            material.Name,
            material.SpecificHeatJPerKgK,
            material.ThermalConductivityWPerM_K,
            linearExpansionCoefficientPerK: -1f,
            referenceDensityKgPerM3: material.DensityKgPerM3,
            meltingPointC: material.MeltingPointC,
            freezingPointC: 0f,
            latentHeatOfFusionJPerKg: -1f,
            referencePhase: phase,
            notes: "Generated from the Material database. Add expansion, phase-transition, combustion, degradation, and temperature-curve data as required.",
            hasMeltingPoint: material.MeltingPointC != -1f,
            hasFreezingPoint: false);
    }

    private static ThermalPhase ConvertPhase(
        ParticleChemistry.Material.PhysicalPhase phase)
    {
        switch (phase)
        {
            case ParticleChemistry.Material.PhysicalPhase.Solid:
                return ThermalPhase.Solid;
            case ParticleChemistry.Material.PhysicalPhase.Liquid:
                return ThermalPhase.Liquid;
            case ParticleChemistry.Material.PhysicalPhase.Gas:
                return ThermalPhase.Gas;
            case ParticleChemistry.Material.PhysicalPhase.Plasma:
                return ThermalPhase.Plasma;
            default:
                return ThermalPhase.Unknown;
        }
    }

    private void MarkAssetDirty()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // ============================================================
    // PROFILE LOOKUP AND CREATION
    // ============================================================

    public bool HasProfile(string materialId)
    {
        if (string.IsNullOrWhiteSpace(materialId) || profiles == null)
            return false;

        foreach (ThermalMaterialProfile profile in profiles)
        {
            if (profile != null && string.Equals(
                profile.MaterialId,
                materialId,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public ThermalMaterialProfile GetProfile(string materialId)
    {
        if (string.IsNullOrWhiteSpace(materialId))
            throw new ArgumentException("Material ID cannot be empty.", nameof(materialId));

        if (profiles != null)
        {
            foreach (ThermalMaterialProfile profile in profiles)
            {
                if (profile != null && string.Equals(
                    profile.MaterialId,
                    materialId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return profile;
                }
            }
        }

        throw new ArgumentException(
            $"No thermal profile exists for material ID '{materialId}'. Generate profiles from the Material database or add one manually.",
            nameof(materialId));
    }

    public void AddProfile(ThermalMaterialProfile profile)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        if (string.IsNullOrWhiteSpace(profile.MaterialId))
            throw new ArgumentException("A thermal profile must have a material ID.");

        if (HasProfile(profile.MaterialId))
            throw new InvalidOperationException(
                $"A thermal profile for '{profile.MaterialId}' already exists.");

        if (profiles == null)
            profiles = new List<ThermalMaterialProfile>();

        profiles.Add(profile);
        MarkAssetDirty();
    }

    // ============================================================
    // RUNTIME SAMPLE CREATION
    // ============================================================

    public ThermalSample CreateSample(
        string materialId,
        float massKg,
        float initialTemperatureC = 20f,
        float initialCombustibleMassKg = -1f)
    {
        if (massKg <= 0f || float.IsNaN(massKg) || float.IsInfinity(massKg))
            throw new ArgumentOutOfRangeException(nameof(massKg), "Mass must be a finite value greater than zero.");

        if (initialTemperatureC < AbsoluteZeroCelsius ||
            float.IsNaN(initialTemperatureC) || float.IsInfinity(initialTemperatureC))
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialTemperatureC),
                "Temperature must be finite and cannot be below absolute zero.");
        }

        ThermalMaterialProfile profile = GetProfile(materialId);
        ThermalPhase phase = DeterminePhase(profile, initialTemperatureC);

        if (initialCombustibleMassKg < -1f ||
            float.IsNaN(initialCombustibleMassKg) ||
            float.IsInfinity(initialCombustibleMassKg))
            throw new ArgumentOutOfRangeException(nameof(initialCombustibleMassKg));

        return new ThermalSample(
            profile, massKg, initialTemperatureC, phase, initialCombustibleMassKg);
    }

    private static ThermalPhase DeterminePhase(
        ThermalMaterialProfile profile,
        float temperatureC)
    {
        if (profile.ReferencePhase == ThermalPhase.Gas ||
            profile.ReferencePhase == ThermalPhase.Plasma)
        {
            return profile.ReferencePhase;
        }

        if (profile.HasMeltingPoint && temperatureC >= profile.MeltingPointC)
            return ThermalPhase.Liquid;

        return profile.ReferencePhase == ThermalPhase.Unknown
            ? ThermalPhase.Solid
            : profile.ReferencePhase;
    }

    // ============================================================
    // TEMPERATURE-DEPENDENT PROPERTY LOOKUPS
    // ============================================================

    public float GetSpecificHeatJPerKgK(
        ThermalMaterialProfile profile,
        float temperatureC)
    {
        ValidateProfile(profile);
        return ApplyTemperatureMultiplier(
            profile.SpecificHeatJPerKgK,
            profile.TemperatureCurves == null ? null : profile.TemperatureCurves.SpecificHeat,
            temperatureC);
    }

    public float GetThermalConductivityWPerMK(
        ThermalMaterialProfile profile,
        float temperatureC)
    {
        ValidateProfile(profile);
        return ApplyTemperatureMultiplier(
            profile.ConductivityWPerMK,
            profile.TemperatureCurves == null ? null : profile.TemperatureCurves.Conductivity,
            temperatureC);
    }

    public float GetLinearExpansionCoefficientPerK(
        ThermalMaterialProfile profile,
        float temperatureC)
    {
        ValidateProfile(profile);
        return ApplyTemperatureMultiplier(
            profile.LinearExpansionCoefficientPerK,
            profile.TemperatureCurves == null ? null : profile.TemperatureCurves.Expansion,
            temperatureC);
    }

    public float GetDensityKgPerM3(
        ThermalMaterialProfile profile,
        float temperatureC)
    {
        ValidateProfile(profile);
        return ApplyTemperatureMultiplier(
            profile.ReferenceDensityKgPerM3,
            profile.TemperatureCurves == null ? null : profile.TemperatureCurves.Density,
            temperatureC);
    }

    public float GetStrengthFractionAtTemperature(
        ThermalSample sample)
    {
        ValidateSample(sample);

        ThermalMaterialProfile profile = sample.Profile;
        float thermalMultiplier = EvaluateMultiplier(
            profile.TemperatureCurves == null ? null : profile.TemperatureCurves.Strength,
            sample.TemperatureC);

        float degradationReduction =
            sample.ThermalDamageFraction *
            profile.Degradation.MaximumStrengthLossFraction;

        return Mathf.Clamp01(thermalMultiplier * (1f - degradationReduction));
    }

    private static float ApplyTemperatureMultiplier(
        float baseValue,
        AnimationCurve curve,
        float temperatureC)
    {
        if (baseValue < 0f)
            return -1f;

        return baseValue * EvaluateMultiplier(curve, temperatureC);
    }

    private static float EvaluateMultiplier(AnimationCurve curve, float temperatureC)
    {
        if (curve == null || curve.length == 0)
            return 1f;

        return Mathf.Max(0f, curve.Evaluate(temperatureC));
    }

    private static void ValidateProfile(ThermalMaterialProfile profile)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));
    }

    private static void ValidateSample(ThermalSample sample)
    {
        if (sample == null)
            throw new ArgumentNullException(nameof(sample));
    }

    /// <summary>
    /// Applies mass gained or lost by another material subsystem (for example
    /// fluid absorption, corrosion, wear, or biological decomposition) to the
    /// thermal sample so all systems continue to describe the same physical body.
    /// Positive values add mass; negative values remove mass.
    /// This does not add absorbed fluid to the combustible-fuel pool.
    /// </summary>
    public void ApplyExternalMassChange(ThermalSample sample, float massChangeKg)
    {
        ValidateSample(sample);
        if (float.IsNaN(massChangeKg) || float.IsInfinity(massChangeKg))
            throw new ArgumentOutOfRangeException(nameof(massChangeKg));

        if (massChangeKg < 0f)
            sample.ApplyDegradation(0f, -massChangeKg);
        else if (massChangeKg > 0f)
            sample.AddExternalMass(massChangeKg);
    }

    // ============================================================
    // HEAT CAPACITY AND SENSIBLE HEAT
    // ============================================================

    /// <summary>
    /// Total heat capacity C = mass × specific heat capacity.
    /// Result is in joules per kelvin (J/K).
    /// </summary>
    public float CalculateHeatCapacityJPerK(
        ThermalMaterialProfile profile,
        float massKg,
        float temperatureC)
    {
        if (massKg < 0f)
            throw new ArgumentOutOfRangeException(nameof(massKg));

        float specificHeat = GetSpecificHeatJPerKgK(profile, temperatureC);
        if (specificHeat <= 0f)
            throw new InvalidOperationException(
                $"Specific heat capacity is not defined for '{profile.DisplayName}'.");

        return massKg * specificHeat;
    }

    /// <summary>
    /// Calculates sensible heat Q for a temperature change, integrating the
    /// specific-heat curve. Positive result means heating; negative means cooling.
    /// Latent heat from phase changes is not included in this calculation.
    /// </summary>
    public float CalculateSensibleHeatJ(
        ThermalMaterialProfile profile,
        float massKg,
        float startTemperatureC,
        float endTemperatureC)
    {
        ValidateProfile(profile);

        if (massKg < 0f)
            throw new ArgumentOutOfRangeException(nameof(massKg));

        if (Mathf.Approximately(startTemperatureC, endTemperatureC) || massKg == 0f)
            return 0f;

        const int integrationSteps = 32;
        float interval = (endTemperatureC - startTemperatureC) / integrationSteps;
        float total = 0f;

        for (int i = 0; i < integrationSteps; i++)
        {
            float t0 = startTemperatureC + interval * i;
            float t1 = t0 + interval;
            float cp0 = GetSpecificHeatJPerKgK(profile, t0);
            float cp1 = GetSpecificHeatJPerKgK(profile, t1);

            if (cp0 <= 0f || cp1 <= 0f)
                throw new InvalidOperationException(
                    $"Specific heat capacity is not defined across the requested temperature range for '{profile.DisplayName}'.");

            total += massKg * (cp0 + cp1) * 0.5f * interval;
        }

        return total;
    }

    // ============================================================
    // MELTING, FREEZING, AND APPLIED THERMAL ENERGY
    // ============================================================

    /// <summary>
    /// Adds or removes thermal energy from a sample.
    /// Handles a single solid/liquid transition using latent heat of fusion.
    /// The returned RemainingEnergyJ is energy that could not be applied
    /// because the sample ran out of mass or the relevant properties are absent.
    /// </summary>
    public ThermalEnergyResult ApplyThermalEnergy(
        ThermalSample sample,
        float energyJ)
    {
        ValidateSample(sample);

        if (float.IsNaN(energyJ) || float.IsInfinity(energyJ))
            throw new ArgumentOutOfRangeException(nameof(energyJ));

        ThermalEnergyResult result = new ThermalEnergyResult
        {
            IsValid = true,
            InputEnergyJ = energyJ,
            RemainingEnergyJ = energyJ,
            StartingTemperatureC = sample.TemperatureC,
            FinalTemperatureC = sample.TemperatureC,
            LatentEnergyUsedJ = 0f,
            PhaseChanged = false
        };

        if (Mathf.Approximately(energyJ, 0f) || sample.MassKg <= 0f)
            return result;

        ThermalMaterialProfile profile = sample.Profile;
        ThermalPhase startingPhase = sample.Phase;
        float startingLiquidFraction = sample.LiquidFraction;

        if (energyJ > 0f)
            ApplyHeating(sample, ref result);
        else
            ApplyCooling(sample, ref result);

        result.FinalTemperatureC = sample.TemperatureC;
        result.PhaseChanged = sample.Phase != startingPhase ||
            !Mathf.Approximately(sample.LiquidFraction, startingLiquidFraction);

        return result;
    }

    private void ApplyHeating(ThermalSample sample, ref ThermalEnergyResult result)
    {
        ThermalMaterialProfile profile = sample.Profile;
        float remaining = result.RemainingEnergyJ;
        float meltingPoint = profile.MeltingPointC;

        if ((sample.Phase == ThermalPhase.Solid ||
             sample.Phase == ThermalPhase.SolidLiquidMixture) &&
            profile.HasMeltingPoint)
        {
            if (sample.TemperatureC < meltingPoint)
            {
                float required = CalculateSensibleHeatJ(
                    profile, sample.MassKg, sample.TemperatureC, meltingPoint);

                if (remaining < required)
                {
                    sample.SetTemperature(FindTemperatureAfterEnergy(
                        profile, sample.MassKg, sample.TemperatureC, remaining));
                    result.RemainingEnergyJ = 0f;
                    return;
                }

                sample.SetTemperature(meltingPoint);
                remaining -= required;
            }

            float latentPerKg = profile.LatentHeatOfFusionJPerKg;
            if (latentPerKg > 0f)
            {
                float totalLatentEnergy = latentPerKg * sample.MassKg;
                float latentNeeded = totalLatentEnergy * (1f - sample.LiquidFraction);
                float used = Mathf.Min(remaining, latentNeeded);

                float newFraction = sample.LiquidFraction + used / totalLatentEnergy;
                sample.SetLiquidFraction(newFraction);
                remaining -= used;
                result.LatentEnergyUsedJ += used;

                if (sample.LiquidFraction < 1f)
                {
                    result.RemainingEnergyJ = remaining;
                    return;
                }
            }
            else
            {
                sample.SetLiquidFraction(1f);
            }
        }

        if (remaining > 0f)
        {
            float finalTemperature = FindTemperatureAfterEnergy(
                profile, sample.MassKg, sample.TemperatureC, remaining);
            sample.SetTemperature(finalTemperature);
            remaining = 0f;
        }

        result.RemainingEnergyJ = remaining;
    }

    private void ApplyCooling(ThermalSample sample, ref ThermalEnergyResult result)
    {
        ThermalMaterialProfile profile = sample.Profile;
        float remainingMagnitude = -result.RemainingEnergyJ;
        float freezingPoint = profile.FreezingPointC;

        if ((sample.Phase == ThermalPhase.Liquid ||
             sample.Phase == ThermalPhase.SolidLiquidMixture) &&
            profile.HasFreezingPoint)
        {
            if (sample.TemperatureC > freezingPoint)
            {
                float required = -CalculateSensibleHeatJ(
                    profile, sample.MassKg, sample.TemperatureC, freezingPoint);

                if (remainingMagnitude < required)
                {
                    sample.SetTemperature(FindTemperatureAfterEnergy(
                        profile, sample.MassKg, sample.TemperatureC, -remainingMagnitude));
                    result.RemainingEnergyJ = 0f;
                    return;
                }

                sample.SetTemperature(freezingPoint);
                remainingMagnitude -= required;
            }

            float latentPerKg = profile.LatentHeatOfFusionJPerKg;
            if (latentPerKg > 0f)
            {
                float totalLatentEnergy = latentPerKg * sample.MassKg;
                float latentNeeded = totalLatentEnergy * sample.LiquidFraction;
                float used = Mathf.Min(remainingMagnitude, latentNeeded);

                float newFraction = sample.LiquidFraction - used / totalLatentEnergy;
                sample.SetLiquidFraction(newFraction);
                remainingMagnitude -= used;
                result.LatentEnergyUsedJ += used;

                if (sample.LiquidFraction > 0f)
                {
                    result.RemainingEnergyJ = -remainingMagnitude;
                    return;
                }
            }
            else
            {
                sample.SetLiquidFraction(0f);
            }
        }

        if (remainingMagnitude > 0f)
        {
            float finalTemperature = FindTemperatureAfterEnergy(
                profile, sample.MassKg, sample.TemperatureC, -remainingMagnitude);
            sample.SetTemperature(finalTemperature);
            remainingMagnitude = 0f;
        }

        result.RemainingEnergyJ = -remainingMagnitude;
    }

    private float FindTemperatureAfterEnergy(
        ThermalMaterialProfile profile,
        float massKg,
        float startTemperatureC,
        float energyJ)
    {
        if (massKg <= 0f || Mathf.Approximately(energyJ, 0f))
            return startTemperatureC;

        float low;
        float high;

        if (energyJ > 0f)
        {
            low = startTemperatureC;
            high = startTemperatureC + Mathf.Max(1f, energyJ / massKg);

            int expansionCount = 0;
            while (CalculateSensibleHeatJ(profile, massKg, startTemperatureC, high) < energyJ &&
                   expansionCount < 32)
            {
                high = startTemperatureC + (high - startTemperatureC) * 2f;
                expansionCount++;
            }
        }
        else
        {
            high = startTemperatureC;
            low = Mathf.Max(AbsoluteZeroCelsius, startTemperatureC - Mathf.Max(1f, -energyJ / massKg));

            int expansionCount = 0;
            while (CalculateSensibleHeatJ(profile, massKg, startTemperatureC, low) > energyJ &&
                   low > AbsoluteZeroCelsius && expansionCount < 32)
            {
                low = Mathf.Max(
                    AbsoluteZeroCelsius,
                    startTemperatureC - (startTemperatureC - low) * 2f);
                expansionCount++;
            }
        }

        for (int i = 0; i < 40; i++)
        {
            float middle = (low + high) * 0.5f;
            float calculated = CalculateSensibleHeatJ(
                profile, massKg, startTemperatureC, middle);

            if (calculated < energyJ)
                low = middle;
            else
                high = middle;
        }

        return Mathf.Clamp((low + high) * 0.5f, AbsoluteZeroCelsius, float.MaxValue);
    }

    // ============================================================
    // THERMAL CONDUCTIVITY AND HEAT TRANSFER
    // ============================================================

    /// <summary>
    /// Calculates one-dimensional conductive heat transfer using Fourier's
    /// law: Q = k × A × (T_hot - T_cold) / distance × time.
    /// Positive result means energy flows from the hot side to the cold side.
    /// This assumes constant geometry and an approximately constant gradient
    /// during the supplied time interval.
    /// </summary>
    public float CalculateConductiveHeatTransferJ(
        ThermalMaterialProfile profile,
        float hotTemperatureC,
        float coldTemperatureC,
        float areaM2,
        float conductionDistanceM,
        float durationSeconds)
    {
        ValidateProfile(profile);

        if (areaM2 < 0f)
            throw new ArgumentOutOfRangeException(nameof(areaM2));
        if (conductionDistanceM <= 0f)
            throw new ArgumentOutOfRangeException(nameof(conductionDistanceM));
        if (durationSeconds < 0f)
            throw new ArgumentOutOfRangeException(nameof(durationSeconds));

        float meanTemperature = (hotTemperatureC + coldTemperatureC) * 0.5f;
        float conductivity = GetThermalConductivityWPerMK(profile, meanTemperature);

        if (conductivity < 0f)
            throw new InvalidOperationException(
                $"Thermal conductivity is not defined for '{profile.DisplayName}'.");

        float heatRateW = conductivity * areaM2 *
            (hotTemperatureC - coldTemperatureC) / conductionDistanceM;

        return heatRateW * durationSeconds;
    }

    /// <summary>
    /// Applies the same conductive energy transfer to two samples.
    /// The first sample is treated as the hot side and the second as the cold
    /// side; the sign of the calculated energy still determines direction.
    /// </summary>
    public float TransferHeatBetweenSamples(
        ThermalSample first,
        ThermalSample second,
        float areaM2,
        float conductionDistanceM,
        float durationSeconds)
    {
        ValidateSample(first);
        ValidateSample(second);

        float energy = CalculateConductiveHeatTransferJ(
            first.Profile,
            first.TemperatureC,
            second.TemperatureC,
            areaM2,
            conductionDistanceM,
            durationSeconds);

        ApplyThermalEnergy(first, -energy);
        ApplyThermalEnergy(second, energy);

        return energy;
    }

    // ============================================================
    // THERMAL EXPANSION AND DENSITY CHANGE
    // ============================================================

    /// <summary>
    /// Calculates linear expansion: ΔL = α × L₀ × ΔT.
    /// Assumes an isotropic material and uses the expansion coefficient at
    /// the mean temperature of the interval.
    /// </summary>
    public float CalculateLinearExpansionM(
        ThermalMaterialProfile profile,
        float originalLengthM,
        float startTemperatureC,
        float endTemperatureC)
    {
        ValidateProfile(profile);

        if (originalLengthM < 0f)
            throw new ArgumentOutOfRangeException(nameof(originalLengthM));

        float meanTemperature = (startTemperatureC + endTemperatureC) * 0.5f;
        float alpha = GetLinearExpansionCoefficientPerK(profile, meanTemperature);

        if (alpha < 0f)
            throw new InvalidOperationException(
                $"Thermal expansion coefficient is not defined for '{profile.DisplayName}'.");

        return alpha * originalLengthM * (endTemperatureC - startTemperatureC);
    }

    /// <summary>
    /// Approximate volumetric expansion for an isotropic solid: ΔV/V ≈ 3αΔT.
    /// This is an engineering approximation, not suitable near phase transitions.
    /// </summary>
    public float CalculateVolumetricExpansionFraction(
        ThermalMaterialProfile profile,
        float startTemperatureC,
        float endTemperatureC)
    {
        float meanTemperature = (startTemperatureC + endTemperatureC) * 0.5f;
        float alpha = GetLinearExpansionCoefficientPerK(profile, meanTemperature);

        if (alpha < 0f)
            throw new InvalidOperationException(
                $"Thermal expansion coefficient is not defined for '{profile.DisplayName}'.");

        return 3f * alpha * (endTemperatureC - startTemperatureC);
    }

    /// <summary>
    /// Estimates density after thermal expansion, assuming mass is constant
    /// and expansion is isotropic. Returns reference density when expansion
    /// data is unavailable.
    /// </summary>
    public float EstimateDensityAtTemperature(
        ThermalMaterialProfile profile,
        float temperatureC)
    {
        ValidateProfile(profile);

        float referenceDensity = GetDensityKgPerM3(
            profile, profile.ReferenceTemperatureC);

        if (referenceDensity < 0f)
            return -1f;

        float alpha = GetLinearExpansionCoefficientPerK(
            profile, (temperatureC + profile.ReferenceTemperatureC) * 0.5f);

        if (alpha < 0f)
            return referenceDensity;

        float deltaTemperature = temperatureC - profile.ReferenceTemperatureC;
        float volumeRatio = 1f + (3f * alpha * deltaTemperature);

        if (volumeRatio <= MinimumPositiveValue)
            throw new InvalidOperationException(
                "The calculated thermal expansion produced an invalid volume ratio. Check the expansion coefficient and temperature range.");

        return referenceDensity / volumeRatio;
    }

    // ============================================================
    // COMBUSTION AND OXIDATIVE BURNING
    // ============================================================

    /// <summary>
    /// Advances a simplified combustion reaction for a time interval.
    /// oxygenAvailableKg is the oxygen mass available to this sample during
    /// that interval. The method returns released heat; it does not apply that
    /// heat to the sample automatically. Call ApplyThermalEnergy with the
    /// retained portion of HeatReleasedJ to model self-heating.
    /// </summary>
    public CombustionResult AdvanceCombustion(
        ThermalSample sample,
        float deltaTimeSeconds,
        float oxygenAvailableKg)
    {
        ValidateSample(sample);

        if (deltaTimeSeconds < 0f || float.IsNaN(deltaTimeSeconds) || float.IsInfinity(deltaTimeSeconds))
            throw new ArgumentOutOfRangeException(nameof(deltaTimeSeconds));
        if (oxygenAvailableKg < 0f || float.IsNaN(oxygenAvailableKg) || float.IsInfinity(oxygenAvailableKg))
            throw new ArgumentOutOfRangeException(nameof(oxygenAvailableKg));

        CombustionResult result = new CombustionResult();
        CombustionDefinition combustion = sample.Profile.Combustion;

        if (!combustion.Combustible ||
            sample.RemainingFuelMassKg <= 0f ||
            sample.TemperatureC < combustion.IgnitionTemperatureC ||
            oxygenAvailableKg <= 0f ||
            deltaTimeSeconds <= 0f ||
            combustion.OxygenRequiredKgPerKgFuel <= 0f ||
            combustion.HeatOfCombustionJPerKg <= 0f)
        {
            return result;
        }

        result.Ignited = true;

        float rate = CalculateArrheniusRate(
            combustion.PreExponentialFactorPerSecond,
            combustion.ActivationEnergyJPerMol,
            sample.TemperatureC);

        result.ReactionRatePerSecond = rate;

        float fractionReacted = 1f - Mathf.Exp(-rate * deltaTimeSeconds);
        float possibleFuelConsumed = sample.RemainingFuelMassKg * Mathf.Clamp01(fractionReacted);
        float oxygenLimitedFuel = oxygenAvailableKg / combustion.OxygenRequiredKgPerKgFuel;
        float fuelConsumed = Mathf.Min(possibleFuelConsumed, oxygenLimitedFuel);

        if (fuelConsumed <= 0f)
            return result;

        float oxygenConsumed = fuelConsumed * combustion.OxygenRequiredKgPerKgFuel;
        float charProduced = fuelConsumed * combustion.CharYieldFraction;
        float heatReleased = fuelConsumed * combustion.HeatOfCombustionJPerKg * combustion.CombustionEfficiency;

        sample.ConsumeFuel(fuelConsumed, combustion.CharYieldFraction);

        result.FuelConsumedKg = fuelConsumed;
        result.OxygenConsumedKg = oxygenConsumed;
        result.CharProducedKg = charProduced;
        result.HeatReleasedJ = heatReleased;

        return result;
    }

    // ============================================================
    // THERMAL DEGRADATION / PYROLYSIS-LIKE DAMAGE
    // ============================================================

    /// <summary>
    /// Advances heat-driven degradation. This can represent polymer breakdown,
    /// binder decomposition, loss of organic structure, or other thermally
    /// activated damage. It is deliberately separate from combustion.
    /// </summary>
    public DegradationResult AdvanceThermalDegradation(
        ThermalSample sample,
        float deltaTimeSeconds)
    {
        ValidateSample(sample);

        if (deltaTimeSeconds < 0f || float.IsNaN(deltaTimeSeconds) || float.IsInfinity(deltaTimeSeconds))
            throw new ArgumentOutOfRangeException(nameof(deltaTimeSeconds));

        DegradationResult result = new DegradationResult
        {
            TotalDamageFraction = sample.ThermalDamageFraction
        };

        ThermalDegradationDefinition degradation = sample.Profile.Degradation;

        if (!degradation.Enabled ||
            sample.MassKg <= 0f ||
            sample.TemperatureC < degradation.OnsetTemperatureC ||
            deltaTimeSeconds <= 0f)
        {
            return result;
        }

        float rate = CalculateArrheniusRate(
            degradation.PreExponentialFactorPerSecond,
            degradation.ActivationEnergyJPerMol,
            sample.TemperatureC);

        float previousDamage = sample.ThermalDamageFraction;
        float newDamage = 1f -
            ((1f - previousDamage) * Mathf.Exp(-rate * deltaTimeSeconds));

        float damageIncrease = Mathf.Clamp01(newDamage - previousDamage);
        float requestedMassLoss = sample.InitialMassKg *
            degradation.MaximumMassLossFraction * damageIncrease;
        float actualMassLoss = Mathf.Min(requestedMassLoss, sample.MassKg);

        sample.ApplyDegradation(damageIncrease, actualMassLoss);

        result.DegradationActive = true;
        result.DamageIncrease = damageIncrease;
        result.TotalDamageFraction = sample.ThermalDamageFraction;
        result.MassLostKg = actualMassLoss;
        result.ReactionRatePerSecond = rate;

        return result;
    }

    // ============================================================
    // SHARED ARRHENIUS RATE CALCULATION
    // ============================================================

    /// <summary>
    /// Calculates k = A × exp(-Ea / (R × T)).
    /// Activation energy is in J/mol and temperature is converted to kelvin.
    /// </summary>
    private static float CalculateArrheniusRate(
        float preExponentialFactorPerSecond,
        float activationEnergyJPerMol,
        float temperatureC)
    {
        float temperatureK = temperatureC - AbsoluteZeroCelsius;

        if (temperatureK <= 0f || preExponentialFactorPerSecond <= 0f)
            return 0f;

        float exponent = -Mathf.Max(0f, activationEnergyJPerMol) /
            (GasConstantJPerMolK * temperatureK);

        return preExponentialFactorPerSecond * Mathf.Exp(exponent);
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    [ContextMenu("Validate Thermal Profiles")]
    public void ValidateProfiles()
    {
        if (profiles == null)
        {
            Debug.LogError("Thermatics: profile list is null.", this);
            return;
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int errors = 0;

        foreach (ThermalMaterialProfile profile in profiles)
        {
            if (profile == null)
            {
                Debug.LogError("Thermatics: null thermal profile found.", this);
                errors++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(profile.MaterialId))
            {
                Debug.LogError("Thermatics: a profile has an empty material ID.", this);
                errors++;
                continue;
            }

            if (!ids.Add(profile.MaterialId))
            {
                Debug.LogError(
                    $"Thermatics: duplicate material ID '{profile.MaterialId}'.",
                    this);
                errors++;
            }

            if (profile.SpecificHeatJPerKgK == 0f || profile.ConductivityWPerMK == 0f)
            {
                Debug.LogWarning(
                    $"Thermatics: '{profile.DisplayName}' has a zero thermal property. Use -1 for unknown data, not zero.",
                    this);
            }

            if (profile.HasMeltingPoint &&
                profile.HasFreezingPoint &&
                profile.FreezingPointC > profile.MeltingPointC)
            {
                Debug.LogWarning(
                    $"Thermatics: '{profile.DisplayName}' has a freezing point above its melting point. Check whether this is intentional hysteresis.",
                    this);
            }

            if (profile.Combustion.Combustible &&
                (profile.Combustion.IgnitionTemperatureC < 0f ||
                 profile.Combustion.HeatOfCombustionJPerKg <= 0f ||
                 profile.Combustion.OxygenRequiredKgPerKgFuel <= 0f))
            {
                Debug.LogWarning(
                    $"Thermatics: combustible profile '{profile.DisplayName}' is missing ignition, heat-of-combustion, or oxygen-demand data.",
                    this);
            }
        }

        if (errors == 0)
            Debug.Log($"Thermatics: validation completed. {profiles.Count} profiles checked.", this);
        else
            Debug.LogError($"Thermatics: validation found {errors} profile errors.", this);
    }
}
}
