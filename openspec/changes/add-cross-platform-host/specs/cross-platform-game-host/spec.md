## Purpose

Provide a non-destructive launcher that activates first-party quality-of-life features in a supported Terraria installation on Windows, Linux, and macOS.

## ADDED Requirements

### Requirement: Supported installations can be located
The host SHALL accept an explicit Terraria installation path and SHALL also detect standard Steam installation layouts for the current operating system.

#### Scenario: Explicit installation path
- **WHEN** a user supplies a valid Terraria installation path
- **THEN** the host selects that installation without requiring it to be moved, linked, or modified

#### Scenario: Standard Steam installation
- **WHEN** no explicit path is supplied and Terraria exists in a standard Steam library
- **THEN** the host locates the platform-specific game assembly, launcher, runtime, and native-library directory

#### Scenario: Installation cannot be found
- **WHEN** no valid installation can be selected
- **THEN** the host exits without launching Terraria and reports the paths it checked and how to provide one explicitly

### Requirement: Terraria is launched non-destructively
The host SHALL activate the QoL feature runtime without replacing, renaming, patching on disk, or redistributing Terraria executables, assemblies, native libraries, saves, or worlds.

#### Scenario: Host-enabled launch
- **WHEN** a supported installation is launched through the host
- **THEN** Terraria starts with registered first-party features active and the original installation files remain byte-for-byte unchanged

#### Scenario: Normal Steam launch
- **WHEN** Terraria is later launched normally through Steam
- **THEN** it starts without the host or its features

### Requirement: Supported platforms use their native Terraria runtime
The host SHALL support Windows x64, Linux x64, macOS x64, and macOS arm64 by using the managed runtime and native libraries appropriate to the selected Terraria installation.

#### Scenario: Apple Silicon launch
- **WHEN** an Apple Silicon user selects the native macOS Terraria installation
- **THEN** the host launches the arm64 game path without requiring Rosetta or a separately installed Mono runtime

#### Scenario: Linux launch
- **WHEN** a Linux x64 user selects a supported native Terraria installation
- **THEN** the host launches through Terraria's Linux runtime and native libraries rather than Wine or Proton

#### Scenario: Windows launch
- **WHEN** a Windows x64 user selects a supported Terraria installation
- **THEN** the host launches the managed game with the required in-process feature bootstrap

### Requirement: Unsupported game versions fail safely
The host SHALL verify the Terraria managed assembly version before activating features and SHALL leave normal game execution available when the version is unsupported.

#### Scenario: Supported version
- **WHEN** the detected managed assembly version is Terraria 1.4.5.8
- **THEN** the host activates the registered features

#### Scenario: Unsupported version
- **WHEN** the detected managed assembly version is not supported
- **THEN** the host does not apply runtime patches and reports the expected and detected versions

### Requirement: Launch failures are actionable
The host SHALL report platform, architecture, selected paths, detected version, and the failing launch stage without exposing unrelated personal information.

#### Scenario: Runtime bootstrap fails
- **WHEN** the platform runtime starts but cannot load the feature bootstrap
- **THEN** the host exits or leaves Terraria unmodified and reports the failed stage and relevant file path

