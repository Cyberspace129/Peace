using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chemicals is a ScriptableObject asset containing reusable particle data.
///
/// A ScriptableObject is an asset stored in the Unity project.
/// It is useful for data that many runtime objects need to reference.
///
/// Example:
///     Chemicals data = ...;
///     Quark up = data.CreateQuark('u', ColorCharge.Red);
/// </summary>
[CreateAssetMenu(
    fileName = "Chemicals",
    menuName = "Particle Physics/Chemicals")]
public class Chemicals : ScriptableObject
{
    // ============================================================
    // QUARK FLAVOUR DEFINITIONS
    // ============================================================

    /// <summary>
    /// Describes a quark flavour.
    ///
    /// This is a definition, not an individual quark.
    /// Many Quark objects can refer to the same definition.
    ///
    /// [Serializable] allows Unity to serialize this class when it is
    /// stored inside a serialized field.
    /// </summary>
    [Serializable]
    public class QuarkFlavorDefinition
    {
        private const int ChargeDenominator = 3;

        [SerializeField] private char symbol;
        [SerializeField] private int generation;

        // Electric charge is stored as a numerator over 3.
        //
        // Up-type quarks:   +2/3
        // Down-type quarks: -1/3
        //
        // This avoids using floating-point values for exact charges.
        [SerializeField] private int chargeNumerator;

        // Quark masses are expressed in MeV/c².
        // These are approximate reference values.
        [SerializeField] private float massMeVPerC2;

        // Public properties provide read-only access to private data.
        public char Symbol => symbol;
        public int Generation => generation;

        public int ChargeNumerator => chargeNumerator;
        public int ChargeDenominatorValue => ChargeDenominator;

        public float MassMeVPerC2 => massMeVPerC2;

        public float ElectricCharge =>
            (float)chargeNumerator / ChargeDenominator;

        public QuarkFlavorDefinition(
            char symbol,
            int generation,
            int chargeNumerator,
            float massMeVPerC2)
        {
            this.symbol = symbol;
            this.generation = generation;
            this.chargeNumerator = chargeNumerator;
            this.massMeVPerC2 = massMeVPerC2;
        }
    }


    // ============================================================
    // FLAVOUR CATALOGUE
    // ============================================================

    /// <summary>
    /// The six quark flavour definitions.
    ///
    /// The list is serialized into the Chemicals asset, allowing
    /// the definitions to be inspected and edited in the Inspector.
    /// </summary>
    [SerializeField]
    private List<QuarkFlavorDefinition> flavors =
        new List<QuarkFlavorDefinition>
        {
            // Generation 1
            new QuarkFlavorDefinition('u', 1, +2, 2.16f),
            new QuarkFlavorDefinition('d', 1, -1, 4.70f),

            // Generation 2
            new QuarkFlavorDefinition('c', 2, +2, 1272.9f),
            new QuarkFlavorDefinition('s', 2, -1, 92.9f),

            // Generation 3
            new QuarkFlavorDefinition('t', 3, +2, 172600f),
            new QuarkFlavorDefinition('b', 3, -1, 4186f)
        };

    /// <summary>
    /// Exposes the catalogue as a read-only list.
    ///
    /// IReadOnlyList prevents callers from adding or removing entries
    /// through this property.
    /// </summary>
    public IReadOnlyList<QuarkFlavorDefinition> Flavors => flavors;


    /// <summary>
    /// Finds a flavour definition by its symbol.
    ///
    /// Throws an exception if the symbol is not in the catalogue.
    /// This is preferable to silently creating an invalid quark.
    /// </summary>
    public QuarkFlavorDefinition GetFlavor(char symbol)
    {
        foreach (QuarkFlavorDefinition flavor in flavors)
        {
            if (flavor.Symbol == symbol)
                return flavor;
        }

        throw new ArgumentException(
            $"Unknown quark flavour: '{symbol}'.",
            nameof(symbol));
    }


    /// <summary>
    /// Factory method that creates an individual quark.
    ///
    /// A factory method centralizes object creation.
    /// It ensures the quark uses a valid flavour definition.
    /// </summary>
    public Quark CreateQuark(
        char flavorSymbol,
        ColorCharge color,
        bool isAntiquark = false)
    {
        QuarkFlavorDefinition definition = GetFlavor(flavorSymbol);

        return new Quark(
            definition,
            color,
            isAntiquark);
    }


    // ============================================================
    // COLOUR CHARGE
    // ============================================================

    /// <summary>
    /// The three colour-charge labels used in Quantum Chromodynamics.
    ///
    /// These are quantum labels, not visible colours.
    /// They are not the same thing as RGB light.
    ///
    /// An antiquark has an anticolour corresponding to its colour label.
    /// </summary>
    public enum ColorCharge
    {
        Red,
        Green,
        Blue
    }


    // ============================================================
    // PARTICLE BASE CLASS
    // ============================================================

    /// <summary>
    /// Base class for particle objects.
    ///
    /// In OOP terminology, this is an abstract base class.
    /// It defines properties shared by different particle types.
    ///
    /// A Particle cannot be instantiated directly because it is abstract.
    /// Concrete classes such as Quark, Electron, and Gluon inherit from it.
    /// </summary>
    [Serializable]
    public abstract class Particle
    {
        // Electric charge is stored in units of one-third of the
        // elementary charge.
        //
        // Example:
        //     +2 means +2/3 e
        //     -3 means -1 e
        protected int chargeThirds;

        // Mass is expressed in MeV/c².
        protected float massMeVPerC2;

        // Spin is expressed in units of ħ.
        protected float spin;

        // Baryon number is also stored in thirds.
        //
        // Quark:       +1/3  -> +1
        // Antiquark:   -1/3  -> -1
        // Proton:      +1    -> +3
        // Meson:        0
        protected int baryonNumberThirds;

        public int ChargeThirds => chargeThirds;

        public float ElectricCharge =>
            chargeThirds / 3.0f;

        public float MassMeVPerC2 => massMeVPerC2;

        public float Spin => spin;

        public float BaryonNumber =>
            baryonNumberThirds / 3.0f;

        protected Particle(
            int chargeThirds,
            float massMeVPerC2,
            float spin,
            int baryonNumberThirds)
        {
            this.chargeThirds = chargeThirds;
            this.massMeVPerC2 = massMeVPerC2;
            this.spin = spin;
            this.baryonNumberThirds = baryonNumberThirds;
        }
    }


    // ============================================================
    // QUARK
    // ============================================================

    /// <summary>
    /// Represents one quark or antiquark.
    ///
    /// A Quark object references a flavour definition and stores
    /// the properties of this individual quark:
    ///
    /// - flavour
    /// - colour charge
    /// - particle or antiparticle identity
    ///
    /// Quarks are spin-1/2 fermions.
    /// </summary>
    [Serializable]
    public class Quark : Particle
    {
        private const float QuarkSpin = 0.5f;

        // The shared definition of this quark's flavour.
        private QuarkFlavorDefinition flavor;

        // The colour label of this quark.
        private ColorCharge color;

        // True means this object represents an antiquark.
        private bool isAntiquark;

        public QuarkFlavorDefinition Flavor => flavor;

        public ColorCharge Color => color;

        public bool IsAntiquark => isAntiquark;

        /// <summary>
        /// Returns the colour label in a readable form.
        ///
        /// For an antiquark, this label represents the corresponding
        /// anticolour state.
        /// </summary>
        public string ColorLabel =>
            isAntiquark
                ? $"Anti-{color}"
                : color.ToString();

        public Quark(
            QuarkFlavorDefinition flavor,
            ColorCharge color,
            bool isAntiquark = false)
            : base(
                // Antiquarks have the opposite electric charge.
                isAntiquark
                    ? -flavor.ChargeNumerator
                    : flavor.ChargeNumerator,

                flavor.MassMeVPerC2,

                QuarkSpin,

                // Quarks have baryon number +1/3.
                // Antiquarks have baryon number -1/3.
                isAntiquark ? -1 : +1)
        {
            if (flavor == null)
                throw new ArgumentNullException(nameof(flavor));

            this.flavor = flavor;
            this.color = color;
            this.isAntiquark = isAntiquark;
        }
    }


    // ============================================================
    // GLUON
    // ============================================================

    /// <summary>
    /// Represents a gluon in a simplified colour-flow model.
    ///
    /// Gluons are spin-1 gauge bosons of Quantum Chromodynamics (QCD).
    /// They carry colour charge and mediate the strong interaction.
    ///
    /// Important:
    /// A colour-anticolour pair is a useful teaching representation,
    /// but the physical gluon states are eight independent SU(3)
    /// colour states. The nine possible simple colour/anticolour
    /// combinations are not nine physical gluons.
    ///
    /// This class models a colour-flow label, not a complete
    /// mathematical representation of the QCD gluon field.
    /// </summary>
    [Serializable]
    public class Gluon : Particle
    {
        private const float GluonSpin = 1.0f;

        [SerializeField] private ColorCharge color;
        [SerializeField] private ColorCharge anticolor;

        public ColorCharge Color => color;
        public ColorCharge Anticolor => anticolor;

        public string ColorFlow =>
            $"{color} -> Anti-{anticolor}";

        public Gluon(
            ColorCharge color,
            ColorCharge anticolor)
            : base(
                chargeThirds: 0,
                massMeVPerC2: 0f,
                spin: GluonSpin,
                baryonNumberThirds: 0)
        {
            this.color = color;
            this.anticolor = anticolor;
        }
    }


    // ============================================================
    // ELECTRON
    // ============================================================

    /// <summary>
    /// Represents an electron.
    ///
    /// Electrons are elementary spin-1/2 leptons.
    /// They do not carry QCD colour charge.
    /// </summary>
    [Serializable]
    public class Electron : Particle
    {
        public const float ElectronMassMeVPerC2 = 0.51099895f;

        public Electron()
            : base(
                chargeThirds: -3,
                massMeVPerC2: ElectronMassMeVPerC2,
                spin: 0.5f,
                baryonNumberThirds: 0)
        {
        }
    }


    // ============================================================
    // HADRON BASE CLASS
    // ============================================================

    /// <summary>
    /// A hadron is a composite particle made from quarks and gluons.
    ///
    /// This simplified model stores the valence-quark content.
    /// It does not explicitly simulate the full gluon field or
    /// sea-quark contributions inside a hadron.
    /// </summary>
    [Serializable]
    public abstract class Hadron : Particle
    {
        protected Hadron(
            int chargeThirds,
            float massMeVPerC2,
            float spin,
            int baryonNumberThirds)
            : base(
                chargeThirds,
                massMeVPerC2,
                spin,
                baryonNumberThirds)
        {
        }
    }


    // ============================================================
    // BARYON
    // ============================================================

    /// <summary>
    /// A baryon is a hadron made from three quarks.
    ///
    /// Examples:
    ///     Proton:  uud
    ///     Neutron: udd
    ///
    /// A colour-neutral baryon is represented here by one red,
    /// one green, and one blue valence quark.
    ///
    /// This is a simplified colour basis. Real baryons are described
    /// by a quantum superposition of colour states.
    /// </summary>
    [Serializable]
    public class Baryon : Hadron
    {
        private readonly Quark[] quarks;

        public IReadOnlyList<Quark> Quarks => quarks;

        public Baryon(
            float massMeVPerC2,
            float spin,
            params Quark[] quarks)
            : base(
                chargeThirds: CalculateCharge(quarks),
                massMeVPerC2: massMeVPerC2,
                spin: spin,
                baryonNumberThirds: CalculateBaryonNumber(quarks))
        {
            if (quarks == null || quarks.Length != 3)
            {
                throw new ArgumentException(
                    "A baryon must contain exactly three quarks.");
            }

            if (!HasOneOfEachColor(quarks))
            {
                throw new ArgumentException(
                    "A baryon must be colour-neutral: " +
                    "one red, one green, and one blue quark " +
                    "in this simplified model.");
            }

            this.quarks = quarks;
        }

        private static int CalculateCharge(Quark[] quarks)
        {
            int total = 0;

            if (quarks == null)
                return 0;

            foreach (Quark quark in quarks)
                total += quark.ChargeThirds;

            return total;
        }

        private static int CalculateBaryonNumber(Quark[] quarks)
        {
            int total = 0;

            if (quarks == null)
                return 0;

            foreach (Quark quark in quarks)
                total += quark.IsAntiquark ? -1 : +1;

            return total;
        }

        private static bool HasOneOfEachColor(Quark[] quarks)
        {
            if (quarks == null || quarks.Length != 3)
                return false;

            bool red = false;
            bool green = false;
            bool blue = false;

            foreach (Quark quark in quarks)
            {
                if (quark.IsAntiquark)
                    return false;

                switch (quark.Color)
                {
                    case ColorCharge.Red:
                        if (red) return false;
                        red = true;
                        break;

                    case ColorCharge.Green:
                        if (green) return false;
                        green = true;
                        break;

                    case ColorCharge.Blue:
                        if (blue) return false;
                        blue = true;
                        break;
                }
            }

            return red && green && blue;
        }
    }


    // ============================================================
    // MESON
    // ============================================================

    /// <summary>
    /// A meson is a hadron containing a quark and an antiquark.
    ///
    /// Examples:
    ///     Pion:    u + anti-d
    ///     Kaon:    u + anti-s
    ///
    /// In this simplified colour model, the quark and antiquark
    /// must have matching colour/anticolour labels.
    /// </summary>
    [Serializable]
    public class Meson : Hadron
    {
        private readonly Quark quark;
        private readonly Quark antiquark;

        public Quark QuarkPart => quark;
        public Quark AntiquarkPart => antiquark;

        public Meson(
            float massMeVPerC2,
            float spin,
            Quark quark,
            Quark antiquark)
            : base(
                chargeThirds:
                    quark.ChargeThirds + antiquark.ChargeThirds,

                massMeVPerC2: massMeVPerC2,

                spin: spin,

                baryonNumberThirds: 0)
        {
            if (quark == null || antiquark == null)
                throw new ArgumentNullException(
                    "A meson requires a quark and an antiquark.");

            if (quark.IsAntiquark)
                throw new ArgumentException(
                    "The first component must be a quark.");

            if (!antiquark.IsAntiquark)
                throw new ArgumentException(
                    "The second component must be an antiquark.");

            if (quark.Color != antiquark.Color)
            {
                throw new ArgumentException(
                    "The quark and antiquark must have matching " +
                    "colour/anticolour labels in this model.");
            }

            this.quark = quark;
            this.antiquark = antiquark;
        }
    }


    // ============================================================
    // PROTON
    // ============================================================

    /// <summary>
    /// A proton is a baryon with valence-quark content uud.
    ///
    /// Its electric charge is:
    ///     +2/3 +2/3 -1/3 = +1
    ///
    /// The proton mass is measured as a whole.
    /// It is not equal to the sum of the current masses of u, u, d.
    /// </summary>
    [Serializable]
    public class Proton : Baryon
    {
        public const float ProtonMassMeVPerC2 = 938.272f;

        public Proton(Chemicals chemicals)
            : base(
                ProtonMassMeVPerC2,
                spin: 0.5f,

                chemicals.CreateQuark('u', ColorCharge.Red),
                chemicals.CreateQuark('u', ColorCharge.Green),
                chemicals.CreateQuark('d', ColorCharge.Blue))
        {
        }
    }


    // ============================================================
    // NEUTRON
    // ============================================================

    /// <summary>
    /// A neutron is a baryon with valence-quark content udd.
    ///
    /// Its electric charge is:
    ///     +2/3 -1/3 -1/3 = 0
    /// </summary>
    [Serializable]
    public class Neutron : Baryon
    {
        public const float NeutronMassMeVPerC2 = 939.565f;

        public Neutron(Chemicals chemicals)
            : base(
                NeutronMassMeVPerC2,
                spin: 0.5f,

                chemicals.CreateQuark('u', ColorCharge.Red),
                chemicals.CreateQuark('d', ColorCharge.Green),
                chemicals.CreateQuark('d', ColorCharge.Blue))
        {
        }
    }


    // ============================================================
    // ATOMIC SYSTEM
    // ============================================================

    /// <summary>
    /// Represents a simplified atom.
    ///
    /// An atom contains:
    ///     - a nucleus made from protons and neutrons
    ///     - electrons surrounding the nucleus
    ///
    /// Atomic number = number of protons.
    /// Mass number   = number of protons + neutrons.
    ///
    /// The number of protons determines the chemical element.
    /// Changing the neutron count produces a different isotope.
    /// </summary>
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

        public int MassNumber =>
            protons.Count + neutrons.Count;

        public int NetChargeThirds => ChargeThirds;

        public Atomic(
            int protonCount,
            int neutronCount,
            int electronCount)
            : base(
                chargeThirds:
                    (protonCount * 3) - (electronCount * 3),

                massMeVPerC2:
                    CalculateApproximateMass(
                        protonCount,
                        neutronCount,
                        electronCount),

                // This is only a placeholder.
                // Actual atomic spin depends on nuclear and
                // electron quantum states.
                spin: 0f,

                baryonNumberThirds:
                    (protonCount + neutronCount) * 3)
        {
            if (protonCount < 0 ||
                neutronCount < 0 ||
                electronCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    "Particle counts cannot be negative.");
            }

            protons = new List<Proton>();
            neutrons = new List<Neutron>();
            electrons = new List<Electron>();

            // Create the nucleus.
            //
            // These are individual runtime objects.
            // They are not the same thing as the reusable
            // flavour definitions stored in Chemicals.
            for (int i = 0; i < protonCount; i++)
                protons.Add(null);

            for (int i = 0; i < neutronCount; i++)
                neutrons.Add(null);

            // Create the electron cloud representation.
            for (int i = 0; i < electronCount; i++)
                electrons.Add(new Electron());
        }

        private static float CalculateApproximateMass(
            int protonCount,
            int neutronCount,
            int electronCount)
        {
            // This is a simple sum of constituent rest masses.
            //
            // It does NOT include nuclear binding energy.
            // Therefore it is an approximation, not a precision
            // atomic mass calculation.

            return
                protonCount * Proton.ProtonMassMeVPerC2 +
                neutronCount * Neutron.NeutronMassMeVPerC2 +
                electronCount * Electron.ElectronMassMeVPerC2;
        }
    }


    // ============================================================
    // RADIATION
    // ============================================================

    /// <summary>
    /// Describes common radiation categories.
    ///
    /// Radiation is energy or particles emitted by a source.
    /// Alpha, beta, gamma, and neutron radiation are not all
    /// the same kind of physical object.
    /// </summary>
    public enum RadiationType
    {
        Alpha,
        BetaMinus,
        BetaPlus,
        Gamma,
        Neutron
    }


    /// <summary>
    /// Represents the primary emitted particle in a radiation event.
    ///
    /// Simplification:
    /// Beta-minus decay also emits an electron antineutrino.
    /// Beta-plus decay also emits an electron neutrino.
    /// This class represents only the main emitted particle.
    /// </summary>
    [Serializable]
    public class Radiation : Particle
    {
        public RadiationType Type { get; }

        public Radiation(RadiationType type)
            : base(
                chargeThirds: GetCharge(type),
                massMeVPerC2: GetMass(type),
                spin: GetSpin(type),
                baryonNumberThirds: GetBaryonNumber(type))
        {
            Type = type;
        }

        private static int GetCharge(RadiationType type)
        {
            switch (type)
            {
                case RadiationType.BetaMinus:
                    return -3;

                case RadiationType.BetaPlus:
                    return +3;

                case RadiationType.Alpha:
                    return +6;

                default:
                    return 0;
            }
        }

        private static float GetMass(RadiationType type)
        {
            switch (type)
            {
                case RadiationType.Alpha:
                    // Approximate mass of a helium-4 nucleus.
                    return 3727.38f;

                case RadiationType.BetaMinus:
                case RadiationType.BetaPlus:
                    return Electron.ElectronMassMeVPerC2;

                case RadiationType.Neutron:
                    return Neutron.NeutronMassMeVPerC2;

                case RadiationType.Gamma:
                    return 0f;

                default:
                    return 0f;
            }
        }

        private static float GetSpin(RadiationType type)
        {
            switch (type)
            {
                case RadiationType.Alpha:
                    return 0f;

                case RadiationType.BetaMinus:
                case RadiationType.BetaPlus:
                    return 0.5f;

                case RadiationType.Gamma:
                    return 1f;

                case RadiationType.Neutron:
                    return 0.5f;

                default:
                    return 0f;
            }
        }

        private static int GetBaryonNumber(RadiationType type)
        {
            // Alpha particle = two protons + two neutrons.
            // Its baryon number is +4.
            if (type == RadiationType.Alpha)
                return 12; // 4 baryons, stored in thirds

            if (type == RadiationType.Neutron)
                return 3; // baryon number +1

            return 0;
        }
    }
}