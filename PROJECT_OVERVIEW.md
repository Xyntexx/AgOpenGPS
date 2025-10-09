# AgOpenGPS - Project Overview & Quick Start

> **Quick Reference Guide for Developers & AI Assistants**
>
> Last Updated: 2025-01-09 | Current Branch: `develop` | Status: Active Development

---

## TL;DR - Essential Info

**What**: GPS guidance software for precision agriculture (autonomous tractor steering, section control)

**Tech Stack**: C# / .NET Framework 4.8 / Windows Forms / OpenGL

**Primary Files**: Main app in `SourceCode/GPS/`, communication hub in `SourceCode/AgIO/`

**Build**: Open `SourceCode/AgOpenGPS.sln` in Visual Studio 2022, press F5

**Target Users**: Farmers using tractors with GPS for precision field work

**License**: GPLv3 (open source)

---

## Project Status

### Current State

| Aspect | Status | Notes |
|--------|--------|-------|
| **Active Development** | ✅ Yes | `develop` branch actively maintained |
| **Production Ready** | ✅ Yes | Used by real farms worldwide |
| **Latest Release** | v6.8 | See [GitHub Releases](https://github.com/agopengps-official/AgOpenGPS/releases) |
| **Test Coverage** | 🟡 Limited | ~5% code coverage, mostly unit tests |
| **Architecture** | 🔴 Needs Work | Tightly coupled, monolithic FormGPS |
| **Documentation** | 🟡 Fair | Forum + YouTube, code docs sparse |
| **Community** | ✅ Active | Forum, Discord, regular contributions |

### Known Issues

- **Tight coupling**: Most classes depend on `FormGPS` "god object"
- **Hard to test**: UI and logic intertwined
- **OpenGL dependency**: Blocks headless testing
- **Legacy patterns**: Written before modern C# practices

### Current Priorities (as of Jan 2025)

1. ✅ **Improve test coverage** (in progress - this analysis)
2. 🎯 **Fix critical bugs** (OpenGL crashes, GPS edge cases)
3. 🎯 **Performance improvements** (reduce memory allocations)
4. 📋 **Translation completion** (via Weblate)
5. 📋 **WPF migration** (long-term modernization)

---

## Project Structure

```
AgOpenGPS/
├── 📄 README.md                    # Main project readme
├── 📄 LICENSE                      # GPLv3 license
│
├── 📁 SourceCode/                  # All source code
│   ├── 📄 AgOpenGPS.sln           # Main solution file (open this!)
│   │
│   ├── 📁 GPS/                     ⭐ MAIN APPLICATION
│   │   ├── Program.cs             # Entry point
│   │   ├── Forms/
│   │   │   ├── FormGPS.cs         # Main form (god object ~1200 lines)
│   │   │   ├── Position.designer.cs  # UpdateFixPosition() - main loop
│   │   │   └── ...
│   │   ├── Classes/                # Core business logic
│   │   │   ├── CGuidance.cs       # Stanley controller (guidance algorithms)
│   │   │   ├── CSim.cs            # Simulator
│   │   │   ├── CSection.cs        # Section control
│   │   │   ├── CVehicle.cs        # Vehicle configuration
│   │   │   ├── CNMEA.cs           # GPS data parser
│   │   │   └── ...
│   │   └── Protocols/             # Communication protocols
│   │
│   ├── 📁 AgIO/                    ⭐ COMMUNICATION HUB
│   │   └── Source/                # Network I/O, serial ports, UDP
│   │
│   ├── 📁 AgOpenGPS.Core/          # Shared library
│   │   ├── Models/                # Data structures (Wgs84, GeoCoord, etc.)
│   │   ├── Interfaces/            # Abstractions
│   │   └── Translations/          # Localization
│   │
│   ├── 📁 AgLibrary/               # Utility functions
│   ├── 📁 ModSim/                  # GPS Simulator (standalone)
│   │
│   ├── 📁 AgOpenGPS.WpfApp/        # WPF version (future)
│   ├── 📁 AgOpenGPS.WpfViews/      # WPF UI components
│   │
│   └── 📁 *.Tests/                 # Test projects
│       ├── AgLibrary.Tests/       # Library tests
│       └── AgOpenGPS.Core.Tests/  # Core model tests
│
└── 📁 .github/                     # GitHub Actions, issue templates
```

---

## Core Architecture

### Main Components

```
┌─────────────────────────────────────────────────────────┐
│                        FormGPS                          │
│              (Main Form - Everything Connects Here)     │
│                                                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐            │
│  │ CNMEA    │  │ CVehicle │  │ CGuidance│            │
│  │ GPS Data │  │ Config   │  │ Stanley  │            │
│  └──────────┘  └──────────┘  └──────────┘            │
│                                                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐            │
│  │ CSection │  │ CABLine  │  │ CYouTurn │            │
│  │ Control  │  │ Guidance │  │ Auto Turn│            │
│  └──────────┘  └──────────┘  └──────────┘            │
│                                                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐            │
│  │ CSim     │  │ CBoundary│  │ CTool    │            │
│  │ Simulator│  │ Fields   │  │ Implement│            │
│  └──────────┘  └──────────┘  └──────────┘            │
└─────────────────────────────────────────────────────────┘
                         │
                         ↓
                  ┌─────────────┐
                  │   OpenGL    │
                  │  Rendering  │
                  └─────────────┘
```

### Data Flow (Simplified)

```
GPS Hardware
    ↓ (NMEA via UDP)
AgIO (Communication Hub)
    ↓ (UDP to localhost)
FormGPS.ReceiveFromUDP()
    ↓
CNMEA.ParseNMEA()
    ↓ (updates pn.fix, pn.speed)
UpdateFixPosition()  ← MAIN LOOP (called ~10Hz)
    ↓
Calculate Heading
    ↓
CGuidance.StanleyGuidance*()  ← Calculate steer angle
    ↓
Update Sections
    ↓
Render with OpenGL
```

### Key Classes to Understand

| Class | File | Purpose | Lines | Complexity |
|-------|------|---------|-------|------------|
| `FormGPS` | GPS/Forms/FormGPS.cs | Main application form, UI, orchestration | ~1200 | 🔴 High |
| `UpdateFixPosition()` | GPS/Forms/Position.designer.cs:128 | Main update loop, called every GPS tick | ~800 | 🔴 High |
| `CGuidance` | GPS/Classes/CGuidance.cs | Stanley controller, guidance algorithms | ~410 | 🟡 Medium |
| `CSim` | GPS/Classes/CSim.cs | Simulator, vehicle physics | ~125 | 🟢 Low |
| `CNMEA` | GPS/Classes/CNMEA.cs | GPS data parser | ~400 | 🟡 Medium |
| `CSection` | GPS/Classes/CSection.cs | Section control logic | ~76 | 🟢 Low |
| `CVehicle` | GPS/Classes/CVehicle.cs | Vehicle configuration | ~150 | 🟢 Low |

---

## Technology Stack

### Languages & Frameworks

- **Primary**: C# 7.3
- **Framework**: .NET Framework 4.8 (Windows-specific)
- **UI**: Windows Forms (legacy), WPF (migration in progress)
- **Graphics**: OpenTK (OpenGL wrapper)
- **Testing**: NUnit 3.x

### Dependencies

- **OpenTK**: OpenGL rendering
- **Newtonsoft.Json**: JSON serialization
- **SharpKML**: KML file parsing
- **NetTopologySuite**: Geometry operations

### Build Tools

- **IDE**: Visual Studio 2022 (Community Edition recommended)
- **Build**: MSBuild / `dotnet build`
- **Package Manager**: NuGet

### Platform Requirements

- **OS**: Windows 10/11 (primary), Windows 7/8 (legacy)
- **Hardware**: Runs on tablets to laptops (wide performance range)
- **.NET**: Framework 4.8 SDK required

---

## Getting Started (5-Minute Setup)

### Prerequisites

```bash
# Check if you have .NET Framework 4.8
dotnet --version

# Install Visual Studio 2022 Community (free)
# https://visualstudio.microsoft.com/downloads/
# Select: ".NET desktop development" workload
```

### Clone & Build

```bash
# 1. Clone repository
git clone https://github.com/agopengps-official/AgOpenGPS.git
cd AgOpenGPS

# 2. Checkout develop branch
git checkout develop

# 3. Open in Visual Studio
start SourceCode/AgOpenGPS.sln

# OR build from command line
cd SourceCode
dotnet restore AgOpenGPS.sln
dotnet build AgOpenGPS.sln

# 4. Run
dotnet run --project GPS/AgOpenGPS.csproj
```

### First Test Run

1. Press **F5** in Visual Studio (Start Debugging)
2. AgOpenGPS window opens
3. Click **"Simulator"** button (bottom right)
4. Click **"Start Sim"**
5. Watch the tractor drive itself! 🚜

---

## Contributing

**Want to contribute?** See [CONTRIBUTING.md](CONTRIBUTING.md) for:
- Development workflow
- Branch strategy
- Commit message format
- Pull request process
- Code review guidelines

---

## Common Development Tasks

### Quick File Locations

**Need to understand:**
- **GPS parsing** → [`GPS/Classes/CNMEA.cs`](SourceCode/GPS/Classes/CNMEA.cs)
- **Guidance algorithms** → [`GPS/Classes/CGuidance.cs`](SourceCode/GPS/Classes/CGuidance.cs)
- **Main loop** → [`GPS/Forms/Position.designer.cs:128`](SourceCode/GPS/Forms/Position.designer.cs#L128)
- **Simulator** → [`GPS/Classes/CSim.cs`](SourceCode/GPS/Classes/CSim.cs)
- **Section control** → [`GPS/Classes/CSection.cs`](SourceCode/GPS/Classes/CSection.cs)

**Need to modify:**
- **Add UI element** → `GPS/Forms/FormGPS.Designer.cs`
- **Add settings** → `GPS/Properties/Settings.settings`
- **Add translation** → Use [Weblate](https://hosted.weblate.org/engage/agopengps) (web interface)
- **Add test** → `AgOpenGPS.Core.Tests/` or `AgLibrary.Tests/`

### Debugging Common Issues

| Problem | Solution |
|---------|----------|
| **Build fails** | Run `dotnet restore`, check .NET 4.8 installed |
| **OpenGL crash** | Update graphics drivers, disable hardware acceleration |
| **GPS not working** | Check UDP port 9999, firewall settings |
| **Simulator frozen** | Stop sim, restart application |

---

## Code Style

AgOpenGPS follows standard C# conventions:
- **Classes/Methods/Properties**: PascalCase
- **Private fields/locals**: camelCase
- **Bracing**: Allman style (opening brace on new line)
- **Comments**: XML docs for public APIs, inline for "why" not "what"

**Full style guide**: See [CONTRIBUTING.md - Coding Guidelines](CONTRIBUTING.md#coding-guidelines)

---

## Testing

⚠️ **Test coverage is currently ~5%** - mostly unit tests for core models and utilities.

**Not tested yet**:
- Guidance algorithms
- Section control logic
- Main update loop
- UI interactions

**Tests are highly encouraged!** They're easy to review and don't risk breaking production code.

**How to add tests**: See [CONTRIBUTING.md - Testing](CONTRIBUTING.md#testing)

---

## Important Files for AI/Developers

### Read These First

1. **[README.md](README.md)** - Project overview (5 min)
2. This file (**PROJECT_OVERVIEW.md**) - Everything else (10 min)

### Architecture Deep Dives

- 🚫 ~~AUTOMATED_TESTING_REFACTORING_PLAN.md~~ *(uncommitted)*
- 🚫 ~~BALANCED_TESTING_APPROACH.md~~ *(uncommitted)*
- 🚫 ~~CODE_READING_GUIDE.md~~ *(uncommitted)*
- 🚫 ~~COMMUNITY_TESTING_PLAN.md~~ *(uncommitted)*
- 🚫 ~~CONTRIBUTING.md~~ *(uncommitted)*
- 🚫 ~~MINIMAL_TESTING_APPROACH.md~~ *(uncommitted)*
- 🚫 ~~TESTING_STRATEGY.md~~ *(uncommitted)*

*Note: These files exist locally but aren't committed yet - check if they become available*


---

## Key Concepts

### Coordinate Systems

AgOpenGPS uses **two coordinate systems** (critical to understand):

1. **WGS84**: GPS coordinates (latitude, longitude in degrees)
   - From GPS hardware: `$GPGGA` NMEA sentences
   - Class: `Wgs84` in `AgOpenGPS.Core/Models/Wgs84.cs`

2. **Local Plane**: Cartesian coordinates (easting, northing in meters)
   - Used for all calculations (guidance, boundaries, etc.)
   - Class: `GeoCoord` in `AgOpenGPS.Core/Models/GeoCoord.cs`
   - Conversion: `LocalPlane.ConvertWgs84ToGeoCoord()`

**Why two systems?** GPS gives spherical coordinates, but math is easier in flat Cartesian space.

### Guidance Modes

AgOpenGPS supports multiple guidance modes:

| Mode | Description | Class | Complexity |
|------|-------------|-------|------------|
| **AB Line** | Straight line guidance | `CABLine.cs` | 🟢 Simple |
| **AB Curve** | Curved path guidance | `CABCurve.cs` | 🟡 Medium |
| **Contour** | Follow terrain contours | `CContour.cs` | 🟡 Medium |
| **Recorded Path** | Replay recorded track | `CRecordedPath.cs` | 🟢 Simple |

All use the **Stanley controller** algorithm (`CGuidance.DoSteerAngleCalc()`)

### Stanley Controller

The core guidance algorithm (simplified):

```
steer_angle = atan(cross_track_error × K / speed) + heading_error + integral_term

Where:
- cross_track_error = perpendicular distance from line (meters)
- K = distance gain (tuning parameter)
- speed = vehicle speed (m/s)
- heading_error = angle difference from line (radians)
- integral_term = accumulated error over time
```

Implementation: `GPS/Classes/CGuidance.cs:42` (`DoSteerAngleCalc()`)

### Section Control

**Sections** are parts of an implement that can be turned on/off independently:

```
       Tractor
          |
    ┌─────┴─────┐
    │ Implement │
    └───────────┘
    │ │ │ │ │ │ │  ← 7 sections (example)
    S1 S2 S3...S7

Each section:
- Has left/right position (meters from centerline)
- Can be Off / Auto / On
- Tracks if in boundary, headland
- Records applied area
```

**Purpose**: Prevent over-application at field edges, headlands, or already-covered areas.

---

## Performance Considerations

AgOpenGPS runs on **low-end tablets** to **gaming laptops**. Keep in mind:

### DO:
- ✅ Cache calculations in hot loops
- ✅ Reuse collections (don't allocate in 10Hz loop)
- ✅ Profile before optimizing
- ✅ Test on low-end hardware

### DON'T:
- ❌ Allocate in `UpdateFixPosition()` (called 10Hz)
- ❌ Use LINQ in critical paths (overhead)
- ❌ Block main thread (use async/await)
- ❌ Assume fast GPU (software OpenGL fallback needed)

**Critical path**: `UpdateFixPosition()` runs every GPS tick (~10-20 Hz). Keep it fast!

---

## Community & Resources

### Getting Help

- **Forum**: [discourse.agopengps.com](https://discourse.agopengps.com/) - Main discussion
- **Discord**: Real-time chat (link in forum)
- **GitHub Issues**: Bug reports, feature requests
- **GitHub Discussions**: Questions, ideas

### Documentation

- **User Docs**: [docs.agopengps.com](https://docs.agopengps.com/)
- **YouTube**: [AgOpenGPS Channel](https://www.youtube.com/c/agopengps) - Tutorials
- **Forum**: Setup guides, troubleshooting

### Related Projects

- **[Boards Repository](https://github.com/agopengps-official/Boards)** - Hardware PCBs, firmware
- **[Rate Control](https://github.com/agopengps-official/Rate_Control)** - Variable rate application
- **[Weblate](https://hosted.weblate.org/engage/agopengps)** - Translation platform

---

## Release Information

### Current Version

- **v6.8** (Current) - Czech translation, bug fixes
- Released to `master` branch
- Development on `develop` branch

### Seasonal Considerations

⚠️ **Critical farming periods** - avoid breaking changes:
- **Spring** (Mar-May): Planting season - stability critical
- **Fall** (Sep-Nov): Harvest season - stability critical
- **Summer/Winter**: Active development OK

**Why this matters**: AgOpenGPS is used on working farms during critical operations. A bug during planting/harvest season can have real financial consequences.

---

## FAQ for Developers

### Q: Why is FormGPS so big (1200+ lines)?

**A**: Legacy design - everything connects through main form. This is a known issue and refactoring is planned but requires careful testing to avoid breaking production users.

### Q: Can I add async/await?

**A**: Yes, but carefully. Main GPS loop is synchronous by design (10Hz tick rate). Async is fine for I/O, file operations, but don't block the main update cycle.

### Q: Why .NET Framework 4.8, not .NET 6/7/8?

**A**: Legacy - project started on Framework. Migration to modern .NET is planned long-term but requires significant effort. Framework 4.8 is final version and supported until 2028.

### Q: Why Windows Forms instead of WPF/Avalonia?

**A**: Historical - project began in ~2015. WPF migration is in progress (see `AgOpenGPS.WpfApp`), but Forms version is still primary and production.

### Q: How do I test without GPS hardware?

**A**: Use the built-in simulator! Click "Simulator" button in UI, or run `ModSim` project for external GPS simulator.

### Q: Can this run on Linux/Mac?

**A**: Not officially - relies on Windows-specific APIs (Windows Forms, .NET Framework). Possible with Wine/Mono but unsupported. Future .NET migration may enable cross-platform.

### Q: What's the biggest pain point for new contributors?

**A**: Understanding the tight coupling (everything depends on FormGPS). Read the architecture docs to understand current state and planned improvements.

---

## Glossary

| Term | Meaning |
|------|---------|
| **AB Line** | Straight guidance line from point A to B |
| **Autosteer** | Automatic steering (vehicle follows guidance) |
| **Contour** | Follow terrain contours (elevation-based guidance) |
| **Cross-track error** | Perpendicular distance from guidance line |
| **Dual GPS** | Two GPS receivers for heading (no IMU needed) |
| **Headland** | Border area of field (turn around zone) |
| **IMU** | Inertial Measurement Unit (gyro + accelerometer) |
| **NMEA** | GPS data format (e.g., `$GPGGA` sentences) |
| **PGN** | Parameter Group Number (J1939 CAN message ID) |
| **Section** | Part of implement that can be controlled independently |
| **Stanley controller** | Guidance algorithm (cross-track + heading correction) |
| **U-Turn** | Automatic headland turn |
| **WGS84** | GPS coordinate system (lat/lon) |

---

## Quick Links

### Essential
- [GitHub Repository](https://github.com/agopengps-official/AgOpenGPS)
- [Latest Release](https://github.com/agopengps-official/AgOpenGPS/releases/latest)
- [Forum](https://discourse.agopengps.com/)

### Documentation
- [User Docs](https://docs.agopengps.com/)
- [YouTube Tutorials](https://www.youtube.com/c/agopengps)

### Contributing
- [Issues](https://github.com/agopengps-official/AgOpenGPS/issues)
- [Pull Requests](https://github.com/agopengps-official/AgOpenGPS/pulls)
- [Weblate (Translation)](https://hosted.weblate.org/engage/agopengps)

---

## For AI Assistants

### Context for Code Analysis

**This is a production agriculture application** used by real farmers during critical planting/harvest seasons. Consider:

1. **Safety**: Bugs can cause equipment damage or crop loss
2. **Reliability**: Downtime during season costs money
3. **Backwards compatibility**: Field files must remain compatible
4. **Performance**: Runs on low-end hardware (tablets)
5. **Real-time constraints**: 10Hz GPS update loop is critical path

### When Analyzing Code

**Look for**:
- Tight coupling to FormGPS (common issue)
- Allocations in UpdateFixPosition() (performance issue)
- Coordinate system confusion (WGS84 vs local)
- Missing null checks (stability issue)
- Magic numbers without explanation (maintenance issue)

**Don't suggest**:
- Complete rewrites (too risky for production)
- Breaking API changes (affects users)
- Unproven libraries (stability risk)
- Complex abstractions (over-engineering)

**Do suggest**:
- Incremental improvements with tests
- Extracting pure functions
- Adding documentation
- Small refactorings with safety net

### Common Patterns to Recognize

```csharp
// Pattern 1: Everything depends on FormGPS
public class SomeFeature
{
    private readonly FormGPS mf;  // "mf" = main form
    public SomeFeature(FormGPS _f) { mf = _f; }
}

// Pattern 2: 10Hz update loop (don't block this!)
public void UpdateFixPosition()  // Called every GPS tick
{
    // Critical path - keep allocations minimal
}

// Pattern 3: Coordinate conversion
var latLon = new Wgs84(lat, lon);  // GPS coordinates
var local = localPlane.ConvertWgs84ToGeoCoord(latLon);  // Cartesian

// Pattern 4: Guidance calculation
double crossTrackError = CalculateDistanceFromLine();
double steerAngle = StanleyController(crossTrackError);
```

---

**Questions?** Ask in [GitHub Discussions](https://github.com/agopengps-official/AgOpenGPS/discussions) or [Forum](https://discourse.agopengps.com/)

**Happy Coding!** 🚜💻
