using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParticleChemistry
{
    /// <summary>
    /// Mechanical material database and engineering-scale mechanics model.
    ///
    /// MaterialDefinition data is imported from the existing Material asset.
    /// MechanicalMaterialProfile stores reusable mechanical properties.
    /// MechanicalSample stores the state of one runtime piece of material.
    ///
    /// Models included:
    /// - Density, mass and volume conversion.
    /// - Linear elasticity and axial stiffness.
    /// - Simplified bilinear plastic deformation.
    /// - Tensile/compressive failure and a mode-I fracture-toughness check.
    /// - Mohs and Vickers hardness.
    /// - Static/kinetic contact friction.
    /// - Archard sliding-wear estimation.
    ///
    /// This is a reduced engineering model, not a finite-element solver.
    /// It does not model complex 3D stress fields, fatigue, crack propagation,
    /// viscoelasticity, strain-rate effects or detailed contact mechanics.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Mechanics",
        menuName = "Particle Physics/Mechanics")]
    public class Mechanics : ScriptableObject
    {
        private const float MinimumPositiveValue = 0.000001f;
        private const float Pi = Mathf.PI;

        // ============================================================
        // ENUMERATIONS
        // ============================================================

        public enum DeformationState
        {
            Elastic,
            Plastic,
            Fractured,
            Unknown
        }

        public enum LoadMode
        {
            Tension,
            Compression
        }

        // ============================================================
        // MECHANICAL MATERIAL PROFILE
        // ============================================================

        /// <summary>
        /// Reusable mechanical properties for a material type.
        ///
        /// All stresses and moduli are in pascals (Pa).
        /// HardnessPa is indentation hardness, normally supplied as a
        /// Vickers hardness converted to pascals. Mohs hardness is a
        /// separate, qualitative scratch scale.
        ///
        /// Use -1 for unknown or unavailable properties.
        /// </summary>
        [Serializable]
        public class MechanicalMaterialProfile
        {
            [Header("Identity")]
            [SerializeField] private string materialId;
            [SerializeField] private string displayName;

            [Header("Density and Hardness")]
            [Tooltip("Bulk density in kg/m³. Use -1 when unknown.")]
            [SerializeField] private float densityKgPerM3 = -1f;

            [Tooltip("Mohs scratch hardness, usually 1 to 10. This is not a pressure value.")]
            [SerializeField] private float mohsHardness = -1f;

            [Tooltip("Indentation hardness in Pa, usually converted from Vickers hardness. Required by the Archard wear model.")]
            [SerializeField] private float indentationHardnessPa = -1f;

            [Header("Elastic Properties")]
            [Tooltip("Young's modulus in Pa. Required for elastic deformation and axial stiffness.")]
            [SerializeField] private float youngsModulusPa = -1f;

            [Tooltip("Poisson's ratio. Common isotropic solids are usually between 0 and 0.5. Use -1 when unknown.")]
            [SerializeField] private float poissonsRatio = -1f;

            [Tooltip("Shear modulus in Pa. If unknown, it can be calculated from Young's modulus and Poisson's ratio.")]
            [SerializeField] private float shearModulusPa = -1f;

            [Tooltip("Bulk modulus in Pa. If unknown, it can be calculated from Young's modulus and Poisson's ratio.")]
            [SerializeField] private float bulkModulusPa = -1f;

            [Header("Plastic Deformation")]
            [Tooltip("Tensile yield strength in Pa. Use -1 when unknown.")]
            [SerializeField] private float tensileYieldStrengthPa = -1f;

            [Tooltip("Compressive yield strength in Pa. Use -1 when unknown.")]
            [SerializeField] private float compressiveYieldStrengthPa = -1f;

            [Tooltip("Bilinear tangent modulus in Pa after yielding. Zero represents ideal plastic flow in this simplified model.")]
            [SerializeField] private float plasticTangentModulusPa = 0f;

            [Header("Fracture and Strength")]
            [Tooltip("Ultimate tensile strength in Pa. Use -1 when unknown.")]
            [SerializeField] private float ultimateTensileStrengthPa = -1f;

            [Tooltip("Ultimate compressive strength in Pa. Use -1 when unknown.")]
            [SerializeField] private float ultimateCompressiveStrengthPa = -1f;

            [Tooltip("Mode-I fracture toughness KIC in Pa·√m. Use -1 when unknown.")]
            [SerializeField] private float fractureToughnessPaSqrtM = -1f;

            [Tooltip("Optional geometry factor Y used in K = Y × stress × sqrt(pi × crack length). Common simplified edge-crack estimates use about 1.12.")]
            [Min(0f)]
            [SerializeField] private float fractureGeometryFactor = 1.12f;

            [Header("Default Surface Friction")]
            [Tooltip("Optional default static friction coefficient. A contact-pair profile can override this. Use -1 when unknown.")]
            [SerializeField] private float defaultStaticFriction = -1f;

            [Tooltip("Optional default kinetic friction coefficient. A contact-pair profile can override this. Use -1 when unknown.")]
            [SerializeField] private float defaultKineticFriction = -1f;

            [TextArea(2, 4)]
            [SerializeField] private string notes;

            public string MaterialId { get { return materialId; } }
            public string DisplayName { get { return displayName; } }
            public float DensityKgPerM3 { get { return densityKgPerM3; } }
            public float MohsHardness { get { return mohsHardness; } }
            public float IndentationHardnessPa { get { return indentationHardnessPa; } }
            public float YoungsModulusPa { get { return youngsModulusPa; } }
            public float PoissonsRatio { get { return poissonsRatio; } }
            public float ShearModulusPa { get { return shearModulusPa; } }
            public float BulkModulusPa { get { return bulkModulusPa; } }
            public float TensileYieldStrengthPa { get { return tensileYieldStrengthPa; } }
            public float CompressiveYieldStrengthPa { get { return compressiveYieldStrengthPa; } }
            public float PlasticTangentModulusPa { get { return plasticTangentModulusPa; } }
            public float UltimateTensileStrengthPa { get { return ultimateTensileStrengthPa; } }
            public float UltimateCompressiveStrengthPa { get { return ultimateCompressiveStrengthPa; } }
            public float FractureToughnessPaSqrtM { get { return fractureToughnessPaSqrtM; } }
            public float FractureGeometryFactor { get { return fractureGeometryFactor; } }
            public float DefaultStaticFriction { get { return defaultStaticFriction; } }
            public float DefaultKineticFriction { get { return defaultKineticFriction; } }
            public string Notes { get { return notes; } }

            public MechanicalMaterialProfile(
                string materialId,
                string displayName,
                float densityKgPerM3 = -1f,
                float mohsHardness = -1f,
                float indentationHardnessPa = -1f,
                float youngsModulusPa = -1f,
                float poissonsRatio = -1f,
                float shearModulusPa = -1f,
                float bulkModulusPa = -1f,
                float tensileYieldStrengthPa = -1f,
                float compressiveYieldStrengthPa = -1f,
                float plasticTangentModulusPa = 0f,
                float ultimateTensileStrengthPa = -1f,
                float ultimateCompressiveStrengthPa = -1f,
                float fractureToughnessPaSqrtM = -1f,
                float fractureGeometryFactor = 1.12f,
                float defaultStaticFriction = -1f,
                float defaultKineticFriction = -1f,
                string notes = "")
            {
                if (string.IsNullOrWhiteSpace(materialId))
                    throw new ArgumentException("A mechanical profile requires a material ID.", nameof(materialId));

                this.materialId = materialId.Trim();
                this.displayName = string.IsNullOrWhiteSpace(displayName) ? materialId : displayName;
                this.densityKgPerM3 = densityKgPerM3;
                this.mohsHardness = mohsHardness;
                this.indentationHardnessPa = indentationHardnessPa;
                this.youngsModulusPa = youngsModulusPa;
                this.poissonsRatio = poissonsRatio;
                this.shearModulusPa = shearModulusPa;
                this.bulkModulusPa = bulkModulusPa;
                this.tensileYieldStrengthPa = tensileYieldStrengthPa;
                this.compressiveYieldStrengthPa = compressiveYieldStrengthPa;
                this.plasticTangentModulusPa = Mathf.Max(0f, plasticTangentModulusPa);
                this.ultimateTensileStrengthPa = ultimateTensileStrengthPa;
                this.ultimateCompressiveStrengthPa = ultimateCompressiveStrengthPa;
                this.fractureToughnessPaSqrtM = fractureToughnessPaSqrtM;
                this.fractureGeometryFactor = Mathf.Max(0f, fractureGeometryFactor);
                this.defaultStaticFriction = defaultStaticFriction;
                this.defaultKineticFriction = defaultKineticFriction;
                this.notes = notes;
            }

            public float GetShearModulusPa()
            {
                if (shearModulusPa > 0f)
                    return shearModulusPa;

                if (youngsModulusPa > 0f && poissonsRatio >= 0f && poissonsRatio < 0.5f)
                    return youngsModulusPa / (2f * (1f + poissonsRatio));

                return -1f;
            }

            public float GetBulkModulusPa()
            {
                if (bulkModulusPa > 0f)
                    return bulkModulusPa;

                if (youngsModulusPa > 0f && poissonsRatio >= 0f && poissonsRatio < 0.5f)
                    return youngsModulusPa / (3f * (1f - 2f * poissonsRatio));

                return -1f;
            }
        }

        // ============================================================
        // CONTACT-PAIR FRICTION AND WEAR PROFILE
        // ============================================================

        /// <summary>
        /// Friction and wear belong to a pair of contacting surfaces.
        /// The two Archard wear coefficients may differ for each surface.
        /// Coefficients are dimensionless empirical values and require calibration.
        /// </summary>
        [Serializable]
        public class FrictionPairProfile
        {
            [SerializeField] private string materialAId;
            [SerializeField] private string materialBId;
            [SerializeField] private float staticFrictionCoefficient = -1f;
            [SerializeField] private float kineticFrictionCoefficient = -1f;
            [SerializeField] private float wearCoefficientA = -1f;
            [SerializeField] private float wearCoefficientB = -1f;

            [TextArea(1, 3)]
            [SerializeField] private string notes;

            public string MaterialAId { get { return materialAId; } }
            public string MaterialBId { get { return materialBId; } }
            public float StaticFrictionCoefficient { get { return staticFrictionCoefficient; } }
            public float KineticFrictionCoefficient { get { return kineticFrictionCoefficient; } }
            public float WearCoefficientA { get { return wearCoefficientA; } }
            public float WearCoefficientB { get { return wearCoefficientB; } }
            public string Notes { get { return notes; } }

            public FrictionPairProfile(
                string materialAId,
                string materialBId,
                float staticFrictionCoefficient,
                float kineticFrictionCoefficient,
                float wearCoefficientA = -1f,
                float wearCoefficientB = -1f,
                string notes = "")
            {
                if (string.IsNullOrWhiteSpace(materialAId) || string.IsNullOrWhiteSpace(materialBId))
                    throw new ArgumentException("A friction pair requires two material IDs.");
                if (string.Equals(materialAId, materialBId, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("A friction pair must contain two different material IDs.");
                if (staticFrictionCoefficient < 0f || kineticFrictionCoefficient < 0f)
                    throw new ArgumentOutOfRangeException(nameof(staticFrictionCoefficient), "Friction coefficients cannot be negative.");
                if (kineticFrictionCoefficient > staticFrictionCoefficient)
                    throw new ArgumentException("Kinetic friction is normally no greater than static friction.");

                // Canonical ordering makes lookup independent of the order
                // in which the two contacting materials are supplied.
                bool swap = string.Compare(materialAId, materialBId, StringComparison.OrdinalIgnoreCase) > 0;
                this.materialAId = swap ? materialBId.Trim() : materialAId.Trim();
                this.materialBId = swap ? materialAId.Trim() : materialBId.Trim();
                this.staticFrictionCoefficient = staticFrictionCoefficient;
                this.kineticFrictionCoefficient = kineticFrictionCoefficient;
                this.wearCoefficientA = swap ? wearCoefficientB : wearCoefficientA;
                this.wearCoefficientB = swap ? wearCoefficientA : wearCoefficientB;
                this.notes = notes;
            }

            public float GetWearCoefficient(string materialId)
            {
                if (string.Equals(materialId, materialAId, StringComparison.OrdinalIgnoreCase))
                    return wearCoefficientA;
                if (string.Equals(materialId, materialBId, StringComparison.OrdinalIgnoreCase))
                    return wearCoefficientB;
                return -1f;
            }
        }

        // ============================================================
        // RUNTIME MATERIAL SAMPLE
        // ============================================================

        /// <summary>
        /// One physical piece of material. Its dimensions, mass, plastic strain,
        /// crack length and wear state belong here, not in the shared profile.
        /// The volume is treated as a reference bulk volume for this reduced model.
        /// </summary>
        [Serializable]
        public class MechanicalSample
        {
            private readonly MechanicalMaterialProfile profile;
            [SerializeField] private float massKg;
            [SerializeField] private float volumeM3;
            [SerializeField] private float accumulatedWearVolumeM3;
            [SerializeField] private float currentAxialStrain;
            [SerializeField] private float permanentPlasticStrain;
            [SerializeField] private float crackLengthM;
            [SerializeField] private bool isFractured;

            public MechanicalMaterialProfile Profile { get { return profile; } }
            public float MassKg { get { return massKg; } }
            public float VolumeM3 { get { return volumeM3; } }
            public float DensityKgPerM3 { get { return volumeM3 > 0f ? massKg / volumeM3 : -1f; } }
            public float AccumulatedWearVolumeM3 { get { return accumulatedWearVolumeM3; } }
            public float CurrentAxialStrain { get { return currentAxialStrain; } }
            public float PermanentPlasticStrain { get { return permanentPlasticStrain; } }
            public float CrackLengthM { get { return crackLengthM; } }
            public bool IsFractured { get { return isFractured; } }

            internal MechanicalSample(MechanicalMaterialProfile profile, float massKg, float volumeM3)
            {
                if (profile == null)
                    throw new ArgumentNullException(nameof(profile));
                ValidatePositive(massKg, nameof(massKg));
                ValidatePositive(volumeM3, nameof(volumeM3));

                this.profile = profile;
                this.massKg = massKg;
                this.volumeM3 = volumeM3;
                accumulatedWearVolumeM3 = 0f;
                currentAxialStrain = 0f;
                permanentPlasticStrain = 0f;
                crackLengthM = 0f;
                isFractured = false;
            }

            internal void SetAxialState(float totalStrain, float plasticStrain, bool fractured)
            {
                currentAxialStrain = totalStrain;
                permanentPlasticStrain = plasticStrain;
                if (fractured)
                    isFractured = true;
            }

            internal void SetCrackLength(float value)
            {
                crackLengthM = Mathf.Max(0f, value);
            }

            internal void AddExternalMass(float addedMassKg)
            {
                massKg += Mathf.Max(0f, addedMassKg);
            }

            internal void RemoveExternalMassWithoutVolumeChange(float removedMassKg)
            {
                if (float.IsNaN(removedMassKg) || float.IsInfinity(removedMassKg) || removedMassKg < 0f)
                    throw new ArgumentOutOfRangeException(nameof(removedMassKg));
                massKg = Mathf.Max(0f, massKg - removedMassKg);
            }

            internal void SetVolumeM3(float value)
            {
                if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(value));
                volumeM3 = Mathf.Max(MinimumPositiveValue, value);
            }

            internal float ApplyWearVolume(float requestedVolumeM3)
            {
                if (requestedVolumeM3 <= 0f || volumeM3 <= 0f || massKg <= 0f)
                    return 0f;

                float removedVolume = Mathf.Min(requestedVolumeM3, volumeM3);
                float density = DensityKgPerM3;
                float removedMass = Mathf.Min(massKg, removedVolume * density);

                volumeM3 -= removedVolume;
                massKg -= removedMass;
                accumulatedWearVolumeM3 += removedVolume;

                if (volumeM3 <= MinimumPositiveValue || massKg <= MinimumPositiveValue)
                {
                    volumeM3 = 0f;
                    massKg = 0f;
                }

                return removedVolume;
            }
        }

        // ============================================================
        // RESULT TYPES
        // ============================================================

        [Serializable]
        public class AxialResponse
        {
            public float AppliedStrain { get; private set; }
            public float StressPa { get; private set; }
            public float ForceN { get; private set; }
            public float ElasticStrain { get; private set; }
            public float PlasticStrain { get; private set; }
            public float DisplacementM { get; private set; }
            public float AxialStiffnessNPerM { get; private set; }
            public float SafetyFactor { get; private set; }
            public bool HasYielded { get; private set; }
            public bool HasFractured { get; private set; }
            public bool FractureToughnessExceeded { get; private set; }
            public DeformationState State { get; private set; }

            public AxialResponse(
                float appliedStrain,
                float stressPa,
                float forceN,
                float elasticStrain,
                float plasticStrain,
                float displacementM,
                float axialStiffnessNPerM,
                float safetyFactor,
                bool hasYielded,
                bool hasFractured,
                bool fractureToughnessExceeded,
                DeformationState state)
            {
                AppliedStrain = appliedStrain;
                StressPa = stressPa;
                ForceN = forceN;
                ElasticStrain = elasticStrain;
                PlasticStrain = plasticStrain;
                DisplacementM = displacementM;
                AxialStiffnessNPerM = axialStiffnessNPerM;
                SafetyFactor = safetyFactor;
                HasYielded = hasYielded;
                HasFractured = hasFractured;
                FractureToughnessExceeded = fractureToughnessExceeded;
                State = state;
            }
        }

        [Serializable]
        public class FrictionResult
        {
            public float AppliedTangentialForceN { get; private set; }
            public float FrictionForceN { get; private set; }
            public float MaximumStaticFrictionN { get; private set; }
            public float MaximumKineticFrictionN { get; private set; }
            public bool IsSliding { get; private set; }
            public bool HasProfile { get; private set; }

            public FrictionResult(
                float appliedTangentialForceN,
                float frictionForceN,
                float maximumStaticFrictionN,
                float maximumKineticFrictionN,
                bool isSliding,
                bool hasProfile)
            {
                AppliedTangentialForceN = appliedTangentialForceN;
                FrictionForceN = frictionForceN;
                MaximumStaticFrictionN = maximumStaticFrictionN;
                MaximumKineticFrictionN = maximumKineticFrictionN;
                IsSliding = isSliding;
                HasProfile = hasProfile;
            }
        }

        [Serializable]
        public class WearResult
        {
            public float RequestedWearVolumeM3 { get; private set; }
            public float RemovedWearVolumeM3 { get; private set; }
            public float RemovedMassKg { get; private set; }
            public float RemainingMassKg { get; private set; }
            public float RemainingVolumeM3 { get; private set; }
            public bool HasSufficientData { get; private set; }

            public WearResult(
                float requestedWearVolumeM3,
                float removedWearVolumeM3,
                float removedMassKg,
                float remainingMassKg,
                float remainingVolumeM3,
                bool hasSufficientData)
            {
                RequestedWearVolumeM3 = requestedWearVolumeM3;
                RemovedWearVolumeM3 = removedWearVolumeM3;
                RemovedMassKg = removedMassKg;
                RemainingMassKg = remainingMassKg;
                RemainingVolumeM3 = remainingVolumeM3;
                HasSufficientData = hasSufficientData;
            }
        }

        // ============================================================
        // SERIALIZED CATALOGUES
        // ============================================================

        [Header("Mechanical Material Profiles")]
        [SerializeField] private List<MechanicalMaterialProfile> profiles =
            new List<MechanicalMaterialProfile>();

        [Header("Contact Friction and Wear Profiles")]
        [SerializeField] private List<FrictionPairProfile> frictionPairs =
            new List<FrictionPairProfile>();

        [Header("Optional Material Database")]
        [SerializeField] private ParticleChemistry.Material materialDatabase;

        public IReadOnlyList<MechanicalMaterialProfile> Profiles { get { return profiles; } }
        public IReadOnlyList<FrictionPairProfile> FrictionPairs { get { return frictionPairs; } }
        public ParticleChemistry.Material MaterialDatabase { get { return materialDatabase; } }

        // ============================================================
        // INITIALIZATION AND PROFILE IMPORT
        // ============================================================

        private void OnEnable()
        {
            if (profiles == null)
                profiles = new List<MechanicalMaterialProfile>();
            if (frictionPairs == null)
                frictionPairs = new List<FrictionPairProfile>();
        }

        /// <summary>
        /// Imports every material that does not already have a mechanical profile.
        /// Density and Mohs hardness are copied when available. Mechanical strength,
        /// elastic modulus, indentation hardness and friction remain unknown.
        /// </summary>
        [ContextMenu("Add Missing Profiles From Material Database")]
        public void AddMissingProfilesFromMaterialDatabase()
        {
            if (materialDatabase == null)
                throw new InvalidOperationException("Assign a ParticleChemistry.Material database first.");

            EnsureLists();
            int added = 0;

            foreach (ParticleChemistry.Material.MaterialDefinition material in materialDatabase.Materials)
            {
                if (material == null || string.IsNullOrWhiteSpace(material.Id) || HasProfile(material.Id))
                    continue;

                profiles.Add(new MechanicalMaterialProfile(
                    materialId: material.Id,
                    displayName: material.Name,
                    densityKgPerM3: material.DensityKgPerM3,
                    mohsHardness: material.MohsHardness,
                    notes: "Imported from the Material database. Add measured elastic, strength, indentation-hardness and friction data.")
                );
                added++;
            }

            MarkAssetDirty();
            Debug.Log("Mechanics: added " + added + " missing mechanical profiles.", this);
        }

        /// <summary>
        /// Imports missing mechanical profiles from the central material database.
        /// Density and Mohs hardness are copied when available. Unknown elastic,
        /// strength, indentation and friction values remain explicitly unknown.
        /// </summary>
        public void AddMissingProfilesFromMaterialDatabase(ParticleChemistry.Material database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            EnsureLists();
            int added = 0;
            foreach (ParticleChemistry.Material.MaterialDefinition material in database.Materials)
            {
                if (material == null || string.IsNullOrWhiteSpace(material.Id) || HasProfile(material.Id))
                    continue;

                profiles.Add(new MechanicalMaterialProfile(
                    materialId: material.Id,
                    displayName: material.Name,
                    densityKgPerM3: material.DensityKgPerM3,
                    mohsHardness: material.MohsHardness,
                    notes: "Imported from the central Material database. Add measured elastic, strength, indentation-hardness and friction data."));
                added++;
            }

            if (added > 0)
                MarkAssetDirty();
        }

        // ============================================================
        // PROFILE LOOKUP AND MANAGEMENT
        // ============================================================

        public bool HasProfile(string materialId)
        {
            if (string.IsNullOrWhiteSpace(materialId) || profiles == null)
                return false;

            foreach (MechanicalMaterialProfile profile in profiles)
            {
                if (profile != null && string.Equals(
                    profile.MaterialId, materialId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public MechanicalMaterialProfile GetProfile(string materialId)
        {
            if (string.IsNullOrWhiteSpace(materialId))
                throw new ArgumentException("Material ID cannot be empty.", nameof(materialId));

            if (profiles != null)
            {
                foreach (MechanicalMaterialProfile profile in profiles)
                {
                    if (profile != null && string.Equals(
                        profile.MaterialId, materialId, StringComparison.OrdinalIgnoreCase))
                        return profile;
                }
            }

            throw new ArgumentException("No mechanical profile exists for material '" + materialId + "'.");
        }

        public void AddProfile(MechanicalMaterialProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            if (HasProfile(profile.MaterialId))
                throw new InvalidOperationException("Mechanical profile '" + profile.MaterialId + "' already exists.");

            EnsureLists();
            profiles.Add(profile);
            MarkAssetDirty();
        }

        // ============================================================
        // FRICTION-PAIR LOOKUP AND MANAGEMENT
        // ============================================================

        public bool HasFrictionPair(string materialAId, string materialBId)
        {
            return FindFrictionPair(materialAId, materialBId) != null;
        }

        public FrictionPairProfile GetFrictionPair(string materialAId, string materialBId)
        {
            FrictionPairProfile result = FindFrictionPair(materialAId, materialBId);
            if (result == null)
                throw new ArgumentException("No friction-pair profile exists for '" + materialAId + "' and '" + materialBId + "'.");
            return result;
        }

        private FrictionPairProfile FindFrictionPair(string materialAId, string materialBId)
        {
            if (string.IsNullOrWhiteSpace(materialAId) || string.IsNullOrWhiteSpace(materialBId) || frictionPairs == null)
                return null;

            foreach (FrictionPairProfile pair in frictionPairs)
            {
                if (pair == null)
                    continue;

                bool direct = string.Equals(pair.MaterialAId, materialAId, StringComparison.OrdinalIgnoreCase) &&
                              string.Equals(pair.MaterialBId, materialBId, StringComparison.OrdinalIgnoreCase);
                bool reverse = string.Equals(pair.MaterialAId, materialBId, StringComparison.OrdinalIgnoreCase) &&
                               string.Equals(pair.MaterialBId, materialAId, StringComparison.OrdinalIgnoreCase);
                if (direct || reverse)
                    return pair;
            }

            return null;
        }

        public void AddFrictionPair(FrictionPairProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            if (!HasProfile(profile.MaterialAId) || !HasProfile(profile.MaterialBId))
                throw new ArgumentException("Both materials must have mechanical profiles before adding a friction pair.");
            if (HasFrictionPair(profile.MaterialAId, profile.MaterialBId))
                throw new InvalidOperationException("A friction profile for this material pair already exists.");

            EnsureLists();
            frictionPairs.Add(profile);
            MarkAssetDirty();
        }

        // ============================================================
        // DENSITY, MASS AND VOLUME
        // ============================================================

        public float CalculateMassKg(string materialId, float volumeM3)
        {
            ValidatePositive(volumeM3, nameof(volumeM3));
            float density = GetProfile(materialId).DensityKgPerM3;
            if (density <= 0f)
                throw new InvalidOperationException("Material '" + materialId + "' has no valid density.");
            return density * volumeM3;
        }

        public float CalculateVolumeM3(string materialId, float massKg)
        {
            ValidatePositive(massKg, nameof(massKg));
            float density = GetProfile(materialId).DensityKgPerM3;
            if (density <= 0f)
                throw new InvalidOperationException("Material '" + materialId + "' has no valid density.");
            return massKg / density;
        }

        public float CalculateDensityKgPerM3(float massKg, float volumeM3)
        {
            ValidatePositive(massKg, nameof(massKg));
            ValidatePositive(volumeM3, nameof(volumeM3));
            return massKg / volumeM3;
        }

        public MechanicalSample CreateSample(string materialId, float volumeM3)
        {
            float massKg = CalculateMassKg(materialId, volumeM3);
            return new MechanicalSample(GetProfile(materialId), massKg, volumeM3);
        }

        /// <summary>
        /// Creates a mechanical sample using measured mass and measured bulk volume.
        /// Unlike CreateSample, this does not infer one from the profile density.
        /// </summary>
        public MechanicalSample CreateSampleWithGeometry(
            string materialId,
            float massKg,
            float volumeM3)
        {
            ValidatePositive(massKg, nameof(massKg));
            ValidatePositive(volumeM3, nameof(volumeM3));
            return new MechanicalSample(GetProfile(materialId), massKg, volumeM3);
        }

        public MechanicalSample CreateSampleByMass(string materialId, float massKg)
        {
            float volumeM3 = CalculateVolumeM3(materialId, massKg);
            return new MechanicalSample(GetProfile(materialId), massKg, volumeM3);
        }

        /// <summary>
        /// Synchronises mass changes caused by thermal, chemical, fluid or biological
        /// processes. Negative values remove mass at the sample's current bulk density;
        /// positive values add mass without changing the external volume (for example,
        /// fluid filling existing pores).
        /// </summary>
        public void ApplyExternalMassChange(
            MechanicalSample sample,
            float massChangeKg,
            bool preserveVolumeOnMassLoss = false)
        {
            if (sample == null)
                throw new ArgumentNullException(nameof(sample));
            if (float.IsNaN(massChangeKg) || float.IsInfinity(massChangeKg))
                throw new ArgumentOutOfRangeException(nameof(massChangeKg));

            if (massChangeKg < 0f)
            {
                if (preserveVolumeOnMassLoss)
                {
                    sample.RemoveExternalMassWithoutVolumeChange(-massChangeKg);
                }
                else
                {
                    float density = sample.DensityKgPerM3;
                    if (density <= 0f)
                        throw new InvalidOperationException("Cannot remove mass when sample density is undefined.");
                    sample.ApplyWearVolume((-massChangeKg) / density);
                }
            }
            else if (massChangeKg > 0f)
            {
                sample.AddExternalMass(massChangeKg);
            }
        }

        // ============================================================
        // ELASTICITY AND STIFFNESS
        // ============================================================

        /// <summary>
        /// Linear elastic axial stiffness: k = E A / L.
        /// Cross-sectional area is in m², length in m, result in N/m.
        /// </summary>
        public float CalculateAxialStiffnessNPerM(
            string materialId,
            float crossSectionAreaM2,
            float gaugeLengthM)
        {
            ValidatePositive(crossSectionAreaM2, nameof(crossSectionAreaM2));
            ValidatePositive(gaugeLengthM, nameof(gaugeLengthM));

            float youngsModulus = GetProfile(materialId).YoungsModulusPa;
            if (youngsModulus <= 0f)
                throw new InvalidOperationException("Material '" + materialId + "' has no Young's modulus.");

            return youngsModulus * crossSectionAreaM2 / gaugeLengthM;
        }

        /// <summary>
        /// Calculates a uniaxial stress-strain response using a bilinear model.
        /// Positive strain is tension; negative strain is compression.
        /// This method does not modify the sample.
        /// </summary>
        public AxialResponse CalculateAxialResponse(
            string materialId,
            float engineeringStrain,
            float crossSectionAreaM2,
            float gaugeLengthM,
            float crackLengthM = 0f)
        {
            MechanicalMaterialProfile profile = GetProfile(materialId);
            return EvaluateAxialResponse(
                profile, engineeringStrain, crossSectionAreaM2, gaugeLengthM,
                crackLengthM, false);
        }

        /// <summary>
        /// Applies a specified total engineering strain to a runtime sample.
        /// The bilinear law uses Young's modulus up to yield, then the configured
        /// plastic tangent modulus. Sample volume is updated using the axial strain
        /// and, when known, an isotropic Poisson contraction approximation. The
        /// calculation is monotonic and does not model cyclic unloading or the
        /// Bauschinger effect.
        /// </summary>
        public AxialResponse ApplyAxialStrain(
            MechanicalSample sample,
            float engineeringStrain,
            float crossSectionAreaM2,
            float gaugeLengthM)
        {
            if (sample == null)
                throw new ArgumentNullException(nameof(sample));

            float previousStrain = sample.CurrentAxialStrain;
            AxialResponse response = EvaluateAxialResponse(
                sample.Profile,
                engineeringStrain,
                crossSectionAreaM2,
                gaugeLengthM,
                sample.CrackLengthM,
                sample.IsFractured);

            // Update the specimen geometry from the change in total strain.
            // If Poisson's ratio is unknown, assume a constant cross-section.
            // This is an isotropic small-strain approximation, not a 3D solution.
            float previousAxialScale = Mathf.Max(MinimumPositiveValue, 1f + previousStrain);
            float newAxialScale = Mathf.Max(MinimumPositiveValue, 1f + response.AppliedStrain);
            float lateralScaleRatio = 1f;
            float poissonRatio = sample.Profile.PoissonsRatio;
            if (poissonRatio >= 0f && poissonRatio < 0.5f)
            {
                float previousLateralScale = Mathf.Max(
                    MinimumPositiveValue, 1f - poissonRatio * previousStrain);
                float newLateralScale = Mathf.Max(
                    MinimumPositiveValue, 1f - poissonRatio * response.AppliedStrain);
                lateralScaleRatio = newLateralScale / previousLateralScale;
            }

            float volumeScale = (newAxialScale / previousAxialScale) *
                lateralScaleRatio * lateralScaleRatio;
            float updatedVolume = sample.VolumeM3 * volumeScale;
            if (updatedVolume > MinimumPositiveValue &&
                !float.IsNaN(updatedVolume) && !float.IsInfinity(updatedVolume))
            {
                sample.SetVolumeM3(updatedVolume);
            }

            sample.SetAxialState(
                response.AppliedStrain,
                response.PlasticStrain,
                response.HasFractured);

            return response;
        }

        private static AxialResponse EvaluateAxialResponse(
            MechanicalMaterialProfile profile,
            float engineeringStrain,
            float crossSectionAreaM2,
            float gaugeLengthM,
            float crackLengthM,
            bool alreadyFractured)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            ValidateFinite(engineeringStrain, nameof(engineeringStrain));
            if (engineeringStrain <= -1f)
                throw new ArgumentOutOfRangeException(
                    nameof(engineeringStrain), "Engineering compression strain must be greater than -1.");
            ValidatePositive(crossSectionAreaM2, nameof(crossSectionAreaM2));
            ValidatePositive(gaugeLengthM, nameof(gaugeLengthM));
            ValidateNonNegative(crackLengthM, nameof(crackLengthM));

            float youngsModulus = profile.YoungsModulusPa;
            if (youngsModulus <= 0f)
                throw new InvalidOperationException("Material '" + profile.MaterialId + "' has no Young's modulus.");

            bool tension = engineeringStrain >= 0f;
            float sign = tension ? 1f : -1f;
            float absoluteStrain = Mathf.Abs(engineeringStrain);
            float yieldStrength = tension
                ? profile.TensileYieldStrengthPa
                : profile.CompressiveYieldStrengthPa;
            float ultimateStrength = tension
                ? profile.UltimateTensileStrengthPa
                : profile.UltimateCompressiveStrengthPa;

            float stressMagnitude;
            float plasticMagnitude = 0f;
            bool yielded = false;

            if (yieldStrength > 0f)
            {
                float yieldStrain = yieldStrength / youngsModulus;
                if (absoluteStrain <= yieldStrain)
                {
                    stressMagnitude = youngsModulus * absoluteStrain;
                }
                else
                {
                    yielded = true;
                    float tangent = profile.PlasticTangentModulusPa;
                    if (tangent > 0f)
                    {
                        plasticMagnitude = (absoluteStrain - yieldStrain) /
                            (1f + tangent / youngsModulus);
                        stressMagnitude = yieldStrength + tangent * plasticMagnitude;
                    }
                    else
                    {
                        plasticMagnitude = absoluteStrain - yieldStrain;
                        stressMagnitude = yieldStrength;
                    }
                }
            }
            else
            {
                // Without a yield value, the model remains linear elastic.
                stressMagnitude = youngsModulus * absoluteStrain;
            }

            float stress = sign * stressMagnitude;
            float force = stress * crossSectionAreaM2;
            float elasticStrain = stressMagnitude / youngsModulus;
            float signedPlasticStrain = sign * plasticMagnitude;
            float displacement = engineeringStrain * gaugeLengthM;
            float stiffness = youngsModulus * crossSectionAreaM2 / gaugeLengthM;

            bool strengthExceeded = ultimateStrength > 0f && stressMagnitude >= ultimateStrength;
            float safetyFactor = ultimateStrength > 0f && stressMagnitude > 0f
                ? ultimateStrength / stressMagnitude
                : -1f;

            bool toughnessExceeded = false;
            if (profile.FractureToughnessPaSqrtM > 0f && crackLengthM > 0f)
            {
                float stressIntensity = profile.FractureGeometryFactor *
                    stressMagnitude * Mathf.Sqrt(Pi * crackLengthM);
                toughnessExceeded = stressIntensity >= profile.FractureToughnessPaSqrtM;
            }

            bool fractured = alreadyFractured || strengthExceeded || toughnessExceeded;
            DeformationState state = fractured
                ? DeformationState.Fractured
                : (yielded ? DeformationState.Plastic : DeformationState.Elastic);

            return new AxialResponse(
                engineeringStrain,
                stress,
                force,
                sign * elasticStrain,
                signedPlasticStrain,
                displacement,
                stiffness,
                safetyFactor,
                yielded,
                fractured,
                toughnessExceeded,
                state);
        }

        /// <summary>
        /// Sets a representative crack length for later fracture-toughness checks.
        /// This does not itself mean the sample has fractured.
        /// </summary>
        public void SetCrackLength(MechanicalSample sample, float crackLengthM)
        {
            if (sample == null)
                throw new ArgumentNullException(nameof(sample));
            ValidateNonNegative(crackLengthM, nameof(crackLengthM));
            sample.SetCrackLength(crackLengthM);
        }

        // ============================================================
        // HARDNESS QUERIES
        // ============================================================

        public float GetMohsHardness(string materialId)
        {
            return GetProfile(materialId).MohsHardness;
        }

        public float GetIndentationHardnessPa(string materialId)
        {
            return GetProfile(materialId).IndentationHardnessPa;
        }

        // ============================================================
        // FRICTION
        // ============================================================

        /// <summary>
        /// Calculates the friction force opposing an applied tangential force.
        /// Static friction is used until its maximum is exceeded; kinetic friction
        /// is used once sliding occurs. A pair-specific profile is required.
        /// </summary>
        public FrictionResult CalculateContactFriction(
            string materialAId,
            string materialBId,
            float normalLoadN,
            float appliedTangentialForceN,
            bool isAlreadySliding = false)
        {
            ValidateNonNegative(normalLoadN, nameof(normalLoadN));
            ValidateFinite(appliedTangentialForceN, nameof(appliedTangentialForceN));

            FrictionPairProfile pair = GetFrictionPair(materialAId, materialBId);
            float maxStatic = pair.StaticFrictionCoefficient * normalLoadN;
            float maxKinetic = pair.KineticFrictionCoefficient * normalLoadN;
            float appliedMagnitude = Mathf.Abs(appliedTangentialForceN);
            bool sliding = isAlreadySliding || appliedMagnitude > maxStatic;
            float frictionMagnitude;

            if (sliding)
                frictionMagnitude = maxKinetic;
            else
                frictionMagnitude = appliedMagnitude;

            float direction = appliedTangentialForceN >= 0f ? -1f : 1f;
            float frictionForce = direction * frictionMagnitude;

            return new FrictionResult(
                appliedTangentialForceN,
                frictionForce,
                maxStatic,
                maxKinetic,
                sliding,
                true);
        }

        // ============================================================
        // SLIDING WEAR: ARCHARD MODEL
        // ============================================================

        /// <summary>
        /// Estimates wear volume using Archard's law:
        /// V = k × F × s / H.
        /// k is a dimensionless empirical wear coefficient, F is normal load
        /// in newtons, s is sliding distance in metres and H is hardness in Pa.
        /// The pair profile supplies the wear coefficient for this sample's side.
        /// </summary>
        public WearResult ApplySlidingWear(
            MechanicalSample sample,
            string counterfaceMaterialId,
            float normalLoadN,
            float slidingDistanceM)
        {
            if (sample == null)
                throw new ArgumentNullException(nameof(sample));
            ValidateNonNegative(normalLoadN, nameof(normalLoadN));
            ValidateNonNegative(slidingDistanceM, nameof(slidingDistanceM));

            FrictionPairProfile pair = GetFrictionPair(sample.Profile.MaterialId, counterfaceMaterialId);
            float wearCoefficient = pair.GetWearCoefficient(sample.Profile.MaterialId);
            float hardness = sample.Profile.IndentationHardnessPa;

            if (wearCoefficient < 0f || hardness <= 0f || normalLoadN <= 0f || slidingDistanceM <= 0f)
            {
                return new WearResult(0f, 0f, 0f, sample.MassKg, sample.VolumeM3, false);
            }

            float requestedWearVolume =
                wearCoefficient * normalLoadN * slidingDistanceM / hardness;
            requestedWearVolume = Mathf.Max(0f, requestedWearVolume);

            float massBeforeWear = sample.MassKg;
            float removedVolume = sample.ApplyWearVolume(requestedWearVolume);
            float removedMass = Mathf.Max(0f, massBeforeWear - sample.MassKg);

            return new WearResult(
                requestedWearVolume,
                removedVolume,
                removedMass,
                sample.MassKg,
                sample.VolumeM3,
                true);
        }

        // ============================================================
        // ASSET VALIDATION
        // ============================================================

        [ContextMenu("Validate Mechanical Profiles")]
        public void ValidateProfiles()
        {
            EnsureLists();
            int errors = 0;
            int warnings = 0;
            HashSet<string> materialIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (MechanicalMaterialProfile profile in profiles)
            {
                if (profile == null)
                {
                    Debug.LogError("Mechanics: null mechanical profile found.", this);
                    errors++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(profile.MaterialId) || !materialIds.Add(profile.MaterialId))
                {
                    Debug.LogError("Mechanics: empty or duplicate material ID '" + profile.MaterialId + "'.", this);
                    errors++;
                }

                if (profile.DensityKgPerM3 <= 0f)
                {
                    Debug.LogWarning("Mechanics: '" + profile.DisplayName + "' has no density; mass/volume conversion is unavailable.", this);
                    warnings++;
                }
                if (profile.YoungsModulusPa <= 0f)
                {
                    Debug.LogWarning("Mechanics: '" + profile.DisplayName + "' has no Young's modulus; elasticity calculations are unavailable.", this);
                    warnings++;
                }
                if (profile.IndentationHardnessPa <= 0f)
                {
                    Debug.LogWarning("Mechanics: '" + profile.DisplayName + "' has no indentation hardness; Archard wear is unavailable.", this);
                    warnings++;
                }
            }

            HashSet<string> pairKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (FrictionPairProfile pair in frictionPairs)
            {
                if (pair == null)
                {
                    Debug.LogError("Mechanics: null friction-pair profile found.", this);
                    errors++;
                    continue;
                }

                string key = pair.MaterialAId + "|" + pair.MaterialBId;
                if (!pairKeys.Add(key))
                {
                    Debug.LogError("Mechanics: duplicate friction pair '" + key + "'.", this);
                    errors++;
                }
                if (!HasProfile(pair.MaterialAId) || !HasProfile(pair.MaterialBId))
                {
                    Debug.LogError("Mechanics: friction pair '" + key + "' references a missing material profile.", this);
                    errors++;
                }
            }

            Debug.Log("Mechanics validation completed. Errors: " + errors + ". Warnings: " + warnings + ".", this);
        }

        // ============================================================
        // INTERNAL HELPERS
        // ============================================================

        private void EnsureLists()
        {
            if (profiles == null)
                profiles = new List<MechanicalMaterialProfile>();
            if (frictionPairs == null)
                frictionPairs = new List<FrictionPairProfile>();
        }

        private void MarkAssetDirty()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private static void ValidatePositive(float value, string parameterName)
        {
            if (value <= 0f || !IsFinite(value))
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite and greater than zero.");
        }

        private static void ValidateNonNegative(float value, string parameterName)
        {
            if (value < 0f || !IsFinite(value))
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite and non-negative.");
        }

        private static void ValidateFinite(float value, string parameterName)
        {
            if (!IsFinite(value))
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite.");
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
