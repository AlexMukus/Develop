<!--
SYNC IMPACT REPORT
==================
Version change: (unratified scaffold) → 1.0.0 (initial ratification, MINOR-equivalent first adoption)
Modified principles: none (initial content)
Added sections:
  - I. Strict Layered Architecture
  - II. Zero-Warning Quality Gate
  - III. MVVM and WPF Conventions
  - IV. Test Discipline
  - V. Testability and Local-Only Operation
  - Technology Stack and Constraints
  - Development Workflow and Quality Gates
  - Governance
Removed sections: none
Deferred placeholders: none
Note: this report is temporary review scratch; remove before committing.
-->

# KeyboardTester Constitution

## Core Principles

### I. Strict Layered Architecture
Dependencies flow in one direction only: Core has zero project references and no
Windows API usage; Infrastructure and Application reference only Core; UI
composes everything and depends on Application plus Infrastructure. Reverse
references are forbidden and must be rejected in review. Rationale: the domain
logic (chatter analysis, statistics, ghosting) stays portable and unit-testable;
Windows specifics (Raw Input, P/Invoke) stay behind the layer boundary.

### II. Zero-Warning Quality Gate
The solution builds with `TreatWarningsAsErrors=true` and must emit no warnings
in any configuration. Nullable reference types are enabled everywhere; code uses
file-scoped namespaces, explicit accessibility modifiers, and mandatory braces.
Public types and members carry XML documentation in Russian. Rationale: warnings
in a measurement-oriented app mask real defects (struct sizes, interop
signatures); a clean build is the cheapest regression detector available.

### III. MVVM and WPF Conventions
ViewModels live in the Application layer and use CommunityToolkit.Mvvm source
generators (`[ObservableProperty]`, `[RelayCommand]`); views stay logic-free.
All services are registered as singletons in the DI graph, which is resolved in
`App.OnStartup` before the main window is shown, because `RawInputCapture` and
`StatisticsEngine` require UI-thread affinity. Styles shared across themes live
in `Resources/Styles.xaml` on `DynamicResource` and must survive theme switches.
Rationale: a single composition point and generator-based VMs keep the MVVM
graph predictable and prevent dispatcher and resource-lookup bugs.

### IV. Test Discipline
Tests use xUnit with FluentAssertions and Moq, named `Method_Scenario_ExpectedResult`
(in Russian project convention: `Метод_Сценарий_ОжидаемыйРезультат`). Pure logic
(chatter analysis, statistics, layouts) is covered by unit tests; Raw Input,
sessions, ViewModels, and UI converters by integration tests through fakes
(`FakeNativeMethods`, `FakeRawInputCapture`) and the STA test helper. WPF
components are exercised on an STA thread. `dotnet test KeyboardTester.sln`
must be fully green before any task is considered complete. Rationale: the app's
value is measurement correctness, which is only provable through tests; fakes
keep Windows-hardware dependencies out of the test path.

### V. Testability and Local-Only Operation
All Win32 interaction is isolated behind interfaces (`INativeMethods`) so it can
be faked in tests; P/Invoke declarations must match Win32 documentation exactly
for signatures and structure sizes. The application performs no network calls
and stores settings, session history, and logs only under
`%AppData%/KeyboardTester/`. New work follows YAGNI: no speculative features,
dependencies, or abstractions. Rationale: an offline diagnostic tool must be
dependable and self-contained; exact interop contracts and fakeability are what
make failures diagnosable.

## Technology Stack and Constraints

- Language/runtime: C# 12 on .NET 9; UI is WPF (Windows-only, `net9.0-windows`).
- Dependency versions are centralized in `Directory.Packages.props` (CPM);
  `PackageReference` entries never carry inline versions.
- `SkiaSharp.Views.WPF` is pinned to 2.88.9 (3.x pulls .NETFramework-only
  assets, NU1701); do not upgrade without explicit verification.
- Logging via Serilog to `%AppData%/KeyboardTester/logs/`; UI and log messages
  are in Russian; localization resources live in `Strings.Designer.cs`.
- PDF/report export is out of scope; QuestPDF is not used and must not be added.
- Coding standards: `.editorconfig`, `Directory.Build.props`, `AGENTS.md`, and
  the `csharp-desktop-rules` skill (`.roo/skills/csharp-desktop-rules/`) are
  binding sources; project conventions override generic skill rules where they
  conflict (documented divergences are intentional).

## Development Workflow and Quality Gates

- Before writing or reviewing C#/XAML, read the `csharp-desktop-rules` skill
  entry point; pull in reference files (`mvvm.md`, `xaml.md`, `wpf.md`,
  `performance.md`) per task, not wholesale.
- Quality gate on every change: `dotnet build KeyboardTester.sln` with zero
  warnings, then `dotnet test KeyboardTester.sln` fully green.
- Portable release builds go through `build-portable.ps1` (runs tests unless
  `-SkipTests`) and land in `artifacts/`; CI mirrors the same gate sequence.
- Releases: bump `<Version>` in `Directory.Build.props`, push a `vX.Y.Z` tag;
  `release.yml` produces the GitHub Release from the portable ZIP.
- When a change alters something `AGENTS.md` documents, update `AGENTS.md` in
  the same change.

## Governance

This constitution supersedes all other coding practices in the repository;
`AGENTS.md` and the `csharp-desktop-rules` skill are subordinate operational
guidance and must not contradict it. Amendments require: a documented diff in
the Sync Impact Report, a semantic version bump (MAJOR for principle removals
or redefinitions, MINOR for added or materially expanded principles/sections,
PATCH for clarifications and wording), and a full green build plus test run
before commit. All code reviews must verify compliance with these principles;
any deviation must be justified in the change description. Deviations discovered
between the skill and project conventions are resolved in favor of the project,
flagged to the user, and left in place rather than silently rewritten.

**Version**: 1.0.0 | **Ratified**: 2026-09-11 | **Last Amended**: 2026-09-11
