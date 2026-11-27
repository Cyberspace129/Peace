using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParticleChemistry
{
    /// <summary>
    /// Central catalogue of physical materials. Specialist systems use each
    /// MaterialDefinition.Id as the stable key for their own profiles.
    /// Unknown measured values are represented by -1, not by invented data.
    /// </summary>
    [CreateAssetMenu(fileName = "MaterialSystem", menuName = "Particle Physics/Material System")]
    public class Material : ScriptableObject
    {
        public enum PhysicalPhase
        {
            Unknown,
            Solid,
            Liquid,
            Gas,
            Plasma
        }

        public enum MaterialCategory
        {
            Other,
            Element,
            Metal,
            NonMetal,
            Mineral,
            Ceramic,
            Glass,
            Polymer,
            Composite,
            BiologicalMaterial,
            Soil,
            Food,
            Textile,
            Fluid,
            Gas
        }

        [Serializable]
        public class MaterialDefinition
        {
            [Header("Identity")]
            [SerializeField] private string id;
            [SerializeField] private string materialName;
            [TextArea(2, 4)]
            [SerializeField] private string description;

            [Header("Classification")]
            [SerializeField] private MaterialCategory category = MaterialCategory.Other;
            [SerializeField] private PhysicalPhase phase = PhysicalPhase.Unknown;

            [Header("Physical Properties")]
            [Tooltip("Bulk density in kg/m³. Use -1 when unknown.")]
            [SerializeField] private float densityKgPerM3 = -1f;
            [Tooltip("Specific heat capacity in J/(kg·K). Use -1 when unknown.")]
            [SerializeField] private float specificHeatJPerKgK = -1f;
            [Tooltip("Thermal conductivity in W/(m·K). Use -1 when unknown.")]
            [SerializeField] private float thermalConductivityWPerM_K = -1f;
            [Tooltip("Melting point in °C. Use -1 when unknown or not applicable.")]
            [SerializeField] private float meltingPointC = -1f;
            [Tooltip("Mohs scratch hardness, normally 1–10. Use -1 when unknown.")]
            [SerializeField] private float mohsHardness = -1f;
            [Tooltip("Porosity as a fraction from 0 to 1. Use -1 when unknown.")]
            [SerializeField] private float porosity = -1f;

            public string Id => id;
            public string Name => materialName;
            public string Description => description;
            public MaterialCategory Category => category;
            public PhysicalPhase Phase => phase;
            public float DensityKgPerM3 => densityKgPerM3;
            public float SpecificHeatJPerKgK => specificHeatJPerKgK;
            public float ThermalConductivityWPerM_K => thermalConductivityWPerM_K;
            public float MeltingPointC => meltingPointC;
            public float MohsHardness => mohsHardness;
            public float Porosity => porosity;

            public MaterialDefinition(
                string id,
                string name,
                MaterialCategory category = MaterialCategory.Other,
                PhysicalPhase phase = PhysicalPhase.Unknown,
                float densityKgPerM3 = -1f,
                float specificHeatJPerKgK = -1f,
                float thermalConductivityWPerM_K = -1f,
                float meltingPointC = -1f,
                float mohsHardness = -1f,
                float porosity = -1f,
                string description = "")
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Material ID cannot be empty.", nameof(id));
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Material name cannot be empty.", nameof(name));

                this.id = id.Trim();
                materialName = name.Trim();
                this.category = category;
                this.phase = phase;
                this.densityKgPerM3 = densityKgPerM3;
                this.specificHeatJPerKgK = specificHeatJPerKgK;
                this.thermalConductivityWPerM_K = thermalConductivityWPerM_K;
                this.meltingPointC = meltingPointC;
                this.mohsHardness = mohsHardness;
                this.porosity = porosity;
                this.description = description ?? string.Empty;
            }
        }

        [Header("Material Catalogue")]
        [SerializeField] private List<MaterialDefinition> materials =
            new List<MaterialDefinition>();

        public IReadOnlyList<MaterialDefinition> Materials => materials;

        private void OnEnable()
        {
            if (materials == null)
                materials = new List<MaterialDefinition>();
        }

        public MaterialDefinition GetMaterial(string materialId)
        {
            if (string.IsNullOrWhiteSpace(materialId) || materials == null)
                return null;

            for (int i = 0; i < materials.Count; i++)
            {
                MaterialDefinition entry = materials[i];
                if (entry != null && string.Equals(
                    entry.Id, materialId, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }

            return null;
        }

        public bool ContainsMaterial(string materialId)
        {
            return GetMaterial(materialId) != null;
        }

        public void AddMaterial(MaterialDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (ContainsMaterial(definition.Id))
                throw new InvalidOperationException(
                    "A material with ID '" + definition.Id + "' already exists.");

            if (materials == null)
                materials = new List<MaterialDefinition>();

            materials.Add(definition);
            MarkAssetDirty();
        }

        public bool RemoveMaterial(string materialId)
        {
            MaterialDefinition existing = GetMaterial(materialId);
            if (existing == null)
                return false;

            materials.Remove(existing);
            MarkAssetDirty();
            return true;
        }

        [ContextMenu("Validate Material Catalogue")]
        public void ValidateMaterialCatalogue()
        {
            if (materials == null)
            {
                Debug.LogError("Material catalogue list is null.", this);
                return;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int invalidCount = 0;

            foreach (MaterialDefinition entry in materials)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id) ||
                    string.IsNullOrWhiteSpace(entry.Name))
                {
                    invalidCount++;
                    continue;
                }

                if (!ids.Add(entry.Id))
                {
                    invalidCount++;
                    Debug.LogError("Duplicate material ID: " + entry.Id, this);
                }
            }

            if (invalidCount == 0)
                Debug.Log("Material catalogue validation passed. Entries: " + materials.Count, this);
            else
                Debug.LogError("Material catalogue validation found " + invalidCount + " invalid or duplicate entries.", this);
        }

        private void MarkAssetDirty()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
