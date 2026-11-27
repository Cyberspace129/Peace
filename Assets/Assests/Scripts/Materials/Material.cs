using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ParticleChemistry
{
    /// <summary>
    /// A Unity asset containing two related catalogues:
    /// 1. The 118 chemical elements.
    /// 2. Reusable definitions for naturally occurring and manufactured materials.
    ///
    /// IMPORTANT:
    /// This class is a MATERIAL DATABASE, not Unity's rendering Material.
    /// It stores chemistry and physical-property data. A separate visual system
    /// can use this data to select or generate UnityEngine.Material instances.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Material",
        menuName = "Particle Physics/Periodic Table and Materials")]
    public class Material : ScriptableObject
    {
        // =====================================================
        // ELEMENT CATEGORY
        // =====================================================

        public enum ElementCategory
        {
            Nonmetal,
            NobleGas,
            AlkaliMetal,
            AlkalineEarthMetal,
            Metalloid,
            PostTransitionMetal,
            TransitionMetal,
            Lanthanoid,
            Actinoid,
            Halogen
        }

        // =====================================================
        // MATERIAL CATEGORIES AND PHYSICAL STATES
        // =====================================================

        public enum MaterialCategory
        {
            ElementalSubstance,
            Mineral,
            Metal,
            Alloy,
            Ceramic,
            Glass,
            Polymer,
            Composite,
            PorousMaterial,
            BiologicalMaterial,
            Textile,
            ConstructionMaterial,
            Soil,
            Food,
            Fluid,
            Gas,
            Other
        }

        public enum CompositionType
        {
            PureElement,
            StoichiometricCompound,
            Alloy,
            Mixture,
            Composite,
            Biological,
            Polymer,
            PorousNetwork
        }

        public enum PhysicalPhase
        {
            Solid,
            Liquid,
            Gas,
            Plasma,
            Variable
        }

        public enum ProportionBasis
        {
            AtomicRatio,
            MoleFraction,
            MassFraction,
            VolumeFraction
        }

        // =====================================================
        // ELEMENT DEFINITION
        // =====================================================

        /// <summary>
        /// Describes an element in the periodic table.
        /// An Element is a reusable definition, not an individual atom.
        /// </summary>
        [Serializable]
        public class Element
        {
            [SerializeField] private int atomicNumber;
            [SerializeField] private string symbol;
            [SerializeField] private string elementName;
            [SerializeField] private float atomicMass;
            [SerializeField] private int representativeMassNumber;
            [SerializeField] private int group;
            [SerializeField] private int period;
            [SerializeField] private ElementCategory category;

            public int AtomicNumber => atomicNumber;
            public string Symbol => symbol;
            public string Name => elementName;
            public float AtomicMass => atomicMass;
            public int RepresentativeMassNumber => representativeMassNumber;
            public int Group => group;
            public int Period => period;
            public ElementCategory Category => category;

            public Element(
                int atomicNumber,
                string symbol,
                string elementName,
                float atomicMass,
                int representativeMassNumber,
                int group,
                int period,
                ElementCategory category)
            {
                this.atomicNumber = atomicNumber;
                this.symbol = symbol;
                this.elementName = elementName;
                this.atomicMass = atomicMass;
                this.representativeMassNumber = representativeMassNumber;
                this.group = group;
                this.period = period;
                this.category = category;
            }
        }

        // =====================================================
        // MATERIAL COMPOSITION ENTRY
        // =====================================================

        /// <summary>
        /// One ingredient or constituent of a material.
        ///
        /// Examples:
        /// - Quartz: silicon (Si) and oxygen (O) in a 1:2 atomic ratio.
        /// - A composite: carbon-fibre and epoxy by volume fraction.
        /// - A fresh leaf: water and organic tissue by approximate mass fraction.
        ///
        /// The proportion is interpreted using Basis. It is not always an
        /// atomic ratio; this distinction matters for mixtures and composites.
        /// </summary>
        [Serializable]
        public class MaterialConstituent
        {
            [SerializeField] private string constituentName;
            [SerializeField] private string formula;
            [SerializeField] private float proportion;
            [SerializeField] private ProportionBasis basis;
            [SerializeField] private string notes;

            public string Name => constituentName;
            public string Formula => formula;
            public float Proportion => proportion;
            public ProportionBasis Basis => basis;
            public string Notes => notes;

            public MaterialConstituent(
                string constituentName,
                string formula,
                float proportion,
                ProportionBasis basis,
                string notes = "")
            {
                this.constituentName = constituentName;
                this.formula = formula;
                this.proportion = proportion;
                this.basis = basis;
                this.notes = notes;
            }
        }

        // =====================================================
        // MATERIAL DEFINITION
        // =====================================================

        /// <summary>
        /// A reusable description of a material such as quartz, steel,
        /// tungsten carbide, silica aerogel, wood, or leaf tissue.
        ///
        /// This is not an individual physical object. A runtime material
        /// sample should reference one of these definitions and separately
        /// store its dimensions, temperature, moisture, damage, and so on.
        /// </summary>
        [Serializable]
        public class MaterialDefinition
        {
            [Header("Identity")]
            [SerializeField] private string id;
            [SerializeField] private string materialName;
            [SerializeField] private string formula;
            [SerializeField] private MaterialCategory category;
            [SerializeField] private CompositionType compositionType;
            [SerializeField] private PhysicalPhase phase;

            [TextArea(2, 5)]
            [SerializeField] private string description;

            [Header("Physical Properties")]
            [Tooltip("Bulk or apparent density in kg/m³. Use -1 when unknown or not meaningful.")]
            [SerializeField] private float densityKgPerM3 = -1f;

            [Tooltip("Estimated void-volume fraction from 0 to 1. Use -1 when unknown.")]
            [Range(-1f, 1f)]
            [SerializeField] private float porosity = -1f;

            [Tooltip("Thermal conductivity in W/(m·K). Use -1 when unknown.")]
            [SerializeField] private float thermalConductivityWPerM_K = -1f;

            [Tooltip("Specific heat capacity in J/(kg·K). Use -1 when unknown.")]
            [SerializeField] private float specificHeatJPerKgK = -1f;

            [Tooltip("Mohs hardness. This scale is qualitative and is mainly useful for minerals.")]
            [SerializeField] private float mohsHardness = -1f;

            [Tooltip("Approximate melting point or characteristic thermal transition in °C. Use -1 when not applicable or not reliably defined.")]
            [SerializeField] private float meltingPointC = -1f;

            [Header("Composition")]
            [SerializeField] private List<MaterialConstituent> constituents =
                new List<MaterialConstituent>();

            [TextArea(2, 5)]
            [SerializeField] private string propertyNotes;

            public string Id => id;
            public string Name => materialName;
            public string Formula => formula;
            public MaterialCategory Category => category;
            public CompositionType Composition => compositionType;
            public PhysicalPhase Phase => phase;
            public string Description => description;
            public float DensityKgPerM3 => densityKgPerM3;
            public float Porosity => porosity;
            public float ThermalConductivityWPerM_K => thermalConductivityWPerM_K;
            public float SpecificHeatJPerKgK => specificHeatJPerKgK;
            public float MohsHardness => mohsHardness;
            public float MeltingPointC => meltingPointC;
            public IReadOnlyList<MaterialConstituent> Constituents => constituents;
            public string PropertyNotes => propertyNotes;

            public MaterialDefinition(
                string id,
                string materialName,
                string formula,
                MaterialCategory category,
                CompositionType compositionType,
                PhysicalPhase phase,
                float densityKgPerM3,
                float porosity,
                float thermalConductivityWPerM_K,
                float specificHeatJPerKgK,
                float mohsHardness,
                float meltingPointC,
                string description,
                string propertyNotes = "",
                params MaterialConstituent[] constituents)
            {
                this.id = id;
                this.materialName = materialName;
                this.formula = formula;
                this.category = category;
                this.compositionType = compositionType;
                this.phase = phase;
                this.densityKgPerM3 = densityKgPerM3;
                this.porosity = porosity;
                this.thermalConductivityWPerM_K = thermalConductivityWPerM_K;
                this.specificHeatJPerKgK = specificHeatJPerKgK;
                this.mohsHardness = mohsHardness;
                this.meltingPointC = meltingPointC;
                this.description = description;
                this.propertyNotes = propertyNotes;
                this.constituents = constituents == null
                    ? new List<MaterialConstituent>()
                    : new List<MaterialConstituent>(constituents);
            }

            /// <summary>
            /// Parses a simple chemical formula into element atom counts.
            ///
            /// Supports element symbols, integer subscripts, nested brackets,
            /// and hydrate separators such as the middle dot in CaSO4·2H2O.
            /// It intentionally returns false for non-fixed compositions such
            /// as steel, wood, leaves, and most biological materials.
            /// </summary>
            public bool TryGetElementCounts(out Dictionary<string, int> atomCounts)
            {
                return ChemicalFormulaParser.TryParse(formula, out atomCounts);
            }
        }

        // =====================================================
        // SERIALIZED CATALOGUES
        // =====================================================

        [Header("Periodic Table")]
        [SerializeField] private List<Element> elements = new List<Element>();

        [Header("Material Library")]
        [SerializeField] private List<MaterialDefinition> materials =
            new List<MaterialDefinition>();

        public IReadOnlyList<Element> Elements => elements;
        public IReadOnlyList<MaterialDefinition> Materials => materials;

        // =====================================================
        // UNITY INITIALIZATION
        // =====================================================

        private void OnEnable()
        {
            // Initialize each catalogue independently. This also handles
            // an older asset that already has its elements but no materials.
            bool changed = false;

            if (elements == null || elements.Count == 0)
            {
                LoadPeriodicTable();
                changed = true;
            }

            if (materials == null || materials.Count == 0)
            {
                LoadMaterialCatalogue();
                changed = true;
            }

#if UNITY_EDITOR
            // Mark generated ScriptableObject data as modified so Unity can
            // serialize the newly-created catalogue when the project is saved.
            if (changed)
                UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Rebuilds both default catalogues from this file's data.
        /// Use this from the asset's Inspector context menu if you want to
        /// reset the catalogue. It overwrites custom edits to these lists.
        /// </summary>
        [ContextMenu("Rebuild Default Periodic Table and Material Catalogue")]
        private void RebuildDefaultCatalogues()
        {
            LoadPeriodicTable();
            LoadMaterialCatalogue();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        // =====================================================
        // ELEMENT LOOKUP
        // =====================================================

        public Element GetElement(int atomicNumber)
        {
            foreach (Element element in elements)
            {
                if (element.AtomicNumber == atomicNumber)
                    return element;
            }

            throw new ArgumentException(
                $"No element with atomic number {atomicNumber}.",
                nameof(atomicNumber));
        }

        public Element GetElement(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Element symbol cannot be empty.", nameof(symbol));

            foreach (Element element in elements)
            {
                if (string.Equals(
                    element.Symbol,
                    symbol,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return element;
                }
            }

            throw new ArgumentException($"Unknown element symbol: {symbol}", nameof(symbol));
        }

        // =====================================================
        // MATERIAL LOOKUP
        // =====================================================

        /// <summary>Finds a material using its stable ID, e.g. "silica-aerogel".</summary>
        public MaterialDefinition GetMaterial(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Material ID cannot be empty.", nameof(id));

            foreach (MaterialDefinition item in materials)
            {
                if (string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
                    return item;
            }

            throw new ArgumentException($"Unknown material ID: {id}", nameof(id));
        }

        /// <summary>Finds a material by its display name, e.g. "Quartz".</summary>
        public MaterialDefinition GetMaterialByName(string materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName))
                throw new ArgumentException("Material name cannot be empty.", nameof(materialName));

            foreach (MaterialDefinition item in materials)
            {
                if (string.Equals(
                    item.Name,
                    materialName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }

            throw new ArgumentException($"Unknown material: {materialName}", nameof(materialName));
        }

        /// <summary>Returns all materials belonging to a category.</summary>
        public List<MaterialDefinition> GetMaterialsByCategory(MaterialCategory category)
        {
            List<MaterialDefinition> result = new List<MaterialDefinition>();

            foreach (MaterialDefinition item in materials)
            {
                if (item.Category == category)
                    result.Add(item);
            }

            return result;
        }

        // =====================================================
        // ATOM CREATION
        // =====================================================

        /// <summary>
        /// Creates an atom from an element's atomic number and an isotope.
        ///
        /// Atomic number Z = proton count.
        /// Mass number A = proton count + neutron count.
        /// Ionic charge = proton charge - electron charge.
        ///
        /// This creates a simplified atom object. It does not model electron
        /// orbitals, chemical bonds, nuclear binding energy, or decay.
        /// </summary>
        public Chemicals.Atomic CreateAtom(
            Chemicals chemicals,
            int atomicNumber,
            int massNumber = 0,
            int ionicCharge = 0)
        {
            if (chemicals == null)
                throw new ArgumentNullException(nameof(chemicals));

            Element element = GetElement(atomicNumber);

            if (massNumber == 0)
                massNumber = element.RepresentativeMassNumber;

            int protonCount = atomicNumber;
            int neutronCount = massNumber - atomicNumber;
            int electronCount = atomicNumber - ionicCharge;

            if (neutronCount < 0)
                throw new ArgumentException("Mass number cannot be less than atomic number.");

            if (electronCount < 0)
                throw new ArgumentException("An atom cannot have a negative electron count.");

            return new Chemicals.Atomic(
                chemicals,
                protonCount,
                neutronCount,
                electronCount);
        }

        // =====================================================
        // PERIODIC TABLE DATA
        // =====================================================

        private void LoadPeriodicTable()
        {
            elements = new List<Element>
            {
                // Format: Z, symbol, name, average atomic mass, representative A, group, period, category
                new Element(1, "H", "Hydrogen", 1.008f, 1, 1, 1, ElementCategory.Nonmetal),
                new Element(2, "He", "Helium", 4.0026f, 4, 18, 1, ElementCategory.NobleGas),
                new Element(3, "Li", "Lithium", 6.94f, 7, 1, 2, ElementCategory.AlkaliMetal),
                new Element(4, "Be", "Beryllium", 9.0122f, 9, 2, 2, ElementCategory.AlkalineEarthMetal),
                new Element(5, "B", "Boron", 10.81f, 11, 13, 2, ElementCategory.Metalloid),
                new Element(6, "C", "Carbon", 12.011f, 12, 14, 2, ElementCategory.Nonmetal),
                new Element(7, "N", "Nitrogen", 14.007f, 14, 15, 2, ElementCategory.Nonmetal),
                new Element(8, "O", "Oxygen", 15.999f, 16, 16, 2, ElementCategory.Nonmetal),
                new Element(9, "F", "Fluorine", 18.998f, 19, 17, 2, ElementCategory.Halogen),
                new Element(10, "Ne", "Neon", 20.180f, 20, 18, 2, ElementCategory.NobleGas),
                new Element(11, "Na", "Sodium", 22.990f, 23, 1, 3, ElementCategory.AlkaliMetal),
                new Element(12, "Mg", "Magnesium", 24.305f, 24, 2, 3, ElementCategory.AlkalineEarthMetal),
                new Element(13, "Al", "Aluminium", 26.982f, 27, 13, 3, ElementCategory.PostTransitionMetal),
                new Element(14, "Si", "Silicon", 28.085f, 28, 14, 3, ElementCategory.Metalloid),
                new Element(15, "P", "Phosphorus", 30.974f, 31, 15, 3, ElementCategory.Nonmetal),
                new Element(16, "S", "Sulfur", 32.06f, 32, 16, 3, ElementCategory.Nonmetal),
                new Element(17, "Cl", "Chlorine", 35.45f, 35, 17, 3, ElementCategory.Halogen),
                new Element(18, "Ar", "Argon", 39.948f, 40, 18, 3, ElementCategory.NobleGas),
                new Element(19, "K", "Potassium", 39.098f, 39, 1, 4, ElementCategory.AlkaliMetal),
                new Element(20, "Ca", "Calcium", 40.078f, 40, 2, 4, ElementCategory.AlkalineEarthMetal),
                new Element(21, "Sc", "Scandium", 44.956f, 45, 3, 4, ElementCategory.TransitionMetal),
                new Element(22, "Ti", "Titanium", 47.867f, 48, 4, 4, ElementCategory.TransitionMetal),
                new Element(23, "V", "Vanadium", 50.942f, 51, 5, 4, ElementCategory.TransitionMetal),
                new Element(24, "Cr", "Chromium", 51.996f, 52, 6, 4, ElementCategory.TransitionMetal),
                new Element(25, "Mn", "Manganese", 54.938f, 55, 7, 4, ElementCategory.TransitionMetal),
                new Element(26, "Fe", "Iron", 55.845f, 56, 8, 4, ElementCategory.TransitionMetal),
                new Element(27, "Co", "Cobalt", 58.933f, 59, 9, 4, ElementCategory.TransitionMetal),
                new Element(28, "Ni", "Nickel", 58.693f, 59, 10, 4, ElementCategory.TransitionMetal),
                new Element(29, "Cu", "Copper", 63.546f, 64, 11, 4, ElementCategory.TransitionMetal),
                new Element(30, "Zn", "Zinc", 65.38f, 65, 12, 4, ElementCategory.TransitionMetal),
                new Element(31, "Ga", "Gallium", 69.723f, 70, 13, 4, ElementCategory.PostTransitionMetal),
                new Element(32, "Ge", "Germanium", 72.630f, 73, 14, 4, ElementCategory.Metalloid),
                new Element(33, "As", "Arsenic", 74.922f, 75, 15, 4, ElementCategory.Metalloid),
                new Element(34, "Se", "Selenium", 78.971f, 79, 16, 4, ElementCategory.Nonmetal),
                new Element(35, "Br", "Bromine", 79.904f, 80, 17, 4, ElementCategory.Halogen),
                new Element(36, "Kr", "Krypton", 83.798f, 84, 18, 4, ElementCategory.NobleGas),
                new Element(37, "Rb", "Rubidium", 85.468f, 85, 1, 5, ElementCategory.AlkaliMetal),
                new Element(38, "Sr", "Strontium", 87.62f, 88, 2, 5, ElementCategory.AlkalineEarthMetal),
                new Element(39, "Y", "Yttrium", 88.906f, 89, 3, 5, ElementCategory.TransitionMetal),
                new Element(40, "Zr", "Zirconium", 91.224f, 91, 4, 5, ElementCategory.TransitionMetal),
                new Element(41, "Nb", "Niobium", 92.906f, 93, 5, 5, ElementCategory.TransitionMetal),
                new Element(42, "Mo", "Molybdenum", 95.95f, 96, 6, 5, ElementCategory.TransitionMetal),
                new Element(43, "Tc", "Technetium", 98f, 98, 7, 5, ElementCategory.TransitionMetal),
                new Element(44, "Ru", "Ruthenium", 101.07f, 101, 8, 5, ElementCategory.TransitionMetal),
                new Element(45, "Rh", "Rhodium", 102.906f, 103, 9, 5, ElementCategory.TransitionMetal),
                new Element(46, "Pd", "Palladium", 106.42f, 106, 10, 5, ElementCategory.TransitionMetal),
                new Element(47, "Ag", "Silver", 107.868f, 108, 11, 5, ElementCategory.TransitionMetal),
                new Element(48, "Cd", "Cadmium", 112.414f, 112, 12, 5, ElementCategory.TransitionMetal),
                new Element(49, "In", "Indium", 114.818f, 115, 13, 5, ElementCategory.PostTransitionMetal),
                new Element(50, "Sn", "Tin", 118.710f, 119, 14, 5, ElementCategory.PostTransitionMetal),
                new Element(51, "Sb", "Antimony", 121.760f, 122, 15, 5, ElementCategory.Metalloid),
                new Element(52, "Te", "Tellurium", 127.60f, 128, 16, 5, ElementCategory.Metalloid),
                new Element(53, "I", "Iodine", 126.904f, 127, 17, 5, ElementCategory.Halogen),
                new Element(54, "Xe", "Xenon", 131.293f, 131, 18, 5, ElementCategory.NobleGas),
                new Element(55, "Cs", "Caesium", 132.905f, 133, 1, 6, ElementCategory.AlkaliMetal),
                new Element(56, "Ba", "Barium", 137.327f, 137, 2, 6, ElementCategory.AlkalineEarthMetal),
                new Element(57, "La", "Lanthanum", 138.905f, 139, 3, 6, ElementCategory.Lanthanoid),
                new Element(58, "Ce", "Cerium", 140.116f, 140, 0, 6, ElementCategory.Lanthanoid),
                new Element(59, "Pr", "Praseodymium", 140.908f, 141, 0, 6, ElementCategory.Lanthanoid),
                new Element(60, "Nd", "Neodymium", 144.242f, 144, 0, 6, ElementCategory.Lanthanoid),
                new Element(61, "Pm", "Promethium", 145f, 145, 0, 6, ElementCategory.Lanthanoid),
                new Element(62, "Sm", "Samarium", 150.36f, 150, 0, 6, ElementCategory.Lanthanoid),
                new Element(63, "Eu", "Europium", 151.964f, 152, 0, 6, ElementCategory.Lanthanoid),
                new Element(64, "Gd", "Gadolinium", 157.25f, 157, 0, 6, ElementCategory.Lanthanoid),
                new Element(65, "Tb", "Terbium", 158.925f, 159, 0, 6, ElementCategory.Lanthanoid),
                new Element(66, "Dy", "Dysprosium", 162.500f, 163, 0, 6, ElementCategory.Lanthanoid),
                new Element(67, "Ho", "Holmium", 164.930f, 165, 0, 6, ElementCategory.Lanthanoid),
                new Element(68, "Er", "Erbium", 167.259f, 167, 0, 6, ElementCategory.Lanthanoid),
                new Element(69, "Tm", "Thulium", 168.934f, 169, 0, 6, ElementCategory.Lanthanoid),
                new Element(70, "Yb", "Ytterbium", 173.045f, 173, 0, 6, ElementCategory.Lanthanoid),
                new Element(71, "Lu", "Lutetium", 174.967f, 175, 0, 6, ElementCategory.Lanthanoid),
                new Element(72, "Hf", "Hafnium", 178.49f, 178, 4, 6, ElementCategory.TransitionMetal),
                new Element(73, "Ta", "Tantalum", 180.948f, 181, 5, 6, ElementCategory.TransitionMetal),
                new Element(74, "W", "Tungsten", 183.84f, 184, 6, 6, ElementCategory.TransitionMetal),
                new Element(75, "Re", "Rhenium", 186.207f, 186, 7, 6, ElementCategory.TransitionMetal),
                new Element(76, "Os", "Osmium", 190.23f, 190, 8, 6, ElementCategory.TransitionMetal),
                new Element(77, "Ir", "Iridium", 192.217f, 192, 9, 6, ElementCategory.TransitionMetal),
                new Element(78, "Pt", "Platinum", 195.084f, 195, 10, 6, ElementCategory.TransitionMetal),
                new Element(79, "Au", "Gold", 196.967f, 197, 11, 6, ElementCategory.TransitionMetal),
                new Element(80, "Hg", "Mercury", 200.592f, 201, 12, 6, ElementCategory.TransitionMetal),
                new Element(81, "Tl", "Thallium", 204.38f, 204, 13, 6, ElementCategory.PostTransitionMetal),
                new Element(82, "Pb", "Lead", 207.2f, 207, 14, 6, ElementCategory.PostTransitionMetal),
                new Element(83, "Bi", "Bismuth", 208.980f, 209, 15, 6, ElementCategory.PostTransitionMetal),
                new Element(84, "Po", "Polonium", 209f, 209, 16, 6, ElementCategory.PostTransitionMetal),
                new Element(85, "At", "Astatine", 210f, 210, 17, 6, ElementCategory.Halogen),
                new Element(86, "Rn", "Radon", 222f, 222, 18, 6, ElementCategory.NobleGas),
                new Element(87, "Fr", "Francium", 223f, 223, 1, 7, ElementCategory.AlkaliMetal),
                new Element(88, "Ra", "Radium", 226f, 226, 2, 7, ElementCategory.AlkalineEarthMetal),
                new Element(89, "Ac", "Actinium", 227f, 227, 3, 7, ElementCategory.Actinoid),
                new Element(90, "Th", "Thorium", 232.038f, 232, 0, 7, ElementCategory.Actinoid),
                new Element(91, "Pa", "Protactinium", 231.036f, 231, 0, 7, ElementCategory.Actinoid),
                new Element(92, "U", "Uranium", 238.029f, 238, 0, 7, ElementCategory.Actinoid),
                new Element(93, "Np", "Neptunium", 237f, 237, 0, 7, ElementCategory.Actinoid),
                new Element(94, "Pu", "Plutonium", 244f, 244, 0, 7, ElementCategory.Actinoid),
                new Element(95, "Am", "Americium", 243f, 243, 0, 7, ElementCategory.Actinoid),
                new Element(96, "Cm", "Curium", 247f, 247, 0, 7, ElementCategory.Actinoid),
                new Element(97, "Bk", "Berkelium", 247f, 247, 0, 7, ElementCategory.Actinoid),
                new Element(98, "Cf", "Californium", 251f, 251, 0, 7, ElementCategory.Actinoid),
                new Element(99, "Es", "Einsteinium", 252f, 252, 0, 7, ElementCategory.Actinoid),
                new Element(100, "Fm", "Fermium", 257f, 257, 0, 7, ElementCategory.Actinoid),
                new Element(101, "Md", "Mendelevium", 258f, 258, 0, 7, ElementCategory.Actinoid),
                new Element(102, "No", "Nobelium", 259f, 259, 0, 7, ElementCategory.Actinoid),
                new Element(103, "Lr", "Lawrencium", 266f, 266, 0, 7, ElementCategory.Actinoid),
                new Element(104, "Rf", "Rutherfordium", 267f, 267, 4, 7, ElementCategory.TransitionMetal),
                new Element(105, "Db", "Dubnium", 268f, 268, 5, 7, ElementCategory.TransitionMetal),
                new Element(106, "Sg", "Seaborgium", 269f, 269, 6, 7, ElementCategory.TransitionMetal),
                new Element(107, "Bh", "Bohrium", 270f, 270, 7, 7, ElementCategory.TransitionMetal),
                new Element(108, "Hs", "Hassium", 277f, 277, 8, 7, ElementCategory.TransitionMetal),
                new Element(109, "Mt", "Meitnerium", 278f, 278, 9, 7, ElementCategory.TransitionMetal),
                new Element(110, "Ds", "Darmstadtium", 281f, 281, 10, 7, ElementCategory.TransitionMetal),
                new Element(111, "Rg", "Roentgenium", 282f, 282, 11, 7, ElementCategory.TransitionMetal),
                new Element(112, "Cn", "Copernicium", 285f, 285, 12, 7, ElementCategory.TransitionMetal),
                new Element(113, "Nh", "Nihonium", 286f, 286, 13, 7, ElementCategory.PostTransitionMetal),
                new Element(114, "Fl", "Flerovium", 289f, 289, 14, 7, ElementCategory.PostTransitionMetal),
                new Element(115, "Mc", "Moscovium", 290f, 290, 15, 7, ElementCategory.PostTransitionMetal),
                new Element(116, "Lv", "Livermorium", 293f, 293, 16, 7, ElementCategory.PostTransitionMetal),
                new Element(117, "Ts", "Tennessine", 294f, 294, 17, 7, ElementCategory.Halogen),
                new Element(118, "Og", "Oganesson", 294f, 294, 18, 7, ElementCategory.NobleGas)
            };
        }

        // =====================================================
        // MATERIAL CATALOGUE DATA
        // =====================================================

        private void LoadMaterialCatalogue()
        {
            materials = new List<MaterialDefinition>();

            // Numeric property conventions:
            // density: kg/m³
            // porosity: fraction from 0 to 1
            // thermal conductivity: W/(m·K)
            // specific heat: J/(kg·K)
            // Mohs hardness: approximate mineral hardness
            // melting point: °C
            // -1 means unknown, not applicable, or too variable for one value.
            // Values are representative approximations, usually near room temperature.

            // -------------------------------------------------
            // ELEMENTAL MATERIALS AND CARBON ALLOTROPES
            // -------------------------------------------------

            Add("graphite", "Graphite", "C", MaterialCategory.ElementalSubstance,
                CompositionType.PureElement, PhysicalPhase.Solid,
                2260, 0.02f, 119, 710, 1.5f, -1,
                "Layered crystalline allotrope of carbon; layers shear easily.",
                "Thermal and electrical conductivity depend strongly on crystal direction. Sublimes at ambient pressure rather than having a simple melting point.");

            Add("diamond", "Diamond", "C", MaterialCategory.ElementalSubstance,
                CompositionType.PureElement, PhysicalPhase.Solid,
                3515, 0, 1000, 509, 10, -1,
                "Three-dimensional tetrahedral carbon crystal with extremely high hardness.",
                "Thermal conductivity is very high and purity-dependent. Does not have a normal ambient-pressure melting point.");

            Add("graphene", "Graphene", "C", MaterialCategory.ElementalSubstance,
                CompositionType.PureElement, PhysicalPhase.Solid,
                -1, 0, 2000, 700, 10, -1,
                "Single-atom-thick sheet of sp²-bonded carbon.",
                "A conventional bulk density is not meaningful for an ideal one-atom-thick sheet; use areal density for thin-sheet mechanics.");

            Add("silicon-crystal", "Single-crystal silicon", "Si", MaterialCategory.ElementalSubstance,
                CompositionType.PureElement, PhysicalPhase.Solid,
                2329, 0, 149, 700, 6.5f, 1414,
                "Semiconductor-grade crystalline silicon.");

            Add("aluminium-metal", "Aluminium", "Al", MaterialCategory.Metal,
                CompositionType.PureElement, PhysicalPhase.Solid,
                2700, 0, 237, 900, 2.75f, 660.3f,
                "Lightweight, electrically and thermally conductive metal.");

            Add("copper-metal", "Copper", "Cu", MaterialCategory.Metal,
                CompositionType.PureElement, PhysicalPhase.Solid,
                8960, 0, 401, 385, 3, 1084.6f,
                "Highly conductive reddish metal.");

            Add("iron-metal", "Iron", "Fe", MaterialCategory.Metal,
                CompositionType.PureElement, PhysicalPhase.Solid,
                7874, 0, 80, 449, 4, 1538,
                "Ferromagnetic metal and principal constituent of many steels.");

            Add("nickel-metal", "Nickel", "Ni", MaterialCategory.Metal,
                CompositionType.PureElement, PhysicalPhase.Solid,
                8908, 0, 91, 440, 4, 1455,
                "Corrosion-resistant transition metal.");

            Add("zinc-metal", "Zinc", "Zn", MaterialCategory.Metal,
                CompositionType.PureElement, PhysicalPhase.Solid,
                7140, 0, 116, 388, 2.5f, 419.5f,
                "Metal commonly used for galvanising steel.");

            Add("magnesium-metal", "Magnesium", "Mg", MaterialCategory.Metal,
                CompositionType.PureElement, PhysicalPhase.Solid,
                1740, 0, 156, 1020, 2.5f, 650,
                "Very low-density structural metal; fine powder and shavings are flammable.");

            Add("titanium-metal", "Titanium", "Ti", MaterialCategory.Metal,
                CompositionType.PureElement, PhysicalPhase.Solid,
                4507, 0, 22, 520, 6, 1668,
                "Strong, corrosion-resistant metal with a high strength-to-weight ratio.");

            Add("tungsten-metal", "Tungsten", "W", MaterialCategory.Metal,
                CompositionType.PureElement, PhysicalPhase.Solid,
                19300, 0, 174, 134, 7.5f, 3422,
                "Very dense refractory metal with an exceptionally high melting point.");

            Add("molybdenum-metal", "Molybdenum", "Mo", MaterialCategory.Metal,
                CompositionType.PureElement, PhysicalPhase.Solid,
                10280, 0, 139, 251, 5.5f, 2623,
                "Refractory metal used in high-temperature alloys.");

            Add("chromium-metal", "Chromium", "Cr", MaterialCategory.Metal,
                CompositionType.PureElement, PhysicalPhase.Solid,
                7190, 0, 94, 449, 8.5f, 1907,
                "Hard, corrosion-resistant metal used in stainless steels and coatings.");

            Add("cobalt-metal", "Cobalt", "Co", MaterialCategory.Metal,
                CompositionType.PureElement, PhysicalPhase.Solid,
                8900, 0, 100, 421, 5, 1495,
                "Ferromagnetic transition metal used in high-performance alloys.");

            Add("silver-metal", "Silver", "Ag", MaterialCategory.Metal,
                CompositionType.PureElement, PhysicalPhase.Solid,
                10490, 0, 429, 235, 2.5f, 961.8f,
                "Metal with the highest electrical and thermal conductivity among common pure metals.");

            Add("gold-metal", "Gold", "Au", MaterialCategory.Metal,
                CompositionType.PureElement, PhysicalPhase.Solid,
                19320, 0, 318, 129, 2.5f, 1064.2f,
                "Dense, chemically resistant, highly ductile metal.");

            Add("platinum-metal", "Platinum", "Pt", MaterialCategory.Metal,
                CompositionType.PureElement, PhysicalPhase.Solid,
                21450, 0, 72, 133, 3.5f, 1768.3f,
                "Dense noble metal used in catalysts and laboratory equipment.");

            Add("lead-metal", "Lead", "Pb", MaterialCategory.Metal,
                CompositionType.PureElement, PhysicalPhase.Solid,
                11340, 0, 35, 128, 1.5f, 327.5f,
                "Very dense, soft, toxic metal. Avoid treating exposure as harmless.");

            Add("tin-metal", "Tin", "Sn", MaterialCategory.Metal,
                CompositionType.PureElement, PhysicalPhase.Solid,
                7310, 0, 67, 228, 1.5f, 231.9f,
                "Low-melting metal used in solders and tinplate.");

            Add("uranium-metal", "Uranium metal", "U", MaterialCategory.Metal,
                CompositionType.PureElement, PhysicalPhase.Solid,
                19050, 0, 27, 116, 6, 1132,
                "Dense radioactive actinide metal.",
                "Representative physical values only. Radioactivity and radiological controls are outside this material-data model.");

            Add("sulfur-element", "Sulfur", "S", MaterialCategory.ElementalSubstance,
                CompositionType.PureElement, PhysicalPhase.Solid,
                2070, 0, 0.2f, 710, 2, 115.2f,
                "Yellow molecular solid, commonly present as S8 rings.");

            // -------------------------------------------------
            // MINERALS AND ROCK-FORMING MATERIALS
            // -------------------------------------------------

            Add("quartz", "Quartz", "SiO2", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                2650, 0, 7, 740, 7, 1713,
                "Crystalline silicon dioxide; a major constituent of many rocks.",
                "Density and thermal conductivity depend on crystal direction, temperature, and defects.",
                new MaterialConstituent("Silicon", "Si", 1, ProportionBasis.AtomicRatio),
                new MaterialConstituent("Oxygen", "O", 2, ProportionBasis.AtomicRatio));

            Add("amethyst", "Amethyst", "SiO2", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                2650, 0, 7, 740, 7, 1713,
                "Purple variety of quartz; colour is associated with trace impurities and irradiation.");

            Add("rose-quartz", "Rose quartz", "SiO2", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                2650, 0, 7, 740, 7, 1713,
                "Pink variety of quartz. Trace inclusions and defects influence colour.");

            Add("feldspar-orthoclase", "Orthoclase feldspar", "KAlSi3O8", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                2560, 0.01f, 2.3f, 750, 6, 1200,
                "Potassium feldspar, a common rock-forming mineral.");

            Add("feldspar-albite", "Albite", "NaAlSi3O8", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                2620, 0.01f, 2.3f, 750, 6, 1118,
                "Sodium-rich plagioclase feldspar.");

            Add("feldspar-anorthite", "Anorthite", "CaAl2Si2O8", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                2760, 0.01f, 2.1f, 780, 6, 1550,
                "Calcium-rich plagioclase feldspar.");

            Add("muscovite", "Muscovite mica", "KAl2(AlSi3O10)(OH)2", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                2830, 0.01f, 0.75f, 800, 2.5f, -1,
                "Sheet silicate that cleaves into thin elastic flakes.");

            Add("biotite", "Biotite mica", "Variable K(Mg,Fe)3AlSi3O10(OH)2", MaterialCategory.Mineral,
                CompositionType.Mixture, PhysicalPhase.Solid,
                3000, 0.02f, 2, 800, 2.5f, -1,
                "Iron- and magnesium-bearing mica with variable composition.");

            Add("calcite", "Calcite", "CaCO3", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                2710, 0.01f, 3.5f, 820, 3, -1,
                "Common calcium carbonate mineral; a principal constituent of limestone.");

            Add("aragonite", "Aragonite", "CaCO3", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                2930, 0.01f, 2.5f, 800, 3.5f, -1,
                "Orthorhombic crystalline form of calcium carbonate.");

            Add("dolomite-mineral", "Dolomite mineral", "CaMg(CO3)2", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                2850, 0.01f, 5, 900, 3.5f, -1,
                "Calcium magnesium carbonate mineral.");

            Add("gypsum", "Gypsum", "CaSO4·2H2O", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                2320, 0.02f, 0.5f, 1090, 2, -1,
                "Hydrated calcium sulfate; loses water when heated.",
                "Gypsum dehydrates on heating; it does not have a simple melting point under ordinary conditions.");

            Add("anhydrite", "Anhydrite", "CaSO4", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                2980, 0.01f, 3, 650, 3.5f, 1450,
                "Anhydrous calcium sulfate mineral.");

            Add("halite", "Halite", "NaCl", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                2160, 0, 6.5f, 850, 2.5f, 801,
                "Crystalline sodium chloride, commonly known as rock salt.");

            Add("fluorite", "Fluorite", "CaF2", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                3180, 0, 3, 850, 4, 1418,
                "Calcium fluoride mineral, often strongly coloured by impurities.");

            Add("olivine", "Olivine", "(Mg,Fe)2SiO4", MaterialCategory.Mineral,
                CompositionType.Mixture, PhysicalPhase.Solid,
                3500, 0.01f, 4, 800, 6.5f, 1800,
                "Magnesium-iron silicate solid-solution series.");

            Add("talc", "Talc", "Mg3Si4O10(OH)2", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                2750, 0.02f, 6, 900, 1, 900,
                "Very soft sheet silicate used in powders and ceramics.");

            Add("kaolinite", "Kaolinite", "Al2Si2O5(OH)4", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                2600, 0.05f, 1.5f, 900, 2, -1,
                "Clay mineral used in porcelain and paper coatings.",
                "Heating causes dehydroxylation and phase changes rather than ordinary melting at the stated temperature.");

            Add("montmorillonite", "Montmorillonite clay", "Variable (Na,Ca)0.33(Al,Mg)2Si4O10(OH)2·nH2O", MaterialCategory.Mineral,
                CompositionType.Mixture, PhysicalPhase.Solid,
                2350, 0.35f, 1.5f, 900, 1.5f, -1,
                "Expandable clay mineral that absorbs water between silicate layers.");

            Add("hematite", "Hematite", "Fe2O3", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                5260, 0.01f, 11, 650, 5.5f, 1565,
                "Iron oxide and an important iron ore.");

            Add("magnetite", "Magnetite", "Fe3O4", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                5170, 0.01f, 5, 670, 5.5f, 1597,
                "Magnetic iron oxide mineral.");

            Add("pyrite", "Pyrite", "FeS2", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                5010, 0.01f, 19, 670, 6, 743,
                "Iron sulfide known as fool's gold.");

            Add("chalcopyrite", "Chalcopyrite", "CuFeS2", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                4200, 0.01f, 3, 550, 3.5f, 950,
                "Copper iron sulfide and important copper ore.");

            Add("galena", "Galena", "PbS", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                7600, 0.01f, 2, 210, 2.5f, 1114,
                "Lead sulfide mineral; lead-bearing dust is hazardous.");

            Add("corundum", "Corundum", "Al2O3", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                3980, 0, 30, 750, 9, 2050,
                "Crystalline aluminium oxide; ruby and sapphire are varieties.");

            Add("ruby", "Ruby", "Al2O3:Cr", MaterialCategory.Mineral,
                CompositionType.Mixture, PhysicalPhase.Solid,
                4000, 0, 30, 750, 9, 2050,
                "Red corundum coloured mainly by chromium impurities.");

            Add("sapphire", "Sapphire", "Al2O3 with trace elements", MaterialCategory.Mineral,
                CompositionType.Mixture, PhysicalPhase.Solid,
                4000, 0, 30, 750, 9, 2050,
                "Gem-quality corundum in colours other than ruby red, or blue in common usage.");

            Add("zircon", "Zircon", "ZrSiO4", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                4650, 0, 5, 500, 7.5f, 2550,
                "Zirconium silicate mineral used in ceramics and geochronology.");

            Add("rutile", "Rutile", "TiO2", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                4240, 0, 8, 690, 6.5f, 1843,
                "Titanium dioxide mineral and important titanium ore.");

            Add("ilmenite", "Ilmenite", "FeTiO3", MaterialCategory.Mineral,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                4720, 0.01f, 3, 700, 5.5f, 1365,
                "Iron titanium oxide ore mineral.");

            Add("apatite", "Apatite", "Ca5(PO4)3(F,Cl,OH)", MaterialCategory.Mineral,
                CompositionType.Mixture, PhysicalPhase.Solid,
                3200, 0.01f, 2, 750, 5, 1600,
                "Phosphate mineral group; composition varies by halogen and hydroxyl content.");

            Add("garnet", "Garnet group", "Variable silicate", MaterialCategory.Mineral,
                CompositionType.Mixture, PhysicalPhase.Solid,
                4100, 0.01f, 3, 700, 7, 1300,
                "Group of related silicate minerals with different chemical compositions.");

            Add("bauxite", "Bauxite ore", "Hydrated aluminium oxides and hydroxides", MaterialCategory.Mineral,
                CompositionType.Mixture, PhysicalPhase.Solid,
                2500, 0.15f, 1.5f, 900, 2.5f, -1,
                "Variable natural mixture of aluminium-bearing minerals and impurities.");

            Add("granite", "Granite", "Quartz + feldspar + mica", MaterialCategory.Composite,
                CompositionType.Mixture, PhysicalPhase.Solid,
                2700, 0.01f, 2.5f, 790, 6.5f, -1,
                "Coarse-grained igneous rock with variable mineral proportions.");

            Add("basalt", "Basalt", "Plagioclase + pyroxene + olivine", MaterialCategory.Composite,
                CompositionType.Mixture, PhysicalPhase.Solid,
                2950, 0.03f, 1.5f, 840, 6, -1,
                "Fine-grained volcanic rock; density and strength vary with vesicles and mineral content.");

            Add("limestone", "Limestone", "Mostly CaCO3", MaterialCategory.Mineral,
                CompositionType.Mixture, PhysicalPhase.Solid,
                2600, 0.12f, 1.3f, 900, 3, -1,
                "Sedimentary carbonate rock with variable calcite, fossils, and impurities.");

            Add("sandstone", "Sandstone", "Mostly quartz grains", MaterialCategory.Composite,
                CompositionType.Mixture, PhysicalPhase.Solid,
                2300, 0.18f, 2, 800, 6, -1,
                "Cemented sedimentary rock; porosity and strength vary widely.");

            Add("marble", "Marble", "Mostly CaCO3", MaterialCategory.Mineral,
                CompositionType.Mixture, PhysicalPhase.Solid,
                2700, 0.01f, 2.5f, 880, 3, -1,
                "Metamorphic carbonate rock, commonly recrystallized calcite or dolomite.");

            // -------------------------------------------------
            // TECHNICAL CERAMICS AND CARBIDES
            // -------------------------------------------------

            Add("alumina-ceramic", "Alumina ceramic", "Al2O3", MaterialCategory.Ceramic,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                3950, 0.01f, 30, 880, 9, 2072,
                "Dense aluminium oxide engineering ceramic.");

            Add("zirconia-ceramic", "Zirconia ceramic", "ZrO2", MaterialCategory.Ceramic,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                6000, 0.02f, 2.5f, 500, 8.5f, 2715,
                "Zirconium dioxide ceramic; stabilizers control its crystal phases and toughness.");

            Add("silicon-carbide", "Silicon carbide", "SiC", MaterialCategory.Ceramic,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                3210, 0.01f, 120, 750, 9.5f, 2730,
                "Very hard covalent ceramic used in abrasives, electronics, and high-temperature components.");

            Add("tungsten-carbide", "Tungsten carbide", "WC", MaterialCategory.Ceramic,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                15600, 0.01f, 110, 203, 9, 2785,
                "Dense, extremely hard tungsten-carbon ceramic used in cutting tools and wear parts.",
                "WC-Co cemented carbide is a different composite material and has different properties.",
                new MaterialConstituent("Tungsten", "W", 1, ProportionBasis.AtomicRatio),
                new MaterialConstituent("Carbon", "C", 1, ProportionBasis.AtomicRatio));

            Add("boron-carbide", "Boron carbide", "B4C", MaterialCategory.Ceramic,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                2520, 0.02f, 30, 950, 9.5f, 2450,
                "Low-density, very hard ceramic used in abrasives and armour systems.");

            Add("silicon-nitride", "Silicon nitride", "Si3N4", MaterialCategory.Ceramic,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                3200, 0.02f, 25, 700, 9, 1900,
                "High-strength engineering ceramic with good thermal-shock resistance.");

            Add("aluminium-nitride", "Aluminium nitride", "AlN", MaterialCategory.Ceramic,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                3260, 0.02f, 170, 740, 9, 2200,
                "Ceramic with high thermal conductivity and electrical insulation.");

            Add("boron-nitride", "Hexagonal boron nitride", "BN", MaterialCategory.Ceramic,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                2100, 0.02f, 30, 800, 2, 2973,
                "Layered ceramic sometimes called white graphite; properties are anisotropic.");

            Add("titanium-carbide", "Titanium carbide", "TiC", MaterialCategory.Ceramic,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                4930, 0.01f, 21, 540, 9, 3160,
                "Hard refractory carbide used in cutting tools and coatings.");

            Add("titanium-nitride", "Titanium nitride", "TiN", MaterialCategory.Ceramic,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                5220, 0.01f, 29, 540, 9, 2950,
                "Hard golden-coloured ceramic coating used for wear resistance.");

            Add("porcelain", "Porcelain", "Kaolin + feldspar + quartz", MaterialCategory.Ceramic,
                CompositionType.Mixture, PhysicalPhase.Solid,
                2400, 0.02f, 1.5f, 900, 6, 1400,
                "Vitrified ceramic made from clay, feldspar, and silica; formulation varies.");

            Add("terracotta", "Terracotta", "Fired clay", MaterialCategory.Ceramic,
                CompositionType.Mixture, PhysicalPhase.Solid,
                1900, 0.12f, 0.7f, 840, 5, 1000,
                "Porous earthenware ceramic fired at relatively low temperature.");

            Add("ceramic-brick", "Fired clay brick", "Fired clay minerals", MaterialCategory.ConstructionMaterial,
                CompositionType.Mixture, PhysicalPhase.Solid,
                1800, 0.2f, 0.7f, 840, 5, 1000,
                "Building brick; density depends on clay, firing, and void structure.");

            // -------------------------------------------------
            // GLASSES
            // -------------------------------------------------

            Add("soda-lime-glass", "Soda-lime glass", "SiO2-Na2O-CaO", MaterialCategory.Glass,
                CompositionType.Mixture, PhysicalPhase.Solid,
                2500, 0, 1, 840, 5.5f, 1500,
                "Common window and bottle glass; a non-crystalline silicate network.");

            Add("borosilicate-glass", "Borosilicate glass", "SiO2-B2O3-Na2O", MaterialCategory.Glass,
                CompositionType.Mixture, PhysicalPhase.Solid,
                2230, 0, 1.2f, 830, 6, 820,
                "Low-expansion glass used in laboratory and heat-resistant cookware.");

            Add("fused-silica", "Fused silica glass", "SiO2", MaterialCategory.Glass,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                2200, 0, 1.4f, 740, 6, 1710,
                "Amorphous high-purity silica glass with low thermal expansion.");

            Add("fibreglass", "Glass fibre", "Silicate glass", MaterialCategory.Glass,
                CompositionType.Mixture, PhysicalPhase.Solid,
                2550, 0, 1, 840, 6, 1000,
                "Fine glass fibres; a bulk mat or cloth has much lower apparent density due to voids.");

            // -------------------------------------------------
            // METALS AND ALLOYS
            // -------------------------------------------------

            Add("mild-steel", "Mild steel", "Fe-C alloy", MaterialCategory.Alloy,
                CompositionType.Alloy, PhysicalPhase.Solid,
                7850, 0, 50, 490, -1, 1450,
                "Low-carbon steel used in structural and general fabrication.");

            Add("carbon-steel", "Medium-carbon steel", "Fe-C alloy", MaterialCategory.Alloy,
                CompositionType.Alloy, PhysicalPhase.Solid,
                7850, 0, 45, 480, -1, 1420,
                "Iron-carbon alloy; heat treatment and carbon fraction strongly affect properties.");

            Add("stainless-304", "Stainless steel 304", "Fe-Cr-Ni alloy", MaterialCategory.Alloy,
                CompositionType.Alloy, PhysicalPhase.Solid,
                8000, 0, 16, 500, -1, 1400,
                "Austenitic chromium-nickel stainless steel.");

            Add("stainless-316", "Stainless steel 316", "Fe-Cr-Ni-Mo alloy", MaterialCategory.Alloy,
                CompositionType.Alloy, PhysicalPhase.Solid,
                8000, 0, 16, 500, -1, 1375,
                "Molybdenum-bearing austenitic stainless steel with improved corrosion resistance in many environments.");

            Add("cast-iron", "Grey cast iron", "Fe-C-Si alloy", MaterialCategory.Alloy,
                CompositionType.Alloy, PhysicalPhase.Solid,
                7200, 0, 50, 460, -1, 1200,
                "Cast iron with graphite flakes; good damping and compressive behaviour.");

            Add("brass", "Brass", "Cu-Zn alloy", MaterialCategory.Alloy,
                CompositionType.Alloy, PhysicalPhase.Solid,
                8500, 0, 120, 380, -1, 930,
                "Copper-zinc alloy; properties vary with zinc fraction and phase structure.");

            Add("bronze", "Tin bronze", "Cu-Sn alloy", MaterialCategory.Alloy,
                CompositionType.Alloy, PhysicalPhase.Solid,
                8800, 0, 60, 380, -1, 950,
                "Copper-tin alloy; composition varies by grade.");

            Add("aluminium-6061", "Aluminium 6061-T6", "Al-Mg-Si alloy", MaterialCategory.Alloy,
                CompositionType.Alloy, PhysicalPhase.Solid,
                2700, 0, 167, 896, -1, 582,
                "Heat-treatable aluminium alloy widely used in structural components.");

            Add("aluminium-7075", "Aluminium 7075-T6", "Al-Zn-Mg-Cu alloy", MaterialCategory.Alloy,
                CompositionType.Alloy, PhysicalPhase.Solid,
                2810, 0, 130, 960, -1, 477,
                "High-strength aluminium alloy used in aerospace structures.");

            Add("duralumin", "Duralumin", "Al-Cu-Mg-Mn alloy", MaterialCategory.Alloy,
                CompositionType.Alloy, PhysicalPhase.Solid,
                2780, 0, 160, 900, -1, 500,
                "Historical family of age-hardenable aluminium alloys.");

            Add("titanium-ti64", "Ti-6Al-4V titanium alloy", "Ti-Al-V alloy", MaterialCategory.Alloy,
                CompositionType.Alloy, PhysicalPhase.Solid,
                4430, 0, 7, 560, -1, 1660,
                "Widely used alpha-beta titanium alloy for aerospace and biomedical components.");

            Add("inconel-718", "Inconel 718", "Ni-Fe-Cr-Nb-Mo alloy", MaterialCategory.Alloy,
                CompositionType.Alloy, PhysicalPhase.Solid,
                8190, 0, 11, 435, -1, 1260,
                "Nickel-based precipitation-hardened superalloy for high-temperature service.");

            Add("nichrome", "Nichrome", "Ni-Cr alloy", MaterialCategory.Alloy,
                CompositionType.Alloy, PhysicalPhase.Solid,
                8400, 0, 11, 450, -1, 1400,
                "Nickel-chromium resistance alloy used in heating elements.");

            Add("nitinol", "Nitinol", "Ni-Ti alloy", MaterialCategory.Alloy,
                CompositionType.Alloy, PhysicalPhase.Solid,
                6450, 0, 18, 450, -1, 1310,
                "Nickel-titanium shape-memory alloy; transformation temperatures depend on composition and processing.");

            Add("wc-cobalt", "Cemented tungsten carbide (WC-Co)", "WC + Co", MaterialCategory.Composite,
                CompositionType.Composite, PhysicalPhase.Solid,
                14500, 0.01f, 70, 220, 9, -1,
                "Tungsten-carbide grains bonded by a cobalt metallic binder.",
                "This is not pure WC. Density, hardness, toughness, and binder percentage vary by grade.");

            Add("galvanised-steel", "Galvanised steel", "Steel with Zn coating", MaterialCategory.Composite,
                CompositionType.Composite, PhysicalPhase.Solid,
                7850, 0, 50, 490, -1, 1450,
                "Steel substrate protected by a zinc coating; coating thickness is a separate layer property.");

            // -------------------------------------------------
            // POLYMERS AND ELASTOMERS
            // -------------------------------------------------

            Add("polyethylene-hdpe", "HDPE", "(C2H4)n", MaterialCategory.Polymer,
                CompositionType.Polymer, PhysicalPhase.Solid,
                950, 0, 0.45f, 1900, -1, 130,
                "High-density polyethylene; semi-crystalline thermoplastic.");

            Add("polyethylene-ldpe", "LDPE", "(C2H4)n", MaterialCategory.Polymer,
                CompositionType.Polymer, PhysicalPhase.Solid,
                920, 0, 0.33f, 2200, -1, 110,
                "Low-density polyethylene; flexible thermoplastic.");

            Add("polypropylene", "Polypropylene", "(C3H6)n", MaterialCategory.Polymer,
                CompositionType.Polymer, PhysicalPhase.Solid,
                900, 0, 0.22f, 1900, -1, 160,
                "Lightweight semi-crystalline thermoplastic.");

            Add("pvc", "PVC", "(C2H3Cl)n", MaterialCategory.Polymer,
                CompositionType.Polymer, PhysicalPhase.Solid,
                1400, 0, 0.19f, 900, -1, 200,
                "Polyvinyl chloride; rigid or flexible depending on plasticizer content.",
                "PVC commonly softens and decomposes; the listed transition is a rough processing reference, not a universal melting point.");

            Add("pet", "PET", "(C10H8O4)n", MaterialCategory.Polymer,
                CompositionType.Polymer, PhysicalPhase.Solid,
                1380, 0, 0.24f, 1200, -1, 250,
                "Polyethylene terephthalate used in bottles and fibres.");

            Add("polystyrene", "Polystyrene", "(C8H8)n", MaterialCategory.Polymer,
                CompositionType.Polymer, PhysicalPhase.Solid,
                1050, 0, 0.13f, 1300, -1, 240,
                "General-purpose polystyrene; expanded grades have much lower bulk density.");

            Add("pmma", "PMMA acrylic", "(C5H8O2)n", MaterialCategory.Polymer,
                CompositionType.Polymer, PhysicalPhase.Solid,
                1180, 0, 0.19f, 1470, -1, 160,
                "Transparent acrylic thermoplastic commonly used as a glass substitute.");

            Add("polycarbonate", "Polycarbonate", "(C16H14O3)n", MaterialCategory.Polymer,
                CompositionType.Polymer, PhysicalPhase.Solid,
                1200, 0, 0.20f, 1200, -1, 267,
                "Tough, transparent engineering thermoplastic.");

            Add("ptfe", "PTFE (Teflon)", "(C2F4)n", MaterialCategory.Polymer,
                CompositionType.Polymer, PhysicalPhase.Solid,
                2200, 0, 0.25f, 1000, -1, 327,
                "Chemically resistant fluoropolymer with very low friction.");

            Add("nylon-6", "Nylon 6", "(C6H11NO)n", MaterialCategory.Polymer,
                CompositionType.Polymer, PhysicalPhase.Solid,
                1130, 0, 0.25f, 1700, -1, 220,
                "Polyamide engineering thermoplastic; absorbs moisture.");

            Add("nylon-66", "Nylon 6,6", "(C12H22N2O2)n", MaterialCategory.Polymer,
                CompositionType.Polymer, PhysicalPhase.Solid,
                1140, 0, 0.25f, 1600, -1, 265,
                "Polyamide used in gears, bearings, fibres, and engineering parts.");

            Add("peek", "PEEK", "(C19H14O3)n", MaterialCategory.Polymer,
                CompositionType.Polymer, PhysicalPhase.Solid,
                1300, 0, 0.25f, 1300, -1, 343,
                "High-performance semi-crystalline engineering thermoplastic.");

            Add("abs", "ABS", "Acrylonitrile-butadiene-styrene", MaterialCategory.Polymer,
                CompositionType.Mixture, PhysicalPhase.Solid,
                1050, 0, 0.18f, 1400, -1, 105,
                "Tough multiphase thermoplastic common in housings and 3D printing.");

            Add("pla", "PLA", "(C3H4O2)n", MaterialCategory.Polymer,
                CompositionType.Polymer, PhysicalPhase.Solid,
                1240, 0, 0.13f, 1800, -1, 160,
                "Polylactic acid thermoplastic often used in additive manufacturing.");

            Add("pom", "Acetal (POM)", "(CH2O)n", MaterialCategory.Polymer,
                CompositionType.Polymer, PhysicalPhase.Solid,
                1410, 0, 0.31f, 1500, -1, 175,
                "Polyoxymethylene engineering plastic with low friction and good dimensional stability.");

            Add("polyurethane", "Solid polyurethane", "Variable polymer", MaterialCategory.Polymer,
                CompositionType.Mixture, PhysicalPhase.Solid,
                1200, 0.02f, 0.20f, 1400, -1, 180,
                "Polyurethane elastomer or plastic; chemistry and properties vary greatly by formulation.");

            Add("epoxy-resin", "Cured epoxy resin", "Cross-linked epoxy network", MaterialCategory.Polymer,
                CompositionType.Composite, PhysicalPhase.Solid,
                1200, 0.01f, 0.20f, 1100, -1, 120,
                "Thermosetting polymer; cured properties depend on resin, hardener, and cure schedule.");

            Add("silicone-rubber", "Silicone rubber", "Siloxane polymer", MaterialCategory.Polymer,
                CompositionType.Polymer, PhysicalPhase.Solid,
                1100, 0, 0.20f, 1500, -1, -1,
                "Flexible silicone elastomer with a broad service-temperature range.");

            Add("natural-rubber", "Natural rubber", "(C5H8)n", MaterialCategory.Polymer,
                CompositionType.Biological, PhysicalPhase.Solid,
                920, 0, 0.13f, 1900, -1, -1,
                "Elastomer based mainly on cis-1,4-polyisoprene.");

            // -------------------------------------------------
            // AEROGELS, FOAMS, AND OTHER POROUS MATERIALS
            // -------------------------------------------------

            Add("silica-aerogel", "Silica aerogel", "SiO2", MaterialCategory.PorousMaterial,
                CompositionType.PorousNetwork, PhysicalPhase.Solid,
                100, 0.95f, 0.015f, 1000, -1, -1,
                "Nanoporous silica network in which most of the volume is gas-filled pore space.",
                "Representative monolith: density around 100 kg/m³, porosity around 95%, thermal conductivity around 0.01-0.02 W/(m·K). Actual products span a broad range.",
                new MaterialConstituent("Silica skeleton", "SiO2", 0.05f, ProportionBasis.VolumeFraction, "Approximate solid-skeleton fraction for a 95% porous example."),
                new MaterialConstituent("Pore gas", "Air", 0.95f, ProportionBasis.VolumeFraction, "Gas-filled void volume."));

            Add("alumina-aerogel", "Alumina aerogel", "Al2O3", MaterialCategory.PorousMaterial,
                CompositionType.PorousNetwork, PhysicalPhase.Solid,
                150, 0.94f, 0.03f, 900, -1, -1,
                "Highly porous aluminium-oxide aerogel; properties depend on synthesis and heat treatment.");

            Add("carbon-aerogel", "Carbon aerogel", "C", MaterialCategory.PorousMaterial,
                CompositionType.PorousNetwork, PhysicalPhase.Solid,
                150, 0.95f, 0.05f, 800, -1, -1,
                "Conductive or semi-conductive nanoporous carbon network; structure depends on precursor and carbonisation.");

            Add("cellulose-aerogel", "Cellulose aerogel", "Cellulose porous network", MaterialCategory.PorousMaterial,
                CompositionType.PorousNetwork, PhysicalPhase.Solid,
                50, 0.97f, 0.025f, 1400, -1, -1,
                "Very lightweight porous network formed from cellulose fibres or nanofibrils.");

            Add("activated-carbon", "Activated carbon", "Porous carbon", MaterialCategory.PorousMaterial,
                CompositionType.PorousNetwork, PhysicalPhase.Solid,
                500, 0.55f, 0.20f, 700, -1, 3500,
                "Highly porous carbon adsorbent; bulk density varies with granule and powder form.");

            Add("charcoal", "Charcoal", "Mostly carbon with ash", MaterialCategory.PorousMaterial,
                CompositionType.Mixture, PhysicalPhase.Solid,
                250, 0.70f, 0.15f, 1000, -1, -1,
                "Porous carbon-rich solid produced by heating biomass with limited oxygen.");

            Add("pumice", "Pumice", "Silicate volcanic glass with vesicles", MaterialCategory.PorousMaterial,
                CompositionType.PorousNetwork, PhysicalPhase.Solid,
                600, 0.65f, 0.15f, 900, 6, -1,
                "Highly vesicular volcanic rock; density varies enough that some samples float.");

            Add("expanded-perlite", "Expanded perlite", "Expanded volcanic glass", MaterialCategory.PorousMaterial,
                CompositionType.PorousNetwork, PhysicalPhase.Solid,
                100, 0.90f, 0.05f, 900, -1, -1,
                "Lightweight expanded glass used in insulation and horticulture.");

            Add("cork", "Natural cork", "Suberin + cellulose + lignin", MaterialCategory.PorousMaterial,
                CompositionType.Biological, PhysicalPhase.Solid,
                240, 0.70f, 0.04f, 1800, -1, -1,
                "Cellular plant bark tissue with sealed gas-filled cells.");

            Add("eps-foam", "Expanded polystyrene foam (EPS)", "Polystyrene + gas cells", MaterialCategory.PorousMaterial,
                CompositionType.PorousNetwork, PhysicalPhase.Solid,
                20, 0.98f, 0.035f, 1300, -1, 100,
                "Very lightweight closed-cell polystyrene foam; density depends on grade.");

            Add("xps-foam", "Extruded polystyrene foam (XPS)", "Polystyrene + gas cells", MaterialCategory.PorousMaterial,
                CompositionType.PorousNetwork, PhysicalPhase.Solid,
                35, 0.97f, 0.030f, 1450, -1, 100,
                "Closed-cell insulation foam with relatively low water absorption.");

            Add("pu-foam", "Flexible polyurethane foam", "Polyurethane + gas cells", MaterialCategory.PorousMaterial,
                CompositionType.PorousNetwork, PhysicalPhase.Solid,
                30, 0.97f, 0.035f, 1400, -1, -1,
                "Open-cell flexible foam used in cushions and packaging.");

            Add("mineral-wool", "Mineral wool insulation", "Silicate fibres + air", MaterialCategory.PorousMaterial,
                CompositionType.Composite, PhysicalPhase.Solid,
                100, 0.95f, 0.04f, 840, -1, 1000,
                "Fibrous insulation made from spun rock or slag; bulk density depends on product grade.");

            // -------------------------------------------------
            // BIOLOGICAL MATERIALS, PLANTS, AND TEXTILES
            // -------------------------------------------------

            Add("oak-wood", "Oak wood", "Cellulose + hemicellulose + lignin", MaterialCategory.BiologicalMaterial,
                CompositionType.Biological, PhysicalPhase.Solid,
                700, 0.45f, 0.17f, 1700, -1, -1,
                "Hardwood; strongly anisotropic because of its cellular grain structure.");

            Add("pine-wood", "Pine wood", "Cellulose + hemicellulose + lignin", MaterialCategory.BiologicalMaterial,
                CompositionType.Biological, PhysicalPhase.Solid,
                500, 0.55f, 0.12f, 1600, -1, -1,
                "Softwood with density and stiffness strongly dependent on moisture and growth direction.");

            Add("balsa-wood", "Balsa wood", "Cellulose + hemicellulose + lignin", MaterialCategory.BiologicalMaterial,
                CompositionType.Biological, PhysicalPhase.Solid,
                160, 0.85f, 0.055f, 1300, -1, -1,
                "Very low-density wood with a cellular structure.");

            Add("bamboo", "Bamboo", "Cellulose + hemicellulose + lignin", MaterialCategory.BiologicalMaterial,
                CompositionType.Biological, PhysicalPhase.Solid,
                700, 0.45f, 0.20f, 1600, -1, -1,
                "Graded natural fibre-reinforced cellular material; properties vary along and across the culm.");

            Add("plywood", "Plywood", "Cross-laminated wood veneers + adhesive", MaterialCategory.Composite,
                CompositionType.Composite, PhysicalPhase.Solid,
                550, 0.25f, 0.13f, 1500, -1, -1,
                "Layered wood composite with alternating veneer directions.");

            Add("mdf", "Medium-density fibreboard", "Wood fibres + resin", MaterialCategory.Composite,
                CompositionType.Composite, PhysicalPhase.Solid,
                750, 0.12f, 0.12f, 1500, -1, -1,
                "Engineered wood panel made from fibres and binder.");

            Add("paper", "Paper", "Cellulose fibres", MaterialCategory.BiologicalMaterial,
                CompositionType.Biological, PhysicalPhase.Solid,
                800, 0.45f, 0.10f, 1300, -1, -1,
                "Compressed cellulose fibre network; basis weight, coatings, and moisture alter properties.");

            Add("cardboard", "Corrugated cardboard", "Cellulose fibres + adhesive", MaterialCategory.Composite,
                CompositionType.Composite, PhysicalPhase.Solid,
                250, 0.80f, 0.06f, 1300, -1, -1,
                "Layered paperboard with fluted core; effective properties depend on flute geometry and orientation.");

            Add("cotton", "Cotton fibre", "Cellulose", MaterialCategory.Textile,
                CompositionType.Biological, PhysicalPhase.Solid,
                1540, 0.05f, 0.04f, 1300, -1, -1,
                "Natural cellulose fibre; a cotton textile has a much lower bulk density due to spaces between fibres.");

            Add("wool", "Wool fibre", "Keratin protein", MaterialCategory.Textile,
                CompositionType.Biological, PhysicalPhase.Solid,
                1310, 0.10f, 0.04f, 1700, -1, -1,
                "Crimped animal-protein fibre that traps air in textile structures.");

            Add("silk", "Silk fibre", "Fibroin protein", MaterialCategory.Textile,
                CompositionType.Biological, PhysicalPhase.Solid,
                1300, 0.05f, 0.05f, 1300, -1, -1,
                "Natural protein filament with high tensile strength for its mass.");

            Add("leather", "Leather", "Collagen + water + tanning agents", MaterialCategory.BiologicalMaterial,
                CompositionType.Biological, PhysicalPhase.Solid,
                860, 0.20f, 0.20f, 1500, -1, -1,
                "Processed animal hide; thickness, tanning, moisture, and fibre orientation affect properties.");

            Add("cortical-bone", "Cortical bone", "Hydroxyapatite + collagen", MaterialCategory.BiologicalMaterial,
                CompositionType.Composite, PhysicalPhase.Solid,
                1900, 0.05f, 0.3f, 1300, -1, -1,
                "Dense outer bone tissue; mineral-reinforced collagen composite with anisotropic behaviour.");

            Add("trabecular-bone", "Trabecular bone", "Hydroxyapatite + collagen + marrow spaces", MaterialCategory.BiologicalMaterial,
                CompositionType.Composite, PhysicalPhase.Solid,
                500, 0.70f, 0.2f, 1300, -1, -1,
                "Porous internal bone structure; apparent density varies widely by anatomical site.");

            Add("chitin", "Chitin", "(C8H13O5N)n", MaterialCategory.BiologicalMaterial,
                CompositionType.Biological, PhysicalPhase.Solid,
                1400, 0.05f, 0.20f, 1300, -1, -1,
                "Structural polysaccharide found in arthropod exoskeletons and fungal cell walls.");

            Add("cellulose", "Cellulose", "(C6H10O5)n", MaterialCategory.BiologicalMaterial,
                CompositionType.Biological, PhysicalPhase.Solid,
                1500, 0.10f, 0.20f, 1300, -1, -1,
                "Structural plant polysaccharide; crystalline and amorphous regions coexist.");

            Add("fresh-leaf", "Fresh green leaf tissue", "Variable biological mixture", MaterialCategory.BiologicalMaterial,
                CompositionType.Biological, PhysicalPhase.Solid,
                950, 0.20f, 0.50f, 3900, -1, -1,
                "Living leaf tissue: water-filled cells, cell walls, air spaces, chloroplasts, pigments, and vascular tissue.",
                "Highly variable by species, age, hydration, and measurement method. The constituent proportions below are an illustrative normalised fresh-leaf recipe, not a universal botanical composition.",
                new MaterialConstituent("Water", "H2O", 0.92f, ProportionBasis.MassFraction, "Representative fresh-leaf water fraction; actual values vary considerably."),
                new MaterialConstituent("Cellulose", "(C6H10O5)n", 0.03f, ProportionBasis.MassFraction),
                new MaterialConstituent("Hemicellulose", "Variable polysaccharides", 0.02f, ProportionBasis.MassFraction),
                new MaterialConstituent("Proteins", "Proteins", 0.015f, ProportionBasis.MassFraction),
                new MaterialConstituent("Lignin", "Lignin", 0.005f, ProportionBasis.MassFraction),
                new MaterialConstituent("Sugars and starch", "Carbohydrates", 0.005f, ProportionBasis.MassFraction),
                new MaterialConstituent("Lipids, waxes, pigments, and minerals", "Variable", 0.005f, ProportionBasis.MassFraction));

            Add("dry-leaf-litter", "Dry leaf litter", "Cellulose + lignin + minerals", MaterialCategory.BiologicalMaterial,
                CompositionType.Biological, PhysicalPhase.Solid,
                250, 0.65f, 0.08f, 1400, -1, -1,
                "Dried fallen leaves; decomposition, species, and moisture change composition and density.");

            Add("bark", "Tree bark", "Cellulose + lignin + suberin", MaterialCategory.BiologicalMaterial,
                CompositionType.Biological, PhysicalPhase.Solid,
                500, 0.55f, 0.10f, 1500, -1, -1,
                "Protective outer plant tissue with highly variable structure and moisture.");

            Add("straw", "Dry straw", "Cellulose + hemicellulose + lignin", MaterialCategory.BiologicalMaterial,
                CompositionType.Biological, PhysicalPhase.Solid,
                100, 0.80f, 0.05f, 1400, -1, -1,
                "Dried grass or cereal stems; hollow plant structure creates low bulk density.");

            Add("grass-tissue", "Fresh grass tissue", "Variable biological mixture", MaterialCategory.BiologicalMaterial,
                CompositionType.Biological, PhysicalPhase.Solid,
                950, 0.20f, 0.50f, 3800, -1, -1,
                "Living grass tissue with high and variable water content.");

            // -------------------------------------------------
            // CONSTRUCTION MATERIALS AND SOILS
            // -------------------------------------------------

            Add("ordinary-concrete", "Ordinary concrete", "Cement paste + aggregate + water", MaterialCategory.ConstructionMaterial,
                CompositionType.Composite, PhysicalPhase.Solid,
                2400, 0.08f, 1.7f, 880, -1, -1,
                "Hardened cementitious composite; aggregate, water-cement ratio, curing, and age affect properties.");

            Add("reinforced-concrete", "Reinforced concrete", "Concrete + steel reinforcement", MaterialCategory.Composite,
                CompositionType.Composite, PhysicalPhase.Solid,
                2500, 0.08f, 1.8f, 900, -1, -1,
                "Concrete with embedded steel; directional and structural behaviour depends on reinforcement layout.");

            Add("portland-cement", "Portland cement powder", "Calcium silicates + aluminates + sulfates", MaterialCategory.ConstructionMaterial,
                CompositionType.Mixture, PhysicalPhase.Solid,
                3150, 0.35f, 0.3f, 750, -1, -1,
                "Hydraulic binder powder. Hydration creates a changing multiphase solid; do not model it as one pure compound.");

            Add("asphalt", "Asphalt concrete", "Mineral aggregate + bitumen", MaterialCategory.Composite,
                CompositionType.Composite, PhysicalPhase.Solid,
                2400, 0.05f, 1.2f, 920, -1, -1,
                "Road paving composite; viscoelastic behaviour changes strongly with temperature and loading rate.");

            Add("sand", "Dry quartz sand", "Mostly SiO2 grains", MaterialCategory.Soil,
                CompositionType.Mixture, PhysicalPhase.Solid,
                1600, 0.40f, 0.30f, 800, 7, 1713,
                "Loose granular material; bulk density depends on grain size, shape, packing, and moisture.");

            Add("clay-soil", "Clay soil", "Clay minerals + water + organics", MaterialCategory.Soil,
                CompositionType.Mixture, PhysicalPhase.Solid,
                1700, 0.45f, 1.1f, 900, -1, -1,
                "Fine-grained soil; water content controls plasticity, strength, and shrink-swell behaviour.");

            Add("loam-soil", "Loam soil", "Sand + silt + clay + organic matter", MaterialCategory.Soil,
                CompositionType.Mixture, PhysicalPhase.Solid,
                1300, 0.50f, 0.8f, 1200, -1, -1,
                "Soil texture mixture with sand, silt, and clay; organic matter and moisture vary.");

            Add("topsoil", "Topsoil", "Minerals + organic matter + water + air", MaterialCategory.Soil,
                CompositionType.Mixture, PhysicalPhase.Solid,
                1200, 0.50f, 0.6f, 1300, -1, -1,
                "Biologically active upper soil layer; extremely variable in composition and moisture.");

            Add("peat", "Peat", "Partially decomposed plant matter", MaterialCategory.Soil,
                CompositionType.Biological, PhysicalPhase.Solid,
                400, 0.80f, 0.08f, 1500, -1, -1,
                "Water-retaining accumulation of partially decomposed organic matter.");

            // -------------------------------------------------
            // COMMON LIQUIDS AND GASES
            // -------------------------------------------------

            Add("water-liquid", "Liquid water", "H2O", MaterialCategory.Fluid,
                CompositionType.StoichiometricCompound, PhysicalPhase.Liquid,
                998, 0, 0.60f, 4182, -1, 0,
                "Pure water near 20 °C and atmospheric pressure.",
                "Density, heat capacity, and conductivity vary with temperature and dissolved substances.");

            Add("ice", "Ice Ih", "H2O", MaterialCategory.Other,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                917, 0, 2.2f, 2100, -1, 0,
                "Common hexagonal crystalline ice near 0 °C.");

            Add("water-vapour", "Water vapour (steam)", "H2O", MaterialCategory.Gas,
                CompositionType.StoichiometricCompound, PhysicalPhase.Gas,
                0.60f, 0, 0.025f, 2010, -1, -1,
                "Approximate saturated steam density near 100 °C and 1 atmosphere.",
                "Gas density depends strongly on temperature and pressure; 0.60 kg/m³ is not a universal value.");

            Add("ethanol", "Ethanol", "C2H6O", MaterialCategory.Fluid,
                CompositionType.StoichiometricCompound, PhysicalPhase.Liquid,
                789, 0, 0.17f, 2440, -1, -114,
                "Ethanol liquid near room temperature.");

            Add("glycerol", "Glycerol", "C3H8O3", MaterialCategory.Fluid,
                CompositionType.StoichiometricCompound, PhysicalPhase.Liquid,
                1260, 0, 0.29f, 2400, -1, 18,
                "Viscous polyol liquid; density and viscosity depend on temperature and water content.");

            Add("vegetable-oil", "Vegetable oil", "Variable triglycerides", MaterialCategory.Fluid,
                CompositionType.Mixture, PhysicalPhase.Liquid,
                920, 0, 0.17f, 1900, -1, -10,
                "Mixture of triglycerides; species and processing determine exact properties.");

            Add("mercury-liquid", "Liquid mercury", "Hg", MaterialCategory.Fluid,
                CompositionType.PureElement, PhysicalPhase.Liquid,
                13534, 0, 8.3f, 140, -1, -38.8f,
                "Dense liquid metal near room temperature; mercury is highly toxic.");

            Add("seawater", "Seawater", "H2O + dissolved salts", MaterialCategory.Fluid,
                CompositionType.Mixture, PhysicalPhase.Liquid,
                1025, 0, 0.60f, 3990, -1, -1,
                "Representative ocean water; salinity and temperature affect density and freezing point.");

            Add("air", "Dry air", "N2 + O2 + Ar + CO2", MaterialCategory.Gas,
                CompositionType.Mixture, PhysicalPhase.Gas,
                1.204f, 0, 0.026f, 1005, -1, -1,
                "Approximate dry air at 20 °C and 1 atmosphere.",
                "Gas density and thermal properties depend on pressure, humidity, and temperature.");

            Add("nitrogen-gas", "Nitrogen gas", "N2", MaterialCategory.Gas,
                CompositionType.StoichiometricCompound, PhysicalPhase.Gas,
                1.165f, 0, 0.026f, 1040, -1, -210,
                "Nitrogen gas near 20 °C and 1 atmosphere.");

            Add("oxygen-gas", "Oxygen gas", "O2", MaterialCategory.Gas,
                CompositionType.StoichiometricCompound, PhysicalPhase.Gas,
                1.331f, 0, 0.027f, 918, -1, -218.8f,
                "Oxygen gas near 20 °C and 1 atmosphere.");

            Add("carbon-dioxide-gas", "Carbon dioxide gas", "CO2", MaterialCategory.Gas,
                CompositionType.StoichiometricCompound, PhysicalPhase.Gas,
                1.842f, 0, 0.016f, 844, -1, -1,
                "Carbon dioxide gas near 20 °C and 1 atmosphere.",
                "At atmospheric pressure CO2 sublimes rather than forming a stable liquid phase; -56.6 °C is its triple-point temperature.");

            Add("methane-gas", "Methane gas", "CH4", MaterialCategory.Gas,
                CompositionType.StoichiometricCompound, PhysicalPhase.Gas,
                0.668f, 0, 0.034f, 2220, -1, -182.5f,
                "Methane gas near 20 °C and 1 atmosphere; flammable.");

            Add("helium-gas", "Helium gas", "He", MaterialCategory.Gas,
                CompositionType.PureElement, PhysicalPhase.Gas,
                0.166f, 0, 0.15f, 5190, -1, -269,
                "Helium gas near 20 °C and 1 atmosphere.");

            Add("argon-gas", "Argon gas", "Ar", MaterialCategory.Gas,
                CompositionType.PureElement, PhysicalPhase.Gas,
                1.66f, 0, 0.018f, 520, -1, -189.3f,
                "Argon gas near 20 °C and 1 atmosphere.");

            // -------------------------------------------------
            // FOOD AND BIOCHEMICAL REFERENCE MATERIALS
            // -------------------------------------------------

            Add("sucrose", "Sucrose", "C12H22O11", MaterialCategory.Food,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                1587, 0, 0.15f, 1250, -1, 186,
                "Common crystalline table sugar; decomposes when heated.");

            Add("glucose", "Glucose", "C6H12O6", MaterialCategory.Food,
                CompositionType.StoichiometricCompound, PhysicalPhase.Solid,
                1540, 0, 0.2f, 1500, -1, 146,
                "Simple sugar used as a primary energy substrate in biology.");

            Add("starch", "Starch", "(C6H10O5)n", MaterialCategory.Food,
                CompositionType.Biological, PhysicalPhase.Solid,
                1500, 0.10f, 0.15f, 1300, -1, -1,
                "Plant storage polysaccharide; granule structure and moisture determine behaviour.");

            Add("egg-shell", "Eggshell", "Mostly CaCO3", MaterialCategory.BiologicalMaterial,
                CompositionType.Composite, PhysicalPhase.Solid,
                2300, 0.10f, 0.5f, 900, 3, -1,
                "Biomineral composite of calcium carbonate and organic matrix with pores.");

            Add("sea-shell", "Seashell", "CaCO3 + organic matrix", MaterialCategory.BiologicalMaterial,
                CompositionType.Composite, PhysicalPhase.Solid,
                2700, 0.05f, 1, 900, 3.5f, -1,
                "Biomineral composite, commonly aragonite or calcite with organic material.");

            // Attach representative detailed compositions to selected entries.
            // The full catalogue remains usable without these optional recipes.
        }

        // =====================================================
        // MATERIAL CREATION HELPER
        // =====================================================

        private void Add(
            string id,
            string materialName,
            string formula,
            MaterialCategory category,
            CompositionType compositionType,
            PhysicalPhase phase,
            float densityKgPerM3,
            float porosity,
            float thermalConductivityWPerM_K,
            float specificHeatJPerKgK,
            float mohsHardness,
            float meltingPointC,
            string description,
            string propertyNotes = "",
            params MaterialConstituent[] constituents)
        {
            materials.Add(new MaterialDefinition(
                id,
                materialName,
                formula,
                category,
                compositionType,
                phase,
                densityKgPerM3,
                porosity,
                thermalConductivityWPerM_K,
                specificHeatJPerKgK,
                mohsHardness,
                meltingPointC,
                description,
                propertyNotes,
                constituents));
        }

        // =====================================================
        // SIMPLE CHEMICAL FORMULA PARSER
        // =====================================================

        /// <summary>
        /// Parses formulas such as H2O, SiO2, Ca(OH)2, Al2(SO4)3,
        /// and CaSO4·2H2O into element counts.
        ///
        /// It does not parse variable formulas, alloy notation, polymers
        /// with repeating-unit suffixes, or fractional occupancies.
        /// Those require a richer chemical-composition model.
        /// </summary>
        private static class ChemicalFormulaParser
        {
            public static bool TryParse(
                string formula,
                out Dictionary<string, int> atomCounts)
            {
                atomCounts = new Dictionary<string, int>(StringComparer.Ordinal);

                if (string.IsNullOrWhiteSpace(formula))
                    return false;

                string[] hydrateParts = formula.Replace(" ", "").Split('·');

                foreach (string rawPart in hydrateParts)
                {
                    if (string.IsNullOrEmpty(rawPart))
                        return false;

                    string part = rawPart;
                    int leadingMultiplier = ReadLeadingNumber(part, out int prefixLength);

                    if (prefixLength > 0)
                        part = part.Substring(prefixLength);

                    if (part.Length == 0)
                        return false;

                    int index = 0;
                    Dictionary<string, int> partCounts =
                        new Dictionary<string, int>(StringComparer.Ordinal);

                    if (!ParseGroup(part, ref index, '\0', partCounts))
                        return false;

                    if (index != part.Length)
                        return false;

                    foreach (KeyValuePair<string, int> pair in partCounts)
                    {
                        if (!atomCounts.ContainsKey(pair.Key))
                            atomCounts[pair.Key] = 0;

                        atomCounts[pair.Key] += pair.Value * leadingMultiplier;
                    }
                }

                return atomCounts.Count > 0;
            }

            private static bool ParseGroup(
                string formula,
                ref int index,
                char closingCharacter,
                Dictionary<string, int> result)
            {
                bool foundSomething = false;

                while (index < formula.Length)
                {
                    char current = formula[index];

                    if (closingCharacter != '\0' && current == closingCharacter)
                        return foundSomething;

                    if (current == '(' || current == '[')
                    {
                        char expectedClosing = current == '(' ? ')' : ']';
                        index++;

                        Dictionary<string, int> nested =
                            new Dictionary<string, int>(StringComparer.Ordinal);

                        if (!ParseGroup(formula, ref index, expectedClosing, nested))
                            return false;

                        if (index >= formula.Length || formula[index] != expectedClosing)
                            return false;

                        index++; // Consume closing bracket.
                        int multiplier = ReadNumber(formula, ref index);

                        Merge(result, nested, multiplier);
                        foundSomething = true;
                        continue;
                    }

                    if (char.IsUpper(current))
                    {
                        string symbol = current.ToString();
                        index++;

                        if (index < formula.Length && char.IsLower(formula[index]))
                        {
                            symbol += formula[index];
                            index++;
                        }

                        int multiplier = ReadNumber(formula, ref index);

                        if (!result.ContainsKey(symbol))
                            result[symbol] = 0;

                        result[symbol] += multiplier;
                        foundSomething = true;
                        continue;
                    }

                    // A closing bracket not expected here, a lowercase
                    // letter without an element symbol, or any other
                    // unsupported character makes the formula invalid.
                    return false;
                }

                return closingCharacter == '\0' && foundSomething;
            }

            private static int ReadNumber(string formula, ref int index)
            {
                int start = index;

                while (index < formula.Length && char.IsDigit(formula[index]))
                    index++;

                if (start == index)
                    return 1;

                return int.Parse(
                    formula.Substring(start, index - start),
                    CultureInfo.InvariantCulture);
            }

            private static int ReadLeadingNumber(string formula, out int consumed)
            {
                consumed = 0;

                while (consumed < formula.Length && char.IsDigit(formula[consumed]))
                    consumed++;

                if (consumed == 0)
                    return 1;

                return int.Parse(
                    formula.Substring(0, consumed),
                    CultureInfo.InvariantCulture);
            }

            private static void Merge(
                Dictionary<string, int> destination,
                Dictionary<string, int> source,
                int multiplier)
            {
                foreach (KeyValuePair<string, int> pair in source)
                {
                    if (!destination.ContainsKey(pair.Key))
                        destination[pair.Key] = 0;

                    destination[pair.Key] += pair.Value * multiplier;
                }
            }
        }
    }
}
