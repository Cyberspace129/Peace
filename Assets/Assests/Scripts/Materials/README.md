# SIMBAKE Material System (Unity C#)

This package contains six cooperating Unity `ScriptableObject` systems:

- `Material.cs` — central element and material catalogue, runtime specimen, and cross-system coordinator.
- `Chemicals.cs` — particle definitions, chemical species, mixtures, reactions, acidity, dissolution, contamination, and corrosion.
- `Thermatics.cs` — heat capacity, heat transfer, thermal expansion, phase transitions, combustion, and thermal degradation.
- `Fuilds.cs` — fluid definitions, porosity, permeability, wetting, capillary action, absorption, evaporation, retention, and drainage.
- `Mechanics.cs` — density, elasticity, plasticity, hardness, strength, fracture, friction, and wear.
- `Biological.cs` — organism growth, nutrient use, surface colonisation, fungal decay, and organic decomposition.

All six scripts are in the `ParticleChemistry` namespace. The spelling `Fuilds` is retained to match the existing project class and asset names.

## Installation

1. Back up the existing scripts.
2. Replace the existing versions of all six `.cs` files with the files in this package. Do not keep duplicate copies of the old classes.
3. Put all six files in a normal runtime folder under `Assets`, not inside an `Editor` folder. They reference one another and should compile into the same Unity runtime assembly.
4. Let Unity recompile. Resolve any project-specific compile errors before creating assets.
5. Create five specialist assets using **Assets → Create → Particle Physics**:
   - Chemicals
   - Thermatics
   - Fuilds
   - Mechanics
   - Biological
6. Create the central asset using **Assets → Create → Particle Physics → Material System**.
7. Select the Material System asset and assign the five specialist assets in its **Integrated Physics and Biology Systems** fields.
8. Use the Material System asset's context menu and run **Synchronise All Material Subsystems**.
9. Run **Validate Material System Integration** and inspect the Console warnings. Warnings for missing measured properties are expected until you enter real data.

The central Material asset loads its default periodic table and material catalogue when created. It contains 118 elements and 175 material definitions. The Fuilds asset adds starter fluid definitions when its fluid list is empty. The first material-system synchronisation adds starter biological organism and substrate profiles when those catalogues are empty, then imports missing profiles for the materials in the central catalogue.

## Using the material database from another script

The data class is `ParticleChemistry.Material`. Unity also has a rendering class named `UnityEngine.Material`, so use an alias when both are needed:

```csharp
using UnityEngine;
using MaterialDatabase = ParticleChemistry.Material;

public class MaterialSampleTest : MonoBehaviour
{
    [SerializeField] private MaterialDatabase database;

    private void Start()
    {
        // Create a 0.5 kg quartz specimen at 20 °C.
        MaterialDatabase.MaterialSample quartz =
            database.CreateSample("quartz", 0.5f, 20f);

        Debug.Log($"Mass: {quartz.MassKg} kg");
        Debug.Log($"Volume: {quartz.VolumeM3} m³");
        Debug.Log($"Density: {quartz.DensityKgPerM3} kg/m³");
    }
}
```

Use `CreateSample(materialId, massKg, ...)` when the central database has a useful density. Use `CreateSampleWithGeometry(materialId, massKg, bulkVolumeM3, ...)` when you have measured specimen dimensions or the catalogue density is unknown. For sheets, pipes, coatings, and other non-cubic geometries, pass a measured `initialThicknessMillimetres` when creating a sample that has a corrosion profile.

The runtime `MaterialSample` is the shared specimen. Its specialist state is available through:

- `sample.Thermal`
- `sample.Mechanical`
- `sample.Porous`
- `sample.Organic`
- `sample.ChemicalComposition`
- `sample.Corrosion`
- `sample.Populations`

The central database exposes coordinating operations such as `ApplyThermalEnergy`, `TransferHeat`, `AdvanceCombustion`, `ApplyAxialStrain`, `ApplySlidingWear`, `AbsorbFluid`, `EvaporateAbsorbedFluid`, `DrainAbsorbedFluid`, `AdvanceDecomposition`, `AdvanceFungalDecay`, `AdvanceChemicalReaction`, `DissolveMaterialIntoSolution`, `AssessContamination`, and `AdvanceCorrosion`.

## What synchronisation does

Synchronisation only creates missing starter profiles. Existing specialist profiles are preserved.

| System | Automatically imported or generated | Still requires project data |
|---|---|---|
| Chemistry | Parses supported fixed formulas and registers chemical species for known fluid and material constituents | Reaction stoichiometry and kinetics, pKa/pKb data, solubility profiles, contaminant limits, and corrosion rates |
| Thermatics | Density, specific heat capacity, thermal conductivity, reference phase, and listed melting point | Expansion coefficients, latent heats, freezing behaviour, combustion data, degradation kinetics, and temperature curves |
| Fuilds | Porosity where present in the central material definition; starter fluid definitions | Permeability, pore size, retention, wetting/contact-angle pairs, and calibrated evaporation coefficients |
| Mechanics | Density and Mohs hardness where available | Young's modulus, Poisson ratio, yield and ultimate strengths, fracture toughness, Vickers hardness, and friction-pair coefficients |
| Biology | Starter organism/substrate presets and broad decomposition profiles for eligible material categories | Species-specific growth parameters, nutrient pools, moisture response, substrate composition, and environmental calibration |

Unknown values are left unset rather than populated with invented constants. A missing value can disable a calculation or cause the relevant method to report that more data is required.

## Model boundaries and scientific limits

This is a coupled, engineering-scale material framework, not a complete microscopic or multiphysics solver.

- Chemistry works with registered species and configured reactions. It does not infer every possible reaction from a formula or run quantum chemistry.
- Thermal calculations use lumped sample temperatures and simplified heat-transfer and phase-change models. They do not solve full spatial heat fields or fluid convection.
- Fluid absorption uses simplified Darcy flow, capillary pressure, and empirical mass-transfer models. A porous sample tracks one absorbed fluid at a time; it is not a multicomponent pore-network or CFD solver.
- Mechanics uses simplified uniaxial constitutive response, fracture checks, contact friction, and Archard wear. It is not finite-element analysis and does not resolve arbitrary 3D stress fields.
- Biology uses configured population and substrate response models. It is not a species-resolved ecosystem or genome-scale metabolic simulator.
- Cross-system mass changes are synchronised by the central Material sample. Some physical products, such as escaped gases, corrosion products, mineral precipitates, and biomass entering or leaving the specimen, still require explicit modelling in the relevant environment.

The central sample keeps thermal mass as its primary mass ledger when a thermal model exists, with mechanics mirroring mass and geometry. Fluid and biological objects hold subsystem state for the same physical specimen; their masses must not be added to the central sample a second time. Attached organism biomass is reported separately through `TotalMassIncludingBiomassKg`.

## Validation and calibration

Use **Validate Material System Integration** from the Material System asset after synchronising. Then validate each specialist asset from its own context menu. Pay particular attention to profiles with `-1` properties: these mean unknown, not zero.

For predictive simulation, populate properties from a source appropriate to the exact material grade, temperature, moisture content, pressure, processing state, and test method. Material properties can vary significantly with all of these conditions. The starter values are educational/engineering defaults and should not be treated as certified design data.

## Build verification

The source files were checked for balanced C# delimiters and basic cross-file API consistency. A Unity editor/compiler was not available in the generation environment, so this package has **not** been verified by an actual Unity compilation or runtime test. The first step after importing is to let the target Unity version compile the scripts and review its Console output.
