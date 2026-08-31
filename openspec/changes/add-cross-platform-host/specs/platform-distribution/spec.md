## Purpose

Produce platform-specific development and release artefacts that activate the QoL host without including copyrighted Terraria files or obsolete framework dependencies.

## ADDED Requirements

### Requirement: Platform distributions contain only project dependencies
Release artefacts SHALL contain the host, first-party features, permitted third-party dependencies, and platform launch scripts, but SHALL NOT contain Terraria executables, assemblies, native libraries, saves, or worlds.

#### Scenario: Release archive inspection
- **WHEN** a platform release archive is assembled
- **THEN** its manifest contains no file copied from a Terraria installation

### Requirement: Each platform has a direct launch entry point
Each supported platform distribution SHALL provide a documented launch entry point that accepts an optional Terraria installation path.

#### Scenario: Default launch
- **WHEN** a user invokes the platform launch entry point without arguments
- **THEN** it detects a standard installation and starts host-enabled Terraria

#### Scenario: Explicit launch path
- **WHEN** a user invokes the launch entry point with an installation path
- **THEN** it launches that valid installation or reports why it is incompatible

### Requirement: Build outputs are reproducible from the repository
The repository SHALL document and automate generation of each platform distribution from source without requiring a checked-in Terraria installation or absolute developer-machine paths.

#### Scenario: Clean build environment
- **WHEN** the documented build command runs with the required SDK and an explicitly supplied local Terraria reference path
- **THEN** it produces the expected platform artefacts without editing the Terraria installation

### Requirement: Legacy dependencies are removed after parity
Once the new host passes the supported compatibility matrix, release artefacts SHALL no longer include TerrariaModder or TerrariaInjector.

#### Scenario: Completed migration bundle
- **WHEN** a release is assembled after host parity is declared
- **THEN** neither TerrariaModder nor TerrariaInjector files are present in the bundle

