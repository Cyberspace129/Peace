using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParticleChemistry
{

/// <summary>
/// Fuilds is a ScriptableObject database for fluid behaviour and fluid
/// interactions with porous materials.
///
/// The asset stores reusable definitions. FluidSample and
/// PorousMaterialSample are runtime objects containing the state of an
/// individual quantity of fluid or porous material.
///
/// Main systems:
/// - Fluid properties and temperature-independent reference values.
/// - Porosity, permeability and pore capacity.
/// - Absorption modelled with Darcy flow and capillary pressure.
/// - Contact-angle-based surface wetting.
/// - Evaporation using vapour-pressure and mass-transfer calculations.
/// - Fluid retention and gravity-driven drainage.
/// - Reversible swelling and shrinkage as pore saturation changes.
///
/// This is an engineering-scale approximation. It is not a CFD solver and
/// does not resolve individual molecules or detailed pore geometry.
/// </summary>
[CreateAssetMenu(fileName = "Fuilds", menuName = "Particle Physics/Fuilds")]
public class Fuilds : ScriptableObject
{
    private const float UniversalGasConstantJPerMolK = 8.314462618f;
    private const float CelsiusToKelvin = 273.15f;
    private const float MmHgToPascal = 133.322368f;
    private const float StandardGravityMPerS2 = 9.80665f;
    private const float MinimumPositiveValue = 0.000001f;

    // ============================================================
    // ENUMERATIONS
    // ============================================================

    public enum FluidPhase
    {
        Liquid,
        Gas,
        Supercritical,
        Unknown
    }

    public enum WettingState
    {
        CompleteWetting,
        Wetting,
        Neutral,
        NonWetting,
        StronglyNonWetting,
        Unknown
    }

    public enum VapourPressureModel
    {
        None,
        Antoine,
        ClausiusClapeyron
    }

    // ============================================================
    // FLUID DEFINITION
    // ============================================================

    /// <summary>
    /// Reusable reference properties for a fluid such as water, ethanol,
    /// acetone or glycerol. This is a definition, not a runtime quantity.
    /// </summary>
    [Serializable]
    public class FluidDefinition
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string chemicalFormula;
        [SerializeField] private FluidPhase phase = FluidPhase.Liquid;

        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Reference Properties")]
        [Tooltip("Reference temperature in degrees Celsius for the properties below.")]
        [SerializeField] private float referenceTemperatureC = 20f;

        [Tooltip("Density in kg/m³ at the reference temperature. Must be greater than zero.")]
        [SerializeField] private float densityKgPerM3 = -1f;

        [Tooltip("Dynamic viscosity in Pa·s at the reference temperature. Must be greater than zero for Darcy absorption calculations.")]
        [SerializeField] private float dynamicViscosityPaS = -1f;

        [Tooltip("Surface tension in N/m at the reference temperature. Required for capillary calculations.")]
        [SerializeField] private float surfaceTensionNPerM = -1f;

        [Tooltip("Molar mass in kg/mol. Optional; used for some gas calculations.")]
        [SerializeField] private float molarMassKgPerMol = -1f;

        [Tooltip("Specific heat capacity in J/(kg·K). Used for estimated evaporative cooling.")]
        [SerializeField] private float specificHeatJPerKgK = -1f;

        [Header("Evaporation and Phase Change")]
        [SerializeField] private VapourPressureModel vapourPressureModel = VapourPressureModel.None;

        [Tooltip("Reference saturation vapour pressure in Pa, used by the Clausius-Clapeyron model.")]
        [SerializeField] private float referenceVapourPressurePa = -1f;

        [Tooltip("Temperature in °C at which the reference vapour pressure is defined.")]
        [SerializeField] private float vapourPressureReferenceTemperatureC = 20f;

        [Tooltip("Antoine A coefficient. Antoine pressure output is converted from mmHg to Pa.")]
        [SerializeField] private float antoineA;

        [Tooltip("Antoine B coefficient.")]
        [SerializeField] private float antoineB;

        [Tooltip("Antoine C coefficient. Antoine temperature input is in °C.")]
        [SerializeField] private float antoineC;

        [Tooltip("Lowest temperature in °C for which the Antoine coefficients should be used.")]
        [SerializeField] private float antoineMinimumTemperatureC = -1f;

        [Tooltip("Highest temperature in °C for which the Antoine coefficients should be used.")]
        [SerializeField] private float antoineMaximumTemperatureC = -1f;

        [Tooltip("Latent heat of vaporisation in J/kg. Required to estimate energy removed by evaporation.")]
        [SerializeField] private float latentHeatOfVaporisationJPerKg = -1f;

        [Tooltip("Normal boiling point in °C. Informational; evaporation can occur below this temperature.")]
        [SerializeField] private float normalBoilingPointC = -1f;

        [Tooltip("Mass-transfer coefficient in kg/(m²·s·Pa). Calibrate for the fluid, air movement and geometry being simulated. A value <= 0 disables calculated evaporation.")]
        [SerializeField] private float evaporationMassTransferCoefficient = -1f;

        [Header("Temperature Dependence")]
        [Tooltip("Optional multiplier curve for dynamic viscosity. X is temperature in °C; Y is a multiplier of the reference viscosity.")]
        [SerializeField] private AnimationCurve viscosityTemperatureMultiplier = CreateConstantCurve();

        [Tooltip("Optional multiplier curve for density. X is temperature in °C; Y is a multiplier of the reference density.")]
        [SerializeField] private AnimationCurve densityTemperatureMultiplier = CreateConstantCurve();

        [Tooltip("Optional multiplier curve for surface tension. X is temperature in °C; Y is a multiplier of the reference surface tension.")]
        [SerializeField] private AnimationCurve surfaceTensionTemperatureMultiplier = CreateConstantCurve();

        [TextArea(2, 4)]
        [SerializeField] private string notes;

        public string Id => id;
        public string DisplayName => displayName;
        public string ChemicalFormula => chemicalFormula;
        public FluidPhase Phase => phase;
        public string Description => description;
        public float ReferenceTemperatureC => referenceTemperatureC;
        public float DensityKgPerM3 => densityKgPerM3;
        public float DynamicViscosityPaS => dynamicViscosityPaS;
        public float SurfaceTensionNPerM => surfaceTensionNPerM;
        public float MolarMassKgPerMol => molarMassKgPerMol;
        public float SpecificHeatJPerKgK => specificHeatJPerKgK;
        public VapourPressureModel VapourModel => vapourPressureModel;
        public float ReferenceVapourPressurePa => referenceVapourPressurePa;
        public float VapourPressureReferenceTemperatureC => vapourPressureReferenceTemperatureC;
        public float AntoineA => antoineA;
        public float AntoineB => antoineB;
        public float AntoineC => antoineC;
        public float AntoineMinimumTemperatureC => antoineMinimumTemperatureC;
        public float AntoineMaximumTemperatureC => antoineMaximumTemperatureC;
        public float LatentHeatOfVaporisationJPerKg => latentHeatOfVaporisationJPerKg;
        public float NormalBoilingPointC => normalBoilingPointC;
        public float EvaporationMassTransferCoefficient => evaporationMassTransferCoefficient;
        public AnimationCurve ViscosityTemperatureMultiplier => viscosityTemperatureMultiplier;
        public AnimationCurve DensityTemperatureMultiplier => densityTemperatureMultiplier;
        public AnimationCurve SurfaceTensionTemperatureMultiplier => surfaceTensionTemperatureMultiplier;
        public string Notes => notes;

        public FluidDefinition(
            string id,
            string displayName,
            string chemicalFormula,
            FluidPhase phase,
            float densityKgPerM3,
            float dynamicViscosityPaS,
            float surfaceTensionNPerM,
            float specificHeatJPerKgK,
            float latentHeatOfVaporisationJPerKg,
            float normalBoilingPointC,
            string description = "",
            VapourPressureModel vapourPressureModel = VapourPressureModel.None,
            float referenceVapourPressurePa = -1f,
            float vapourPressureReferenceTemperatureC = 20f,
            float antoineA = 0f,
            float antoineB = 0f,
            float antoineC = 0f,
            float antoineMinimumTemperatureC = -1f,
            float antoineMaximumTemperatureC = -1f,
            float evaporationMassTransferCoefficient = -1f,
            float molarMassKgPerMol = -1f,
            string notes = "")
        {
            this.id = id;
            this.displayName = displayName;
            this.chemicalFormula = chemicalFormula;
            this.phase = phase;
            this.densityKgPerM3 = densityKgPerM3;
            this.dynamicViscosityPaS = dynamicViscosityPaS;
            this.surfaceTensionNPerM = surfaceTensionNPerM;
            this.specificHeatJPerKgK = specificHeatJPerKgK;
            this.latentHeatOfVaporisationJPerKg = latentHeatOfVaporisationJPerKg;
            this.normalBoilingPointC = normalBoilingPointC;
            this.description = description;
            this.vapourPressureModel = vapourPressureModel;
            this.referenceVapourPressurePa = referenceVapourPressurePa;
            this.vapourPressureReferenceTemperatureC = vapourPressureReferenceTemperatureC;
            this.antoineA = antoineA;
            this.antoineB = antoineB;
            this.antoineC = antoineC;
            this.antoineMinimumTemperatureC = antoineMinimumTemperatureC;
            this.antoineMaximumTemperatureC = antoineMaximumTemperatureC;
            this.evaporationMassTransferCoefficient = evaporationMassTransferCoefficient;
            this.molarMassKgPerMol = molarMassKgPerMol;
            this.notes = notes;
            viscosityTemperatureMultiplier = CreateConstantCurve();
            densityTemperatureMultiplier = CreateConstantCurve();
            surfaceTensionTemperatureMultiplier = CreateConstantCurve();
        }

        private static AnimationCurve CreateConstantCurve()
        {
            return new AnimationCurve(
                new Keyframe(-273.15f, 1f),
                new Keyframe(10000f, 1f));
        }
    }

    // ============================================================
    // POROUS MATERIAL PROFILE
    // ============================================================

    /// <summary>
    /// Describes the pore structure and fluid-storage behaviour of a solid.
    /// Porosity may be imported from the Material database. Permeability,
    /// pore size and retention behaviour must be supplied separately because
    /// they cannot be reliably inferred from porosity alone.
    /// </summary>
    [Serializable]
    public class PorousMaterialProfile
    {
        [Header("Material Link")]
        [SerializeField] private string materialId;
        [SerializeField] private string displayName;

        [Header("Pore Structure")]
        [Tooltip("Void volume divided by total bulk volume. Range 0 to 1.")]
        [Range(0f, 1f)]
        [SerializeField] private float porosity = 0.3f;

        [Tooltip("Intrinsic permeability in m². Use -1 when unknown. This is not the same as porosity.")]
        [SerializeField] private float permeabilityM2 = -1f;

        [Tooltip("Representative pore-throat radius in metres. Required for the capillary model.")]
        [SerializeField] private float meanPoreRadiusM = -1f;

        [Tooltip("Dimensionless tortuosity factor. Values >= 1 represent a longer, more complex pore path.")]
        [Min(1f)]
        [SerializeField] private float tortuosity = 1.5f;

        [Tooltip("Maximum fraction of pore volume that may be filled by the model. Usually below or equal to 1.")]
        [Range(0f, 1f)]
        [SerializeField] private float maximumSaturation = 1f;

        [Header("Retention and Drainage")]
        [Tooltip("Pore-volume fraction retained after gravity drainage has largely finished.")]
        [Range(0f, 1f)]
        [SerializeField] private float retentionSaturation = 0.25f;

        [Tooltip("First-order drainage rate in 1/s. Use 0 to disable automatic drainage.")]
        [Min(0f)]
        [SerializeField] private float drainageRatePerSecond = 0.01f;

        [Header("Swelling and Shrinkage")]
        [Tooltip("Fractional increase in bulk volume at full saturation. 0.1 means up to 10% expansion.")]
        [Min(0f)]
        [SerializeField] private float swellingAtFullSaturation = 0f;

        [Tooltip("Optional notes about pore structure, anisotropy or measurement conditions.")]
        [TextArea(2, 4)]
        [SerializeField] private string notes;

        public string MaterialId => materialId;
        public string DisplayName => displayName;
        public float Porosity => porosity;
        public float PermeabilityM2 => permeabilityM2;
        public float MeanPoreRadiusM => meanPoreRadiusM;
        public float Tortuosity => tortuosity;
        public float MaximumSaturation => maximumSaturation;
        public float RetentionSaturation => retentionSaturation;
        public float DrainageRatePerSecond => drainageRatePerSecond;
        public float SwellingAtFullSaturation => swellingAtFullSaturation;
        public string Notes => notes;

        public PorousMaterialProfile(
            string materialId,
            string displayName,
            float porosity,
            float permeabilityM2 = -1f,
            float meanPoreRadiusM = -1f,
            float tortuosity = 1.5f,
            float maximumSaturation = 1f,
            float retentionSaturation = 0.25f,
            float drainageRatePerSecond = 0.01f,
            float swellingAtFullSaturation = 0f,
            string notes = "")
        {
            this.materialId = materialId;
            this.displayName = displayName;
            this.porosity = Mathf.Clamp01(porosity);
            this.permeabilityM2 = permeabilityM2;
            this.meanPoreRadiusM = meanPoreRadiusM;
            this.tortuosity = Mathf.Max(1f, tortuosity);
            this.maximumSaturation = Mathf.Clamp01(maximumSaturation);
            this.retentionSaturation = Mathf.Clamp01(retentionSaturation);
            this.drainageRatePerSecond = Mathf.Max(0f, drainageRatePerSecond);
            this.swellingAtFullSaturation = Mathf.Max(0f, swellingAtFullSaturation);
            this.notes = notes;
        }
    }

    // ============================================================
    // FLUID / SOLID WETTING PROFILE
    // ============================================================

    /// <summary>
    /// Contact angle belongs to a fluid-and-surface pair, not to the fluid
    /// alone. Contact angle is measured through the liquid phase.
    /// </summary>
    [Serializable]
    public class WettingProfile
    {
        [SerializeField] private string fluidId;
        [SerializeField] private string materialId;
        [Tooltip("Static contact angle in degrees, measured through the liquid. 0° is complete wetting; 180° is complete non-wetting.")]
        [Range(0f, 180f)]
        [SerializeField] private float contactAngleDegrees = 90f;

        [Tooltip("Optional notes about surface preparation, roughness, contamination and measurement conditions.")]
        [TextArea(1, 3)]
        [SerializeField] private string notes;

        public string FluidId => fluidId;
        public string MaterialId => materialId;
        public float ContactAngleDegrees => contactAngleDegrees;
        public string Notes => notes;

        public WettingProfile(
            string fluidId,
            string materialId,
            float contactAngleDegrees,
            string notes = "")
        {
            this.fluidId = fluidId;
            this.materialId = materialId;
            this.contactAngleDegrees = Mathf.Clamp(contactAngleDegrees, 0f, 180f);
            this.notes = notes;
        }
    }

    // ============================================================
    // RUNTIME FLUID SAMPLE
    // ============================================================

    /// <summary>
    /// One actual fluid quantity. Its mass and temperature can change during
    /// absorption and evaporation without changing the shared definition.
    /// </summary>
    [Serializable]
    public class FluidSample
    {
        private readonly FluidDefinition profile;
        [SerializeField] private float massKg;
        [SerializeField] private float temperatureC;

        public FluidDefinition Profile => profile;
        public float MassKg => massKg;
        public float TemperatureC => temperatureC;
        public float DensityKgPerM3 =>
            profile == null ? -1f : Fuilds.GetTemperatureAdjustedDensity(profile, temperatureC);
        public float VolumeM3 =>
            DensityKgPerM3 > 0f ? massKg / DensityKgPerM3 : -1f;
        public float VolumeLitres => VolumeM3 >= 0f ? VolumeM3 * 1000f : -1f;

        public FluidSample(FluidDefinition profile, float massKg, float temperatureC)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            if (massKg < 0f || float.IsNaN(massKg) || float.IsInfinity(massKg))
                throw new ArgumentOutOfRangeException(nameof(massKg));
            Fuilds.ValidateTemperature(temperatureC, nameof(temperatureC));

            this.profile = profile;
            this.massKg = massKg;
            this.temperatureC = temperatureC;
        }

        internal float RemoveMass(float requestedMassKg)
        {
            float removed = Mathf.Clamp(requestedMassKg, 0f, massKg);
            massKg -= removed;
            return removed;
        }

        internal void AddMass(float addedMassKg)
        {
            massKg += Mathf.Max(0f, addedMassKg);
        }

        internal void SetTemperature(float value)
        {
            Fuilds.ValidateTemperature(value, nameof(value));
            temperatureC = value;
        }
    }

    // ============================================================
    // RUNTIME POROUS MATERIAL SAMPLE
    // ============================================================

    /// <summary>
    /// One piece or volume of porous material. AbsorbedFluidMassKg tracks the
    /// fluid held in its pores. Swelling is calculated from pore saturation.
    /// The model assumes one tracked fluid per porous sample.
    /// </summary>
    [Serializable]
    public class PorousMaterialSample
    {
        private readonly PorousMaterialProfile profile;
        [SerializeField] private float dryMassKg;
        [SerializeField] private float dryBulkVolumeM3;
        [SerializeField] private float absorbedFluidMassKg;
        [SerializeField] private float temperatureC;
        [SerializeField] private string absorbedFluidId;

        public PorousMaterialProfile Profile => profile;
        public float DryMassKg => dryMassKg;
        public float DryBulkVolumeM3 => dryBulkVolumeM3;
        public float AbsorbedFluidMassKg => absorbedFluidMassKg;
        public float TemperatureC => temperatureC;
        public string AbsorbedFluidId => absorbedFluidId;
        public float TotalMassKg => dryMassKg + absorbedFluidMassKg;
        public float PoreVolumeM3 => dryBulkVolumeM3 * profile.Porosity;

        public float Saturation =>
            PoreVolumeM3 > 0f && profile.Porosity > 0f
                ? Mathf.Clamp01(CurrentAbsorbedVolumeM3 / PoreVolumeM3)
                : 0f;

        public float CurrentAbsorbedVolumeM3 { get; private set; }

        public float CurrentBulkVolumeM3 =>
            dryBulkVolumeM3 * (1f + profile.SwellingAtFullSaturation * Saturation);

        public float VolumeChangeM3 => CurrentBulkVolumeM3 - dryBulkVolumeM3;

        public float VolumeChangeFraction =>
            dryBulkVolumeM3 > 0f ? VolumeChangeM3 / dryBulkVolumeM3 : 0f;

        public PorousMaterialSample(
            PorousMaterialProfile profile,
            float dryMassKg,
            float dryBulkVolumeM3,
            float temperatureC = 20f)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            if (dryMassKg <= 0f || float.IsNaN(dryMassKg) || float.IsInfinity(dryMassKg))
                throw new ArgumentOutOfRangeException(nameof(dryMassKg), "Dry mass must be finite and greater than zero.");
            if (dryBulkVolumeM3 <= 0f || float.IsNaN(dryBulkVolumeM3) || float.IsInfinity(dryBulkVolumeM3))
                throw new ArgumentOutOfRangeException(nameof(dryBulkVolumeM3), "Bulk volume must be finite and greater than zero.");
            Fuilds.ValidateTemperature(temperatureC, nameof(temperatureC));

            this.profile = profile;
            this.dryMassKg = dryMassKg;
            this.dryBulkVolumeM3 = dryBulkVolumeM3;
            this.temperatureC = temperatureC;
            absorbedFluidMassKg = 0f;
            absorbedFluidId = string.Empty;
            CurrentAbsorbedVolumeM3 = 0f;
        }

        internal void SetTemperature(float value, float absorbedFluidDensityKgPerM3)
        {
            Fuilds.ValidateTemperature(value, nameof(value));
            temperatureC = value;
            if (absorbedFluidMassKg > 0f && absorbedFluidDensityKgPerM3 > 0f)
                CurrentAbsorbedVolumeM3 = absorbedFluidMassKg / absorbedFluidDensityKgPerM3;
        }

        internal void AddAbsorbedFluid(float massKg, float volumeM3, string fluidId)
        {
            absorbedFluidMassKg += Mathf.Max(0f, massKg);
            CurrentAbsorbedVolumeM3 += Mathf.Max(0f, volumeM3);
            if (!string.IsNullOrEmpty(fluidId))
                absorbedFluidId = fluidId;
        }

        /// <summary>
        /// Removes material matrix mass when a solid constituent dissolves or
        /// otherwise leaves the porous skeleton. Pore fluid is not removed.
        /// </summary>
        internal float RemoveDryMass(float requestedMassKg)
        {
            float removed = Mathf.Clamp(requestedMassKg, 0f, dryMassKg);
            dryMassKg = Mathf.Max(0f, dryMassKg - removed);
            return removed;
        }

        internal float RemoveAbsorbedFluid(float requestedMassKg, float fluidDensityKgPerM3)
        {
            if (fluidDensityKgPerM3 <= 0f)
                return 0f;

            float removed = Mathf.Clamp(requestedMassKg, 0f, absorbedFluidMassKg);
            absorbedFluidMassKg -= removed;
            CurrentAbsorbedVolumeM3 = Mathf.Max(
                0f,
                CurrentAbsorbedVolumeM3 - removed / fluidDensityKgPerM3);
            if (absorbedFluidMassKg <= Fuilds.MinimumPositiveValue)
            {
                absorbedFluidMassKg = 0f;
                CurrentAbsorbedVolumeM3 = 0f;
                absorbedFluidId = string.Empty;
            }
            return removed;
        }
    }

    // ============================================================
    // RESULT DATA TYPES
    // ============================================================

    [Serializable]
    public class WettingAssessment
    {
        public string FluidId { get; private set; }
        public string MaterialId { get; private set; }
        public bool HasMeasuredProfile { get; private set; }
        public float ContactAngleDegrees { get; private set; }
        public WettingState State { get; private set; }

        public WettingAssessment(
            string fluidId,
            string materialId,
            bool hasMeasuredProfile,
            float contactAngleDegrees,
            WettingState state)
        {
            FluidId = fluidId;
            MaterialId = materialId;
            HasMeasuredProfile = hasMeasuredProfile;
            ContactAngleDegrees = contactAngleDegrees;
            State = state;
        }
    }

    [Serializable]
    public class CapillaryResult
    {
        public float CapillaryPressurePa { get; private set; }
        public float CapillaryRiseMetres { get; private set; }
        public bool PromotesSpontaneousWetting { get; private set; }
        public WettingAssessment Wetting { get; private set; }

        public CapillaryResult(
            float capillaryPressurePa,
            float capillaryRiseMetres,
            bool promotesSpontaneousWetting,
            WettingAssessment wetting)
        {
            CapillaryPressurePa = capillaryPressurePa;
            CapillaryRiseMetres = capillaryRiseMetres;
            PromotesSpontaneousWetting = promotesSpontaneousWetting;
            Wetting = wetting;
        }
    }

    [Serializable]
    public class AbsorptionResult
    {
        public float FlowRateM3PerSecond { get; private set; }
        public float DrivingPressurePa { get; private set; }
        public float AbsorbedVolumeM3 { get; private set; }
        public float AbsorbedMassKg { get; private set; }
        public float FinalSaturation { get; private set; }
        public float FinalBulkVolumeM3 { get; private set; }
        public bool WasLimitedByPoreCapacity { get; private set; }
        public bool WasLimitedByAvailableFluid { get; private set; }
        public WettingAssessment Wetting { get; private set; }

        public AbsorptionResult(
            float flowRateM3PerSecond,
            float drivingPressurePa,
            float absorbedVolumeM3,
            float absorbedMassKg,
            float finalSaturation,
            float finalBulkVolumeM3,
            bool wasLimitedByPoreCapacity,
            bool wasLimitedByAvailableFluid,
            WettingAssessment wetting)
        {
            FlowRateM3PerSecond = flowRateM3PerSecond;
            DrivingPressurePa = drivingPressurePa;
            AbsorbedVolumeM3 = absorbedVolumeM3;
            AbsorbedMassKg = absorbedMassKg;
            FinalSaturation = finalSaturation;
            FinalBulkVolumeM3 = finalBulkVolumeM3;
            WasLimitedByPoreCapacity = wasLimitedByPoreCapacity;
            WasLimitedByAvailableFluid = wasLimitedByAvailableFluid;
            Wetting = wetting;
        }
    }

    [Serializable]
    public class RetentionResult
    {
        public float RetainedMassKg { get; private set; }
        public float DrainableMassKg { get; private set; }
        public float RetentionFraction { get; private set; }
        public float Saturation { get; private set; }

        public RetentionResult(
            float retainedMassKg,
            float drainableMassKg,
            float retentionFraction,
            float saturation)
        {
            RetainedMassKg = retainedMassKg;
            DrainableMassKg = drainableMassKg;
            RetentionFraction = retentionFraction;
            Saturation = saturation;
        }
    }

    [Serializable]
    public class DrainageResult
    {
        public float DrainedMassKg { get; private set; }
        public float RemainingMassKg { get; private set; }
        public float FinalSaturation { get; private set; }
        public float FinalBulkVolumeM3 { get; private set; }

        public DrainageResult(
            float drainedMassKg,
            float remainingMassKg,
            float finalSaturation,
            float finalBulkVolumeM3)
        {
            DrainedMassKg = drainedMassKg;
            RemainingMassKg = remainingMassKg;
            FinalSaturation = finalSaturation;
            FinalBulkVolumeM3 = finalBulkVolumeM3;
        }
    }

    [Serializable]
    public class EvaporationResult
    {
        public float SaturationVapourPressurePa { get; private set; }
        public float AmbientVapourPressurePa { get; private set; }
        public float EvaporatedMassKg { get; private set; }
        public float LatentEnergyRemovedJ { get; private set; }
        public float FinalMassKg { get; private set; }
        public float EstimatedTemperatureChangeC { get; private set; }

        public EvaporationResult(
            float saturationVapourPressurePa,
            float ambientVapourPressurePa,
            float evaporatedMassKg,
            float latentEnergyRemovedJ,
            float finalMassKg,
            float estimatedTemperatureChangeC)
        {
            SaturationVapourPressurePa = saturationVapourPressurePa;
            AmbientVapourPressurePa = ambientVapourPressurePa;
            EvaporatedMassKg = evaporatedMassKg;
            LatentEnergyRemovedJ = latentEnergyRemovedJ;
            FinalMassKg = finalMassKg;
            EstimatedTemperatureChangeC = estimatedTemperatureChangeC;
        }
    }

    // ============================================================
    // SERIALIZED CATALOGUES
    // ============================================================

    [Header("Fluid Definitions")]
    [SerializeField] private List<FluidDefinition> fluids = new List<FluidDefinition>();

    [Header("Porous Material Profiles")]
    [SerializeField] private List<PorousMaterialProfile> porousMaterials =
        new List<PorousMaterialProfile>();

    [Header("Fluid / Surface Wetting Profiles")]
    [SerializeField] private List<WettingProfile> wettingProfiles =
        new List<WettingProfile>();

    [Header("Optional Material Database")]
    [SerializeField] private ParticleChemistry.Material materialDatabase;

    public IReadOnlyList<FluidDefinition> Fluids => fluids;
    public IReadOnlyList<PorousMaterialProfile> PorousMaterials => porousMaterials;
    public IReadOnlyList<WettingProfile> WettingProfiles => wettingProfiles;
    public ParticleChemistry.Material MaterialDatabase => materialDatabase;

    // ============================================================
    // INITIALIZATION AND EXAMPLE DATA
    // ============================================================

    private void OnEnable()
    {
        if (fluids == null)
            fluids = new List<FluidDefinition>();
        if (porousMaterials == null)
            porousMaterials = new List<PorousMaterialProfile>();
        if (wettingProfiles == null)
            wettingProfiles = new List<WettingProfile>();

        if (fluids.Count == 0)
        {
            AddBuiltInExampleFluids();
        }
    }

    /// <summary>
    /// Adds example fluid definitions. Existing entries are preserved.
    /// The mass-transfer coefficients are starting values only and should
    /// be calibrated for the intended scale and airflow conditions.
    /// </summary>
    [ContextMenu("Add Built-in Example Fluids")]
    public void AddBuiltInExampleFluids()
    {
        EnsureLists();

        AddFluidIfMissing(new FluidDefinition(
            id: "water",
            displayName: "Water",
            chemicalFormula: "H2O",
            phase: FluidPhase.Liquid,
            densityKgPerM3: 998.2f,
            dynamicViscosityPaS: 0.001002f,
            surfaceTensionNPerM: 0.0728f,
            specificHeatJPerKgK: 4182f,
            latentHeatOfVaporisationJPerKg: 2.45e6f,
            normalBoilingPointC: 100f,
            description: "Liquid water near room temperature.",
            vapourPressureModel: VapourPressureModel.Antoine,
            antoineA: 8.07131f,
            antoineB: 1730.63f,
            antoineC: 233.426f,
            antoineMinimumTemperatureC: 1f,
            antoineMaximumTemperatureC: 100f,
            evaporationMassTransferCoefficient: 1.0e-8f,
            molarMassKgPerMol: 0.01801528f,
            notes: "Reference properties are approximate and vary with temperature and dissolved substances."));

        AddFluidIfMissing(new FluidDefinition(
            id: "ethanol",
            displayName: "Ethanol",
            chemicalFormula: "C2H5OH",
            phase: FluidPhase.Liquid,
            densityKgPerM3: 789f,
            dynamicViscosityPaS: 0.001074f,
            surfaceTensionNPerM: 0.0223f,
            specificHeatJPerKgK: 2440f,
            latentHeatOfVaporisationJPerKg: 8.41e5f,
            normalBoilingPointC: 78.37f,
            description: "Ethanol near room temperature.",
            vapourPressureModel: VapourPressureModel.Antoine,
            antoineA: 8.20417f,
            antoineB: 1642.89f,
            antoineC: 230.300f,
            antoineMinimumTemperatureC: 0f,
            antoineMaximumTemperatureC: 78f,
            evaporationMassTransferCoefficient: 1.0e-8f,
            molarMassKgPerMol: 0.04606844f,
            notes: "Pure ethanol reference values; ethanol-water mixtures differ."));

        AddFluidIfMissing(new FluidDefinition(
            id: "acetone",
            displayName: "Acetone",
            chemicalFormula: "C3H6O",
            phase: FluidPhase.Liquid,
            densityKgPerM3: 784.5f,
            dynamicViscosityPaS: 0.000306f,
            surfaceTensionNPerM: 0.0237f,
            specificHeatJPerKgK: 2160f,
            latentHeatOfVaporisationJPerKg: 5.18e5f,
            normalBoilingPointC: 56.05f,
            description: "Acetone near room temperature.",
            vapourPressureModel: VapourPressureModel.Antoine,
            antoineA: 7.11714f,
            antoineB: 1210.595f,
            antoineC: 229.664f,
            antoineMinimumTemperatureC: 1f,
            antoineMaximumTemperatureC: 56f,
            evaporationMassTransferCoefficient: 1.0e-8f,
            molarMassKgPerMol: 0.058080f,
            notes: "Highly volatile and flammable; use suitable safety controls in real experiments."));

        AddFluidIfMissing(new FluidDefinition(
            id: "glycerol",
            displayName: "Glycerol",
            chemicalFormula: "C3H8O3",
            phase: FluidPhase.Liquid,
            densityKgPerM3: 1260f,
            dynamicViscosityPaS: 1.412f,
            surfaceTensionNPerM: 0.0634f,
            specificHeatJPerKgK: 2410f,
            latentHeatOfVaporisationJPerKg: -1f,
            normalBoilingPointC: 290f,
            description: "Viscous glycerol near room temperature.",
            vapourPressureModel: VapourPressureModel.None,
            evaporationMassTransferCoefficient: -1f,
            molarMassKgPerMol: 0.0920938f,
            notes: "High viscosity strongly reduces porous-medium infiltration."));

        MarkAssetDirty();
    }

    private void AddFluidIfMissing(FluidDefinition definition)
    {
        if (!HasFluid(definition.Id))
            fluids.Add(definition);
    }

    // ============================================================
    // OPTIONAL IMPORT FROM MATERIAL DATABASE
    // ============================================================

    /// <summary>
    /// Creates porous profiles for materials whose Material database porosity
    /// is known. Permeability and pore radius remain unknown (-1) and must be
    /// entered from measurements or a suitable reference source.
    /// </summary>
    [ContextMenu("Add Missing Porous Profiles From Material Database")]
    public void AddMissingPorousProfilesFromMaterialDatabase()
    {
        if (materialDatabase == null)
            throw new InvalidOperationException(
                "Assign a ParticleChemistry.Material database first.");

        EnsureLists();

        foreach (ParticleChemistry.Material.MaterialDefinition material
            in materialDatabase.Materials)
        {
            if (material == null || material.Porosity < 0f || HasPorousProfile(material.Id))
                continue;

            porousMaterials.Add(new PorousMaterialProfile(
                materialId: material.Id,
                displayName: material.Name,
                porosity: material.Porosity,
                permeabilityM2: -1f,
                meanPoreRadiusM: -1f,
                notes: "Porosity copied from the Material database. Add measured permeability and pore-size data."));
        }

        MarkAssetDirty();
    }

    /// <summary>
    /// Imports missing porous profiles from an explicitly supplied material database.
    /// Only porosity is imported; permeability, pore radius and retention require
    /// separate measured or literature values.
    /// </summary>
    public void AddMissingPorousProfilesFromMaterialDatabase(ParticleChemistry.Material database)
    {
        if (database == null)
            throw new ArgumentNullException(nameof(database));

        EnsureLists();
        int added = 0;
        foreach (ParticleChemistry.Material.MaterialDefinition material in database.Materials)
        {
            if (material == null || material.Porosity < 0f || HasPorousProfile(material.Id))
                continue;

            porousMaterials.Add(new PorousMaterialProfile(
                materialId: material.Id,
                displayName: material.Name,
                porosity: material.Porosity,
                permeabilityM2: -1f,
                meanPoreRadiusM: -1f,
                notes: "Porosity copied from the central Material database. Supply measured permeability, pore radius and retention data."));
            added++;
        }

        if (added > 0)
            MarkAssetDirty();
    }

    // ============================================================
    // FLUID LOOKUP AND PROFILE MANAGEMENT
    // ============================================================

    public bool HasFluid(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || fluids == null)
            return false;

        foreach (FluidDefinition fluid in fluids)
        {
            if (fluid != null && string.Equals(
                fluid.Id, id, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public FluidDefinition GetFluid(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Fluid ID cannot be empty.", nameof(id));

        if (fluids != null)
        {
            foreach (FluidDefinition fluid in fluids)
            {
                if (fluid != null && string.Equals(
                    fluid.Id, id, StringComparison.OrdinalIgnoreCase))
                    return fluid;
            }
        }

        throw new ArgumentException(
            $"Unknown fluid ID '{id}'. Add it to the Fuilds asset first.", nameof(id));
    }

    public void AddFluid(FluidDefinition definition)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));
        if (string.IsNullOrWhiteSpace(definition.Id))
            throw new ArgumentException("Fluid definition requires a stable ID.");
        if (HasFluid(definition.Id))
            throw new InvalidOperationException($"Fluid '{definition.Id}' already exists.");

        EnsureLists();
        fluids.Add(definition);
        MarkAssetDirty();
    }

    public bool HasPorousProfile(string materialId)
    {
        if (string.IsNullOrWhiteSpace(materialId) || porousMaterials == null)
            return false;

        foreach (PorousMaterialProfile profile in porousMaterials)
        {
            if (profile != null && string.Equals(
                profile.MaterialId, materialId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public PorousMaterialProfile GetPorousProfile(string materialId)
    {
        if (string.IsNullOrWhiteSpace(materialId))
            throw new ArgumentException("Material ID cannot be empty.", nameof(materialId));

        if (porousMaterials != null)
        {
            foreach (PorousMaterialProfile profile in porousMaterials)
            {
                if (profile != null && string.Equals(
                    profile.MaterialId, materialId, StringComparison.OrdinalIgnoreCase))
                    return profile;
            }
        }

        throw new ArgumentException(
            $"No porous-material profile exists for '{materialId}'. Add one to the Fuilds asset.",
            nameof(materialId));
    }

    public void AddPorousProfile(PorousMaterialProfile profile)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));
        if (string.IsNullOrWhiteSpace(profile.MaterialId))
            throw new ArgumentException("Porous-material profile requires a material ID.");
        if (HasPorousProfile(profile.MaterialId))
            throw new InvalidOperationException(
                $"Porous-material profile '{profile.MaterialId}' already exists.");

        EnsureLists();
        porousMaterials.Add(profile);
        MarkAssetDirty();
    }

    // ============================================================
    // WETTING PROFILE MANAGEMENT
    // ============================================================

    public bool HasWettingProfile(string fluidId, string materialId)
    {
        if (string.IsNullOrWhiteSpace(fluidId) ||
            string.IsNullOrWhiteSpace(materialId) || wettingProfiles == null)
            return false;

        foreach (WettingProfile profile in wettingProfiles)
        {
            if (profile != null &&
                string.Equals(profile.FluidId, fluidId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(profile.MaterialId, materialId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public WettingProfile GetWettingProfile(string fluidId, string materialId)
    {
        if (wettingProfiles != null)
        {
            foreach (WettingProfile profile in wettingProfiles)
            {
                if (profile != null &&
                    string.Equals(profile.FluidId, fluidId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(profile.MaterialId, materialId, StringComparison.OrdinalIgnoreCase))
                    return profile;
            }
        }

        throw new ArgumentException(
            $"No wetting profile exists for fluid '{fluidId}' on material '{materialId}'.");
    }

    public void AddWettingProfile(WettingProfile profile)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));
        if (string.IsNullOrWhiteSpace(profile.FluidId) ||
            string.IsNullOrWhiteSpace(profile.MaterialId))
            throw new ArgumentException("Wetting profile requires both a fluid ID and material ID.");
        if (!HasFluid(profile.FluidId))
            throw new ArgumentException($"Unknown fluid ID '{profile.FluidId}'.");
        if (HasWettingProfile(profile.FluidId, profile.MaterialId))
            throw new InvalidOperationException(
                $"Wetting profile for '{profile.FluidId}' and '{profile.MaterialId}' already exists.");

        EnsureLists();
        wettingProfiles.Add(profile);
        MarkAssetDirty();
    }

    public WettingAssessment AssessWetting(string fluidId, string materialId)
    {
        GetFluid(fluidId);
        if (string.IsNullOrWhiteSpace(materialId))
            throw new ArgumentException("Material ID cannot be empty.", nameof(materialId));

        bool hasProfile = HasWettingProfile(fluidId, materialId);
        float angle = hasProfile
            ? GetWettingProfile(fluidId, materialId).ContactAngleDegrees
            : 90f;

        return new WettingAssessment(
            fluidId,
            materialId,
            hasProfile,
            angle,
            ClassifyWetting(angle));
    }

    private static WettingState ClassifyWetting(float angle)
    {
        if (angle <= 10f) return WettingState.CompleteWetting;
        if (angle < 90f) return WettingState.Wetting;
        if (Mathf.Abs(angle - 90f) < 0.0001f) return WettingState.Neutral;
        if (angle < 150f) return WettingState.NonWetting;
        return WettingState.StronglyNonWetting;
    }

    // ============================================================
    // RUNTIME SAMPLE CREATION
    // ============================================================

    public FluidSample CreateFluidSample(
        string fluidId,
        float volumeLitres,
        float temperatureC = 20f)
    {
        if (volumeLitres < 0f || float.IsNaN(volumeLitres) || float.IsInfinity(volumeLitres))
            throw new ArgumentOutOfRangeException(nameof(volumeLitres));
        ValidateTemperature(temperatureC, nameof(temperatureC));

        FluidDefinition profile = GetFluid(fluidId);
        float density = GetTemperatureAdjustedDensity(profile, temperatureC);
        if (density <= 0f)
            throw new InvalidOperationException(
                $"Fluid '{fluidId}' has no valid density at {temperatureC} °C.");

        float massKg = volumeLitres / 1000f * density;
        return new FluidSample(profile, massKg, temperatureC);
    }

    public FluidSample CreateFluidSampleByMass(
        string fluidId,
        float massKg,
        float temperatureC = 20f)
    {
        return new FluidSample(GetFluid(fluidId), massKg, temperatureC);
    }

    public PorousMaterialSample CreatePorousMaterialSample(
        string materialId,
        float dryMassKg,
        float dryBulkVolumeM3,
        float temperatureC = 20f)
    {
        return new PorousMaterialSample(
            GetPorousProfile(materialId),
            dryMassKg,
            dryBulkVolumeM3,
            temperatureC);
    }

    /// <summary>Updates a runtime fluid sample's temperature.</summary>
    public void SetFluidTemperature(FluidSample sample, float temperatureC)
    {
        if (sample == null)
            throw new ArgumentNullException(nameof(sample));
        ValidateTemperature(temperatureC, nameof(temperatureC));
        sample.SetTemperature(temperatureC);
    }

    /// <summary>
    /// Updates a porous sample's temperature and recalculates the volume
    /// occupied by its absorbed fluid using the fluid's adjusted density.
    /// </summary>
    public void SetPorousMaterialTemperature(
        PorousMaterialSample sample,
        float temperatureC)
    {
        if (sample == null)
            throw new ArgumentNullException(nameof(sample));
        ValidateTemperature(temperatureC, nameof(temperatureC));

        float density = -1f;
        if (!string.IsNullOrEmpty(sample.AbsorbedFluidId) &&
            sample.AbsorbedFluidMassKg > 0f)
        {
            density = GetDensityKgPerM3(sample.AbsorbedFluidId, temperatureC);
        }

        sample.SetTemperature(temperatureC, density);
    }

    // ============================================================
    // TEMPERATURE-ADJUSTED FLUID PROPERTIES
    // ============================================================

    public float GetDensityKgPerM3(string fluidId, float temperatureC)
    {
        return GetTemperatureAdjustedDensity(GetFluid(fluidId), temperatureC);
    }

    public float GetDynamicViscosityPaS(string fluidId, float temperatureC)
    {
        FluidDefinition fluid = GetFluid(fluidId);
        ValidateTemperature(temperatureC, nameof(temperatureC));
        if (fluid.DynamicViscosityPaS <= 0f)
            return -1f;

        float multiplier = EvaluateMultiplier(
            fluid.ViscosityTemperatureMultiplier, temperatureC);
        return fluid.DynamicViscosityPaS * multiplier;
    }

    public float GetSurfaceTensionNPerM(string fluidId, float temperatureC)
    {
        FluidDefinition fluid = GetFluid(fluidId);
        ValidateTemperature(temperatureC, nameof(temperatureC));
        if (fluid.SurfaceTensionNPerM < 0f)
            return -1f;

        float multiplier = EvaluateMultiplier(
            fluid.SurfaceTensionTemperatureMultiplier, temperatureC);
        return fluid.SurfaceTensionNPerM * multiplier;
    }

    private static float GetTemperatureAdjustedDensity(
        FluidDefinition fluid,
        float temperatureC)
    {
        ValidateTemperature(temperatureC, nameof(temperatureC));
        if (fluid == null || fluid.DensityKgPerM3 <= 0f)
            return -1f;

        return fluid.DensityKgPerM3 * EvaluateMultiplier(
            fluid.DensityTemperatureMultiplier, temperatureC);
    }

    private static float EvaluateMultiplier(AnimationCurve curve, float temperatureC)
    {
        if (curve == null || curve.length == 0)
            return 1f;
        return Mathf.Max(0f, curve.Evaluate(temperatureC));
    }

    // ============================================================
    // CAPILLARY ACTION
    // ============================================================

    /// <summary>
    /// Young-Laplace capillary pressure: ΔP = 2 γ cos(θ) / r.
    /// A positive value assists spontaneous infiltration; a negative value
    /// means external pressure is required to force the liquid into the pore.
    /// </summary>
    public CapillaryResult CalculateCapillaryAction(
        string fluidId,
        string materialId,
        float temperatureC = 20f)
    {
        FluidDefinition fluid = GetFluid(fluidId);
        PorousMaterialProfile porous = GetPorousProfile(materialId);
        WettingAssessment wetting = AssessWetting(fluidId, materialId);

        float gamma = GetSurfaceTensionNPerM(fluidId, temperatureC);
        if (gamma < 0f || porous.MeanPoreRadiusM <= 0f)
            return new CapillaryResult(0f, 0f, false, wetting);

        float thetaRadians = wetting.ContactAngleDegrees * Mathf.Deg2Rad;
        float capillaryPressure =
            2f * gamma * Mathf.Cos(thetaRadians) / porous.MeanPoreRadiusM;

        float density = GetTemperatureAdjustedDensity(fluid, temperatureC);
        float rise = 0f;
        if (density > 0f)
        {
            rise = capillaryPressure /
                (density * StandardGravityMPerS2);
        }

        return new CapillaryResult(
            capillaryPressure,
            rise,
            capillaryPressure > 0f,
            wetting);
    }

    // ============================================================
    // ABSORPTION / DARCY FLOW
    // ============================================================

    /// <summary>
    /// Moves fluid from a free FluidSample into a porous material sample.
    /// Darcy's law is used for the flow estimate:
    /// Q = k A ΔP / (μ L τ).
    ///
    /// appliedPressurePa is extra pressure applied to the fluid. A non-wetting
    /// surface can require external pressure before infiltration begins.
    /// Flow path length is an approximate distance through the porous sample.
    /// </summary>
    public AbsorptionResult AbsorbIntoPorousMaterial(
        FluidSample fluidSample,
        PorousMaterialSample porousSample,
        float contactAreaM2,
        float flowPathLengthM,
        float deltaTimeSeconds,
        float appliedPressurePa = 0f)
    {
        if (fluidSample == null) throw new ArgumentNullException(nameof(fluidSample));
        if (porousSample == null) throw new ArgumentNullException(nameof(porousSample));
        ValidatePositive(contactAreaM2, nameof(contactAreaM2));
        ValidatePositive(flowPathLengthM, nameof(flowPathLengthM));
        ValidateNonNegative(deltaTimeSeconds, nameof(deltaTimeSeconds));
        ValidateNonNegative(appliedPressurePa, nameof(appliedPressurePa));

        FluidDefinition fluid = fluidSample.Profile;
        PorousMaterialProfile porous = porousSample.Profile;
        if (!string.IsNullOrEmpty(porousSample.AbsorbedFluidId) &&
            !string.Equals(porousSample.AbsorbedFluidId, fluid.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "A PorousMaterialSample tracks one fluid at a time. Use a separate sample or add a mixture model for multiple fluids.");
        }

        WettingAssessment wetting = AssessWetting(fluid.Id, porous.MaterialId);
        CapillaryResult capillary = CalculateCapillaryAction(
            fluid.Id, porous.MaterialId, fluidSample.TemperatureC);

        float viscosity = GetDynamicViscosityPaS(
            fluid.Id, fluidSample.TemperatureC);
        float density = GetTemperatureAdjustedDensity(
            fluid, fluidSample.TemperatureC);

        if (porous.PermeabilityM2 <= 0f || viscosity <= 0f || density <= 0f ||
            porous.Porosity <= 0f || deltaTimeSeconds <= 0f)
        {
            return new AbsorptionResult(
                0f, 0f, 0f, 0f, porousSample.Saturation,
                porousSample.CurrentBulkVolumeM3, false, false, wetting);
        }

        float poreVolume = porousSample.PoreVolumeM3;
        float maximumPoreVolume = poreVolume * porous.MaximumSaturation;
        float remainingPoreVolume = Mathf.Max(
            0f, maximumPoreVolume - porousSample.CurrentAbsorbedVolumeM3);

        float availableFluidVolume = fluidSample.VolumeM3;
        float netPressure = appliedPressurePa + capillary.CapillaryPressurePa;
        netPressure = Mathf.Max(0f, netPressure);

        // Tortuosity reduces effective flow through a winding pore network.
        float flowRate =
            porous.PermeabilityM2 * contactAreaM2 * netPressure /
            (viscosity * flowPathLengthM * Mathf.Max(1f, porous.Tortuosity));

        float theoreticalVolume = flowRate * deltaTimeSeconds;
        float absorbedVolume = Mathf.Min(
            theoreticalVolume,
            Mathf.Min(remainingPoreVolume, availableFluidVolume));
        absorbedVolume = Mathf.Max(0f, absorbedVolume);

        float absorbedMass = absorbedVolume * density;
        fluidSample.RemoveMass(absorbedMass);
        porousSample.AddAbsorbedFluid(absorbedMass, absorbedVolume, fluid.Id);

        bool capacityLimited = theoreticalVolume > 0f && theoreticalVolume >= remainingPoreVolume;
        bool supplyLimited = theoreticalVolume > 0f && theoreticalVolume >= availableFluidVolume;

        return new AbsorptionResult(
            flowRate,
            netPressure,
            absorbedVolume,
            absorbedMass,
            porousSample.Saturation,
            porousSample.CurrentBulkVolumeM3,
            capacityLimited,
            supplyLimited,
            wetting);
    }

    // ============================================================
    // FLUID RETENTION AND DRAINAGE
    // ============================================================

    /// <summary>
    /// Estimates the fluid amount held at or below the profile's retention
    /// saturation. Any additional absorbed fluid is classed as drainable.
    /// </summary>
    public RetentionResult CalculateRetention(
        PorousMaterialSample porousSample,
        string fluidId,
        float temperatureC = 20f)
    {
        if (porousSample == null)
            throw new ArgumentNullException(nameof(porousSample));

        ValidatePorousSampleFluid(porousSample, fluidId);
        float density = GetDensityKgPerM3(fluidId, temperatureC);
        if (density <= 0f)
            throw new InvalidOperationException($"Fluid '{fluidId}' has no valid density.");

        float retainedCapacityVolume =
            porousSample.PoreVolumeM3 * porousSample.Profile.RetentionSaturation;
        float retainedCapacityMass = retainedCapacityVolume * density;
        float retainedMass = Mathf.Min(
            porousSample.AbsorbedFluidMassKg,
            retainedCapacityMass);
        float drainableMass = Mathf.Max(
            0f,
            porousSample.AbsorbedFluidMassKg - retainedMass);

        float retentionFraction = porousSample.AbsorbedFluidMassKg > 0f
            ? retainedMass / porousSample.AbsorbedFluidMassKg
            : 1f;

        return new RetentionResult(
            retainedMass,
            drainableMass,
            retentionFraction,
            porousSample.Saturation);
    }

    /// <summary>
    /// Simulates gradual drainage of fluid above the profile's retention
    /// saturation. The drained mass is returned; it is not automatically
    /// placed into another FluidSample.
    /// </summary>
    public DrainageResult DrainExcessFluid(
        PorousMaterialSample porousSample,
        string fluidId,
        float deltaTimeSeconds,
        float temperatureC = 20f)
    {
        if (porousSample == null)
            throw new ArgumentNullException(nameof(porousSample));
        ValidateNonNegative(deltaTimeSeconds, nameof(deltaTimeSeconds));

        FluidDefinition fluid = GetFluid(fluidId);
        ValidatePorousSampleFluid(porousSample, fluidId);
        float density = GetDensityKgPerM3(fluidId, temperatureC);
        if (density <= 0f)
            throw new InvalidOperationException($"Fluid '{fluidId}' has no valid density.");

        RetentionResult retention = CalculateRetention(
            porousSample, fluidId, temperatureC);

        float rate = porousSample.Profile.DrainageRatePerSecond;
        float drainageFraction = rate <= 0f || deltaTimeSeconds <= 0f
            ? 0f
            : 1f - Mathf.Exp(-rate * deltaTimeSeconds);

        float drainedMass = retention.DrainableMassKg * drainageFraction;
        porousSample.RemoveAbsorbedFluid(drainedMass, density);

        return new DrainageResult(
            drainedMass,
            porousSample.AbsorbedFluidMassKg,
            porousSample.Saturation,
            porousSample.CurrentBulkVolumeM3);
    }

    // ============================================================
    // EVAPORATION
    // ============================================================

    /// <summary>
    /// Calculates saturation vapour pressure using the selected model.
    /// Antoine coefficients are interpreted as producing pressure in mmHg.
    /// The Clausius-Clapeyron option uses a reference vapour pressure and
    /// latent heat of vaporisation.
    /// </summary>
    public float CalculateSaturationVapourPressurePa(
        string fluidId,
        float temperatureC)
    {
        FluidDefinition fluid = GetFluid(fluidId);
        ValidateTemperature(temperatureC, nameof(temperatureC));

        if (fluid.VapourModel == VapourPressureModel.Antoine)
        {
            if (temperatureC < fluid.AntoineMinimumTemperatureC ||
                temperatureC > fluid.AntoineMaximumTemperatureC ||
                Mathf.Abs(fluid.AntoineC + temperatureC) < MinimumPositiveValue)
            {
                return -1f;
            }

            float log10PressureMmHg =
                fluid.AntoineA - fluid.AntoineB / (fluid.AntoineC + temperatureC);
            float pressurePa = Mathf.Pow(10f, log10PressureMmHg) * MmHgToPascal;
            return IsFinite(pressurePa) ? Mathf.Max(0f, pressurePa) : -1f;
        }

        if (fluid.VapourModel == VapourPressureModel.ClausiusClapeyron)
        {
            float referencePressure = fluid.ReferenceVapourPressurePa;
            float latentHeat = fluid.LatentHeatOfVaporisationJPerKg;
            float molarMass = fluid.MolarMassKgPerMol;
            float currentK = temperatureC + CelsiusToKelvin;
            float referenceK = fluid.VapourPressureReferenceTemperatureC + CelsiusToKelvin;

            if (referencePressure <= 0f || latentHeat <= 0f ||
                molarMass <= 0f || currentK <= 0f || referenceK <= 0f)
                return -1f;

            float latentHeatPerMol = latentHeat * molarMass;
            float exponent = -latentHeatPerMol / UniversalGasConstantJPerMolK *
                (1f / currentK - 1f / referenceK);
            float pressure = referencePressure * Mathf.Exp(exponent);
            return IsFinite(pressure) ? Mathf.Max(0f, pressure) : -1f;
        }

        return -1f;
    }

    /// <summary>
    /// Evaporates exposed free fluid. Relative humidity is a fraction from
    /// 0 to 1. The mass-transfer coefficient is an empirical parameter;
    /// without it, the method returns zero evaporation rather than inventing
    /// a rate.
    /// </summary>
    public EvaporationResult EvaporateFreeFluid(
        FluidSample sample,
        float exposedAreaM2,
        float ambientTemperatureC,
        float relativeHumidity,
        float deltaTimeSeconds,
        bool applyEstimatedEvaporativeCooling = false)
    {
        if (sample == null)
            throw new ArgumentNullException(nameof(sample));
        ValidateNonNegative(exposedAreaM2, nameof(exposedAreaM2));
        ValidateTemperature(ambientTemperatureC, nameof(ambientTemperatureC));
        ValidateUnitInterval(relativeHumidity, nameof(relativeHumidity));
        ValidateNonNegative(deltaTimeSeconds, nameof(deltaTimeSeconds));

        FluidDefinition fluid = sample.Profile;
        float surfacePressure = CalculateSaturationVapourPressurePa(
            fluid.Id, sample.TemperatureC);
        float ambientSaturationPressure = CalculateSaturationVapourPressurePa(
            fluid.Id, ambientTemperatureC);

        if (surfacePressure < 0f || ambientSaturationPressure < 0f ||
            fluid.EvaporationMassTransferCoefficient <= 0f ||
            fluid.LatentHeatOfVaporisationJPerKg <= 0f ||
            exposedAreaM2 <= 0f || deltaTimeSeconds <= 0f)
        {
            return new EvaporationResult(
                Mathf.Max(0f, surfacePressure),
                Mathf.Max(0f, ambientSaturationPressure) * relativeHumidity,
                0f, 0f, sample.MassKg, 0f);
        }

        float ambientVapourPressure = ambientSaturationPressure * relativeHumidity;
        float pressureDifference = Mathf.Max(0f, surfacePressure - ambientVapourPressure);

        float evaporationRateKgPerSecond =
            fluid.EvaporationMassTransferCoefficient * exposedAreaM2 * pressureDifference;
        float requestedMass = evaporationRateKgPerSecond * deltaTimeSeconds;
        float evaporatedMass = sample.RemoveMass(requestedMass);
        float latentEnergy = evaporatedMass * fluid.LatentHeatOfVaporisationJPerKg;

        float estimatedTemperatureChange = 0f;
        if (applyEstimatedEvaporativeCooling &&
            fluid.SpecificHeatJPerKgK > 0f && sample.MassKg > 0f)
        {
            estimatedTemperatureChange =
                latentEnergy / (sample.MassKg * fluid.SpecificHeatJPerKgK);
            sample.SetTemperature(Mathf.Max(-273.15f,
                sample.TemperatureC - estimatedTemperatureChange));
        }

        return new EvaporationResult(
            surfacePressure,
            ambientVapourPressure,
            evaporatedMass,
            latentEnergy,
            sample.MassKg,
            estimatedTemperatureChange);
    }

    /// <summary>
    /// Evaporates absorbed fluid from the exposed surface of a porous sample.
    /// The exposure factor is a 0..1 approximation of how much pore liquid is
    /// connected to the exposed surface. It is not a detailed diffusion model.
    /// </summary>
    public EvaporationResult EvaporateFromPorousMaterial(
        PorousMaterialSample porousSample,
        string fluidId,
        float exposedAreaM2,
        float ambientTemperatureC,
        float relativeHumidity,
        float deltaTimeSeconds,
        float surfaceExposureFactor = 0.1f)
    {
        if (porousSample == null)
            throw new ArgumentNullException(nameof(porousSample));
        ValidateNonNegative(exposedAreaM2, nameof(exposedAreaM2));
        ValidateTemperature(ambientTemperatureC, nameof(ambientTemperatureC));
        ValidateUnitInterval(relativeHumidity, nameof(relativeHumidity));
        ValidateNonNegative(deltaTimeSeconds, nameof(deltaTimeSeconds));
        ValidateUnitInterval(surfaceExposureFactor, nameof(surfaceExposureFactor));

        FluidDefinition fluid = GetFluid(fluidId);
        ValidatePorousSampleFluid(porousSample, fluidId);
        float surfaceVapourPressure = CalculateSaturationVapourPressurePa(
            fluidId, porousSample.TemperatureC);
        float ambientSaturationPressure = CalculateSaturationVapourPressurePa(
            fluidId, ambientTemperatureC);

        if (surfaceVapourPressure <= 0f || ambientSaturationPressure < 0f ||
            fluid.EvaporationMassTransferCoefficient <= 0f ||
            fluid.LatentHeatOfVaporisationJPerKg <= 0f ||
            exposedAreaM2 <= 0f || deltaTimeSeconds <= 0f ||
            surfaceExposureFactor <= 0f)
        {
            return new EvaporationResult(
                Mathf.Max(0f, surfaceVapourPressure),
                Mathf.Max(0f, ambientSaturationPressure) * relativeHumidity,
                0f, 0f, porousSample.AbsorbedFluidMassKg, 0f);
        }

        float ambientVapourPressure = ambientSaturationPressure * relativeHumidity;
        float pressureDifference = Mathf.Max(
            0f, surfaceVapourPressure - ambientVapourPressure);

        float rate = fluid.EvaporationMassTransferCoefficient *
            exposedAreaM2 * surfaceExposureFactor * pressureDifference;
        float requestedMass = rate * deltaTimeSeconds;
        float evaporatedMass = porousSample.RemoveAbsorbedFluid(
            requestedMass,
            GetDensityKgPerM3(fluidId, porousSample.TemperatureC));
        float latentEnergy = evaporatedMass * fluid.LatentHeatOfVaporisationJPerKg;

        return new EvaporationResult(
            surfaceVapourPressure,
            ambientVapourPressure,
            evaporatedMass,
            latentEnergy,
            porousSample.AbsorbedFluidMassKg,
            0f);
    }

    // ============================================================
    // POROSITY / CAPACITY QUERIES
    // ============================================================

    public float CalculatePoreVolumeM3(
        string materialId,
        float bulkVolumeM3)
    {
        ValidatePositive(bulkVolumeM3, nameof(bulkVolumeM3));
        return bulkVolumeM3 * GetPorousProfile(materialId).Porosity;
    }

    public float CalculateMaximumAbsorptionMassKg(
        string materialId,
        float bulkVolumeM3,
        string fluidId,
        float temperatureC = 20f)
    {
        ValidatePositive(bulkVolumeM3, nameof(bulkVolumeM3));
        PorousMaterialProfile porous = GetPorousProfile(materialId);
        float density = GetDensityKgPerM3(fluidId, temperatureC);
        if (density <= 0f)
            throw new InvalidOperationException($"Fluid '{fluidId}' has no valid density.");

        return bulkVolumeM3 * porous.Porosity * porous.MaximumSaturation * density;
    }

    // ============================================================
    // ASSET VALIDATION
    // ============================================================

    [ContextMenu("Validate Fluid and Porous Profiles")]
    public void ValidateProfiles()
    {
        EnsureLists();
        int errors = 0;
        int warnings = 0;

        HashSet<string> fluidIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (FluidDefinition fluid in fluids)
        {
            if (fluid == null)
            {
                Debug.LogError("Fuilds: null fluid definition found.", this);
                errors++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(fluid.Id) || !fluidIds.Add(fluid.Id))
            {
                Debug.LogError($"Fuilds: empty or duplicate fluid ID '{fluid.Id}'.", this);
                errors++;
            }

            if (fluid.DensityKgPerM3 <= 0f)
            {
                Debug.LogWarning($"Fuilds: '{fluid.DisplayName}' has no valid density.", this);
                warnings++;
            }
            if (fluid.DynamicViscosityPaS <= 0f)
            {
                Debug.LogWarning($"Fuilds: '{fluid.DisplayName}' has no valid viscosity; Darcy absorption will be disabled.", this);
                warnings++;
            }
            if (fluid.VapourModel == VapourPressureModel.None)
            {
                Debug.LogWarning($"Fuilds: '{fluid.DisplayName}' has no vapour-pressure model; evaporation calculations will be disabled.", this);
                warnings++;
            }
        }

        HashSet<string> materialIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (PorousMaterialProfile porous in porousMaterials)
        {
            if (porous == null)
            {
                Debug.LogError("Fuilds: null porous-material profile found.", this);
                errors++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(porous.MaterialId) || !materialIds.Add(porous.MaterialId))
            {
                Debug.LogError($"Fuilds: empty or duplicate porous material ID '{porous.MaterialId}'.", this);
                errors++;
            }

            if (porous.PermeabilityM2 <= 0f)
            {
                Debug.LogWarning($"Fuilds: '{porous.DisplayName}' has no permeability value; absorption flow cannot be calculated.", this);
                warnings++;
            }
            if (porous.MeanPoreRadiusM <= 0f)
            {
                Debug.LogWarning($"Fuilds: '{porous.DisplayName}' has no pore-radius value; capillary pressure cannot be calculated.", this);
                warnings++;
            }
        }

        HashSet<string> wettingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (WettingProfile wetting in wettingProfiles)
        {
            if (wetting == null)
            {
                Debug.LogError("Fuilds: null wetting profile found.", this);
                errors++;
                continue;
            }

            string key = wetting.FluidId + "|" + wetting.MaterialId;
            if (!wettingKeys.Add(key))
            {
                Debug.LogError($"Fuilds: duplicate wetting pair '{key}'.", this);
                errors++;
            }
            if (!HasFluid(wetting.FluidId))
            {
                Debug.LogError($"Fuilds: wetting profile references unknown fluid '{wetting.FluidId}'.", this);
                errors++;
            }
            if (string.IsNullOrWhiteSpace(wetting.MaterialId))
            {
                Debug.LogError("Fuilds: wetting profile has an empty material ID.", this);
                errors++;
            }
        }

        Debug.Log(
            $"Fuilds validation completed. Errors: {errors}. Warnings: {warnings}.",
            this);
    }

    // ============================================================
    // INTERNAL HELPERS
    // ============================================================

    private static void ValidatePorousSampleFluid(
        PorousMaterialSample sample,
        string fluidId)
    {
        if (sample == null)
            throw new ArgumentNullException(nameof(sample));

        if (!string.IsNullOrEmpty(sample.AbsorbedFluidId) &&
            !string.Equals(sample.AbsorbedFluidId, fluidId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Porous sample contains '{sample.AbsorbedFluidId}', not '{fluidId}'. This sample tracks one fluid at a time.");
        }
    }

    private void EnsureLists()
    {
        if (fluids == null) fluids = new List<FluidDefinition>();
        if (porousMaterials == null) porousMaterials = new List<PorousMaterialProfile>();
        if (wettingProfiles == null) wettingProfiles = new List<WettingProfile>();
    }

    private void MarkAssetDirty()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private static void ValidatePositive(float value, string parameterName)
    {
        if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
            throw new ArgumentOutOfRangeException(
                parameterName, "Value must be finite and greater than zero.");
    }

    private static void ValidateNonNegative(float value, string parameterName)
    {
        if (value < 0f || float.IsNaN(value) || float.IsInfinity(value))
            throw new ArgumentOutOfRangeException(
                parameterName, "Value must be finite and non-negative.");
    }

    private static void ValidateUnitInterval(float value, string parameterName)
    {
        if (value < 0f || value > 1f || float.IsNaN(value) || float.IsInfinity(value))
            throw new ArgumentOutOfRangeException(
                parameterName, "Value must be between 0 and 1.");
    }

    private static void ValidateTemperature(float temperatureC, string parameterName)
    {
        if (temperatureC < -273.15f || float.IsNaN(temperatureC) || float.IsInfinity(temperatureC))
            throw new ArgumentOutOfRangeException(
                parameterName, "Temperature must be finite and cannot be below absolute zero.");
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
}
