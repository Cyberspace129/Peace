using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParticleChemistry
{
    /// <summary>
    /// Reusable particle definitions and chemistry simulation data.
    ///
    /// This asset stores definitions. Runtime objects such as ChemicalMixture,
    /// CorrosionState and ReactionResult represent changing simulation state.
    ///
    /// The chemistry models are engineering/game-simulation approximations.
    /// They are not a quantum-chemistry or industrial process simulator.
    /// </summary>
    [CreateAssetMenu(fileName = "Chemicals", menuName = "Particle Physics/Chemicals")]
    public class Chemicals : ScriptableObject
    {
        // ============================================================
        // QUARK FLAVOUR DEFINITIONS
        // ============================================================

        [Serializable]
        public class QuarkFlavorDefinition
        {
            private const int ChargeDenominator = 3;

            [SerializeField] private char symbol;
            [SerializeField] private int generation;
            [SerializeField] private int chargeNumerator;
            [SerializeField] private float massMeVPerC2;

            public char Symbol => symbol;
            public int Generation => generation;
            public int ChargeNumerator => chargeNumerator;
            public int ChargeDenominatorValue => ChargeDenominator;
            public float MassMeVPerC2 => massMeVPerC2;
            public float ElectricCharge => (float)chargeNumerator / ChargeDenominator;

            public QuarkFlavorDefinition(char symbol, int generation,
                int chargeNumerator, float massMeVPerC2)
            {
                this.symbol = symbol;
                this.generation = generation;
                this.chargeNumerator = chargeNumerator;
                this.massMeVPerC2 = massMeVPerC2;
            }
        }

        [SerializeField]
        private List<QuarkFlavorDefinition> flavors = new List<QuarkFlavorDefinition>
        {
            new QuarkFlavorDefinition('u', 1, +2, 2.16f),
            new QuarkFlavorDefinition('d', 1, -1, 4.70f),
            new QuarkFlavorDefinition('c', 2, +2, 1272.9f),
            new QuarkFlavorDefinition('s', 2, -1, 92.9f),
            new QuarkFlavorDefinition('t', 3, +2, 172600f),
            new QuarkFlavorDefinition('b', 3, -1, 4186f)
        };

        public IReadOnlyList<QuarkFlavorDefinition> Flavors => flavors;

        public QuarkFlavorDefinition GetFlavor(char symbol)
        {
            foreach (QuarkFlavorDefinition flavor in flavors)
            {
                if (flavor != null && flavor.Symbol == symbol)
                    return flavor;
            }

            throw new ArgumentException("Unknown quark flavour: '" + symbol + "'.", nameof(symbol));
        }

        public Quark CreateQuark(char flavorSymbol, ColorCharge color, bool isAntiquark = false)
        {
            return new Quark(GetFlavor(flavorSymbol), color, isAntiquark);
        }

        // ============================================================
        // QUARK COLOUR CHARGE AND PARTICLE BASE TYPES
        // ============================================================

        /// <summary>QCD colour labels. These are not visible RGB colours.</summary>
        public enum ColorCharge { Red, Green, Blue }

        [Serializable]
        public abstract class Particle
        {
            protected int chargeThirds;
            protected float massMeVPerC2;
            protected float spin;
            protected int baryonNumberThirds;

            public int ChargeThirds => chargeThirds;
            public float ElectricCharge => chargeThirds / 3.0f;
            public float MassMeVPerC2 => massMeVPerC2;
            public float Spin => spin;
            public float BaryonNumber => baryonNumberThirds / 3.0f;

            protected Particle(int chargeThirds, float massMeVPerC2,
                float spin, int baryonNumberThirds)
            {
                this.chargeThirds = chargeThirds;
                this.massMeVPerC2 = massMeVPerC2;
                this.spin = spin;
                this.baryonNumberThirds = baryonNumberThirds;
            }
        }

        [Serializable]
        public class Quark : Particle
        {
            private const float QuarkSpin = 0.5f;
            [SerializeField] private QuarkFlavorDefinition flavor;
            [SerializeField] private ColorCharge color;
            [SerializeField] private bool isAntiquark;

            public QuarkFlavorDefinition Flavor => flavor;
            public ColorCharge Color => color;
            public bool IsAntiquark => isAntiquark;
            public string ColorLabel => isAntiquark ? "Anti-" + color : color.ToString();

            public Quark(QuarkFlavorDefinition flavor, ColorCharge color, bool isAntiquark = false)
                : base(GetCharge(flavor, isAntiquark), GetMass(flavor), QuarkSpin,
                    isAntiquark ? -1 : +1)
            {
                this.flavor = flavor;
                this.color = color;
                this.isAntiquark = isAntiquark;
            }

            private static int GetCharge(QuarkFlavorDefinition definition, bool anti)
            {
                if (definition == null)
                    throw new ArgumentNullException(nameof(definition));
                return anti ? -definition.ChargeNumerator : definition.ChargeNumerator;
            }

            private static float GetMass(QuarkFlavorDefinition definition)
            {
                if (definition == null)
                    throw new ArgumentNullException(nameof(definition));
                return definition.MassMeVPerC2;
            }
        }

        [Serializable]
        public class Gluon : Particle
        {
            private const float GluonSpin = 1.0f;
            [SerializeField] private ColorCharge color;
            [SerializeField] private ColorCharge anticolor;

            public ColorCharge Color => color;
            public ColorCharge Anticolor => anticolor;
            public string ColorFlow => color + " -> Anti-" + anticolor;

            public Gluon(ColorCharge color, ColorCharge anticolor)
                : base(0, 0f, GluonSpin, 0)
            {
                this.color = color;
                this.anticolor = anticolor;
            }
        }

        [Serializable]
        public class Electron : Particle
        {
            public const float ElectronMassMeVPerC2 = 0.51099895f;
            public Electron() : base(-3, ElectronMassMeVPerC2, 0.5f, 0) { }
        }

        [Serializable]
        public abstract class Hadron : Particle
        {
            protected Hadron(int chargeThirds, float massMeVPerC2,
                float spin, int baryonNumberThirds)
                : base(chargeThirds, massMeVPerC2, spin, baryonNumberThirds) { }
        }

        [Serializable]
        public class Baryon : Hadron
        {
            private readonly Quark[] quarks;
            public IReadOnlyList<Quark> Quarks => quarks;

            public Baryon(float massMeVPerC2, float spin, params Quark[] quarks)
                : base(CalculateCharge(quarks), massMeVPerC2, spin,
                    CalculateBaryonNumber(quarks))
            {
                if (!HasOneOfEachColor(quarks))
                    throw new ArgumentException("A baryon requires three quarks with one red, one green and one blue colour label.");
                this.quarks = (Quark[])quarks.Clone();
            }

            private static void ValidateQuarkArray(Quark[] values)
            {
                if (values == null || values.Length != 3)
                    throw new ArgumentException("A baryon must contain exactly three quarks.");
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] == null)
                        throw new ArgumentException("A baryon cannot contain a null quark.");
                    if (values[i].IsAntiquark)
                        throw new ArgumentException("A baryon must contain quarks, not antiquarks.");
                }
            }

            private static int CalculateCharge(Quark[] values)
            {
                ValidateQuarkArray(values);
                return values[0].ChargeThirds + values[1].ChargeThirds + values[2].ChargeThirds;
            }

            private static int CalculateBaryonNumber(Quark[] values)
            {
                ValidateQuarkArray(values);
                // ValidateQuarkArray rejects antiquarks, so three quarks
                // contribute +1 baryon number in thirds.
                return 3;
            }

            private static bool HasOneOfEachColor(Quark[] values)
            {
                if (values == null || values.Length != 3) return false;
                bool red = false, green = false, blue = false;
                foreach (Quark q in values)
                {
                    if (q == null || q.IsAntiquark) return false;
                    switch (q.Color)
                    {
                        case ColorCharge.Red: if (red) return false; red = true; break;
                        case ColorCharge.Green: if (green) return false; green = true; break;
                        case ColorCharge.Blue: if (blue) return false; blue = true; break;
                    }
                }
                return red && green && blue;
            }
        }

        [Serializable]
        public class Meson : Hadron
        {
            private readonly Quark quark;
            private readonly Quark antiquark;
            public Quark QuarkPart => quark;
            public Quark AntiquarkPart => antiquark;

            public Meson(float massMeVPerC2, float spin, Quark quark, Quark antiquark)
                : base(CalculateCharge(quark, antiquark), massMeVPerC2, spin, 0)
            {
                if (quark.IsAntiquark || !antiquark.IsAntiquark)
                    throw new ArgumentException("A meson requires one quark followed by one antiquark.");
                if (quark.Color != antiquark.Color)
                    throw new ArgumentException("The quark colour and antiquark anticolour labels must match in this simplified model.");
                this.quark = quark;
                this.antiquark = antiquark;
            }

            private static int CalculateCharge(Quark q, Quark anti)
            {
                if (q == null) throw new ArgumentNullException(nameof(q));
                if (anti == null) throw new ArgumentNullException(nameof(anti));
                return q.ChargeThirds + anti.ChargeThirds;
            }
        }

        [Serializable]
        public class Proton : Baryon
        {
            public const float ProtonMassMeVPerC2 = 938.272f;
            public Proton(Chemicals chemicals)
                : base(ProtonMassMeVPerC2, 0.5f,
                    RequireChemicals(chemicals).CreateQuark('u', ColorCharge.Red),
                    chemicals.CreateQuark('u', ColorCharge.Green),
                    chemicals.CreateQuark('d', ColorCharge.Blue)) { }
        }

        [Serializable]
        public class Neutron : Baryon
        {
            public const float NeutronMassMeVPerC2 = 939.565f;
            public Neutron(Chemicals chemicals)
                : base(NeutronMassMeVPerC2, 0.5f,
                    RequireChemicals(chemicals).CreateQuark('u', ColorCharge.Red),
                    chemicals.CreateQuark('d', ColorCharge.Green),
                    chemicals.CreateQuark('d', ColorCharge.Blue)) { }
        }

        private static Chemicals RequireChemicals(Chemicals chemicals)
        {
            if (chemicals == null) throw new ArgumentNullException(nameof(chemicals));
            return chemicals;
        }

        [Serializable]
        public class Atomic : Particle
        {
            private readonly List<Proton> protons;
            private readonly List<Neutron> neutrons;
            private readonly List<Electron> electrons;

            public IReadOnlyList<Proton> Protons => protons;
            public IReadOnlyList<Neutron> Neutrons => neutrons;
            public IReadOnlyList<Electron> Electrons => electrons;
            public int AtomicNumber => protons.Count;
            public int MassNumber => protons.Count + neutrons.Count;
            public int NetChargeThirds => ChargeThirds;

            // Existing constructor retained for compatibility with code that
            // uses atom counts only. Nucleon instances are not generated here.
            public Atomic(int protonCount, int neutronCount, int electronCount)
                : this(null, protonCount, neutronCount, electronCount) { }

            // This overload creates actual Proton and Neutron objects.
            public Atomic(Chemicals chemicals, int protonCount, int neutronCount, int electronCount)
                : base(CalculateCharge(protonCount, electronCount),
                    CalculateApproximateMass(protonCount, neutronCount, electronCount),
                    0f, (protonCount + neutronCount) * 3)
            {
                ValidateCounts(protonCount, neutronCount, electronCount);
                protons = new List<Proton>(protonCount);
                neutrons = new List<Neutron>(neutronCount);
                electrons = new List<Electron>(electronCount);

                for (int i = 0; i < protonCount; i++)
                    protons.Add(chemicals != null ? new Proton(chemicals) : null);
                for (int i = 0; i < neutronCount; i++)
                    neutrons.Add(chemicals != null ? new Neutron(chemicals) : null);
                for (int i = 0; i < electronCount; i++)
                    electrons.Add(new Electron());
            }

            private static void ValidateCounts(int p, int n, int e)
            {
                if (p < 0 || n < 0 || e < 0)
                    throw new ArgumentOutOfRangeException("Particle counts cannot be negative.");
            }

            private static int CalculateCharge(int p, int e)
            {
                ValidateCounts(p, 0, e);
                return (p - e) * 3;
            }

            private static float CalculateApproximateMass(int p, int n, int e)
            {
                ValidateCounts(p, n, e);
                return p * Proton.ProtonMassMeVPerC2
                    + n * Neutron.NeutronMassMeVPerC2
                    + e * Electron.ElectronMassMeVPerC2;
            }
        }

        // ============================================================
        // CHEMISTRY ENUMERATIONS
        // ============================================================

        public enum ChemicalPhase { Unknown, Solid, Liquid, Gas, Aqueous }
        public enum AcidBaseBehavior { Neutral, Acid, Base, Amphoteric }
        public enum SolutionAcidity { Acidic, Neutral, Alkaline }
        public enum ChemicalReactionType
        {
            General, Synthesis, Decomposition, Combustion, AcidBaseNeutralization,
            Oxidation, Reduction, Redox, Displacement, Precipitation, Polymerization
        }
        public enum ChemicalAgentType
        {
            Water, Acid, Alkali, Oxidizer, Reducer, Salt, OrganicSolvent,
            InorganicSolvent, Fuel, Biological, Other
        }
        public enum CorrosionMechanism
        {
            Uniform, Galvanic, Pitting, Crevice, Intergranular,
            StressCorrosion, ErosionCorrosion
        }
        public enum ContaminationSeverity { None, Trace, Low, Moderate, High, Critical }

        // ============================================================
        // CHEMICAL SPECIES DEFINITIONS
        // ============================================================

        [Serializable]
        public class ElementalComposition
        {
            [SerializeField] private string elementSymbol;
            [SerializeField, Min(0)] private int atomCount;
            public string ElementSymbol => elementSymbol;
            public int AtomCount => atomCount;
            public ElementalComposition() { elementSymbol = string.Empty; atomCount = 0; }

            public ElementalComposition(string symbol, int count)
            {
                elementSymbol = symbol;
                atomCount = count;
            }
        }

        [Serializable]
        public class OxidationStateEntry
        {
            [SerializeField] private string elementSymbol;
            [SerializeField] private int oxidationState;
            public string ElementSymbol => elementSymbol;
            public int OxidationState => oxidationState;
            public OxidationStateEntry() { elementSymbol = string.Empty; oxidationState = 0; }

            public OxidationStateEntry(string symbol, int state)
            {
                elementSymbol = symbol;
                oxidationState = state;
            }
        }

        /// <summary>Solubility and dissolution kinetics for a species in one solvent.</summary>
        [Serializable]
        public class SolubilityProfile
        {
            [SerializeField] private string solventId = "water";
            [SerializeField, Min(0f)] private float maximumSolubilityMolPerLitre;
            [SerializeField, Min(0f)] private float surfaceDissolutionRateMolPerSquareMetreSecond;
            [SerializeField, Min(0f)] private float activationEnergyJPerMol;
            [SerializeField] private float temperatureCoefficientPerKelvin;
            [SerializeField] private float pHEffectPerUnit;
            [SerializeField] private float referenceTemperatureKelvin = 298.15f;

            public string SolventId => solventId;
            public float MaximumSolubilityMolPerLitre => maximumSolubilityMolPerLitre;
            public float SurfaceDissolutionRateMolPerSquareMetreSecond => surfaceDissolutionRateMolPerSquareMetreSecond;
            public float ActivationEnergyJPerMol => activationEnergyJPerMol;

            public SolubilityProfile()
            {
                solventId = "water";
                maximumSolubilityMolPerLitre = 0f;
                surfaceDissolutionRateMolPerSquareMetreSecond = 0f;
                activationEnergyJPerMol = 0f;
                temperatureCoefficientPerKelvin = 0f;
                pHEffectPerUnit = 0f;
                referenceTemperatureKelvin = 298.15f;
            }

            public SolubilityProfile(string solventId, float maximumSolubilityMolPerLitre,
                float surfaceRateMolPerM2Second, float activationEnergyJPerMol = 0f,
                float temperatureCoefficientPerKelvin = 0f, float pHEffectPerUnit = 0f,
                float referenceTemperatureKelvin = 298.15f)
            {
                this.solventId = solventId;
                this.maximumSolubilityMolPerLitre = Mathf.Max(0f, maximumSolubilityMolPerLitre);
                surfaceDissolutionRateMolPerSquareMetreSecond = Mathf.Max(0f, surfaceRateMolPerM2Second);
                this.activationEnergyJPerMol = Mathf.Max(0f, activationEnergyJPerMol);
                this.temperatureCoefficientPerKelvin = temperatureCoefficientPerKelvin;
                this.pHEffectPerUnit = pHEffectPerUnit;
                this.referenceTemperatureKelvin = Mathf.Max(1f, referenceTemperatureKelvin);
            }

            public float CalculateSolubility(float temperatureKelvin, float pH)
            {
                float temperatureDelta = temperatureKelvin - referenceTemperatureKelvin;
                double temperatureFactor = Math.Exp(temperatureCoefficientPerKelvin * temperatureDelta);
                double pHFactor = Math.Pow(10.0, pHEffectPerUnit * Math.Abs(pH - 7f));
                double result = maximumSolubilityMolPerLitre * temperatureFactor * pHFactor;
                if (double.IsNaN(result) || double.IsInfinity(result)) return 0f;
                return Mathf.Max(0f, (float)result);
            }

            public float CalculateDissolutionRate(float temperatureKelvin)
            {
                const double GasConstant = 8.314462618;
                if (temperatureKelvin <= 0f) return 0f;
                // The configured surface rate is the rate at the reference
                // temperature, so normalize the Arrhenius factor to that point.
                double exponent = (activationEnergyJPerMol / GasConstant)
                    * (1.0 / referenceTemperatureKelvin - 1.0 / temperatureKelvin);
                double rate = surfaceDissolutionRateMolPerSquareMetreSecond * Math.Exp(exponent);
                if (double.IsNaN(rate) || double.IsInfinity(rate)) return 0f;
                return Mathf.Max(0f, (float)rate);
            }
        }

        /// <summary>
        /// A chemical species is a defined substance/ion/molecule, not a material.
        /// Materials such as steel or leaf tissue can contain many species.
        /// </summary>
        [Serializable]
        public class ChemicalSpeciesDefinition
        {
            [SerializeField] private string id;
            [SerializeField] private string displayName;
            [SerializeField] private string chemicalFormula;
            [SerializeField, Min(0f)] private float molarMassGramsPerMol;
            [SerializeField] private ChemicalPhase phase;
            [SerializeField] private AcidBaseBehavior acidBaseBehavior;
            [SerializeField] private float pKa;
            [SerializeField] private float pKb;
            [SerializeField] private int formalCharge;
            [SerializeField] private bool consideredContaminant;
            [SerializeField] private List<ElementalComposition> elementalComposition = new List<ElementalComposition>();
            [SerializeField] private List<OxidationStateEntry> oxidationStates = new List<OxidationStateEntry>();
            [SerializeField] private List<SolubilityProfile> solubilityProfiles = new List<SolubilityProfile>();

            public string Id => id;
            public string DisplayName => displayName;
            public string ChemicalFormula => chemicalFormula;
            public float MolarMassGramsPerMol => molarMassGramsPerMol;
            public ChemicalPhase Phase => phase;
            public AcidBaseBehavior AcidBase => acidBaseBehavior;
            public float PKa => pKa;
            public float PKb => pKb;
            public int FormalCharge => formalCharge;
            public bool ConsideredContaminant => consideredContaminant;
            public IReadOnlyList<ElementalComposition> ElementalComposition => elementalComposition;
            public IReadOnlyList<OxidationStateEntry> OxidationStates => oxidationStates;
            public IReadOnlyList<SolubilityProfile> SolubilityProfiles => solubilityProfiles;

            public ChemicalSpeciesDefinition()
            {
                id = string.Empty;
                displayName = string.Empty;
                chemicalFormula = string.Empty;
                molarMassGramsPerMol = 0f;
                phase = ChemicalPhase.Unknown;
                acidBaseBehavior = AcidBaseBehavior.Neutral;
                pKa = 0f;
                pKb = 0f;
                formalCharge = 0;
                consideredContaminant = false;
                elementalComposition = new List<ElementalComposition>();
                oxidationStates = new List<OxidationStateEntry>();
                solubilityProfiles = new List<SolubilityProfile>();
            }

            public ChemicalSpeciesDefinition(string id, string displayName, string formula,
                float molarMassGramsPerMol, ChemicalPhase phase = ChemicalPhase.Unknown,
                AcidBaseBehavior acidBase = AcidBaseBehavior.Neutral, int formalCharge = 0)
            {
                this.id = id;
                this.displayName = displayName;
                chemicalFormula = formula;
                this.molarMassGramsPerMol = Mathf.Max(0f, molarMassGramsPerMol);
                this.phase = phase;
                acidBaseBehavior = acidBase;
                this.formalCharge = formalCharge;
                pKa = 0f;
                pKb = 0f;
                consideredContaminant = false;
                elementalComposition = new List<ElementalComposition>();
                oxidationStates = new List<OxidationStateEntry>();
                solubilityProfiles = new List<SolubilityProfile>();
            }

            public int GetAtomCount(string elementSymbol)
            {
                foreach (ElementalComposition item in elementalComposition)
                    if (item != null && string.Equals(item.ElementSymbol, elementSymbol, StringComparison.OrdinalIgnoreCase))
                        return item.AtomCount;
                return 0;
            }

            public bool TryGetOxidationState(string elementSymbol, out int oxidationState)
            {
                foreach (OxidationStateEntry item in oxidationStates)
                {
                    if (item != null && string.Equals(item.ElementSymbol, elementSymbol, StringComparison.OrdinalIgnoreCase))
                    {
                        oxidationState = item.OxidationState;
                        return true;
                    }
                }
                oxidationState = 0;
                return false;
            }

            public void AddElementalComposition(string symbol, int atomCount)
            {
                if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Element symbol is required.");
                if (atomCount <= 0) throw new ArgumentOutOfRangeException(nameof(atomCount));
                elementalComposition.Add(new ElementalComposition(symbol, atomCount));
            }

            public void AddOxidationState(string symbol, int oxidationState)
            {
                if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Element symbol is required.");
                oxidationStates.Add(new OxidationStateEntry(symbol, oxidationState));
            }

            public void AddSolubilityProfile(SolubilityProfile profile)
            {
                if (profile == null) throw new ArgumentNullException(nameof(profile));
                solubilityProfiles.Add(profile);
            }

            public void SetAcidBaseConstants(float pKa, float pKb)
            {
                this.pKa = pKa;
                this.pKb = pKb;
            }

            public void SetConsideredContaminant(bool value)
            {
                consideredContaminant = value;
            }

            public SolubilityProfile GetSolubilityProfile(string solventId)
            {
                foreach (SolubilityProfile profile in solubilityProfiles)
                    if (profile != null && string.Equals(profile.SolventId, solventId, StringComparison.OrdinalIgnoreCase))
                        return profile;
                return null;
            }
        }

        [SerializeField] private List<ChemicalSpeciesDefinition> species = new List<ChemicalSpeciesDefinition>();
        public IReadOnlyList<ChemicalSpeciesDefinition> Species => species;

        public ChemicalSpeciesDefinition GetSpecies(string speciesId)
        {
            if (string.IsNullOrWhiteSpace(speciesId))
                throw new ArgumentException("A species ID is required.", nameof(speciesId));
            foreach (ChemicalSpeciesDefinition item in species)
                if (item != null && string.Equals(item.Id, speciesId, StringComparison.OrdinalIgnoreCase))
                    return item;
            throw new KeyNotFoundException("No chemical species is registered with ID '" + speciesId + "'.");
        }

        public bool TryGetSpecies(string speciesId, out ChemicalSpeciesDefinition definition)
        {
            foreach (ChemicalSpeciesDefinition item in species)
            {
                if (item != null && string.Equals(item.Id, speciesId, StringComparison.OrdinalIgnoreCase))
                {
                    definition = item;
                    return true;
                }
            }
            definition = null;
            return false;
        }

        // ============================================================
        // CHEMICAL ENVIRONMENT AND MIXTURES
        // ============================================================

        /// <summary>Conditions that influence reactions, dissolution and corrosion.</summary>
        [Serializable]
        public class ChemicalEnvironment
        {
            [SerializeField, Min(1f)] private float temperatureKelvin = 298.15f;
            [SerializeField, Range(0f, 14f)] private float pH = 7f;
            [SerializeField, Range(0f, 1f)] private float relativeHumidity;
            [SerializeField, Range(0f, 1f)] private float oxygenActivity = 0.21f;
            [SerializeField] private ChemicalAgentType primaryAgent = ChemicalAgentType.Water;
            [SerializeField, Min(0f)] private float agentConcentrationMolPerLitre;

            public float TemperatureKelvin => temperatureKelvin;
            public float PH => pH;
            public float RelativeHumidity => relativeHumidity;
            public float OxygenActivity => oxygenActivity;
            public ChemicalAgentType PrimaryAgent => primaryAgent;
            public float AgentConcentrationMolPerLitre => agentConcentrationMolPerLitre;

            public ChemicalEnvironment(float temperatureKelvin = 298.15f, float pH = 7f,
                float relativeHumidity = 0f, float oxygenActivity = 0.21f,
                ChemicalAgentType primaryAgent = ChemicalAgentType.Water,
                float agentConcentrationMolPerLitre = 0f)
            {
                this.temperatureKelvin = Mathf.Max(1f, temperatureKelvin);
                this.pH = Mathf.Clamp(pH, 0f, 14f);
                this.relativeHumidity = Mathf.Clamp01(relativeHumidity);
                this.oxygenActivity = Mathf.Clamp01(oxygenActivity);
                this.primaryAgent = primaryAgent;
                this.agentConcentrationMolPerLitre = Mathf.Max(0f, agentConcentrationMolPerLitre);
            }
        }

        [Serializable]
        public class SpeciesAmount
        {
            [SerializeField] private string speciesId;
            [SerializeField] private double amountMoles;
            public string SpeciesId => speciesId;
            public double AmountMoles => amountMoles;

            public SpeciesAmount() { speciesId = string.Empty; amountMoles = 0.0; }

            public SpeciesAmount(string speciesId, double amountMoles)
            {
                this.speciesId = speciesId;
                this.amountMoles = Math.Max(0.0, amountMoles);
            }

            internal void SetAmount(double value) { amountMoles = Math.Max(0.0, value); }
        }

        /// <summary>A runtime mixture containing amounts of defined chemical species.</summary>
        [Serializable]
        public class ChemicalMixture
        {
            [SerializeField, Min(0.000001f)] private float volumeLitres = 1f;
            [SerializeField] private List<SpeciesAmount> contents = new List<SpeciesAmount>();

            public float VolumeLitres => volumeLitres;
            public IReadOnlyList<SpeciesAmount> Contents => contents;

            public ChemicalMixture(float volumeLitres)
            {
                if (volumeLitres <= 0f) throw new ArgumentOutOfRangeException(nameof(volumeLitres));
                this.volumeLitres = volumeLitres;
                contents = new List<SpeciesAmount>();
            }

            public double GetMoles(string speciesId)
            {
                SpeciesAmount item = FindAmount(speciesId);
                return item == null ? 0.0 : item.AmountMoles;
            }

            public double GetConcentrationMolPerLitre(string speciesId)
            {
                return GetMoles(speciesId) / volumeLitres;
            }

            public void AddMoles(string speciesId, double moles)
            {
                if (string.IsNullOrWhiteSpace(speciesId)) throw new ArgumentException("A species ID is required.");
                if (double.IsNaN(moles) || double.IsInfinity(moles) || moles < 0.0)
                    throw new ArgumentOutOfRangeException(nameof(moles));
                SpeciesAmount existing = FindAmount(speciesId);
                if (existing == null) contents.Add(new SpeciesAmount(speciesId, moles));
                else existing.SetAmount(existing.AmountMoles + moles);
            }

            public bool RemoveMoles(string speciesId, double moles)
            {
                if (moles < 0.0 || double.IsNaN(moles) || double.IsInfinity(moles))
                    throw new ArgumentOutOfRangeException(nameof(moles));
                SpeciesAmount existing = FindAmount(speciesId);
                if (existing == null || existing.AmountMoles + 1e-12 < moles) return false;
                existing.SetAmount(existing.AmountMoles - moles);
                return true;
            }

            private SpeciesAmount FindAmount(string speciesId)
            {
                foreach (SpeciesAmount item in contents)
                    if (item != null && string.Equals(item.SpeciesId, speciesId, StringComparison.OrdinalIgnoreCase))
                        return item;
                return null;
            }
        }

        // ============================================================
        // ACIDITY AND ALKALINITY
        // ============================================================

        /// <summary>Calculates pH from hydrogen-ion concentration in mol/L.</summary>
        public static float CalculatePH(double hydrogenIonConcentrationMolPerLitre)
        {
            if (hydrogenIonConcentrationMolPerLitre <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(hydrogenIonConcentrationMolPerLitre),
                    "Hydrogen-ion concentration must be greater than zero.");
            return (float)-Math.Log10(hydrogenIonConcentrationMolPerLitre);
        }

        /// <summary>Calculates pOH assuming water's pKw is 14 at approximately 25 C.</summary>
        public static float CalculatePOH(float pH) { return 14f - pH; }

        /// <summary>Classifies a solution using the conventional pH=7 neutral point at approximately 25 C.</summary>
        public static SolutionAcidity ClassifyPH(float pH)
        {
            if (pH < 7f) return SolutionAcidity.Acidic;
            if (pH > 7f) return SolutionAcidity.Alkaline;
            return SolutionAcidity.Neutral;
        }

        /// <summary>Calculates hydroxide concentration using pKw = 14 at approximately 25 C.</summary>
        public static double CalculateHydroxideConcentration(float pH)
        {
            return Math.Pow(10.0, -(14.0 - pH));
        }

        /// <summary>Henderson-Hasselbalch approximation for a weak-acid/conjugate-base buffer.</summary>
        public static float CalculateBufferPH(float pKa, double baseConcentration, double acidConcentration)
        {
            if (baseConcentration <= 0.0 || acidConcentration <= 0.0)
                throw new ArgumentOutOfRangeException("Both buffer concentrations must be greater than zero.");
            return pKa + (float)Math.Log10(baseConcentration / acidConcentration);
        }

        /// <summary>
        /// Estimates pH from a specified hydrogen-ion species in a mixture.
        /// This treats concentration as activity, so it is an approximation for
        /// dilute solutions rather than a full electrolyte-activity calculation.
        /// </summary>
        public static float CalculateMixturePH(ChemicalMixture mixture,
            string hydrogenIonSpeciesId = "hydrogen-ion")
        {
            if (mixture == null) throw new ArgumentNullException(nameof(mixture));
            return CalculatePH(mixture.GetConcentrationMolPerLitre(hydrogenIonSpeciesId));
        }

        // ============================================================
        // CHEMICAL REACTIONS AND REACTION RATES
        // ============================================================

        [Serializable]
        public class ReactionParticipant
        {
            [SerializeField] private string speciesId;
            [SerializeField, Min(1)] private int stoichiometricCoefficient = 1;
            [SerializeField] private float reactionOrder = -1f;
            public string SpeciesId => speciesId;
            public int StoichiometricCoefficient => stoichiometricCoefficient;
            public float ReactionOrder => reactionOrder < 0f ? stoichiometricCoefficient : reactionOrder;

            public ReactionParticipant() { speciesId = string.Empty; stoichiometricCoefficient = 1; reactionOrder = -1f; }

            public ReactionParticipant(string speciesId, int coefficient, float reactionOrder = -1f)
            {
                if (coefficient <= 0) throw new ArgumentOutOfRangeException(nameof(coefficient));
                this.speciesId = speciesId;
                stoichiometricCoefficient = coefficient;
                this.reactionOrder = reactionOrder;
            }
        }

        [Serializable]
        public class ChemicalReactionDefinition
        {
            [SerializeField] private string id;
            [SerializeField] private string displayName;
            [SerializeField] private ChemicalReactionType reactionType;
            [SerializeField] private List<ReactionParticipant> reactants = new List<ReactionParticipant>();
            [SerializeField] private List<ReactionParticipant> products = new List<ReactionParticipant>();
            [SerializeField, Min(0f)] private float preExponentialFactor;
            [SerializeField, Min(0f)] private float activationEnergyJPerMol;
            [SerializeField] private float enthalpyJPerMolReactionExtent;
            [SerializeField] private bool requireBalancedEquation = true;

            public string Id => id;
            public string DisplayName => displayName;
            public ChemicalReactionType ReactionType => reactionType;
            public IReadOnlyList<ReactionParticipant> Reactants => reactants;
            public IReadOnlyList<ReactionParticipant> Products => products;
            public float PreExponentialFactor => preExponentialFactor;
            public float ActivationEnergyJPerMol => activationEnergyJPerMol;
            public float EnthalpyJPerMolReactionExtent => enthalpyJPerMolReactionExtent;
            public bool RequireBalancedEquation => requireBalancedEquation;

            public ChemicalReactionDefinition()
            {
                id = string.Empty;
                displayName = string.Empty;
                reactionType = ChemicalReactionType.General;
                reactants = new List<ReactionParticipant>();
                products = new List<ReactionParticipant>();
                preExponentialFactor = 0f;
                activationEnergyJPerMol = 0f;
                enthalpyJPerMolReactionExtent = 0f;
                requireBalancedEquation = true;
            }

            public ChemicalReactionDefinition(string id, string displayName, ChemicalReactionType type,
                float preExponentialFactor, float activationEnergyJPerMol,
                float enthalpyJPerMolReactionExtent = 0f)
            {
                this.id = id;
                this.displayName = displayName;
                reactionType = type;
                this.preExponentialFactor = Mathf.Max(0f, preExponentialFactor);
                this.activationEnergyJPerMol = Mathf.Max(0f, activationEnergyJPerMol);
                this.enthalpyJPerMolReactionExtent = enthalpyJPerMolReactionExtent;
                reactants = new List<ReactionParticipant>();
                products = new List<ReactionParticipant>();
                requireBalancedEquation = true;
            }

            public void AddReactant(string speciesId, int coefficient, float reactionOrder = -1f)
            {
                reactants.Add(new ReactionParticipant(speciesId, coefficient, reactionOrder));
            }

            public void AddProduct(string speciesId, int coefficient)
            {
                products.Add(new ReactionParticipant(speciesId, coefficient));
            }

            public void SetRequireBalancedEquation(bool value)
            {
                requireBalancedEquation = value;
            }
        }

        [SerializeField] private List<ChemicalReactionDefinition> reactions = new List<ChemicalReactionDefinition>();
        public IReadOnlyList<ChemicalReactionDefinition> Reactions => reactions;

        public ChemicalReactionDefinition GetReaction(string reactionId)
        {
            foreach (ChemicalReactionDefinition item in reactions)
                if (item != null && string.Equals(item.Id, reactionId, StringComparison.OrdinalIgnoreCase))
                    return item;
            throw new KeyNotFoundException("No reaction is registered with ID '" + reactionId + "'.");
        }

        public bool IsReactionBalanced(ChemicalReactionDefinition reaction)
        {
            if (reaction == null) throw new ArgumentNullException(nameof(reaction));
            if (reaction.Reactants.Count == 0 || reaction.Products.Count == 0) return false;
            Dictionary<string, long> reactantAtoms = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, long> productAtoms = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            long reactantCharge = 0;
            long productCharge = 0;

            AccumulateReactionSide(reaction.Reactants, reactantAtoms, ref reactantCharge);
            AccumulateReactionSide(reaction.Products, productAtoms, ref productCharge);

            // No elemental composition means the equation cannot be checked.
            if (reactantAtoms.Count == 0 || productAtoms.Count == 0) return false;
            if (reactantCharge != productCharge || reactantAtoms.Count != productAtoms.Count) return false;
            foreach (KeyValuePair<string, long> entry in reactantAtoms)
            {
                long productCount;
                if (!productAtoms.TryGetValue(entry.Key, out productCount) || productCount != entry.Value)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Estimates the number of electrons transferred per stoichiometric
        /// reaction extent from the oxidation states entered for each species.
        /// Returns false if any participating element lacks an oxidation state.
        /// A value of zero usually means the reaction is not a redox reaction.
        /// </summary>
        public bool TryCalculateElectronsTransferred(
            ChemicalReactionDefinition reaction, out double electronsTransferred)
        {
            if (reaction == null) throw new ArgumentNullException(nameof(reaction));
            Dictionary<string, double> oxidationTotals = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            if (!AccumulateOxidationTotals(reaction.Reactants, oxidationTotals, -1.0) ||
                !AccumulateOxidationTotals(reaction.Products, oxidationTotals, +1.0))
            {
                electronsTransferred = 0.0;
                return false;
            }

            // The signed sum across all elements should be zero for a valid,
            // charge-balanced redox equation. Positive changes are oxidation.
            double signedTotal = 0.0;
            double oxidationIncrease = 0.0;
            foreach (double change in oxidationTotals.Values)
            {
                signedTotal += change;
                if (change > 0.0) oxidationIncrease += change;
            }

            if (Math.Abs(signedTotal) > 1e-6)
            {
                electronsTransferred = 0.0;
                return false;
            }

            electronsTransferred = oxidationIncrease;
            return true;
        }

        private bool AccumulateOxidationTotals(IReadOnlyList<ReactionParticipant> participants,
            Dictionary<string, double> totals, double direction)
        {
            foreach (ReactionParticipant participant in participants)
            {
                if (participant == null) return false;
                ChemicalSpeciesDefinition definition;
                if (!TryGetSpecies(participant.SpeciesId, out definition)) return false;
                foreach (ElementalComposition element in definition.ElementalComposition)
                {
                    if (element == null) continue;
                    int oxidationState;
                    if (!definition.TryGetOxidationState(element.ElementSymbol, out oxidationState))
                        return false;

                    double change;
                    totals.TryGetValue(element.ElementSymbol, out change);
                    change += direction * participant.StoichiometricCoefficient
                        * element.AtomCount * oxidationState;
                    totals[element.ElementSymbol] = change;
                }
            }
            return true;
        }

        private void AccumulateReactionSide(IReadOnlyList<ReactionParticipant> participants,
            Dictionary<string, long> totals, ref long charge)
        {
            foreach (ReactionParticipant participant in participants)
            {
                if (participant == null) throw new InvalidOperationException("A reaction contains a missing participant.");
                ChemicalSpeciesDefinition definition = GetSpecies(participant.SpeciesId);
                int coefficient = participant.StoichiometricCoefficient;
                charge += (long)definition.FormalCharge * coefficient;
                foreach (ElementalComposition element in definition.ElementalComposition)
                {
                    if (element == null || string.IsNullOrWhiteSpace(element.ElementSymbol)) continue;
                    long current;
                    totals.TryGetValue(element.ElementSymbol, out current);
                    totals[element.ElementSymbol] = current + (long)element.AtomCount * coefficient;
                }
            }
        }

        /// <summary>
        /// Arrhenius rate law: rate = k * product(concentration^reactionOrder).
        /// The pre-exponential factor's units depend on the total reaction order.
        /// Concentrations are in mol/L; returned rate is mol/L/s when A uses
        /// the corresponding units.
        /// </summary>
        public double CalculateReactionRateMolPerLitreSecond(
            ChemicalReactionDefinition reaction, ChemicalMixture mixture, float temperatureKelvin)
        {
            if (reaction == null) throw new ArgumentNullException(nameof(reaction));
            if (mixture == null) throw new ArgumentNullException(nameof(mixture));
            if (temperatureKelvin <= 0f) throw new ArgumentOutOfRangeException(nameof(temperatureKelvin));
            if (reaction.Reactants.Count == 0) return 0.0;
            if (reaction.PreExponentialFactor <= 0f) return 0.0;

            const double GasConstant = 8.314462618;
            double exponent = -reaction.ActivationEnergyJPerMol / (GasConstant * temperatureKelvin);
            double rateConstant = reaction.PreExponentialFactor * Math.Exp(exponent);
            double concentrationProduct = 1.0;

            foreach (ReactionParticipant participant in reaction.Reactants)
            {
                double concentration = mixture.GetConcentrationMolPerLitre(participant.SpeciesId);
                if (concentration <= 0.0) return 0.0;
                concentrationProduct *= Math.Pow(concentration, participant.ReactionOrder);
            }

            double rate = rateConstant * concentrationProduct;
            if (double.IsNaN(rate) || double.IsInfinity(rate) || rate < 0.0) return 0.0;
            return rate;
        }

        public class ReactionResult
        {
            public bool Occurred { get; internal set; }
            public double ReactionExtentMoles { get; internal set; }
            public double RateMolPerLitreSecond { get; internal set; }
            public float HeatJoules { get; internal set; }
            public string LimitingReactantId { get; internal set; }
            public string Message { get; internal set; }
        }

        /// <summary>
        /// Advances one reaction by a time step. This is a single-step explicit
        /// integration, limited by the available reactants. Use smaller time steps
        /// when rates are high or conditions change quickly.
        /// </summary>
        public ReactionResult AdvanceReaction(ChemicalReactionDefinition reaction,
            ChemicalMixture mixture, ChemicalEnvironment environment, float deltaTimeSeconds)
        {
            if (reaction == null) throw new ArgumentNullException(nameof(reaction));
            if (mixture == null) throw new ArgumentNullException(nameof(mixture));
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            if (deltaTimeSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTimeSeconds));
            if (reaction.RequireBalancedEquation && !IsReactionBalanced(reaction))
                throw new InvalidOperationException("Reaction '" + reaction.Id + "' is not elementally and electrically balanced.");

            double rate = CalculateReactionRateMolPerLitreSecond(reaction, mixture, environment.TemperatureKelvin);
            double requestedExtent = rate * mixture.VolumeLitres * deltaTimeSeconds;
            double maximumExtent = double.PositiveInfinity;
            string limitingReactant = null;

            foreach (ReactionParticipant participant in reaction.Reactants)
            {
                double available = mixture.GetMoles(participant.SpeciesId) / participant.StoichiometricCoefficient;
                if (available < maximumExtent)
                {
                    maximumExtent = available;
                    limitingReactant = participant.SpeciesId;
                }
            }

            double extent = Math.Max(0.0, Math.Min(requestedExtent, maximumExtent));
            if (extent <= 0.0)
            {
                return new ReactionResult
                {
                    Occurred = false,
                    RateMolPerLitreSecond = rate,
                    Message = "No reaction progress: rate is zero or a reactant is unavailable."
                };
            }

            foreach (ReactionParticipant participant in reaction.Reactants)
                if (!mixture.RemoveMoles(participant.SpeciesId,
                    extent * participant.StoichiometricCoefficient))
                    throw new InvalidOperationException("Reactant availability changed while applying the reaction.");

            foreach (ReactionParticipant participant in reaction.Products)
                mixture.AddMoles(participant.SpeciesId,
                    extent * participant.StoichiometricCoefficient);

            return new ReactionResult
            {
                Occurred = true,
                ReactionExtentMoles = extent,
                RateMolPerLitreSecond = rate,
                HeatJoules = (float)(extent * reaction.EnthalpyJPerMolReactionExtent),
                LimitingReactantId = limitingReactant,
                Message = "Reaction step applied."
            };
        }

        // ============================================================
        // DISSOLUTION
        // ============================================================

        public class DissolutionResult
        {
            public double DissolvedMoles { get; internal set; }
            public double RemainingSolidMoles { get; internal set; }
            public double SaturationConcentrationMolPerLitre { get; internal set; }
            public bool ReachedSaturation { get; internal set; }
        }

        /// <summary>
        /// Moves some solid solute into a solution. The model is limited by both
        /// solubility and a surface-area-based dissolution rate.
        /// </summary>
        public DissolutionResult Dissolve(ChemicalMixture solution, string soluteSpeciesId,
            string solventId, double availableSolidMoles, float exposedSurfaceAreaM2,
            float deltaTimeSeconds, ChemicalEnvironment environment)
        {
            if (solution == null) throw new ArgumentNullException(nameof(solution));
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            if (availableSolidMoles < 0.0) throw new ArgumentOutOfRangeException(nameof(availableSolidMoles));
            if (exposedSurfaceAreaM2 < 0f || deltaTimeSeconds < 0f)
                throw new ArgumentOutOfRangeException("Surface area and time must not be negative.");

            ChemicalSpeciesDefinition solute = GetSpecies(soluteSpeciesId);
            SolubilityProfile profile = solute.GetSolubilityProfile(solventId);
            if (profile == null)
                throw new InvalidOperationException("No solubility profile for '" + soluteSpeciesId + "' in solvent '" + solventId + "'.");

            float saturation = profile.CalculateSolubility(environment.TemperatureKelvin, environment.PH);
            double currentConcentration = solution.GetConcentrationMolPerLitre(soluteSpeciesId);
            double remainingCapacity = Math.Max(0.0, (saturation - currentConcentration) * solution.VolumeLitres);
            double kineticLimit = profile.CalculateDissolutionRate(environment.TemperatureKelvin)
                * exposedSurfaceAreaM2 * deltaTimeSeconds;
            double dissolved = Math.Min(availableSolidMoles, Math.Min(remainingCapacity, kineticLimit));
            solution.AddMoles(soluteSpeciesId, dissolved);

            return new DissolutionResult
            {
                DissolvedMoles = dissolved,
                RemainingSolidMoles = Math.Max(0.0, availableSolidMoles - dissolved),
                SaturationConcentrationMolPerLitre = saturation,
                ReachedSaturation = currentConcentration + dissolved / solution.VolumeLitres >= saturation - 1e-9
            };
        }

        // ============================================================
        // CONTAMINATION
        // ============================================================

        [Serializable]
        public class ContaminationRule
        {
            [SerializeField] private string contaminantSpeciesId;
            [SerializeField, Min(0f)] private float traceThresholdMolPerLitre;
            [SerializeField, Min(0f)] private float lowThresholdMolPerLitre;
            [SerializeField, Min(0f)] private float moderateThresholdMolPerLitre;
            [SerializeField, Min(0f)] private float highThresholdMolPerLitre;
            [SerializeField, Min(0f)] private float criticalThresholdMolPerLitre;

            public string ContaminantSpeciesId => contaminantSpeciesId;
            public ContaminationRule()
            {
                contaminantSpeciesId = string.Empty;
                traceThresholdMolPerLitre = 0f;
                lowThresholdMolPerLitre = 0f;
                moderateThresholdMolPerLitre = 0f;
                highThresholdMolPerLitre = 0f;
                criticalThresholdMolPerLitre = 0f;
            }

            public ContaminationRule(string speciesId, float trace, float low,
                float moderate, float high, float critical)
            {
                contaminantSpeciesId = speciesId;
                traceThresholdMolPerLitre = Mathf.Max(0f, trace);
                lowThresholdMolPerLitre = Mathf.Max(traceThresholdMolPerLitre, low);
                moderateThresholdMolPerLitre = Mathf.Max(lowThresholdMolPerLitre, moderate);
                highThresholdMolPerLitre = Mathf.Max(moderateThresholdMolPerLitre, high);
                criticalThresholdMolPerLitre = Mathf.Max(highThresholdMolPerLitre, critical);
            }

            public ContaminationSeverity Evaluate(double concentration)
            {
                if (concentration <= 0.0 || concentration < traceThresholdMolPerLitre)
                    return ContaminationSeverity.None;
                if (concentration < lowThresholdMolPerLitre) return ContaminationSeverity.Trace;
                if (concentration < moderateThresholdMolPerLitre) return ContaminationSeverity.Low;
                if (concentration < highThresholdMolPerLitre) return ContaminationSeverity.Moderate;
                if (concentration < criticalThresholdMolPerLitre) return ContaminationSeverity.High;
                return ContaminationSeverity.Critical;
            }
        }

        public class ContaminationEntry
        {
            public string SpeciesId { get; internal set; }
            public string DisplayName { get; internal set; }
            public double ConcentrationMolPerLitre { get; internal set; }
            public ContaminationSeverity Severity { get; internal set; }
        }

        public class ContaminationReport
        {
            public IReadOnlyList<ContaminationEntry> Entries { get; internal set; }
            public bool IsContaminated { get; internal set; }
            public ContaminationSeverity HighestSeverity { get; internal set; }
        }

        [SerializeField] private List<ContaminationRule> contaminationRules = new List<ContaminationRule>();
        public IReadOnlyList<ContaminationRule> ContaminationRules => contaminationRules;

        public ContaminationReport AssessContamination(ChemicalMixture mixture)
        {
            if (mixture == null) throw new ArgumentNullException(nameof(mixture));
            List<ContaminationEntry> entries = new List<ContaminationEntry>();
            ContaminationSeverity highest = ContaminationSeverity.None;

            foreach (ContaminationRule rule in contaminationRules)
            {
                if (rule == null || string.IsNullOrWhiteSpace(rule.ContaminantSpeciesId)) continue;
                double concentration = mixture.GetConcentrationMolPerLitre(rule.ContaminantSpeciesId);
                ContaminationSeverity severity = rule.Evaluate(concentration);
                if (severity == ContaminationSeverity.None) continue;
                ChemicalSpeciesDefinition definition;
                TryGetSpecies(rule.ContaminantSpeciesId, out definition);
                entries.Add(new ContaminationEntry
                {
                    SpeciesId = rule.ContaminantSpeciesId,
                    DisplayName = definition != null ? definition.DisplayName : rule.ContaminantSpeciesId,
                    ConcentrationMolPerLitre = concentration,
                    Severity = severity
                });
                if (severity > highest) highest = severity;
            }

            return new ContaminationReport
            {
                Entries = entries.AsReadOnly(),
                IsContaminated = entries.Count > 0,
                HighestSeverity = highest
            };
        }

        // ============================================================
        // CHEMICAL RESISTANCE, OXIDATION AND CORROSION
        // ============================================================

        [Serializable]
        public class ChemicalResistanceEntry
        {
            [SerializeField] private ChemicalAgentType agentType;
            [SerializeField, Range(0f, 1f)] private float resistanceRating = 0.5f;
            [SerializeField, TextArea] private string notes;
            public ChemicalAgentType AgentType => agentType;
            public float ResistanceRating => resistanceRating;
            public string Notes => notes;

            public ChemicalResistanceEntry()
            {
                agentType = ChemicalAgentType.Other;
                resistanceRating = 1f;
                notes = string.Empty;
            }

            public ChemicalResistanceEntry(ChemicalAgentType agentType, float resistanceRating, string notes = "")
            {
                this.agentType = agentType;
                this.resistanceRating = Mathf.Clamp01(resistanceRating);
                this.notes = notes;
            }
        }

        /// <summary>
        /// A material's corrosion response. MaterialId is a string key so this
        /// file does not depend on the separate Material ScriptableObject class.
        /// Corrosion rate is a reference uniform penetration rate in mm/year.
        /// </summary>
        [Serializable]
        public class CorrosionProfile
        {
            [SerializeField] private string materialId;
            [SerializeField] private CorrosionMechanism mechanism;
            [SerializeField, Min(0f)] private float referenceCorrosionRateMmPerYear;
            [SerializeField, Min(0f)] private float densityKgPerCubicMetre;
            [SerializeField, Min(0f)] private float activationEnergyJPerMol;
            [SerializeField, Min(1f)] private float referenceTemperatureKelvin = 298.15f;
            [SerializeField, Min(0f)] private float humidityExponent = 1f;
            [SerializeField, Min(0f)] private float oxygenExponent = 1f;
            [SerializeField, Min(0f)] private float acidAccelerationPerPH = 0.15f;
            [SerializeField, Min(0f)] private float alkalineAccelerationPerPH;
            [SerializeField, Range(0f, 1f)] private float passivatedRateMultiplier = 0.05f;
            [SerializeField] private List<ChemicalResistanceEntry> chemicalResistance = new List<ChemicalResistanceEntry>();

            public string MaterialId => materialId;
            public CorrosionMechanism Mechanism => mechanism;
            public float ReferenceCorrosionRateMmPerYear => referenceCorrosionRateMmPerYear;
            public float DensityKgPerCubicMetre => densityKgPerCubicMetre;
            public IReadOnlyList<ChemicalResistanceEntry> ChemicalResistance => chemicalResistance;

            public CorrosionProfile()
            {
                materialId = string.Empty;
                mechanism = CorrosionMechanism.Uniform;
                referenceCorrosionRateMmPerYear = 0f;
                densityKgPerCubicMetre = 0f;
                activationEnergyJPerMol = 0f;
                referenceTemperatureKelvin = 298.15f;
                humidityExponent = 1f;
                oxygenExponent = 1f;
                acidAccelerationPerPH = 0.15f;
                alkalineAccelerationPerPH = 0f;
                passivatedRateMultiplier = 0.05f;
                chemicalResistance = new List<ChemicalResistanceEntry>();
            }

            public CorrosionProfile(string materialId, CorrosionMechanism mechanism,
                float referenceRateMmPerYear, float densityKgPerM3,
                float activationEnergyJPerMol = 0f)
            {
                this.materialId = materialId;
                this.mechanism = mechanism;
                referenceCorrosionRateMmPerYear = Mathf.Max(0f, referenceRateMmPerYear);
                densityKgPerCubicMetre = Mathf.Max(0f, densityKgPerM3);
                this.activationEnergyJPerMol = Mathf.Max(0f, activationEnergyJPerMol);
                referenceTemperatureKelvin = 298.15f;
                humidityExponent = 1f;
                oxygenExponent = 1f;
                acidAccelerationPerPH = 0.15f;
                alkalineAccelerationPerPH = 0f;
                passivatedRateMultiplier = 0.05f;
                chemicalResistance = new List<ChemicalResistanceEntry>();
            }

            public float GetResistanceRating(ChemicalAgentType agent)
            {
                foreach (ChemicalResistanceEntry entry in chemicalResistance)
                    if (entry != null && entry.AgentType == agent)
                        return entry.ResistanceRating;
                // No specific resistance data means no extra attack multiplier.
                return 1f;
            }

            public void AddChemicalResistance(ChemicalResistanceEntry entry)
            {
                if (entry == null) throw new ArgumentNullException(nameof(entry));
                chemicalResistance.Add(entry);
            }

            public void ConfigureEnvironmentalResponse(float humidityExponent, float oxygenExponent,
                float acidAccelerationPerPH, float alkalineAccelerationPerPH,
                float passivatedRateMultiplier)
            {
                this.humidityExponent = Mathf.Max(0f, humidityExponent);
                this.oxygenExponent = Mathf.Max(0f, oxygenExponent);
                this.acidAccelerationPerPH = Mathf.Max(0f, acidAccelerationPerPH);
                this.alkalineAccelerationPerPH = Mathf.Max(0f, alkalineAccelerationPerPH);
                this.passivatedRateMultiplier = Mathf.Clamp01(passivatedRateMultiplier);
            }

            internal float CalculateRate(ChemicalEnvironment environment, bool passivated)
            {
                if (environment == null) throw new ArgumentNullException(nameof(environment));
                const double GasConstant = 8.314462618;
                double temperatureFactor = 1.0;
                if (activationEnergyJPerMol > 0f)
                {
                    temperatureFactor = Math.Exp((activationEnergyJPerMol / GasConstant)
                        * (1.0 / referenceTemperatureKelvin - 1.0 / environment.TemperatureKelvin));
                }

                double humidityFactor = Math.Pow(Math.Max(0.0, environment.RelativeHumidity), humidityExponent);
                double oxygenFactor = Math.Pow(Math.Max(0.0, environment.OxygenActivity / 0.21f), oxygenExponent);

                double pHFactor = 1.0;
                if (environment.PH < 7f)
                    pHFactor += acidAccelerationPerPH * (7f - environment.PH);
                else if (environment.PH > 7f)
                    pHFactor += alkalineAccelerationPerPH * (environment.PH - 7f);

                float resistance = GetResistanceRating(environment.PrimaryAgent);
                double resistanceFactor = 1.0 + (1.0 - resistance) * 9.0;
                double passiveFactor = passivated ? passivatedRateMultiplier : 1.0;

                double rate = referenceCorrosionRateMmPerYear * temperatureFactor
                    * humidityFactor * oxygenFactor * pHFactor * resistanceFactor * passiveFactor;
                if (double.IsNaN(rate) || double.IsInfinity(rate) || rate < 0.0) return 0f;
                return (float)rate;
            }
        }

        [SerializeField] private List<CorrosionProfile> corrosionProfiles = new List<CorrosionProfile>();
        public IReadOnlyList<CorrosionProfile> CorrosionProfiles => corrosionProfiles;

        public CorrosionProfile GetCorrosionProfile(string materialId)
        {
            foreach (CorrosionProfile profile in corrosionProfiles)
                if (profile != null && string.Equals(profile.MaterialId, materialId, StringComparison.OrdinalIgnoreCase))
                    return profile;
            throw new KeyNotFoundException("No corrosion profile is registered for material '" + materialId + "'.");
        }

        public class CorrosionState
        {
            public string MaterialId { get; private set; }
            public float OriginalThicknessMillimetres { get; private set; }
            public float RemainingThicknessMillimetres { get; internal set; }
            public float TotalPenetrationLossMillimetres => OriginalThicknessMillimetres - RemainingThicknessMillimetres;
            public bool IsPassivated { get; set; }
            public bool HasFailed => RemainingThicknessMillimetres <= 0f;

            public CorrosionState(string materialId, float initialThicknessMillimetres, bool isPassivated = false)
            {
                if (initialThicknessMillimetres <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(initialThicknessMillimetres));
                MaterialId = materialId;
                OriginalThicknessMillimetres = initialThicknessMillimetres;
                RemainingThicknessMillimetres = initialThicknessMillimetres;
                IsPassivated = isPassivated;
            }
        }

        public class CorrosionResult
        {
            public float CorrosionRateMmPerYear { get; internal set; }
            public float ThicknessLostMillimetres { get; internal set; }
            public float RemainingThicknessMillimetres { get; internal set; }
            public float EstimatedMassLossKilograms { get; internal set; }
            public bool MaterialFailed { get; internal set; }
        }

        public float CalculateCorrosionRateMmPerYear(string materialId,
            ChemicalEnvironment environment, bool passivated = false)
        {
            return GetCorrosionProfile(materialId).CalculateRate(environment, passivated);
        }

        /// <summary>Applies a time step of corrosion to a material's thickness.</summary>
        public CorrosionResult AdvanceCorrosion(CorrosionState state,
            ChemicalEnvironment environment, float exposedAreaSquareMetres,
            float deltaTimeSeconds)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            if (exposedAreaSquareMetres < 0f || deltaTimeSeconds < 0f)
                throw new ArgumentOutOfRangeException("Area and time must not be negative.");

            CorrosionProfile profile = GetCorrosionProfile(state.MaterialId);
            float rateMmPerYear = profile.CalculateRate(environment, state.IsPassivated);
            const double SecondsPerYear = 365.25 * 24.0 * 60.0 * 60.0;
            float requestedLoss = (float)(rateMmPerYear * deltaTimeSeconds / SecondsPerYear);
            float actualLoss = Mathf.Min(state.RemainingThicknessMillimetres, requestedLoss);
            state.RemainingThicknessMillimetres = Mathf.Max(0f,
                state.RemainingThicknessMillimetres - actualLoss);

            // Convert lost thickness (mm) to volume (m^3), then multiply by density.
            double lostVolumeM3 = exposedAreaSquareMetres * (actualLoss / 1000.0);
            float massLossKg = (float)(lostVolumeM3 * profile.DensityKgPerCubicMetre);

            return new CorrosionResult
            {
                CorrosionRateMmPerYear = rateMmPerYear,
                ThicknessLostMillimetres = actualLoss,
                RemainingThicknessMillimetres = state.RemainingThicknessMillimetres,
                EstimatedMassLossKilograms = massLossKg,
                MaterialFailed = state.HasFailed
            };
        }

        // ============================================================
        // RADIATION
        // ============================================================

        public enum RadiationType { Alpha, BetaMinus, BetaPlus, Gamma, Neutron }

        [Serializable]
        public class Radiation : Particle
        {
            public RadiationType Type { get; private set; }

            public Radiation(RadiationType type)
                : base(GetCharge(type), GetMass(type), GetSpin(type), GetBaryonNumber(type))
            {
                Type = type;
            }

            private static int GetCharge(RadiationType type)
            {
                switch (type)
                {
                    case RadiationType.BetaMinus: return -3;
                    case RadiationType.BetaPlus: return +3;
                    case RadiationType.Alpha: return +6;
                    default: return 0;
                }
            }

            private static float GetMass(RadiationType type)
            {
                switch (type)
                {
                    case RadiationType.Alpha: return 3727.38f;
                    case RadiationType.BetaMinus:
                    case RadiationType.BetaPlus: return Electron.ElectronMassMeVPerC2;
                    case RadiationType.Neutron: return Neutron.NeutronMassMeVPerC2;
                    default: return 0f;
                }
            }

            private static float GetSpin(RadiationType type)
            {
                switch (type)
                {
                    case RadiationType.Alpha: return 0f;
                    case RadiationType.BetaMinus:
                    case RadiationType.BetaPlus:
                    case RadiationType.Neutron: return 0.5f;
                    case RadiationType.Gamma: return 1f;
                    default: return 0f;
                }
            }

            private static int GetBaryonNumber(RadiationType type)
            {
                if (type == RadiationType.Alpha) return 12;
                if (type == RadiationType.Neutron) return 3;
                return 0;
            }
        }

        // ============================================================
        // OPTIONAL PROGRAMMATIC CATALOGUE REGISTRATION
        // ============================================================

        /// <summary>
        /// Adds a definition to this in-memory asset instance. To make changes
        /// permanent in the Unity Editor, edit the serialized lists in the
        /// Inspector or use an Editor tool that marks the asset dirty.
        /// </summary>
        public void RegisterSpecies(ChemicalSpeciesDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrWhiteSpace(definition.Id)) throw new ArgumentException("Species ID is required.");
            if (TryGetSpecies(definition.Id, out _)) throw new InvalidOperationException("Species ID already exists: " + definition.Id);
            species.Add(definition);
        }

        public void RegisterReaction(ChemicalReactionDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            foreach (ChemicalReactionDefinition item in reactions)
                if (item != null && string.Equals(item.Id, definition.Id, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Reaction ID already exists: " + definition.Id);
            reactions.Add(definition);
        }

        public void RegisterCorrosionProfile(CorrosionProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            foreach (CorrosionProfile item in corrosionProfiles)
                if (item != null && string.Equals(item.MaterialId, profile.MaterialId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Corrosion profile already exists for: " + profile.MaterialId);
            corrosionProfiles.Add(profile);
        }

        public void RegisterContaminationRule(ContaminationRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            foreach (ContaminationRule item in contaminationRules)
                if (item != null && string.Equals(item.ContaminantSpeciesId, rule.ContaminantSpeciesId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Contamination rule already exists for: " + rule.ContaminantSpeciesId);
            contaminationRules.Add(rule);
        }
    }
}