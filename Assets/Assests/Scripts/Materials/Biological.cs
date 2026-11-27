using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParticleChemistry
{

/// <summary>
/// Biological is a ScriptableObject database for biological growth,
/// decomposition, fungal decay, nutrient availability and surface colonisation.
///
/// The asset stores reusable biological and substrate definitions.
/// BiologicalEnvironment, OrganismPopulation and OrganicSubstrateSample are
/// runtime objects representing one simulated location or living population.
///
/// This is a configurable ecological / engineering-scale model. It is not a
/// species-specific ecosystem model and does not simulate individual cells,
/// hyphae, spores, molecular pathways or full soil food webs.
/// </summary>
[CreateAssetMenu(fileName = "Biological", menuName = "Particle Physics/Biological")]
public class Biological : ScriptableObject
{
    private const float MinimumPositiveValue = 0.000001f;
    private const float CarbonDioxideToCarbonMassRatio = 12f / 44f;
    private const float CarbonToCarbonDioxideMassRatio = 44f / 12f;

    // ============================================================
    // ENUMERATIONS
    // ============================================================

    public enum OrganismType
    {
        Plant,
        Fungus,
        Moss,
        Algae,
        Bacteria,
        Lichen,
        MicrobialCommunity,
        Other
    }

    public enum MetabolicMode
    {
        Photoautotroph,
        Chemoautotroph,
        Saprotroph,
        Mixotroph,
        Other
    }

    public enum CarbonSource
    {
        AtmosphericCarbonDioxide,
        OrganicCarbon
    }

    public enum NutrientType
    {
        None,
        CarbonDioxide,
        OrganicCarbon,
        Nitrogen,
        Phosphorus,
        Potassium
    }

    public enum DecompositionComponent
    {
        LabileOrganicMatter,
        Cellulose,
        Hemicellulose,
        Lignin,
        Protein,
        OtherOrganicMatter
    }

    // ============================================================
    // NUTRIENT POOL
    // ============================================================

    /// <summary>
    /// Available nutrient quantities for one simulated environment.
    /// All masses are kilograms. Carbon dioxide is stored as CO2 mass;
    /// organic carbon is stored as elemental-carbon mass.
    /// Nitrogen, phosphorus and potassium are stored as elemental nutrient mass.
    /// </summary>
    [Serializable]
    public class NutrientPool
    {
        [Header("Available Carbon")]
        [Min(0f)] [SerializeField] private float availableCarbonDioxideKg;
        [Min(0f)] [SerializeField] private float availableOrganicCarbonKg;

        [Header("Available Mineral Nutrients")]
        [Min(0f)] [SerializeField] private float availableNitrogenKg;
        [Min(0f)] [SerializeField] private float availablePhosphorusKg;
        [Min(0f)] [SerializeField] private float availablePotassiumKg;

        public float AvailableCarbonDioxideKg => availableCarbonDioxideKg;
        public float AvailableOrganicCarbonKg => availableOrganicCarbonKg;
        public float AvailableNitrogenKg => availableNitrogenKg;
        public float AvailablePhosphorusKg => availablePhosphorusKg;
        public float AvailablePotassiumKg => availablePotassiumKg;

        public NutrientPool(
            float carbonDioxideKg = 0f,
            float organicCarbonKg = 0f,
            float nitrogenKg = 0f,
            float phosphorusKg = 0f,
            float potassiumKg = 0f)
        {
            ValidateNonNegative(carbonDioxideKg, nameof(carbonDioxideKg));
            ValidateNonNegative(organicCarbonKg, nameof(organicCarbonKg));
            ValidateNonNegative(nitrogenKg, nameof(nitrogenKg));
            ValidateNonNegative(phosphorusKg, nameof(phosphorusKg));
            ValidateNonNegative(potassiumKg, nameof(potassiumKg));

            availableCarbonDioxideKg = carbonDioxideKg;
            availableOrganicCarbonKg = organicCarbonKg;
            availableNitrogenKg = nitrogenKg;
            availablePhosphorusKg = phosphorusKg;
            availablePotassiumKg = potassiumKg;
        }

        public void Add(
            float carbonDioxideKg = 0f,
            float organicCarbonKg = 0f,
            float nitrogenKg = 0f,
            float phosphorusKg = 0f,
            float potassiumKg = 0f)
        {
            ValidateNonNegative(carbonDioxideKg, nameof(carbonDioxideKg));
            ValidateNonNegative(organicCarbonKg, nameof(organicCarbonKg));
            ValidateNonNegative(nitrogenKg, nameof(nitrogenKg));
            ValidateNonNegative(phosphorusKg, nameof(phosphorusKg));
            ValidateNonNegative(potassiumKg, nameof(potassiumKg));

            availableCarbonDioxideKg += carbonDioxideKg;
            availableOrganicCarbonKg += organicCarbonKg;
            availableNitrogenKg += nitrogenKg;
            availablePhosphorusKg += phosphorusKg;
            availablePotassiumKg += potassiumKg;
        }

        internal float RemoveCarbonDioxide(float requestedKg)
        {
            return Remove(ref availableCarbonDioxideKg, requestedKg);
        }

        internal float RemoveOrganicCarbon(float requestedKg)
        {
            return Remove(ref availableOrganicCarbonKg, requestedKg);
        }

        internal float RemoveNitrogen(float requestedKg)
        {
            return Remove(ref availableNitrogenKg, requestedKg);
        }

        internal float RemovePhosphorus(float requestedKg)
        {
            return Remove(ref availablePhosphorusKg, requestedKg);
        }

        internal float RemovePotassium(float requestedKg)
        {
            return Remove(ref availablePotassiumKg, requestedKg);
        }

        private static float Remove(ref float available, float requested)
        {
            float removed = Mathf.Clamp(requested, 0f, available);
            available = Mathf.Max(0f, available - removed);
            return removed;
        }
    }

    // ============================================================
    // BIOLOGICAL ENVIRONMENT
    // ============================================================

    /// <summary>
    /// Environmental conditions affecting biological activity.
    /// Moisture is a 0..1 fraction of the selected model's reference moisture
    /// or saturation range. OxygenFraction is a gas-volume fraction, so 0.21
    /// represents approximately 21% oxygen. Light is PPFD in µmol/(m²·s).
    /// </summary>
    [Serializable]
    public class BiologicalEnvironment
    {
        [Header("Climate")]
        [SerializeField] private float temperatureC = 20f;
        [Range(0f, 1f)] [SerializeField] private float moistureFraction = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float relativeHumidity = 0.6f;
        [Range(0f, 1f)] [SerializeField] private float oxygenFraction = 0.21f;

        [Header("Chemistry")]
        [Range(0f, 14f)] [SerializeField] private float pH = 7f;
        [Min(0f)] [SerializeField] private float carbonDioxidePpm = 420f;

        [Header("Light")]
        [Min(0f)] [SerializeField] private float photosyntheticPhotonFluxDensity = 200f;

        [Header("Nutrients")]
        [SerializeField] private NutrientPool nutrients = new NutrientPool();

        public float TemperatureC => temperatureC;
        public float MoistureFraction => moistureFraction;
        public float RelativeHumidity => relativeHumidity;
        public float OxygenFraction => oxygenFraction;
        public float PH => pH;
        public float CarbonDioxidePpm => carbonDioxidePpm;
        public float PhotosyntheticPhotonFluxDensity => photosyntheticPhotonFluxDensity;
        public NutrientPool Nutrients => nutrients;

        public BiologicalEnvironment(
            float temperatureC = 20f,
            float moistureFraction = 0.5f,
            float relativeHumidity = 0.6f,
            float oxygenFraction = 0.21f,
            float pH = 7f,
            float carbonDioxidePpm = 420f,
            float photosyntheticPhotonFluxDensity = 200f,
            NutrientPool nutrients = null)
        {
            ValidateTemperature(temperatureC, nameof(temperatureC));
            ValidateUnitInterval(moistureFraction, nameof(moistureFraction));
            ValidateUnitInterval(relativeHumidity, nameof(relativeHumidity));
            ValidateUnitInterval(oxygenFraction, nameof(oxygenFraction));
            ValidateRange(pH, 0f, 14f, nameof(pH));
            ValidateNonNegative(carbonDioxidePpm, nameof(carbonDioxidePpm));
            ValidateNonNegative(photosyntheticPhotonFluxDensity, nameof(photosyntheticPhotonFluxDensity));

            this.temperatureC = temperatureC;
            this.moistureFraction = moistureFraction;
            this.relativeHumidity = relativeHumidity;
            this.oxygenFraction = oxygenFraction;
            this.pH = pH;
            this.carbonDioxidePpm = carbonDioxidePpm;
            this.photosyntheticPhotonFluxDensity = photosyntheticPhotonFluxDensity;
            this.nutrients = nutrients ?? new NutrientPool();
        }

        public void SetConditions(
            float temperatureC,
            float moistureFraction,
            float relativeHumidity,
            float oxygenFraction,
            float pH,
            float carbonDioxidePpm,
            float photosyntheticPhotonFluxDensity)
        {
            ValidateTemperature(temperatureC, nameof(temperatureC));
            ValidateUnitInterval(moistureFraction, nameof(moistureFraction));
            ValidateUnitInterval(relativeHumidity, nameof(relativeHumidity));
            ValidateUnitInterval(oxygenFraction, nameof(oxygenFraction));
            ValidateRange(pH, 0f, 14f, nameof(pH));
            ValidateNonNegative(carbonDioxidePpm, nameof(carbonDioxidePpm));
            ValidateNonNegative(photosyntheticPhotonFluxDensity, nameof(photosyntheticPhotonFluxDensity));

            this.temperatureC = temperatureC;
            this.moistureFraction = moistureFraction;
            this.relativeHumidity = relativeHumidity;
            this.oxygenFraction = oxygenFraction;
            this.pH = pH;
            this.carbonDioxidePpm = carbonDioxidePpm;
            this.photosyntheticPhotonFluxDensity = photosyntheticPhotonFluxDensity;
        }
    }

    // ============================================================
    // ORGANISM DEFINITION
    // ============================================================

    /// <summary>
    /// Reusable growth and environmental-response parameters for an organism
    /// group. Values are representative starting parameters, not universal
    /// biological constants for every species in that group.
    /// </summary>
    [Serializable]
    public class OrganismDefinition
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private OrganismType organismType;
        [SerializeField] private MetabolicMode metabolicMode;
        [SerializeField] private CarbonSource carbonSource;

        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Growth")]
        [Tooltip("Maximum specific growth rate in 1/day under ideal model conditions.")]
        [Min(0f)] [SerializeField] private float maximumSpecificGrowthRatePerDay = 0.05f;
        [Tooltip("Maximum dry biomass supported per square metre of occupied habitat.")]
        [Min(0f)] [SerializeField] private float maximumBiomassKgPerM2 = 1f;

        [Header("Temperature Response (°C)")]
        [SerializeField] private float minimumTemperatureC = 0f;
        [SerializeField] private float optimumTemperatureC = 25f;
        [SerializeField] private float maximumTemperatureC = 45f;

        [Header("Moisture Response (0..1)")]
        [Range(0f, 1f)] [SerializeField] private float minimumMoisture = 0.1f;
        [Range(0f, 1f)] [SerializeField] private float optimumMoisture = 0.6f;
        [Range(0f, 1f)] [SerializeField] private float maximumMoisture = 1f;

        [Header("pH Response")]
        [Range(0f, 14f)] [SerializeField] private float minimumPH = 4f;
        [Range(0f, 14f)] [SerializeField] private float optimumPH = 6.5f;
        [Range(0f, 14f)] [SerializeField] private float maximumPH = 9f;

        [Header("Oxygen Response (gas-volume fraction)")]
        [Range(0f, 1f)] [SerializeField] private float minimumOxygenFraction = 0.01f;
        [Range(0f, 1f)] [SerializeField] private float optimumOxygenFraction = 0.21f;
        [Range(0f, 1f)] [SerializeField] private float maximumOxygenFraction = 1f;

        [Header("Photosynthesis")]
        [Tooltip("PPFD at which the light-response factor reaches 0.5. Used by photoautotrophs.")]
        [Min(0f)] [SerializeField] private float lightHalfSaturationPPFD = 150f;
        [Tooltip("CO2 concentration in ppm at which the CO2-response factor reaches 0.5.")]
        [Min(0f)] [SerializeField] private float carbonDioxideHalfSaturationPpm = 300f;

        [Header("Biomass Nutrient Requirements")]
        [Tooltip("Elemental carbon mass fraction in new dry biomass.")]
        [Range(0f, 1f)] [SerializeField] private float carbonMassFraction = 0.45f;
        [Tooltip("Elemental nitrogen mass fraction in new dry biomass.")]
        [Range(0f, 1f)] [SerializeField] private float nitrogenMassFraction = 0.02f;
        [Tooltip("Elemental phosphorus mass fraction in new dry biomass.")]
        [Range(0f, 1f)] [SerializeField] private float phosphorusMassFraction = 0.002f;
        [Tooltip("Elemental potassium mass fraction in new dry biomass.")]
        [Range(0f, 1f)] [SerializeField] private float potassiumMassFraction = 0.01f;

        [Header("Surface Colonisation")]
        [Min(0f)] [SerializeField] private float surfaceColonisationRatePerDay = 0.1f;
        [Range(0f, 1f)] [SerializeField] private float maximumSurfaceCoverage = 1f;

        [Header("Fungal Decomposition")]
        [Tooltip("Relative enhancement of substrate decay at high fungal biomass. 1 means no enhancement.")]
        [Min(1f)] [SerializeField] private float decompositionEnhancement = 3f;

        [TextArea(1, 3)]
        [SerializeField] private string notes;

        public string Id => id;
        public string DisplayName => displayName;
        public OrganismType Type => organismType;
        public MetabolicMode Metabolism => metabolicMode;
        public CarbonSource Carbon => carbonSource;
        public string Description => description;
        public float MaximumSpecificGrowthRatePerDay => maximumSpecificGrowthRatePerDay;
        public float MaximumBiomassKgPerM2 => maximumBiomassKgPerM2;
        public float MinimumTemperatureC => minimumTemperatureC;
        public float OptimumTemperatureC => optimumTemperatureC;
        public float MaximumTemperatureC => maximumTemperatureC;
        public float MinimumMoisture => minimumMoisture;
        public float OptimumMoisture => optimumMoisture;
        public float MaximumMoisture => maximumMoisture;
        public float MinimumPH => minimumPH;
        public float OptimumPH => optimumPH;
        public float MaximumPH => maximumPH;
        public float MinimumOxygenFraction => minimumOxygenFraction;
        public float OptimumOxygenFraction => optimumOxygenFraction;
        public float MaximumOxygenFraction => maximumOxygenFraction;
        public float LightHalfSaturationPPFD => lightHalfSaturationPPFD;
        public float CarbonDioxideHalfSaturationPpm => carbonDioxideHalfSaturationPpm;
        public float CarbonMassFraction => carbonMassFraction;
        public float NitrogenMassFraction => nitrogenMassFraction;
        public float PhosphorusMassFraction => phosphorusMassFraction;
        public float PotassiumMassFraction => potassiumMassFraction;
        public float SurfaceColonisationRatePerDay => surfaceColonisationRatePerDay;
        public float MaximumSurfaceCoverage => maximumSurfaceCoverage;
        public float DecompositionEnhancement => decompositionEnhancement;
        public string Notes => notes;

        public OrganismDefinition(
            string id,
            string displayName,
            OrganismType organismType,
            MetabolicMode metabolicMode,
            CarbonSource carbonSource,
            float maximumSpecificGrowthRatePerDay,
            float maximumBiomassKgPerM2,
            float minimumTemperatureC,
            float optimumTemperatureC,
            float maximumTemperatureC,
            float minimumMoisture,
            float optimumMoisture,
            float maximumMoisture,
            float minimumPH,
            float optimumPH,
            float maximumPH,
            float minimumOxygenFraction,
            float optimumOxygenFraction,
            float maximumOxygenFraction,
            float lightHalfSaturationPPFD,
            float carbonDioxideHalfSaturationPpm,
            float carbonMassFraction,
            float nitrogenMassFraction,
            float phosphorusMassFraction,
            float potassiumMassFraction,
            float surfaceColonisationRatePerDay,
            float maximumSurfaceCoverage,
            float decompositionEnhancement = 1f,
            string description = "",
            string notes = "")
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Organism definition requires an ID.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Organism definition requires a display name.", nameof(displayName));
            ValidateNonNegative(maximumSpecificGrowthRatePerDay, nameof(maximumSpecificGrowthRatePerDay));
            ValidateNonNegative(maximumBiomassKgPerM2, nameof(maximumBiomassKgPerM2));
            ValidateResponseRange(minimumTemperatureC, optimumTemperatureC, maximumTemperatureC, nameof(minimumTemperatureC));
            ValidateResponseRange(minimumMoisture, optimumMoisture, maximumMoisture, nameof(minimumMoisture));
            ValidateResponseRange(minimumPH, optimumPH, maximumPH, nameof(minimumPH));
            ValidateResponseRange(minimumOxygenFraction, optimumOxygenFraction, maximumOxygenFraction, nameof(minimumOxygenFraction));
            ValidateUnitInterval(carbonMassFraction, nameof(carbonMassFraction));
            ValidateUnitInterval(nitrogenMassFraction, nameof(nitrogenMassFraction));
            ValidateUnitInterval(phosphorusMassFraction, nameof(phosphorusMassFraction));
            ValidateUnitInterval(potassiumMassFraction, nameof(potassiumMassFraction));
            ValidateNonNegative(lightHalfSaturationPPFD, nameof(lightHalfSaturationPPFD));
            ValidateNonNegative(carbonDioxideHalfSaturationPpm, nameof(carbonDioxideHalfSaturationPpm));
            ValidateNonNegative(surfaceColonisationRatePerDay, nameof(surfaceColonisationRatePerDay));
            ValidateUnitInterval(maximumSurfaceCoverage, nameof(maximumSurfaceCoverage));
            if (decompositionEnhancement < 1f || !IsFinite(decompositionEnhancement))
                throw new ArgumentOutOfRangeException(nameof(decompositionEnhancement));

            this.id = id;
            this.displayName = displayName;
            this.organismType = organismType;
            this.metabolicMode = metabolicMode;
            this.carbonSource = carbonSource;
            this.maximumSpecificGrowthRatePerDay = maximumSpecificGrowthRatePerDay;
            this.maximumBiomassKgPerM2 = maximumBiomassKgPerM2;
            this.minimumTemperatureC = minimumTemperatureC;
            this.optimumTemperatureC = optimumTemperatureC;
            this.maximumTemperatureC = maximumTemperatureC;
            this.minimumMoisture = minimumMoisture;
            this.optimumMoisture = optimumMoisture;
            this.maximumMoisture = maximumMoisture;
            this.minimumPH = minimumPH;
            this.optimumPH = optimumPH;
            this.maximumPH = maximumPH;
            this.minimumOxygenFraction = minimumOxygenFraction;
            this.optimumOxygenFraction = optimumOxygenFraction;
            this.maximumOxygenFraction = maximumOxygenFraction;
            this.lightHalfSaturationPPFD = lightHalfSaturationPPFD;
            this.carbonDioxideHalfSaturationPpm = carbonDioxideHalfSaturationPpm;
            this.carbonMassFraction = carbonMassFraction;
            this.nitrogenMassFraction = nitrogenMassFraction;
            this.phosphorusMassFraction = phosphorusMassFraction;
            this.potassiumMassFraction = potassiumMassFraction;
            this.surfaceColonisationRatePerDay = surfaceColonisationRatePerDay;
            this.maximumSurfaceCoverage = maximumSurfaceCoverage;
            this.decompositionEnhancement = decompositionEnhancement;
            this.description = description;
            this.notes = notes;
        }
    }

    // ============================================================
    // DECOMPOSITION / ORGANIC SUBSTRATE DEFINITION
    // ============================================================

    /// <summary>
    /// Dry-basis composition and decay parameters for an organic substrate.
    /// Component fractions refer to total initial dry mass and should sum to
    /// approximately 1. Inert matter is retained by this decomposition model.
    /// </summary>
    [Serializable]
    public class DecompositionProfile
    {
        [Header("Material Link")]
        [SerializeField] private string materialId;
        [SerializeField] private string displayName;
        [SerializeField] private float referenceBulkDensityKgPerM3 = -1f;
        [Range(-1f, 1f)] [SerializeField] private float porosity = -1f;

        [Header("Dry-Mass Composition (fractions of total dry mass)")]
        [Range(0f, 1f)] [SerializeField] private float labileFraction;
        [Range(0f, 1f)] [SerializeField] private float celluloseFraction;
        [Range(0f, 1f)] [SerializeField] private float hemicelluloseFraction;
        [Range(0f, 1f)] [SerializeField] private float ligninFraction;
        [Range(0f, 1f)] [SerializeField] private float proteinFraction;
        [Range(0f, 1f)] [SerializeField] private float otherOrganicFraction;
        [Range(0f, 1f)] [SerializeField] private float inertFraction;

        [Header("First-Order Decay Constants (1/day)")]
        [Min(0f)] [SerializeField] private float labileDecayRatePerDay = 0.02f;
        [Min(0f)] [SerializeField] private float celluloseDecayRatePerDay = 0.001f;
        [Min(0f)] [SerializeField] private float hemicelluloseDecayRatePerDay = 0.003f;
        [Min(0f)] [SerializeField] private float ligninDecayRatePerDay = 0.0001f;
        [Min(0f)] [SerializeField] private float proteinDecayRatePerDay = 0.005f;
        [Min(0f)] [SerializeField] private float otherOrganicDecayRatePerDay = 0.001f;

        [Header("Elemental Content")]
        [Range(0f, 1f)] [SerializeField] private float carbonMassFraction = 0.45f;
        [Range(0f, 1f)] [SerializeField] private float nitrogenMassFraction = 0.01f;
        [Range(0f, 1f)] [SerializeField] private float phosphorusMassFraction = 0.001f;
        [Range(0f, 1f)] [SerializeField] private float nitrogenMineralisationFraction = 0.6f;
        [Range(0f, 1f)] [SerializeField] private float phosphorusMineralisationFraction = 0.6f;
        [Range(0f, 1f)] [SerializeField] private float respiredCarbonFraction = 0.65f;
        [Range(0f, 1f)] [SerializeField] private float humificationFraction = 0.15f;

        [Header("Environmental Response")]
        [SerializeField] private float minimumTemperatureC = 0f;
        [SerializeField] private float optimumTemperatureC = 25f;
        [SerializeField] private float maximumTemperatureC = 50f;
        [Range(0f, 1f)] [SerializeField] private float minimumMoisture = 0.05f;
        [Range(0f, 1f)] [SerializeField] private float optimumMoisture = 0.6f;
        [Range(0f, 1f)] [SerializeField] private float maximumMoisture = 1f;
        [Range(0f, 14f)] [SerializeField] private float minimumPH = 3f;
        [Range(0f, 14f)] [SerializeField] private float optimumPH = 6.5f;
        [Range(0f, 14f)] [SerializeField] private float maximumPH = 10f;
        [SerializeField] private bool requiresOxygen = true;

        [TextArea(2, 4)]
        [SerializeField] private string notes;

        public string MaterialId => materialId;
        public string DisplayName => displayName;
        public float ReferenceBulkDensityKgPerM3 => referenceBulkDensityKgPerM3;
        public float Porosity => porosity;
        public float LabileFraction => labileFraction;
        public float CelluloseFraction => celluloseFraction;
        public float HemicelluloseFraction => hemicelluloseFraction;
        public float LigninFraction => ligninFraction;
        public float ProteinFraction => proteinFraction;
        public float OtherOrganicFraction => otherOrganicFraction;
        public float InertFraction => inertFraction;
        public float LabileDecayRatePerDay => labileDecayRatePerDay;
        public float CelluloseDecayRatePerDay => celluloseDecayRatePerDay;
        public float HemicelluloseDecayRatePerDay => hemicelluloseDecayRatePerDay;
        public float LigninDecayRatePerDay => ligninDecayRatePerDay;
        public float ProteinDecayRatePerDay => proteinDecayRatePerDay;
        public float OtherOrganicDecayRatePerDay => otherOrganicDecayRatePerDay;
        public float CarbonMassFraction => carbonMassFraction;
        public float NitrogenMassFraction => nitrogenMassFraction;
        public float PhosphorusMassFraction => phosphorusMassFraction;
        public float NitrogenMineralisationFraction => nitrogenMineralisationFraction;
        public float PhosphorusMineralisationFraction => phosphorusMineralisationFraction;
        public float RespiredCarbonFraction => respiredCarbonFraction;
        public float HumificationFraction => humificationFraction;
        public float MinimumTemperatureC => minimumTemperatureC;
        public float OptimumTemperatureC => optimumTemperatureC;
        public float MaximumTemperatureC => maximumTemperatureC;
        public float MinimumMoisture => minimumMoisture;
        public float OptimumMoisture => optimumMoisture;
        public float MaximumMoisture => maximumMoisture;
        public float MinimumPH => minimumPH;
        public float OptimumPH => optimumPH;
        public float MaximumPH => maximumPH;
        public bool RequiresOxygen => requiresOxygen;
        public string Notes => notes;

        public float CompositionFractionSum =>
            labileFraction + celluloseFraction + hemicelluloseFraction +
            ligninFraction + proteinFraction + otherOrganicFraction + inertFraction;

        public DecompositionProfile(
            string materialId,
            string displayName,
            float labileFraction,
            float celluloseFraction,
            float hemicelluloseFraction,
            float ligninFraction,
            float proteinFraction,
            float otherOrganicFraction,
            float inertFraction,
            float referenceBulkDensityKgPerM3 = -1f,
            float porosity = -1f,
            float labileDecayRatePerDay = 0.02f,
            float celluloseDecayRatePerDay = 0.001f,
            float hemicelluloseDecayRatePerDay = 0.003f,
            float ligninDecayRatePerDay = 0.0001f,
            float proteinDecayRatePerDay = 0.005f,
            float otherOrganicDecayRatePerDay = 0.001f,
            float carbonMassFraction = 0.45f,
            float nitrogenMassFraction = 0.01f,
            float phosphorusMassFraction = 0.001f,
            float nitrogenMineralisationFraction = 0.6f,
            float phosphorusMineralisationFraction = 0.6f,
            float respiredCarbonFraction = 0.65f,
            float humificationFraction = 0.15f,
            float minimumTemperatureC = 0f,
            float optimumTemperatureC = 25f,
            float maximumTemperatureC = 50f,
            float minimumMoisture = 0.05f,
            float optimumMoisture = 0.6f,
            float maximumMoisture = 1f,
            float minimumPH = 3f,
            float optimumPH = 6.5f,
            float maximumPH = 10f,
            bool requiresOxygen = true,
            string notes = "")
        {
            if (string.IsNullOrWhiteSpace(materialId))
                throw new ArgumentException("A decomposition profile requires a material ID.", nameof(materialId));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("A decomposition profile requires a display name.", nameof(displayName));

            ValidateUnitInterval(labileFraction, nameof(labileFraction));
            ValidateUnitInterval(celluloseFraction, nameof(celluloseFraction));
            ValidateUnitInterval(hemicelluloseFraction, nameof(hemicelluloseFraction));
            ValidateUnitInterval(ligninFraction, nameof(ligninFraction));
            ValidateUnitInterval(proteinFraction, nameof(proteinFraction));
            ValidateUnitInterval(otherOrganicFraction, nameof(otherOrganicFraction));
            ValidateUnitInterval(inertFraction, nameof(inertFraction));
            float sum = labileFraction + celluloseFraction + hemicelluloseFraction +
                        ligninFraction + proteinFraction + otherOrganicFraction + inertFraction;
            if (sum > 1.02f || sum < 0.98f)
                throw new ArgumentException("Dry-mass component fractions must sum to approximately 1.0.");

            ValidateNonNegative(referenceBulkDensityKgPerM3, nameof(referenceBulkDensityKgPerM3), allowUnknown: true);
            if (porosity != -1f)
                ValidateUnitInterval(porosity, nameof(porosity));
            ValidateNonNegative(labileDecayRatePerDay, nameof(labileDecayRatePerDay));
            ValidateNonNegative(celluloseDecayRatePerDay, nameof(celluloseDecayRatePerDay));
            ValidateNonNegative(hemicelluloseDecayRatePerDay, nameof(hemicelluloseDecayRatePerDay));
            ValidateNonNegative(ligninDecayRatePerDay, nameof(ligninDecayRatePerDay));
            ValidateNonNegative(proteinDecayRatePerDay, nameof(proteinDecayRatePerDay));
            ValidateNonNegative(otherOrganicDecayRatePerDay, nameof(otherOrganicDecayRatePerDay));
            ValidateUnitInterval(carbonMassFraction, nameof(carbonMassFraction));
            ValidateUnitInterval(nitrogenMassFraction, nameof(nitrogenMassFraction));
            ValidateUnitInterval(phosphorusMassFraction, nameof(phosphorusMassFraction));
            ValidateUnitInterval(nitrogenMineralisationFraction, nameof(nitrogenMineralisationFraction));
            ValidateUnitInterval(phosphorusMineralisationFraction, nameof(phosphorusMineralisationFraction));
            ValidateUnitInterval(respiredCarbonFraction, nameof(respiredCarbonFraction));
            ValidateUnitInterval(humificationFraction, nameof(humificationFraction));
            ValidateResponseRange(minimumTemperatureC, optimumTemperatureC, maximumTemperatureC, nameof(minimumTemperatureC));
            ValidateResponseRange(minimumMoisture, optimumMoisture, maximumMoisture, nameof(minimumMoisture));
            ValidateResponseRange(minimumPH, optimumPH, maximumPH, nameof(minimumPH));

            this.materialId = materialId;
            this.displayName = displayName;
            this.labileFraction = labileFraction;
            this.celluloseFraction = celluloseFraction;
            this.hemicelluloseFraction = hemicelluloseFraction;
            this.ligninFraction = ligninFraction;
            this.proteinFraction = proteinFraction;
            this.otherOrganicFraction = otherOrganicFraction;
            this.inertFraction = inertFraction;
            this.referenceBulkDensityKgPerM3 = referenceBulkDensityKgPerM3;
            this.porosity = porosity;
            this.labileDecayRatePerDay = labileDecayRatePerDay;
            this.celluloseDecayRatePerDay = celluloseDecayRatePerDay;
            this.hemicelluloseDecayRatePerDay = hemicelluloseDecayRatePerDay;
            this.ligninDecayRatePerDay = ligninDecayRatePerDay;
            this.proteinDecayRatePerDay = proteinDecayRatePerDay;
            this.otherOrganicDecayRatePerDay = otherOrganicDecayRatePerDay;
            this.carbonMassFraction = carbonMassFraction;
            this.nitrogenMassFraction = nitrogenMassFraction;
            this.phosphorusMassFraction = phosphorusMassFraction;
            this.nitrogenMineralisationFraction = nitrogenMineralisationFraction;
            this.phosphorusMineralisationFraction = phosphorusMineralisationFraction;
            this.respiredCarbonFraction = respiredCarbonFraction;
            this.humificationFraction = humificationFraction;
            this.minimumTemperatureC = minimumTemperatureC;
            this.optimumTemperatureC = optimumTemperatureC;
            this.maximumTemperatureC = maximumTemperatureC;
            this.minimumMoisture = minimumMoisture;
            this.optimumMoisture = optimumMoisture;
            this.maximumMoisture = maximumMoisture;
            this.minimumPH = minimumPH;
            this.optimumPH = optimumPH;
            this.maximumPH = maximumPH;
            this.requiresOxygen = requiresOxygen;
            this.notes = notes;
        }
    }

    // ============================================================
    // RUNTIME ORGANISM POPULATION
    // ============================================================

    /// <summary>
    /// A runtime population referencing an organism definition. Biomass is
    /// dry mass in kilograms. Habitat area determines carrying capacity.
    /// SurfaceCoverage is the fraction of the supplied surface currently
    /// colonised by this population.
    /// </summary>
    [Serializable]
    public class OrganismPopulation
    {
        private readonly OrganismDefinition definition;
        [SerializeField] private float biomassKg;
        [SerializeField] private float habitatAreaM2;
        [SerializeField] private float surfaceCoverage;
        [SerializeField] private float ageDays;

        public OrganismDefinition Definition => definition;
        public float BiomassKg => biomassKg;
        public float HabitatAreaM2 => habitatAreaM2;
        public float SurfaceCoverage => surfaceCoverage;
        public float ColonisedAreaM2 => habitatAreaM2 * surfaceCoverage;
        public float AgeDays => ageDays;

        public OrganismPopulation(
            OrganismDefinition definition,
            float initialBiomassKg,
            float habitatAreaM2 = 1f,
            float initialSurfaceCoverage = 0.001f)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            ValidateNonNegative(initialBiomassKg, nameof(initialBiomassKg));
            ValidatePositive(habitatAreaM2, nameof(habitatAreaM2));
            ValidateUnitInterval(initialSurfaceCoverage, nameof(initialSurfaceCoverage));

            this.definition = definition;
            biomassKg = initialBiomassKg;
            this.habitatAreaM2 = habitatAreaM2;
            surfaceCoverage = initialSurfaceCoverage;
            ageDays = 0f;
        }

        internal void AddBiomass(float amountKg)
        {
            biomassKg += Mathf.Max(0f, amountKg);
        }

        internal void SetSurfaceCoverage(float value, float areaM2)
        {
            surfaceCoverage = Mathf.Clamp(value, 0f, definition.MaximumSurfaceCoverage);
            habitatAreaM2 = Mathf.Max(MinimumPositiveValue, areaM2);
        }

        internal void AdvanceAge(float days)
        {
            ageDays += Mathf.Max(0f, days);
        }
    }

    // ============================================================
    // RUNTIME ORGANIC SUBSTRATE SAMPLE
    // ============================================================

    /// <summary>
    /// One physical quantity of organic substrate. Component pools are dry
    /// masses in kilograms. Moisture is tracked separately from dry mass.
    /// Decomposition reduces degradable component pools; inert mass remains.
    /// </summary>
    [Serializable]
    public class OrganicSubstrateSample
    {
        private readonly DecompositionProfile profile;
        [SerializeField] private float labileMassKg;
        [SerializeField] private float celluloseMassKg;
        [SerializeField] private float hemicelluloseMassKg;
        [SerializeField] private float ligninMassKg;
        [SerializeField] private float proteinMassKg;
        [SerializeField] private float otherOrganicMassKg;
        [SerializeField] private float inertMassKg;
        [SerializeField] private float organicNitrogenKg;
        [SerializeField] private float organicPhosphorusKg;
        [SerializeField] private float moistureFraction;

        public DecompositionProfile Profile => profile;
        public float LabileMassKg => labileMassKg;
        public float CelluloseMassKg => celluloseMassKg;
        public float HemicelluloseMassKg => hemicelluloseMassKg;
        public float LigninMassKg => ligninMassKg;
        public float ProteinMassKg => proteinMassKg;
        public float OtherOrganicMassKg => otherOrganicMassKg;
        public float InertMassKg => inertMassKg;
        public float OrganicNitrogenKg => organicNitrogenKg;
        public float OrganicPhosphorusKg => organicPhosphorusKg;
        public float MoistureFraction => moistureFraction;
        public float DryMassKg =>
            labileMassKg + celluloseMassKg + hemicelluloseMassKg + ligninMassKg +
            proteinMassKg + otherOrganicMassKg + inertMassKg;
        public float WaterMassKg =>
            DryMassKg * Mathf.Min(0.999f, moistureFraction) /
            Mathf.Max(0.001f, 1f - Mathf.Min(0.999f, moistureFraction));
        public float WetMassKg => DryMassKg + WaterMassKg;
        public float DegradableOrganicMassKg =>
            labileMassKg + celluloseMassKg + hemicelluloseMassKg + ligninMassKg +
            proteinMassKg + otherOrganicMassKg;

        public OrganicSubstrateSample(
            DecompositionProfile profile,
            float initialDryMassKg,
            float moistureFraction = 0.5f)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            ValidatePositive(initialDryMassKg, nameof(initialDryMassKg));
            ValidateUnitInterval(moistureFraction, nameof(moistureFraction));

            this.profile = profile;
            labileMassKg = initialDryMassKg * profile.LabileFraction;
            celluloseMassKg = initialDryMassKg * profile.CelluloseFraction;
            hemicelluloseMassKg = initialDryMassKg * profile.HemicelluloseFraction;
            ligninMassKg = initialDryMassKg * profile.LigninFraction;
            proteinMassKg = initialDryMassKg * profile.ProteinFraction;
            otherOrganicMassKg = initialDryMassKg * profile.OtherOrganicFraction;
            inertMassKg = initialDryMassKg * profile.InertFraction;
            organicNitrogenKg = initialDryMassKg * profile.NitrogenMassFraction;
            organicPhosphorusKg = initialDryMassKg * profile.PhosphorusMassFraction;
            this.moistureFraction = Mathf.Min(0.999f, moistureFraction);
        }

        public void SetMoistureFraction(float value)
        {
            ValidateUnitInterval(value, nameof(value));
            moistureFraction = Mathf.Min(0.999f, value);
        }

        internal float RemoveComponent(DecompositionComponent component, float requestedKg)
        {
            switch (component)
            {
                case DecompositionComponent.LabileOrganicMatter:
                    return Remove(ref labileMassKg, requestedKg);
                case DecompositionComponent.Cellulose:
                    return Remove(ref celluloseMassKg, requestedKg);
                case DecompositionComponent.Hemicellulose:
                    return Remove(ref hemicelluloseMassKg, requestedKg);
                case DecompositionComponent.Lignin:
                    return Remove(ref ligninMassKg, requestedKg);
                case DecompositionComponent.Protein:
                    return Remove(ref proteinMassKg, requestedKg);
                case DecompositionComponent.OtherOrganicMatter:
                    return Remove(ref otherOrganicMassKg, requestedKg);
                default:
                    return 0f;
            }
        }

        internal void RemoveOrganicNutrients(float nitrogenKg, float phosphorusKg)
        {
            organicNitrogenKg = Mathf.Max(0f, organicNitrogenKg - Mathf.Max(0f, nitrogenKg));
            organicPhosphorusKg = Mathf.Max(0f, organicPhosphorusKg - Mathf.Max(0f, phosphorusKg));
        }

        private static float Remove(ref float available, float requested)
        {
            float removed = Mathf.Clamp(requested, 0f, available);
            available = Mathf.Max(0f, available - removed);
            return removed;
        }
    }

    // ============================================================
    // RESULT TYPES
    // ============================================================

    [Serializable]
    public class GrowthAssessment
    {
        public float TemperatureFactor { get; private set; }
        public float MoistureFactor { get; private set; }
        public float PHFactor { get; private set; }
        public float OxygenFactor { get; private set; }
        public float LightFactor { get; private set; }
        public float CarbonDioxideFactor { get; private set; }
        public float EnvironmentalFactor { get; private set; }
        public float SpecificGrowthRatePerDay { get; private set; }

        public GrowthAssessment(
            float temperatureFactor,
            float moistureFactor,
            float pHFactor,
            float oxygenFactor,
            float lightFactor,
            float carbonDioxideFactor,
            float environmentalFactor,
            float specificGrowthRatePerDay)
        {
            TemperatureFactor = temperatureFactor;
            MoistureFactor = moistureFactor;
            PHFactor = pHFactor;
            OxygenFactor = oxygenFactor;
            LightFactor = lightFactor;
            CarbonDioxideFactor = carbonDioxideFactor;
            EnvironmentalFactor = environmentalFactor;
            SpecificGrowthRatePerDay = specificGrowthRatePerDay;
        }
    }

    [Serializable]
    public class NutrientAvailabilityResult
    {
        public float EstimatedBiomassSupportedKg { get; private set; }
        public NutrientType LimitingNutrient { get; private set; }
        public float CarbonSupportKg { get; private set; }
        public float NitrogenSupportKg { get; private set; }
        public float PhosphorusSupportKg { get; private set; }
        public float PotassiumSupportKg { get; private set; }

        public NutrientAvailabilityResult(
            float estimatedBiomassSupportedKg,
            NutrientType limitingNutrient,
            float carbonSupportKg,
            float nitrogenSupportKg,
            float phosphorusSupportKg,
            float potassiumSupportKg)
        {
            EstimatedBiomassSupportedKg = estimatedBiomassSupportedKg;
            LimitingNutrient = limitingNutrient;
            CarbonSupportKg = carbonSupportKg;
            NitrogenSupportKg = nitrogenSupportKg;
            PhosphorusSupportKg = phosphorusSupportKg;
            PotassiumSupportKg = potassiumSupportKg;
        }
    }

    [Serializable]
    public class GrowthResult
    {
        public float BiomassBeforeKg { get; private set; }
        public float BiomassAfterKg { get; private set; }
        public float BiomassGainedKg { get; private set; }
        public float CarbonConsumedKg { get; private set; }
        public float CarbonDioxideConsumedKg { get; private set; }
        public float NitrogenConsumedKg { get; private set; }
        public float PhosphorusConsumedKg { get; private set; }
        public float PotassiumConsumedKg { get; private set; }
        public NutrientType LimitingNutrient { get; private set; }
        public GrowthAssessment Assessment { get; private set; }

        public GrowthResult(
            float biomassBeforeKg,
            float biomassAfterKg,
            float biomassGainedKg,
            float carbonConsumedKg,
            float carbonDioxideConsumedKg,
            float nitrogenConsumedKg,
            float phosphorusConsumedKg,
            float potassiumConsumedKg,
            NutrientType limitingNutrient,
            GrowthAssessment assessment)
        {
            BiomassBeforeKg = biomassBeforeKg;
            BiomassAfterKg = biomassAfterKg;
            BiomassGainedKg = biomassGainedKg;
            CarbonConsumedKg = carbonConsumedKg;
            CarbonDioxideConsumedKg = carbonDioxideConsumedKg;
            NitrogenConsumedKg = nitrogenConsumedKg;
            PhosphorusConsumedKg = phosphorusConsumedKg;
            PotassiumConsumedKg = potassiumConsumedKg;
            LimitingNutrient = limitingNutrient;
            Assessment = assessment;
        }
    }

    [Serializable]
    public class DecompositionResult
    {
        public float InitialDryMassKg { get; private set; }
        public float FinalDryMassKg { get; private set; }
        public float DecomposedMassKg { get; private set; }
        public float LabileDecomposedKg { get; private set; }
        public float CelluloseDecomposedKg { get; private set; }
        public float HemicelluloseDecomposedKg { get; private set; }
        public float LigninDecomposedKg { get; private set; }
        public float ProteinDecomposedKg { get; private set; }
        public float OtherOrganicDecomposedKg { get; private set; }
        public float CarbonDioxideProducedKg { get; private set; }
        public float NitrogenMineralisedKg { get; private set; }
        public float PhosphorusMineralisedKg { get; private set; }
        public float HumusFormedKg { get; private set; }
        public float EnvironmentalRateFactor { get; private set; }

        public DecompositionResult(
            float initialDryMassKg,
            float finalDryMassKg,
            float decomposedMassKg,
            float labileDecomposedKg,
            float celluloseDecomposedKg,
            float hemicelluloseDecomposedKg,
            float ligninDecomposedKg,
            float proteinDecomposedKg,
            float otherOrganicDecomposedKg,
            float carbonDioxideProducedKg,
            float nitrogenMineralisedKg,
            float phosphorusMineralisedKg,
            float humusFormedKg,
            float environmentalRateFactor)
        {
            InitialDryMassKg = initialDryMassKg;
            FinalDryMassKg = finalDryMassKg;
            DecomposedMassKg = decomposedMassKg;
            LabileDecomposedKg = labileDecomposedKg;
            CelluloseDecomposedKg = celluloseDecomposedKg;
            HemicelluloseDecomposedKg = hemicelluloseDecomposedKg;
            LigninDecomposedKg = ligninDecomposedKg;
            ProteinDecomposedKg = proteinDecomposedKg;
            OtherOrganicDecomposedKg = otherOrganicDecomposedKg;
            CarbonDioxideProducedKg = carbonDioxideProducedKg;
            NitrogenMineralisedKg = nitrogenMineralisedKg;
            PhosphorusMineralisedKg = phosphorusMineralisedKg;
            HumusFormedKg = humusFormedKg;
            EnvironmentalRateFactor = environmentalRateFactor;
        }
    }

    [Serializable]
    public class SurfaceColonisationResult
    {
        public float InitialCoverage { get; private set; }
        public float FinalCoverage { get; private set; }
        public float ColonisedAreaM2 { get; private set; }
        public float BiomassGainedKg { get; private set; }
        public GrowthResult Growth { get; private set; }

        public SurfaceColonisationResult(
            float initialCoverage,
            float finalCoverage,
            float colonisedAreaM2,
            float biomassGainedKg,
            GrowthResult growth)
        {
            InitialCoverage = initialCoverage;
            FinalCoverage = finalCoverage;
            ColonisedAreaM2 = colonisedAreaM2;
            BiomassGainedKg = biomassGainedKg;
            Growth = growth;
        }
    }

    // ============================================================
    // SERIALIZED DATABASES
    // ============================================================

    [Header("Organism Definitions")]
    [SerializeField] private List<OrganismDefinition> organisms = new List<OrganismDefinition>();

    [Header("Organic Substrate / Decomposition Profiles")]
    [SerializeField] private List<DecompositionProfile> decompositionProfiles =
        new List<DecompositionProfile>();

    [Header("Optional Material Database")]
    [SerializeField] private ParticleChemistry.Material materialDatabase;

    public IReadOnlyList<OrganismDefinition> Organisms => organisms;
    public IReadOnlyList<DecompositionProfile> DecompositionProfiles => decompositionProfiles;
    public ParticleChemistry.Material MaterialDatabase => materialDatabase;

    // ============================================================
    // INITIALIZATION AND BUILT-IN DEFINITIONS
    // ============================================================

    private void OnEnable()
    {
        EnsureLists();
    }

    /// <summary>
    /// Adds starter profiles for broad organism groups. These are tunable
    /// illustrative presets, not universal parameters for a species.
    /// </summary>
    [ContextMenu("Add Built-in Organism Profiles")]
    public void AddBuiltInOrganismProfiles()
    {
        EnsureLists();

        AddOrganismIfMissing(new OrganismDefinition(
            "plant-general", "General green plant", OrganismType.Plant,
            MetabolicMode.Photoautotroph, CarbonSource.AtmosphericCarbonDioxide,
            0.035f, 2.5f, 2f, 24f, 43f,
            0.15f, 0.60f, 0.95f, 4.5f, 6.5f, 8.5f,
            0.02f, 0.21f, 1f, 220f, 300f,
            0.45f, 0.025f, 0.003f, 0.012f,
            0.12f, 1f, 1f,
            "Generic photosynthetic plant preset. Species, growth stage and growing conditions strongly affect these values.",
            "Starter model only. Set species-specific response ranges for predictive work."));

        AddOrganismIfMissing(new OrganismDefinition(
            "moss-general", "General moss", OrganismType.Moss,
            MetabolicMode.Photoautotroph, CarbonSource.AtmosphericCarbonDioxide,
            0.025f, 0.45f, -5f, 18f, 35f,
            0.30f, 0.80f, 1f, 3.5f, 5.8f, 7.5f,
            0.01f, 0.21f, 1f, 70f, 300f,
            0.45f, 0.020f, 0.002f, 0.010f,
            0.35f, 1f, 1f,
            "Generic moss preset. Mosses vary widely in desiccation tolerance and light requirements.",
            "Moisture response is a broad approximation; some mosses tolerate long dry periods."));

        AddOrganismIfMissing(new OrganismDefinition(
            "green-algae", "Generic green algae", OrganismType.Algae,
            MetabolicMode.Photoautotroph, CarbonSource.AtmosphericCarbonDioxide,
            0.45f, 0.25f, 5f, 25f, 38f,
            0.65f, 0.95f, 1f, 5f, 7.5f, 10f,
            0.01f, 0.15f, 1f, 160f, 300f,
            0.45f, 0.070f, 0.010f, 0.015f,
            0.65f, 1f, 1f,
            "Generic green-algae preset for wet surfaces or water films. It is not a species-specific culture model.",
            "Growth is highly sensitive to species, nutrients, light spectrum, mixing and temperature."));

        AddOrganismIfMissing(new OrganismDefinition(
            "saprotrophic-fungus", "Saprotrophic fungus", OrganismType.Fungus,
            MetabolicMode.Saprotroph, CarbonSource.OrganicCarbon,
            0.10f, 0.30f, 2f, 25f, 40f,
            0.25f, 0.65f, 0.95f, 3f, 5.5f, 8f,
            0.02f, 0.18f, 1f, 0f, 0f,
            0.48f, 0.035f, 0.004f, 0.010f,
            0.45f, 1f, 4f,
            "Generic decomposer fungus that obtains carbon from organic matter.",
            "The decomposition enhancement is a tunable coupling factor, not a measured universal fungal constant."));

        AddOrganismIfMissing(new OrganismDefinition(
            "wood-decay-fungus", "Wood-decay fungus", OrganismType.Fungus,
            MetabolicMode.Saprotroph, CarbonSource.OrganicCarbon,
            0.055f, 0.20f, 2f, 23f, 38f,
            0.30f, 0.70f, 0.95f, 3f, 5f, 7.5f,
            0.02f, 0.18f, 1f, 0f, 0f,
            0.48f, 0.025f, 0.003f, 0.010f,
            0.30f, 1f, 6f,
            "Broad wood-decay fungal preset for lignocellulosic substrates.",
            "Brown-rot and white-rot fungi need different lignin and cellulose decay parameters."));

        AddOrganismIfMissing(new OrganismDefinition(
            "soil-bacteria", "Generic aerobic soil bacteria", OrganismType.Bacteria,
            MetabolicMode.Saprotroph, CarbonSource.OrganicCarbon,
            0.35f, 0.08f, 5f, 30f, 45f,
            0.20f, 0.60f, 0.90f, 4f, 7f, 9f,
            0.03f, 0.21f, 1f, 0f, 0f,
            0.50f, 0.10f, 0.015f, 0.020f,
            0.55f, 1f, 2f,
            "Generic aerobic heterotrophic bacterial preset.",
            "Bacterial growth rates differ by many orders of magnitude across taxa and environments."));

        AddOrganismIfMissing(new OrganismDefinition(
            "lichen-general", "General lichen association", OrganismType.Lichen,
            MetabolicMode.Mixotroph, CarbonSource.AtmosphericCarbonDioxide,
            0.008f, 0.20f, -10f, 15f, 30f,
            0.10f, 0.45f, 0.90f, 3f, 5.5f, 8f,
            0.01f, 0.21f, 1f, 45f, 300f,
            0.45f, 0.015f, 0.002f, 0.008f,
            0.08f, 1f, 1.5f,
            "Generic lichen preset representing a fungal partner and photosynthetic partner.",
            "A lichen is a symbiotic association, not a single organism with one universal physiology."));

        MarkAssetDirty();
    }

    /// <summary>
    /// Imports decomposition profiles for biological materials, soils, foods
    /// and natural-fibre textiles from the Material database. Composition
    /// fractions are broad editable presets based on material ID/category;
    /// they are not chemical analyses of the assigned asset.
    /// </summary>
    [ContextMenu("Add Missing Decomposition Profiles From Material Database")]
    public void AddMissingDecompositionProfilesFromMaterialDatabase()
    {
        if (materialDatabase == null)
            throw new InvalidOperationException("Assign a ParticleChemistry.Material database first.");

        EnsureLists();

        foreach (ParticleChemistry.Material.MaterialDefinition material in materialDatabase.Materials)
        {
            if (material == null || HasDecompositionProfile(material.Id))
                continue;

            bool supportedCategory =
                material.Category == ParticleChemistry.Material.MaterialCategory.BiologicalMaterial ||
                material.Category == ParticleChemistry.Material.MaterialCategory.Soil ||
                material.Category == ParticleChemistry.Material.MaterialCategory.Food ||
                material.Category == ParticleChemistry.Material.MaterialCategory.Textile;

            if (!supportedCategory)
                continue;

            decompositionProfiles.Add(CreateDefaultDecompositionProfile(material));
        }

        MarkAssetDirty();
    }

    /// <summary>
    /// Imports missing decomposition profiles using the supplied central Material database.
    /// Existing biological decomposition profiles are preserved.
    /// </summary>
    public void AddMissingDecompositionProfilesFromMaterialDatabase(ParticleChemistry.Material database)
    {
        if (database == null)
            throw new ArgumentNullException(nameof(database));

        EnsureLists();
        int added = 0;
        foreach (ParticleChemistry.Material.MaterialDefinition material in database.Materials)
        {
            if (material == null || HasDecompositionProfile(material.Id))
                continue;

            bool supportedCategory =
                material.Category == ParticleChemistry.Material.MaterialCategory.BiologicalMaterial ||
                material.Category == ParticleChemistry.Material.MaterialCategory.Soil ||
                material.Category == ParticleChemistry.Material.MaterialCategory.Food ||
                material.Category == ParticleChemistry.Material.MaterialCategory.Textile;

            if (supportedCategory)
            {
                decompositionProfiles.Add(CreateDefaultDecompositionProfile(material));
                added++;
            }
        }

        if (added > 0)
            MarkAssetDirty();
    }

    [ContextMenu("Add Built-in Substrate Profiles")]
    public void AddBuiltInSubstrateProfiles()
    {
        EnsureLists();

        AddSubstrateIfMissing(new DecompositionProfile(
            "fresh-leaf", "Fresh green leaf tissue",
            0.08f, 0.18f, 0.16f, 0.06f, 0.10f, 0.12f, 0.30f,
            950f, 0.20f,
            0.06f, 0.003f, 0.006f, 0.0003f, 0.012f, 0.002f,
            0.45f, 0.025f, 0.003f, 0.65f, 0.60f, 0.65f, 0.12f,
            0f, 25f, 48f, 0.08f, 0.65f, 1f, 3.5f, 6.5f, 9f, true,
            "Illustrative dry-matter fractions; exclude the leaf's water when setting initial dry mass."));

        AddSubstrateIfMissing(new DecompositionProfile(
            "dry-leaf-litter", "Dry leaf litter",
            0.025f, 0.26f, 0.20f, 0.24f, 0.025f, 0.045f, 0.205f,
            250f, 0.65f,
            0.025f, 0.0015f, 0.0025f, 0.00015f, 0.002f, 0.0008f,
            0.48f, 0.012f, 0.001f, 0.55f, 0.50f, 0.60f, 0.18f,
            0f, 24f, 48f, 0.10f, 0.60f, 1f, 3f, 6f, 9f, true,
            "Illustrative litter composition. Tree species, leaf age and climate alter decay rates."));

        AddSubstrateIfMissing(new DecompositionProfile(
            "oak-wood", "Oak wood",
            0.005f, 0.42f, 0.24f, 0.27f, 0.01f, 0.015f, 0.04f,
            700f, 0.45f,
            0.01f, 0.00035f, 0.0007f, 0.00005f, 0.0005f, 0.0002f,
            0.50f, 0.002f, 0.0003f, 0.45f, 0.45f, 0.65f, 0.12f,
            0f, 23f, 45f, 0.15f, 0.65f, 1f, 3f, 5.5f, 8f, true,
            "Illustrative dry-wood recipe. Heartwood, sapwood and moisture alter decomposability."));

        AddSubstrateIfMissing(new DecompositionProfile(
            "pine-wood", "Pine wood",
            0.008f, 0.40f, 0.25f, 0.26f, 0.01f, 0.02f, 0.04f,
            500f, 0.55f,
            0.012f, 0.00045f, 0.0008f, 0.00006f, 0.0006f, 0.00025f,
            0.50f, 0.002f, 0.0003f, 0.45f, 0.45f, 0.65f, 0.12f,
            0f, 23f, 45f, 0.15f, 0.65f, 1f, 3f, 5.5f, 8f, true,
            "Illustrative softwood composition; resin content and species matter."));

        AddSubstrateIfMissing(new DecompositionProfile(
            "grass-tissue", "Fresh grass tissue",
            0.12f, 0.20f, 0.18f, 0.07f, 0.10f, 0.08f, 0.25f,
            950f, 0.20f,
            0.07f, 0.003f, 0.006f, 0.0004f, 0.012f, 0.002f,
            0.44f, 0.03f, 0.003f, 0.65f, 0.60f, 0.65f, 0.12f,
            0f, 25f, 48f, 0.10f, 0.65f, 1f, 4f, 6.5f, 9f, true,
            "Illustrative dry-matter fractions; use dry mass, not fresh mass."));

        AddSubstrateIfMissing(new DecompositionProfile(
            "straw", "Dry straw",
            0.025f, 0.34f, 0.25f, 0.18f, 0.025f, 0.055f, 0.125f,
            100f, 0.80f,
            0.025f, 0.0012f, 0.0022f, 0.00012f, 0.002f, 0.0007f,
            0.46f, 0.006f, 0.0008f, 0.55f, 0.55f, 0.65f, 0.15f,
            0f, 25f, 48f, 0.10f, 0.65f, 1f, 4f, 6.5f, 9f, true,
            "Cereal straw preset; lignin, silica and nitrogen content vary by crop and maturity."));

        AddSubstrateIfMissing(new DecompositionProfile(
            "peat", "Peat",
            0.015f, 0.18f, 0.12f, 0.38f, 0.02f, 0.085f, 0.20f,
            150f, 0.80f,
            0.008f, 0.0005f, 0.0008f, 0.00004f, 0.0008f, 0.0002f,
            0.52f, 0.012f, 0.001f, 0.50f, 0.50f, 0.55f, 0.20f,
            0f, 18f, 40f, 0.20f, 0.75f, 1f, 2.5f, 4.5f, 7f, false,
            "Illustrative peat preset. Waterlogging and low oxygen often slow decomposition substantially."));

        MarkAssetDirty();
    }

    private DecompositionProfile CreateDefaultDecompositionProfile(
        ParticleChemistry.Material.MaterialDefinition material)
    {
        float labile = 0.03f;
        float cellulose = 0.30f;
        float hemicellulose = 0.20f;
        float lignin = 0.20f;
        float protein = 0.04f;
        float other = 0.08f;
        float inert = 0.15f;
        float nitrogen = 0.01f;
        float phosphorus = 0.001f;
        string note = "Broad editable starting composition generated from material category. Replace with measured dry-basis composition.";

        string id = material.Id == null ? string.Empty : material.Id.ToLowerInvariant();

        if (id.Contains("leaf") || id.Contains("grass"))
        {
            labile = 0.08f; cellulose = 0.20f; hemicellulose = 0.18f;
            lignin = 0.08f; protein = 0.08f; other = 0.13f; inert = 0.25f;
            nitrogen = 0.02f;
        }
        else if (id.Contains("wood") || id.Contains("bark") || id.Contains("bamboo"))
        {
            labile = 0.01f; cellulose = 0.42f; hemicellulose = 0.24f;
            lignin = 0.27f; protein = 0.01f; other = 0.01f; inert = 0.04f;
            nitrogen = 0.002f;
        }
        else if (id.Contains("straw") || id.Contains("paper") || id.Contains("cardboard"))
        {
            labile = 0.03f; cellulose = 0.34f; hemicellulose = 0.25f;
            lignin = 0.18f; protein = 0.025f; other = 0.055f; inert = 0.12f;
            nitrogen = 0.006f;
        }
        else if (material.Category == ParticleChemistry.Material.MaterialCategory.Soil)
        {
            labile = 0.005f; cellulose = 0.015f; hemicellulose = 0.01f;
            lignin = 0.02f; protein = 0.005f; other = 0.025f; inert = 0.92f;
            nitrogen = 0.003f;
            note = "Generic soil preset with a small organic fraction. Replace with measured soil organic matter and nutrient data.";
        }
        else if (material.Category == ParticleChemistry.Material.MaterialCategory.Food)
        {
            labile = 0.35f; cellulose = 0.06f; hemicellulose = 0.02f;
            lignin = 0.01f; protein = 0.14f; other = 0.28f; inert = 0.14f;
            nitrogen = 0.025f;
            note = "Generic food-waste preset. Water, fat, protein and carbohydrate composition varies by food.";
        }
        else if (material.Category == ParticleChemistry.Material.MaterialCategory.Textile)
        {
            labile = 0.01f; cellulose = 0.70f; hemicellulose = 0.05f;
            lignin = 0.01f; protein = 0.12f; other = 0.06f; inert = 0.05f;
            nitrogen = 0.02f;
            note = "Generic natural-textile preset. Only natural biodegradable fibres should use this profile; synthetic fibres require separate treatment.";
        }

        // A few materials have explicit presets with better component splits.
        if (id == "fresh-leaf")
        {
            labile = 0.08f; cellulose = 0.18f; hemicellulose = 0.16f;
            lignin = 0.06f; protein = 0.10f; other = 0.12f; inert = 0.30f;
            nitrogen = 0.025f; phosphorus = 0.003f;
            note = "Illustrative dry-matter fractions; leaf water is not included in the dry substrate mass.";
        }
        else if (id == "dry-leaf-litter")
        {
            labile = 0.025f; cellulose = 0.26f; hemicellulose = 0.20f;
            lignin = 0.24f; protein = 0.025f; other = 0.045f; inert = 0.20f;
            nitrogen = 0.012f; phosphorus = 0.001f;
            note = "Illustrative litter fractions; species and leaf age strongly affect composition.";
        }

        return new DecompositionProfile(
            material.Id,
            material.Name,
            labile, cellulose, hemicellulose, lignin, protein, other, inert,
            material.DensityKgPerM3,
            material.Porosity,
            carbonMassFraction: 0.45f,
            nitrogenMassFraction: nitrogen,
            phosphorusMassFraction: phosphorus,
            notes: note);
    }

    private void AddOrganismIfMissing(OrganismDefinition definition)
    {
        if (!HasOrganism(definition.Id))
            organisms.Add(definition);
    }

    private void AddSubstrateIfMissing(DecompositionProfile profile)
    {
        if (!HasDecompositionProfile(profile.MaterialId))
            decompositionProfiles.Add(profile);
    }

    // ============================================================
    // LOOKUP AND PROFILE MANAGEMENT
    // ============================================================

    public bool HasOrganism(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || organisms == null)
            return false;

        foreach (OrganismDefinition item in organisms)
        {
            if (item != null && string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public OrganismDefinition GetOrganism(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Organism ID cannot be empty.", nameof(id));

        if (organisms != null)
        {
            foreach (OrganismDefinition item in organisms)
            {
                if (item != null && string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
        }
        throw new ArgumentException("Unknown organism ID: " + id, nameof(id));
    }

    public void AddOrganism(OrganismDefinition definition)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));
        if (HasOrganism(definition.Id))
            throw new InvalidOperationException("Organism ID already exists: " + definition.Id);
        EnsureLists();
        organisms.Add(definition);
        MarkAssetDirty();
    }

    public bool HasDecompositionProfile(string materialId)
    {
        if (string.IsNullOrWhiteSpace(materialId) || decompositionProfiles == null)
            return false;

        foreach (DecompositionProfile item in decompositionProfiles)
        {
            if (item != null && string.Equals(item.MaterialId, materialId, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public DecompositionProfile GetDecompositionProfile(string materialId)
    {
        if (string.IsNullOrWhiteSpace(materialId))
            throw new ArgumentException("Material ID cannot be empty.", nameof(materialId));

        if (decompositionProfiles != null)
        {
            foreach (DecompositionProfile item in decompositionProfiles)
            {
                if (item != null && string.Equals(item.MaterialId, materialId, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
        }
        throw new ArgumentException("No decomposition profile exists for material ID: " + materialId, nameof(materialId));
    }

    public void AddDecompositionProfile(DecompositionProfile profile)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));
        if (HasDecompositionProfile(profile.MaterialId))
            throw new InvalidOperationException("Decomposition profile already exists for: " + profile.MaterialId);
        EnsureLists();
        decompositionProfiles.Add(profile);
        MarkAssetDirty();
    }

    // ============================================================
    // RUNTIME OBJECT CREATION
    // ============================================================

    public BiologicalEnvironment CreateEnvironment(
        float temperatureC = 20f,
        float moistureFraction = 0.5f,
        float relativeHumidity = 0.6f,
        float oxygenFraction = 0.21f,
        float pH = 7f,
        float carbonDioxidePpm = 420f,
        float lightPPFD = 200f,
        NutrientPool nutrients = null)
    {
        return new BiologicalEnvironment(
            temperatureC,
            moistureFraction,
            relativeHumidity,
            oxygenFraction,
            pH,
            carbonDioxidePpm,
            lightPPFD,
            nutrients);
    }

    public OrganismPopulation CreatePopulation(
        string organismId,
        float initialBiomassKg,
        float habitatAreaM2 = 1f,
        float initialSurfaceCoverage = 0.001f)
    {
        return new OrganismPopulation(
            GetOrganism(organismId),
            initialBiomassKg,
            habitatAreaM2,
            initialSurfaceCoverage);
    }

    public OrganicSubstrateSample CreateSubstrateSample(
        string materialId,
        float initialDryMassKg,
        float moistureFraction = 0.5f)
    {
        return new OrganicSubstrateSample(
            GetDecompositionProfile(materialId),
            initialDryMassKg,
            moistureFraction);
    }

    // ============================================================
    // BIOLOGICAL GROWTH
    // ============================================================

    /// <summary>
    /// Calculates environmental growth modifiers. The response curves are
    /// triangular approximations: 0 at minimum/maximum and 1 at optimum.
    /// </summary>
    public GrowthAssessment AssessGrowth(
        string organismId,
        BiologicalEnvironment environment)
    {
        return AssessGrowth(GetOrganism(organismId), environment);
    }

    public GrowthAssessment AssessGrowth(
        OrganismDefinition organism,
        BiologicalEnvironment environment)
    {
        if (organism == null)
            throw new ArgumentNullException(nameof(organism));
        if (environment == null)
            throw new ArgumentNullException(nameof(environment));

        float temperature = TriangularResponse(
            environment.TemperatureC,
            organism.MinimumTemperatureC,
            organism.OptimumTemperatureC,
            organism.MaximumTemperatureC);
        float moisture = TriangularResponse(
            environment.MoistureFraction,
            organism.MinimumMoisture,
            organism.OptimumMoisture,
            organism.MaximumMoisture);
        float pH = TriangularResponse(
            environment.PH,
            organism.MinimumPH,
            organism.OptimumPH,
            organism.MaximumPH);
        float oxygen = TriangularResponse(
            environment.OxygenFraction,
            organism.MinimumOxygenFraction,
            organism.OptimumOxygenFraction,
            organism.MaximumOxygenFraction);

        bool usesPhotosynthesis =
            organism.Metabolism == MetabolicMode.Photoautotroph ||
            organism.Metabolism == MetabolicMode.Mixotroph;

        float light = usesPhotosynthesis
            ? SaturationResponse(environment.PhotosyntheticPhotonFluxDensity, organism.LightHalfSaturationPPFD)
            : 1f;
        float carbonDioxide = organism.Carbon == CarbonSource.AtmosphericCarbonDioxide
            ? SaturationResponse(environment.CarbonDioxidePpm, organism.CarbonDioxideHalfSaturationPpm)
            : 1f;

        float combined = temperature * moisture * pH * oxygen * light * carbonDioxide;
        float rate = organism.MaximumSpecificGrowthRatePerDay * combined;

        return new GrowthAssessment(
            temperature, moisture, pH, oxygen, light, carbonDioxide,
            combined, rate);
    }

    /// <summary>
    /// Estimates how much additional dry biomass the currently available
    /// carbon and N/P/K pools could support, assuming the organism's configured
    /// biomass nutrient fractions remain constant.
    /// </summary>
    public NutrientAvailabilityResult AssessNutrientAvailability(
        OrganismPopulation population,
        BiologicalEnvironment environment)
    {
        if (population == null)
            throw new ArgumentNullException(nameof(population));
        if (environment == null)
            throw new ArgumentNullException(nameof(environment));

        OrganismDefinition organism = population.Definition;
        NutrientPool pool = environment.Nutrients;

        float carbonSupport;
        if (organism.Carbon == CarbonSource.AtmosphericCarbonDioxide)
        {
            float carbonKg = pool.AvailableCarbonDioxideKg * CarbonDioxideToCarbonMassRatio;
            carbonSupport = BiomassSupportedBy(carbonKg, organism.CarbonMassFraction);
        }
        else
        {
            carbonSupport = BiomassSupportedBy(
                pool.AvailableOrganicCarbonKg, organism.CarbonMassFraction);
        }

        float nitrogenSupport = BiomassSupportedBy(
            pool.AvailableNitrogenKg, organism.NitrogenMassFraction);
        float phosphorusSupport = BiomassSupportedBy(
            pool.AvailablePhosphorusKg, organism.PhosphorusMassFraction);
        float potassiumSupport = BiomassSupportedBy(
            pool.AvailablePotassiumKg, organism.PotassiumMassFraction);

        float minimum = float.PositiveInfinity;
        NutrientType limiting = NutrientType.None;
        UpdateMinimumSupport(carbonSupport, organism.Carbon == CarbonSource.AtmosphericCarbonDioxide
            ? NutrientType.CarbonDioxide : NutrientType.OrganicCarbon, ref minimum, ref limiting);
        UpdateMinimumSupport(nitrogenSupport, NutrientType.Nitrogen, ref minimum, ref limiting);
        UpdateMinimumSupport(phosphorusSupport, NutrientType.Phosphorus, ref minimum, ref limiting);
        UpdateMinimumSupport(potassiumSupport, NutrientType.Potassium, ref minimum, ref limiting);

        if (float.IsPositiveInfinity(minimum))
            minimum = float.PositiveInfinity;

        return new NutrientAvailabilityResult(
            minimum, limiting, carbonSupport, nitrogenSupport,
            phosphorusSupport, potassiumSupport);
    }

    /// <summary>
    /// Advances an organism population by deltaTimeDays using logistic growth.
    /// Growth is constrained by environmental conditions, carrying capacity
    /// and available C/N/P/K resources. A non-zero initial population is
    /// required; the method does not spontaneously create organisms.
    /// </summary>
    public GrowthResult AdvanceGrowth(
        OrganismPopulation population,
        BiologicalEnvironment environment,
        float deltaTimeDays)
    {
        if (population == null)
            throw new ArgumentNullException(nameof(population));
        if (environment == null)
            throw new ArgumentNullException(nameof(environment));
        ValidateNonNegative(deltaTimeDays, nameof(deltaTimeDays));

        OrganismDefinition organism = population.Definition;
        float before = population.BiomassKg;
        GrowthAssessment assessment = AssessGrowth(organism, environment);

        if (before <= 0f || deltaTimeDays <= 0f ||
            organism.MaximumSpecificGrowthRatePerDay <= 0f ||
            assessment.SpecificGrowthRatePerDay <= 0f)
        {
            population.AdvanceAge(deltaTimeDays);
            return EmptyGrowthResult(population, before, assessment);
        }

        float carryingCapacity = organism.MaximumBiomassKgPerM2 * population.HabitatAreaM2;
        if (carryingCapacity <= before)
        {
            population.AdvanceAge(deltaTimeDays);
            return EmptyGrowthResult(population, before, assessment);
        }

        float exponent = Mathf.Clamp(assessment.SpecificGrowthRatePerDay * deltaTimeDays, 0f, 40f);
        float exponential = Mathf.Exp(exponent);
        float potentialGrowth =
            before * (carryingCapacity - before) * (exponential - 1f) /
            (carryingCapacity + before * (exponential - 1f));
        potentialGrowth = Mathf.Max(0f, potentialGrowth);

        NutrientAvailabilityResult availability = AssessNutrientAvailability(population, environment);
        float actualGrowth = Mathf.Min(potentialGrowth, availability.EstimatedBiomassSupportedKg);
        if (!IsFinite(actualGrowth) || actualGrowth < 0f)
            actualGrowth = 0f;

        NutrientPool pool = environment.Nutrients;
        float carbonRequired = actualGrowth * organism.CarbonMassFraction;
        float carbonConsumed = 0f;
        float co2Consumed = 0f;

        if (organism.Carbon == CarbonSource.AtmosphericCarbonDioxide)
        {
            co2Consumed = carbonRequired * CarbonToCarbonDioxideMassRatio;
            pool.RemoveCarbonDioxide(co2Consumed);
        }
        else
        {
            carbonConsumed = carbonRequired;
            pool.RemoveOrganicCarbon(carbonConsumed);
        }

        float nitrogenConsumed = actualGrowth * organism.NitrogenMassFraction;
        float phosphorusConsumed = actualGrowth * organism.PhosphorusMassFraction;
        float potassiumConsumed = actualGrowth * organism.PotassiumMassFraction;

        pool.RemoveNitrogen(nitrogenConsumed);
        pool.RemovePhosphorus(phosphorusConsumed);
        pool.RemovePotassium(potassiumConsumed);

        population.AddBiomass(actualGrowth);
        population.AdvanceAge(deltaTimeDays);

        NutrientType limiting = actualGrowth + MinimumPositiveValue < potentialGrowth
            ? availability.LimitingNutrient
            : NutrientType.None;

        return new GrowthResult(
            before,
            population.BiomassKg,
            actualGrowth,
            carbonConsumed,
            co2Consumed,
            nitrogenConsumed,
            phosphorusConsumed,
            potassiumConsumed,
            limiting,
            assessment);
    }

    private static GrowthResult EmptyGrowthResult(
        OrganismPopulation population,
        float before,
        GrowthAssessment assessment)
    {
        return new GrowthResult(
            before, population.BiomassKg, 0f, 0f, 0f, 0f, 0f, 0f,
            NutrientType.None, assessment);
    }

    // ============================================================
    // SURFACE COLONISATION: MOSS, ALGAE, FUNGI AND BIOFILMS
    // ============================================================

    /// <summary>
    /// Advances biological surface coverage with a logistic colonisation
    /// equation. Population growth is also advanced using the same time step.
    /// The population should have a non-zero initial coverage to seed a surface.
    /// </summary>
    public SurfaceColonisationResult AdvanceSurfaceColonisation(
        OrganismPopulation population,
        BiologicalEnvironment environment,
        float surfaceAreaM2,
        float deltaTimeDays)
    {
        if (population == null)
            throw new ArgumentNullException(nameof(population));
        if (environment == null)
            throw new ArgumentNullException(nameof(environment));
        ValidatePositive(surfaceAreaM2, nameof(surfaceAreaM2));
        ValidateNonNegative(deltaTimeDays, nameof(deltaTimeDays));

        float initialCoverage = population.SurfaceCoverage;
        // Set the actual colonisable area before calculating logistic biomass
        // growth, because the population's carrying capacity is area-dependent.
        population.SetSurfaceCoverage(initialCoverage, surfaceAreaM2);
        GrowthResult growth = AdvanceGrowth(population, environment, deltaTimeDays);
        OrganismDefinition organism = population.Definition;

        float finalCoverage = initialCoverage;
        float carryingCoverage = organism.MaximumSurfaceCoverage;
        float rate = organism.SurfaceColonisationRatePerDay * growth.Assessment.EnvironmentalFactor;

        if (deltaTimeDays > 0f && rate > 0f && carryingCoverage > 0f &&
            initialCoverage > 0f && initialCoverage < carryingCoverage)
        {
            float exponential = Mathf.Exp(Mathf.Clamp(rate * deltaTimeDays, 0f, 40f));
            finalCoverage = carryingCoverage * initialCoverage * exponential /
                (carryingCoverage + initialCoverage * (exponential - 1f));
        }

        population.SetSurfaceCoverage(finalCoverage, surfaceAreaM2);

        return new SurfaceColonisationResult(
            initialCoverage,
            population.SurfaceCoverage,
            population.ColonisedAreaM2,
            growth.BiomassGainedKg,
            growth);
    }

    // ============================================================
    // DECOMPOSITION AND ORGANIC MATERIAL DEGRADATION
    // ============================================================

    /// <summary>
    /// Advances organic substrate decomposition. Each organic component uses
    /// an independent first-order decay constant and environmental response.
    /// An optional fungus population increases decay according to its biomass
    /// relative to the remaining substrate. Inert material is not decomposed.
    /// </summary>
    public DecompositionResult AdvanceDecomposition(
        OrganicSubstrateSample substrate,
        BiologicalEnvironment environment,
        float deltaTimeDays,
        OrganismPopulation decomposer = null)
    {
        return AdvanceDecompositionInternal(
            substrate, environment, deltaTimeDays, decomposer, false);
    }

    /// <summary>
    /// Explicit fungal decay entry point. The supplied population must be a
    /// fungus. It uses the same component decay model with a fungal activity
    /// multiplier from the organism definition.
    /// </summary>
    public DecompositionResult AdvanceFungalDecay(
        OrganicSubstrateSample substrate,
        BiologicalEnvironment environment,
        OrganismPopulation fungusPopulation,
        float deltaTimeDays)
    {
        if (fungusPopulation == null)
            throw new ArgumentNullException(nameof(fungusPopulation));
        if (fungusPopulation.Definition.Type != OrganismType.Fungus)
            throw new ArgumentException("Fungal decay requires an OrganismType.Fungus population.", nameof(fungusPopulation));

        return AdvanceDecompositionInternal(
            substrate, environment, deltaTimeDays, fungusPopulation, true);
    }

    /// <summary>
    /// Convenience entry point for general organic degradation without a
    /// separately modelled decomposer population.
    /// </summary>
    public DecompositionResult AdvanceOrganicDegradation(
        OrganicSubstrateSample substrate,
        BiologicalEnvironment environment,
        float deltaTimeDays)
    {
        return AdvanceDecompositionInternal(
            substrate, environment, deltaTimeDays, null, false);
    }

    private DecompositionResult AdvanceDecompositionInternal(
        OrganicSubstrateSample substrate,
        BiologicalEnvironment environment,
        float deltaTimeDays,
        OrganismPopulation decomposer,
        bool requireFungus)
    {
        if (substrate == null)
            throw new ArgumentNullException(nameof(substrate));
        if (environment == null)
            throw new ArgumentNullException(nameof(environment));
        ValidateNonNegative(deltaTimeDays, nameof(deltaTimeDays));

        if (requireFungus && decomposer == null)
            throw new ArgumentNullException(nameof(decomposer));

        DecompositionProfile profile = substrate.Profile;
        float initialMass = substrate.DryMassKg;
        if (deltaTimeDays <= 0f || initialMass <= 0f)
            return EmptyDecompositionResult(substrate, initialMass, 0f);
        if (requireFungus && decomposer.BiomassKg <= 0f)
            return EmptyDecompositionResult(substrate, initialMass, 0f);

        float temperatureFactor = TriangularResponse(
            environment.TemperatureC,
            profile.MinimumTemperatureC,
            profile.OptimumTemperatureC,
            profile.MaximumTemperatureC);
        float moistureFactor = TriangularResponse(
            substrate.MoistureFraction,
            profile.MinimumMoisture,
            profile.OptimumMoisture,
            profile.MaximumMoisture);
        float pHFactor = TriangularResponse(
            environment.PH,
            profile.MinimumPH,
            profile.OptimumPH,
            profile.MaximumPH);
        float oxygenFactor = profile.RequiresOxygen
            ? Mathf.Clamp01(environment.OxygenFraction / 0.21f)
            : 1f;

        float fungalFactor = 1f;
        if (decomposer != null && decomposer.BiomassKg > 0f)
        {
            float ratio = decomposer.BiomassKg / Mathf.Max(MinimumPositiveValue, initialMass);
            float saturation = ratio / (ratio + 0.01f);
            fungalFactor = 1f + (decomposer.Definition.DecompositionEnhancement - 1f) * saturation;
        }

        float environmentalFactor =
            temperatureFactor * moistureFactor * pHFactor * oxygenFactor * fungalFactor;
        environmentalFactor = Mathf.Max(0f, environmentalFactor);

        float labileLost = RemoveByFirstOrderRate(
            substrate, DecompositionComponent.LabileOrganicMatter,
            profile.LabileDecayRatePerDay, environmentalFactor, deltaTimeDays);
        float celluloseLost = RemoveByFirstOrderRate(
            substrate, DecompositionComponent.Cellulose,
            profile.CelluloseDecayRatePerDay, environmentalFactor, deltaTimeDays);
        float hemicelluloseLost = RemoveByFirstOrderRate(
            substrate, DecompositionComponent.Hemicellulose,
            profile.HemicelluloseDecayRatePerDay, environmentalFactor, deltaTimeDays);
        float ligninLost = RemoveByFirstOrderRate(
            substrate, DecompositionComponent.Lignin,
            profile.LigninDecayRatePerDay, environmentalFactor, deltaTimeDays);
        float proteinLost = RemoveByFirstOrderRate(
            substrate, DecompositionComponent.Protein,
            profile.ProteinDecayRatePerDay, environmentalFactor, deltaTimeDays);
        float otherLost = RemoveByFirstOrderRate(
            substrate, DecompositionComponent.OtherOrganicMatter,
            profile.OtherOrganicDecayRatePerDay, environmentalFactor, deltaTimeDays);

        float decomposedMass = labileLost + celluloseLost + hemicelluloseLost +
                               ligninLost + proteinLost + otherLost;

        float lostFraction = initialMass > 0f
            ? Mathf.Clamp01(decomposedMass / initialMass)
            : 0f;
        float nitrogenRemovedFromSubstrate = substrate.OrganicNitrogenKg * lostFraction;
        float phosphorusRemovedFromSubstrate = substrate.OrganicPhosphorusKg * lostFraction;
        float nitrogenMineralised = nitrogenRemovedFromSubstrate * profile.NitrogenMineralisationFraction;
        float phosphorusMineralised = phosphorusRemovedFromSubstrate * profile.PhosphorusMineralisationFraction;
        substrate.RemoveOrganicNutrients(nitrogenRemovedFromSubstrate, phosphorusRemovedFromSubstrate);

        environment.Nutrients.Add(
            nitrogenKg: nitrogenMineralised,
            phosphorusKg: phosphorusMineralised);

        float carbonDecomposed = decomposedMass * profile.CarbonMassFraction;
        float co2Produced = carbonDecomposed * profile.RespiredCarbonFraction * CarbonToCarbonDioxideMassRatio;
        float humusFormed = decomposedMass * profile.HumificationFraction;

        return new DecompositionResult(
            initialMass,
            substrate.DryMassKg,
            decomposedMass,
            labileLost,
            celluloseLost,
            hemicelluloseLost,
            ligninLost,
            proteinLost,
            otherLost,
            co2Produced,
            nitrogenMineralised,
            phosphorusMineralised,
            humusFormed,
            environmentalFactor);
    }

    private static float RemoveByFirstOrderRate(
        OrganicSubstrateSample substrate,
        DecompositionComponent component,
        float ratePerDay,
        float environmentalFactor,
        float deltaTimeDays)
    {
        if (ratePerDay <= 0f || environmentalFactor <= 0f || deltaTimeDays <= 0f)
            return 0f;

        float exponent = Mathf.Clamp(ratePerDay * environmentalFactor * deltaTimeDays, 0f, 40f);
        float fractionDecomposed = 1f - Mathf.Exp(-exponent);
        float currentMass = GetComponentMass(substrate, component);
        return substrate.RemoveComponent(component, currentMass * fractionDecomposed);
    }

    private static float GetComponentMass(
        OrganicSubstrateSample substrate,
        DecompositionComponent component)
    {
        switch (component)
        {
            case DecompositionComponent.LabileOrganicMatter: return substrate.LabileMassKg;
            case DecompositionComponent.Cellulose: return substrate.CelluloseMassKg;
            case DecompositionComponent.Hemicellulose: return substrate.HemicelluloseMassKg;
            case DecompositionComponent.Lignin: return substrate.LigninMassKg;
            case DecompositionComponent.Protein: return substrate.ProteinMassKg;
            case DecompositionComponent.OtherOrganicMatter: return substrate.OtherOrganicMassKg;
            default: return 0f;
        }
    }

    private static DecompositionResult EmptyDecompositionResult(
        OrganicSubstrateSample substrate,
        float initialMass,
        float environmentalFactor)
    {
        return new DecompositionResult(
            initialMass, substrate.DryMassKg, 0f,
            0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f, 0f, environmentalFactor);
    }

    // ============================================================
    // NUTRIENT AVAILABILITY AND ADDITION
    // ============================================================

    public void AddNutrients(
        BiologicalEnvironment environment,
        float carbonDioxideKg = 0f,
        float organicCarbonKg = 0f,
        float nitrogenKg = 0f,
        float phosphorusKg = 0f,
        float potassiumKg = 0f)
    {
        if (environment == null)
            throw new ArgumentNullException(nameof(environment));

        environment.Nutrients.Add(
            carbonDioxideKg,
            organicCarbonKg,
            nitrogenKg,
            phosphorusKg,
            potassiumKg);
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    [ContextMenu("Validate Biological Profiles")]
    public void ValidateProfiles()
    {
        EnsureLists();
        int errors = 0;
        int warnings = 0;

        HashSet<string> organismIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (OrganismDefinition organism in organisms)
        {
            if (organism == null)
            {
                Debug.LogError("Biological: null organism definition found.", this);
                errors++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(organism.Id) || !organismIds.Add(organism.Id))
            {
                Debug.LogError("Biological: empty or duplicate organism ID '" + organism.Id + "'.", this);
                errors++;
            }

            float nutrientFractions = organism.CarbonMassFraction +
                                      organism.NitrogenMassFraction +
                                      organism.PhosphorusMassFraction +
                                      organism.PotassiumMassFraction;
            if (nutrientFractions > 1f)
            {
                Debug.LogWarning("Biological: nutrient mass fractions exceed 1 for '" + organism.DisplayName + "'.", this);
                warnings++;
            }
            if (organism.MaximumBiomassKgPerM2 <= 0f)
            {
                Debug.LogWarning("Biological: carrying capacity is zero for '" + organism.DisplayName + "'; growth will be disabled.", this);
                warnings++;
            }
        }

        HashSet<string> substrateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DecompositionProfile profile in decompositionProfiles)
        {
            if (profile == null)
            {
                Debug.LogError("Biological: null decomposition profile found.", this);
                errors++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(profile.MaterialId) || !substrateIds.Add(profile.MaterialId))
            {
                Debug.LogError("Biological: empty or duplicate substrate ID '" + profile.MaterialId + "'.", this);
                errors++;
            }

            if (Mathf.Abs(profile.CompositionFractionSum - 1f) > 0.02f)
            {
                Debug.LogError("Biological: dry-mass fractions for '" + profile.DisplayName + "' do not sum to 1.0.", this);
                errors++;
            }
            if (profile.CarbonMassFraction + profile.NitrogenMassFraction + profile.PhosphorusMassFraction > 1f)
            {
                Debug.LogWarning("Biological: elemental C/N/P fractions exceed 1 for '" + profile.DisplayName + "'.", this);
                warnings++;
            }
        }

        Debug.Log("Biological validation completed. Errors: " + errors + ". Warnings: " + warnings + ".", this);
    }

    // ============================================================
    // INTERNAL HELPERS
    // ============================================================

    private void EnsureLists()
    {
        if (organisms == null)
            organisms = new List<OrganismDefinition>();
        if (decompositionProfiles == null)
            decompositionProfiles = new List<DecompositionProfile>();
    }

    private void MarkAssetDirty()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private static float TriangularResponse(
        float value,
        float minimum,
        float optimum,
        float maximum)
    {
        if (value < minimum || value > maximum)
            return 0f;
        if (Mathf.Abs(value - optimum) < 0.000001f)
            return 1f;
        if (value < optimum)
        {
            float width = optimum - minimum;
            return width <= 0f ? 1f : Mathf.Clamp01((value - minimum) / width);
        }

        float upperWidth = maximum - optimum;
        return upperWidth <= 0f ? 1f : Mathf.Clamp01((maximum - value) / upperWidth);
    }

    private static float SaturationResponse(float value, float halfSaturation)
    {
        if (value <= 0f)
            return 0f;
        if (halfSaturation <= 0f)
            return 1f;
        return value / (value + halfSaturation);
    }

    private static float BiomassSupportedBy(float nutrientMassKg, float massFraction)
    {
        if (massFraction <= 0f)
            return float.PositiveInfinity;
        return Mathf.Max(0f, nutrientMassKg) / massFraction;
    }

    private static void UpdateMinimumSupport(
        float support,
        NutrientType nutrient,
        ref float minimum,
        ref NutrientType limiting)
    {
        if (support < minimum)
        {
            minimum = support;
            limiting = nutrient;
        }
    }

    private static void ValidateResponseRange(
        float minimum,
        float optimum,
        float maximum,
        string parameterName)
    {
        if (!IsFinite(minimum) || !IsFinite(optimum) || !IsFinite(maximum) ||
            minimum > optimum || optimum > maximum || minimum == maximum)
        {
            throw new ArgumentException(
                "Response values must be finite and satisfy minimum <= optimum <= maximum, with a non-zero range.",
                parameterName);
        }
    }

    private static void ValidatePositive(float value, string parameterName)
    {
        if (value <= 0f || !IsFinite(value))
            throw new ArgumentOutOfRangeException(parameterName, "Value must be finite and greater than zero.");
    }

    private static void ValidateNonNegative(
        float value,
        string parameterName,
        bool allowUnknown = false)
    {
        if (!IsFinite(value))
            throw new ArgumentOutOfRangeException(parameterName, "Value must be finite.");
        if (allowUnknown && value == -1f)
            return;
        if (value < 0f)
            throw new ArgumentOutOfRangeException(parameterName, "Value must be non-negative.");
    }

    private static void ValidateUnitInterval(float value, string parameterName)
    {
        if (!IsFinite(value) || value < 0f || value > 1f)
            throw new ArgumentOutOfRangeException(parameterName, "Value must be between 0 and 1.");
    }

    private static void ValidateRange(
        float value,
        float minimum,
        float maximum,
        string parameterName)
    {
        if (!IsFinite(value) || value < minimum || value > maximum)
            throw new ArgumentOutOfRangeException(parameterName);
    }

    private static void ValidateTemperature(float temperatureC, string parameterName)
    {
        if (!IsFinite(temperatureC) || temperatureC < -273.15f)
            throw new ArgumentOutOfRangeException(parameterName, "Temperature must be finite and cannot be below absolute zero.");
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
}
