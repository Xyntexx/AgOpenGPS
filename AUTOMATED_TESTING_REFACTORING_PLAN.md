# Automated Simulation Testing - Refactoring Plan

## Executive Summary

This document outlines the required code changes to enable automated, headless simulation testing for AgOpenGPS. The current architecture tightly couples the simulation logic (`CSim.cs`) with the UI (`FormGPS`) and rendering systems (OpenGL), preventing automated testing. This plan details the refactoring needed to decouple these concerns and enable accelerated, automated testing.

---

## Current Architecture Analysis

### Key Components Identified

| Component | File Location | Responsibility | Dependencies |
|-----------|---------------|----------------|--------------|
| **FormGPS** | `GPS/Forms/FormGPS.cs` | Main application form, god object | Everything |
| **CSim** | `GPS/Classes/CSim.cs` | Simulation logic | FormGPS (tightly coupled) |
| **CGuidance** | `GPS/Classes/CGuidance.cs` | Stanley controller, guidance calculations | FormGPS |
| **CNMEA** | `GPS/Classes/CNMEA.cs` | GPS position data | FormGPS |
| **CSection** | `GPS/Classes/CSection.cs` | Section control state | None (good!) |
| **CModuleComm** | `GPS/Classes/CModuleComm.cs` | Hardware communication | FormGPS |
| **UpdateFixPosition** | `GPS/Forms/Position.designer.cs:128` | Main update loop | FormGPS, many subsystems |

### Data Flow (Current)

```
CSim.DoSimTick()
  └─> Updates mf.pn.fix, mf.pn.speed, mf.ahrs.imuHeading
  └─> Calls mf.UpdateFixPosition()
      └─> Calculates heading from GPS fixes
      └─> Calls guidance calculations (AB Line, Curve, Contour)
          └─> CGuidance.StanleyGuidanceABLine() or StanleyGuidanceCurve()
              └─> Calculates steerAngleGu
              └─> Updates mf.mc.actualSteerAngleDegrees
      └─> Updates section control logic
      └─> Triggers OpenGL rendering (implicit)
```

### Critical Problems for Headless Testing

#### 1. **Tight Coupling to FormGPS**
- Nearly every class takes `FormGPS mf` in constructor
- Direct field access throughout: `mf.pn.fix`, `mf.vehicle.maxSteerAngle`, etc.
- Makes unit testing impossible without instantiating entire UI

#### 2. **No Interface Abstraction**
- Classes are concrete implementations with no interfaces
- Cannot mock or substitute components for testing
- Cannot inject test data sources

#### 3. **OpenGL Rendering Dependency**
- `FormGPS` inherits from `Form` with OpenGL controls
- Rendering happens in main update loop
- Cannot run without graphics context

#### 4. **Global State Management**
- Properties.Settings.Default used throughout
- No dependency injection
- Hard to set up repeatable test conditions

#### 5. **Monolithic Update Method**
- `UpdateFixPosition()` is 800+ lines in single file
- Does position calculation, guidance, section control, rendering
- No clear separation of concerns

---

## Refactoring Strategy

### Phase 1: Extract Interfaces (Week 1-2)

**Goal**: Create abstractions for testability without breaking existing code

#### 1.1 Create Core Interfaces

**File**: `SourceCode/AgOpenGPS.Core/Interfaces/IVehicleState.cs`
```csharp
public interface IVehicleState
{
    // Position
    vec2 CurrentPosition { get; }
    Wgs84 CurrentLatLon { get; }

    // Motion
    double Heading { get; } // radians
    double Speed { get; } // m/s
    bool IsReverse { get; }

    // Orientation (IMU)
    double ImuHeading { get; }
    double ImuRoll { get; }
    double ImuPitch { get; }

    // Vehicle geometry
    double Wheelbase { get; }
    double AntennaOffset { get; }
    double AntennaHeight { get; }
    double AntennaPivot { get; }
}
```

**File**: `SourceCode/AgOpenGPS.Core/Interfaces/IGuidanceState.cs`
```csharp
public interface IGuidanceState
{
    // Guidance line information
    bool IsABLineActive { get; }
    bool IsCurveActive { get; }
    bool IsContourActive { get; }

    // Reference line
    List<vec3> CurrentGuidanceLine { get; }

    // Calculated guidance
    double DistanceFromLine { get; }
    double HeadingError { get; }
    double SteerAngle { get; }
}
```

**File**: `SourceCode/AgOpenGPS.Core/Interfaces/ISectionController.cs`
```csharp
public interface ISectionController
{
    int SectionCount { get; }
    bool IsSectionOn(int index);
    bool IsSectionInBoundary(int index);
    void SetSectionState(int index, bool isOn);

    // For testing
    IReadOnlyList<CSection> GetSections();
}
```

**File**: `SourceCode/AgOpenGPS.Core/Interfaces/ISimulationEnvironment.cs`
```csharp
public interface ISimulationEnvironment
{
    // State access
    IVehicleState VehicleState { get; }
    IGuidanceState GuidanceState { get; }
    ISectionController SectionController { get; }

    // Simulation control
    void Initialize(SimulationConfig config);
    void Step(double deltaTime);
    void SetSteerAngle(double angle);
    void Reset();

    // Events for logging/validation
    event EventHandler<PositionUpdateEventArgs> OnPositionUpdate;
    event EventHandler<SectionStateChangedEventArgs> OnSectionStateChanged;
}
```

#### 1.2 Create Data Transfer Objects

**File**: `SourceCode/AgOpenGPS.Core/Models/SimulationConfig.cs`
```csharp
public class SimulationConfig
{
    // Field setup
    public Wgs84 StartPosition { get; set; }
    public double StartHeading { get; set; }

    // Vehicle configuration
    public VehicleConfiguration Vehicle { get; set; }

    // Guidance setup
    public GuidanceMode Mode { get; set; } // ABLine, Curve, Contour
    public List<vec3> GuidanceLine { get; set; }
    public int NumberOfSections { get; set; }
    public double ToolWidth { get; set; }

    // Field boundaries
    public List<vec2> Boundary { get; set; }

    // Simulation parameters
    public double TimeAcceleration { get; set; } = 1.0;
    public double SimulationSpeed { get; set; } = 10.0; // km/h
}
```

**File**: `SourceCode/AgOpenGPS.Core/Models/SimulationResult.cs`
```csharp
public class SimulationResult
{
    public List<VehicleState> Trajectory { get; set; }
    public List<SteeringCommand> SteeringCommands { get; set; }
    public List<SectionStateChange> SectionHistory { get; set; }
    public AppliedAreaMap CoverageMap { get; set; }

    public SimulationStatistics Statistics { get; set; }
}

public class SimulationStatistics
{
    public double RMSCrossTrackError { get; set; }
    public double MaxCrossTrackError { get; set; }
    public double AverageHeadingError { get; set; }
    public double TotalDistance { get; set; }
    public double AppliedArea { get; set; }
    public double OverlapPercentage { get; set; }
    public double SkipPercentage { get; set; }
}
```

---

### Phase 2: Refactor CSim for Testability (Week 2-3)

#### 2.1 Extract Position Provider Interface

**File**: `SourceCode/AgOpenGPS.Core/Interfaces/IPositionProvider.cs`
```csharp
public interface IPositionProvider
{
    vec2 CurrentPosition { get; }
    Wgs84 CurrentLatLon { get; }
    double Heading { get; }
    double Speed { get; }
    double Altitude { get; }
    int SatellitesTracked { get; }
    double HDOP { get; }
}
```

#### 2.2 Create Testable Simulator

**File**: `SourceCode/AgOpenGPS.Core/Simulation/SimulatorEngine.cs`
```csharp
/// <summary>
/// Headless simulation engine - no UI dependencies
/// </summary>
public class SimulatorEngine : IPositionProvider
{
    private readonly VehiclePhysics physics;
    private readonly SimulationConfig config;

    public Wgs84 CurrentLatLon { get; private set; }
    public double Heading { get; private set; }
    public double Speed { get; private set; }

    private double steerAngle;
    private double steerAngleSmoothed;

    public SimulatorEngine(SimulationConfig config)
    {
        this.config = config;
        this.physics = new VehiclePhysics(config.Vehicle);
        Reset();
    }

    public void Reset()
    {
        CurrentLatLon = config.StartPosition;
        Heading = config.StartHeading;
        Speed = config.SimulationSpeed / 3.6; // Convert km/h to m/s
        steerAngle = 0;
        steerAngleSmoothed = 0;
    }

    public void SetSteerAngle(double angle)
    {
        steerAngle = angle;
    }

    public void Step(double deltaTime)
    {
        // Smooth steering angle (simulate hydraulic lag)
        double diff = Math.Abs(steerAngle - steerAngleSmoothed);
        if (diff > 11)
        {
            steerAngleSmoothed += (steerAngle > steerAngleSmoothed) ? 6 : -6;
        }
        else if (diff > 5)
        {
            steerAngleSmoothed += (steerAngle > steerAngleSmoothed) ? 2 : -2;
        }
        else if (diff > 1)
        {
            steerAngleSmoothed += (steerAngle > steerAngleSmoothed) ? 0.5 : -0.5;
        }
        else
        {
            steerAngleSmoothed = steerAngle;
        }

        // Calculate movement
        double stepDistance = Speed * deltaTime;
        double headingChange = stepDistance *
            Math.Tan(glm.toRadians(steerAngleSmoothed)) /
            physics.Wheelbase;

        Heading += headingChange;
        if (Heading > glm.twoPI) Heading -= glm.twoPI;
        if (Heading < 0) Heading += glm.twoPI;

        // Update position
        CurrentLatLon = CurrentLatLon.CalculateNewPostionFromBearingDistance(
            Heading, stepDistance);
    }

    // IPositionProvider implementation
    vec2 IPositionProvider.CurrentPosition
    {
        get
        {
            // Convert to local coordinates
            // This will need LocalPlane instance
            throw new NotImplementedException();
        }
    }

    // ... other interface members
}
```

**File**: `SourceCode/AgOpenGPS.Core/Simulation/VehiclePhysics.cs`
```csharp
/// <summary>
/// Vehicle physics model for simulation
/// </summary>
public class VehiclePhysics
{
    public double Wheelbase { get; }
    public double MaxSteerAngle { get; }
    public double AntennaOffset { get; }
    public double AntennaHeight { get; }

    public VehiclePhysics(VehicleConfiguration config)
    {
        Wheelbase = config.Wheelbase;
        MaxSteerAngle = config.MaxSteerAngle;
        AntennaOffset = config.AntennaOffset;
        AntennaHeight = config.AntennaHeight;
    }

    public double CalculateHeadingChange(double steerAngle, double distance)
    {
        return distance * Math.Tan(glm.toRadians(steerAngle)) / Wheelbase;
    }
}
```

#### 2.3 Update Existing CSim to Use New Engine

**Modification**: `SourceCode/GPS/Classes/CSim.cs`

```csharp
public class CSim
{
    private readonly FormGPS mf;
    private readonly SimulatorEngine engine; // NEW

    public Wgs84 CurrentLatLon
    {
        get => engine.CurrentLatLon;
        set => throw new NotSupportedException("Use Reset()");
    }

    public CSim(FormGPS _f)
    {
        mf = _f;

        // Create engine with config from FormGPS
        var config = new SimulationConfig
        {
            StartPosition = new Wgs84(
                Properties.Settings.Default.setGPS_SimLatitude,
                Properties.Settings.Default.setGPS_SimLongitude),
            Vehicle = mf.vehicle.VehicleConfig
        };

        engine = new SimulatorEngine(config);
    }

    public void DoSimTick(double _st)
    {
        engine.SetSteerAngle(_st);
        engine.Step(0.1); // 100ms step

        // Update FormGPS state (backward compatibility)
        mf.mc.actualSteerAngleDegrees = engine.CurrentSteerAngle;
        mf.pn.vtgSpeed = Math.Abs(Math.Round(engine.Speed * 3.6 * 10, 2));
        mf.pn.AverageTheSpeed();

        // Update position
        CurrentLatLon = engine.CurrentLatLon;
        GeoCoord fixCoord = mf.AppModel.LocalPlane.ConvertWgs84ToGeoCoord(CurrentLatLon);
        mf.pn.fix.northing = fixCoord.Northing;
        mf.pn.fix.easting = fixCoord.Easting;
        mf.pn.headingTrue = mf.pn.headingTrueDual = glm.toDegrees(engine.Heading);
        mf.ahrs.imuHeading = mf.pn.headingTrue;

        // Rest remains same...
        mf.UpdateFixPosition();
    }
}
```

---

### Phase 3: Refactor Guidance System (Week 3-4)

#### 3.1 Make CGuidance Testable

**Current Problem**: CGuidance requires FormGPS for everything

**Solution**: Create facade/adapter pattern

**File**: `SourceCode/AgOpenGPS.Core/Interfaces/IGuidanceContext.cs`
```csharp
public interface IGuidanceContext
{
    // Vehicle state
    double AvgSpeed { get; }
    bool IsReverse { get; }
    bool IsBtnAutoSteerOn { get; }

    // Vehicle config
    double MaxSteerAngle { get; }
    double Wheelbase { get; }
    double StanleyHeadingErrorGain { get; }
    double StanleyDistanceErrorGain { get; }
    double StanleyIntegralGainAB { get; }

    // IMU
    double ImuRoll { get; }

    // For contour/curve
    CABLine ABLine { get; }
    CABCurve Curve { get; }
}
```

**File**: `SourceCode/GPS/Classes/FormGPSGuidanceContext.cs`
```csharp
/// <summary>
/// Adapter to provide guidance context from FormGPS
/// </summary>
public class FormGPSGuidanceContext : IGuidanceContext
{
    private readonly FormGPS mf;

    public FormGPSGuidanceContext(FormGPS mf)
    {
        this.mf = mf;
    }

    public double AvgSpeed => mf.avgSpeed;
    public bool IsReverse => mf.isReverse;
    public bool IsBtnAutoSteerOn => mf.isBtnAutoSteerOn;
    public double MaxSteerAngle => mf.vehicle.maxSteerAngle;
    // ... etc
}
```

**Modification**: `SourceCode/GPS/Classes/CGuidance.cs`

```csharp
public class CGuidance
{
    private readonly IGuidanceContext context; // Changed from FormGPS mf

    // Keep FormGPS constructor for backward compatibility
    public CGuidance(FormGPS _f)
        : this(new FormGPSGuidanceContext(_f))
    {
    }

    // New testable constructor
    public CGuidance(IGuidanceContext context)
    {
        this.context = context;
        sideHillCompFactor = Properties.Settings.Default.setAS_sideHillComp;
    }

    private void DoSteerAngleCalc()
    {
        if (context.IsReverse) steerHeadingError *= -1;
        steerHeadingError *= context.StanleyHeadingErrorGain;

        double sped = Math.Abs(context.AvgSpeed);
        // ... rest of method uses context instead of mf
    }
}
```

#### 3.2 Extract Pure Functions

**File**: `SourceCode/AgOpenGPS.Core/Guidance/StanleyController.cs`
```csharp
/// <summary>
/// Pure Stanley controller implementation - no dependencies
/// </summary>
public static class StanleyController
{
    public static StanleyResult Calculate(StanleyInput input)
    {
        // Extract pure math from DoSteerAngleCalc()

        double headingError = input.HeadingError;
        if (input.IsReverse) headingError *= -1;
        headingError *= input.HeadingErrorGain;

        double speed = Math.Abs(input.Speed);
        if (speed > 1) speed = 1 + 0.277 * (speed - 1);
        else speed = 1;

        double xteCorrection = Math.Atan(
            (input.CrossTrackError * input.DistanceErrorGain) / speed);

        double steerAngle = glm.toDegrees((xteCorrection + headingError) * -1.0);

        // Apply damping based on distance
        if (Math.Abs(input.CrossTrackError) > 0.5)
            steerAngle *= 0.5;
        else
            steerAngle *= (1 - Math.Abs(input.CrossTrackError));

        // Clamp to max
        if (steerAngle < -input.MaxSteerAngle) steerAngle = -input.MaxSteerAngle;
        else if (steerAngle > input.MaxSteerAngle) steerAngle = input.MaxSteerAngle;

        return new StanleyResult
        {
            SteerAngle = steerAngle,
            CrossTrackCorrection = xteCorrection
        };
    }
}

public struct StanleyInput
{
    public double CrossTrackError { get; set; }
    public double HeadingError { get; set; }
    public double Speed { get; set; }
    public double HeadingErrorGain { get; set; }
    public double DistanceErrorGain { get; set; }
    public double MaxSteerAngle { get; set; }
    public bool IsReverse { get; set; }
}

public struct StanleyResult
{
    public double SteerAngle { get; set; }
    public double CrossTrackCorrection { get; set; }
}
```

---

### Phase 4: Create Headless Test Harness (Week 4-5)

#### 4.1 Create Test Project

**File**: `SourceCode/AgOpenGPS.SimulationTests/AgOpenGPS.SimulationTests.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NUnit" Version="3.13.3" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.6.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\AgOpenGPS.Core\AgOpenGPS.Core.csproj" />
    <ProjectReference Include="..\GPS\GPS.csproj" />
  </ItemGroup>
</Project>
```

#### 4.2 Create Headless Simulation Environment

**File**: `SourceCode/AgOpenGPS.SimulationTests/HeadlessSimulation.cs`
```csharp
/// <summary>
/// Headless simulation environment for automated testing
/// </summary>
public class HeadlessSimulation : ISimulationEnvironment
{
    private readonly SimulatorEngine simulator;
    private readonly SimulationConfig config;
    private readonly GuidanceSystem guidance;
    private readonly SectionController sections;

    public IVehicleState VehicleState => simulator;
    public IGuidanceState GuidanceState => guidance;
    public ISectionController SectionController => sections;

    public event EventHandler<PositionUpdateEventArgs> OnPositionUpdate;
    public event EventHandler<SectionStateChangedEventArgs> OnSectionStateChanged;

    public HeadlessSimulation(SimulationConfig config)
    {
        this.config = config;

        // Create components
        simulator = new SimulatorEngine(config);
        guidance = new GuidanceSystem(config);
        sections = new SectionController(config.NumberOfSections);
    }

    public void Initialize(SimulationConfig config)
    {
        simulator.Reset();
        guidance.SetGuidanceLine(config.GuidanceLine);
        sections.Reset();
    }

    public void Step(double deltaTime)
    {
        // 1. Update position based on current steer angle
        simulator.Step(deltaTime);

        // 2. Calculate guidance (cross-track error, heading error)
        var guidanceResult = guidance.Calculate(simulator);

        // 3. Update steer angle for next step
        simulator.SetSteerAngle(guidanceResult.SteerAngle);

        // 4. Update section states
        sections.Update(simulator.CurrentPosition, config.Boundary);

        // 5. Fire events for logging
        OnPositionUpdate?.Invoke(this, new PositionUpdateEventArgs
        {
            Position = simulator.CurrentPosition,
            Heading = simulator.Heading,
            SteerAngle = guidanceResult.SteerAngle,
            CrossTrackError = guidanceResult.CrossTrackError
        });
    }

    public void SetSteerAngle(double angle)
    {
        simulator.SetSteerAngle(angle);
    }

    public void Reset()
    {
        Initialize(config);
    }
}
```

#### 4.3 Create Simulation Runner

**File**: `SourceCode/AgOpenGPS.SimulationTests/SimulationRunner.cs`
```csharp
/// <summary>
/// Runs simulations and collects results
/// </summary>
public class SimulationRunner
{
    private readonly HeadlessSimulation simulation;
    private readonly SimulationConfig config;

    private readonly List<VehicleState> trajectory = new List<VehicleState>();
    private readonly List<SteeringCommand> steeringCommands = new List<SteeringCommand>();

    public SimulationRunner(SimulationConfig config)
    {
        this.config = config;
        this.simulation = new HeadlessSimulation(config);

        // Subscribe to events
        simulation.OnPositionUpdate += RecordState;
    }

    public SimulationResult Run(double duration)
    {
        trajectory.Clear();
        steeringCommands.Clear();

        simulation.Reset();

        double timeStep = 0.1 / config.TimeAcceleration; // 100ms steps
        double elapsed = 0;

        while (elapsed < duration)
        {
            simulation.Step(timeStep);
            elapsed += timeStep;
        }

        return new SimulationResult
        {
            Trajectory = trajectory,
            SteeringCommands = steeringCommands,
            Statistics = CalculateStatistics()
        };
    }

    private void RecordState(object sender, PositionUpdateEventArgs e)
    {
        trajectory.Add(new VehicleState
        {
            Time = trajectory.Count * 0.1,
            Position = e.Position,
            Heading = e.Heading,
            SteerAngle = e.SteerAngle
        });

        steeringCommands.Add(new SteeringCommand
        {
            Time = trajectory.Count * 0.1,
            Angle = e.SteerAngle
        });
    }

    private SimulationStatistics CalculateStatistics()
    {
        // Calculate RMS error, max error, etc.
        var errors = trajectory.Select(t => t.CrossTrackError).ToList();

        return new SimulationStatistics
        {
            RMSCrossTrackError = Math.Sqrt(errors.Average(e => e * e)),
            MaxCrossTrackError = errors.Max(Math.Abs),
            TotalDistance = trajectory.Sum(t => t.DistanceTraveled),
            // ... etc
        };
    }
}
```

#### 4.4 Create Basic Tests

**File**: `SourceCode/AgOpenGPS.SimulationTests/BasicGuidanceTests.cs`
```csharp
[TestFixture]
public class BasicGuidanceTests
{
    [Test]
    public void StraightABLine_ShouldFollowWithLowError()
    {
        // Arrange
        var config = new SimulationConfig
        {
            StartPosition = new Wgs84(39.0, -104.0),
            StartHeading = 0, // North
            Vehicle = TestVehicles.StandardTractor,
            Mode = GuidanceMode.ABLine,
            GuidanceLine = CreateStraightLine(500), // 500m line
            SimulationSpeed = 10.0, // 10 km/h
            TimeAcceleration = 10.0 // 10x real-time
        };

        var runner = new SimulationRunner(config);

        // Act
        var result = runner.Run(duration: 180); // 3 minutes real-time = 18 sec test time

        // Assert
        Assert.That(result.Statistics.RMSCrossTrackError,
                    Is.LessThan(0.02),
                    "RMS cross-track error should be < 2cm");

        Assert.That(result.Statistics.MaxCrossTrackError,
                    Is.LessThan(0.10),
                    "Max cross-track error should be < 10cm");

        Assert.That(result.Statistics.TotalDistance,
                    Is.EqualTo(500).Within(5),
                    "Should travel approximately 500m");
    }

    [Test]
    public void StraightABLine_At45Degrees_ShouldFollowAccurately()
    {
        // Test diagonal line
        var config = CreateTestConfig(heading: Math.PI / 4); // 45 degrees
        var runner = new SimulationRunner(config);

        var result = runner.Run(duration: 180);

        Assert.That(result.Statistics.RMSCrossTrackError, Is.LessThan(0.02));
    }

    [Test]
    public void SCurve_ShouldFollowWithAcceptableError()
    {
        // Arrange
        var config = new SimulationConfig
        {
            StartPosition = new Wgs84(39.0, -104.0),
            Vehicle = TestVehicles.StandardTractor,
            Mode = GuidanceMode.Curve,
            GuidanceLine = CreateSCurve(500, curvatureRadius: 100),
            SimulationSpeed = 8.0,
            TimeAcceleration = 10.0
        };

        var runner = new SimulationRunner(config);

        // Act
        var result = runner.Run(duration: 225); // Slower for curve

        // Assert
        Assert.That(result.Statistics.RMSCrossTrackError,
                    Is.LessThan(0.05),
                    "RMS error on curves should be < 5cm");
    }

    private List<vec3> CreateStraightLine(double length)
    {
        var line = new List<vec3>();
        for (double i = 0; i < length; i += 1.0)
        {
            line.Add(new vec3(0, i, 0)); // Straight north
        }
        return line;
    }

    private List<vec3> CreateSCurve(double length, double curvatureRadius)
    {
        // Create S-shaped curve for testing
        // ... implementation
        throw new NotImplementedException();
    }
}
```

---

### Phase 5: Section Control Testing (Week 5-6)

#### 5.1 Create Section Controller Tests

**File**: `SourceCode/AgOpenGPS.SimulationTests/SectionControlTests.cs`
```csharp
[TestFixture]
public class SectionControlTests
{
    [Test]
    public void Sections_ShouldTurnOnWhenInBoundary()
    {
        // Arrange
        var config = new SimulationConfig
        {
            StartPosition = new Wgs84(39.0, -104.0),
            Vehicle = TestVehicles.StandardTractor,
            GuidanceLine = CreateStraightLine(500),
            Boundary = CreateRectangularBoundary(500, 50),
            NumberOfSections = 8,
            ToolWidth = 12.0,
            SimulationSpeed = 10.0
        };

        var runner = new SimulationRunner(config);

        // Act
        var result = runner.Run(duration: 180);

        // Assert
        var sectionHistory = result.SectionHistory;

        // All sections should have been on at some point
        for (int i = 0; i < 8; i++)
        {
            Assert.That(sectionHistory.Any(s => s.SectionIndex == i && s.IsOn),
                       $"Section {i} should have turned on");
        }
    }

    [Test]
    public void AppliedArea_ShouldMatchExpected()
    {
        // Test that applied area calculation is correct
        var config = CreateFieldCoverageTest();
        var runner = new SimulationRunner(config);

        var result = runner.Run(duration: 600); // Cover entire field

        double expectedArea = 500 * 50; // 25,000 m²
        Assert.That(result.Statistics.AppliedArea,
                   Is.EqualTo(expectedArea).Within(5.Percent()));
    }

    [Test]
    public void OverlapAndSkip_ShouldBeMinimal()
    {
        // Run multiple passes
        var config = CreateMultiPassTest();
        var runner = new SimulationRunner(config);

        var result = runner.Run(duration: 1200);

        Assert.That(result.Statistics.OverlapPercentage, Is.LessThan(3.0));
        Assert.That(result.Statistics.SkipPercentage, Is.LessThan(1.0));
    }
}
```

---

## Implementation Roadmap

### Week 1-2: Foundation
- [ ] Create interface definitions
- [ ] Create DTO classes (SimulationConfig, SimulationResult, etc.)
- [ ] Set up SimulationTests project
- [ ] Create basic SimulatorEngine without dependencies

### Week 3-4: Core Refactoring
- [ ] Refactor CSim to use SimulatorEngine
- [ ] Create IGuidanceContext and adapter
- [ ] Extract pure functions from CGuidance
- [ ] Test existing functionality still works

### Week 5-6: Headless Implementation
- [ ] Create HeadlessSimulation class
- [ ] Create SimulationRunner
- [ ] Write first 5 basic tests
- [ ] Achieve 10x time acceleration

### Week 7-8: Section Control
- [ ] Implement SectionController for testing
- [ ] Add applied area calculation
- [ ] Write section control tests
- [ ] Add overlap/skip detection

### Week 9-10: Polish & CI
- [ ] Add 20+ comprehensive tests
- [ ] Set up GitHub Actions CI
- [ ] Document testing framework
- [ ] Create test data generators

---

## Breaking Changes & Mitigation

### Potential Breaking Changes

1. **CGuidance constructor signature**
   - **Mitigation**: Keep old constructor, add new one

2. **CSim behavior changes**
   - **Mitigation**: Extensive manual testing after refactor

3. **Properties.Settings access**
   - **Mitigation**: Gradual migration, keep backward compatibility

### Testing Strategy During Refactor

1. **Capture baseline behavior**
   - Record actual field test data before changes
   - Use as regression tests

2. **Parallel implementation**
   - Keep old code path working
   - Add new code alongside
   - Switch via feature flag

3. **Incremental rollout**
   - Test each phase independently
   - Don't break existing simulator

---

## Success Criteria

### Phase 1-2 Success
- [ ] All existing tests pass
- [ ] Simulator still works in UI
- [ ] Can create SimulatorEngine instance in test

### Phase 3-4 Success
- [ ] 5+ headless tests passing
- [ ] Tests run in < 30 seconds
- [ ] 10x time acceleration achieved
- [ ] CI pipeline green

### Phase 5 Success
- [ ] 20+ tests covering main scenarios
- [ ] Section control validated
- [ ] Applied area accuracy verified
- [ ] Full test suite < 5 minutes

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Breaking existing simulator | Medium | High | Keep old code path, extensive testing |
| Performance issues | Low | Medium | Profile and optimize |
| OpenGL still required | Low | High | Use software rendering fallback |
| Team resistance to refactor | Medium | Medium | Show value with early wins |
| Time estimate too optimistic | High | Low | Prioritize phases, can stop early |

---

## Resources Required

### Development Time
- Senior developer: 10 weeks
- Or 2 developers: 6 weeks (with coordination)

### Tools Needed
- NUnit test framework (already in project)
- Code coverage tools (dotCover, Coverlet)
- CI system (GitHub Actions - free)

### Documentation
- Architecture diagrams
- Testing guide
- API documentation for new interfaces

---

## Appendix A: File Structure Changes

```
SourceCode/
├── AgOpenGPS.Core/
│   ├── Interfaces/
│   │   ├── IVehicleState.cs [NEW]
│   │   ├── IGuidanceState.cs [NEW]
│   │   ├── ISectionController.cs [NEW]
│   │   ├── ISimulationEnvironment.cs [NEW]
│   │   ├── IGuidanceContext.cs [NEW]
│   │   └── IPositionProvider.cs [NEW]
│   ├── Simulation/
│   │   ├── SimulatorEngine.cs [NEW]
│   │   ├── VehiclePhysics.cs [NEW]
│   │   └── GuidanceSystem.cs [NEW]
│   ├── Guidance/
│   │   └── StanleyController.cs [NEW - extracted pure functions]
│   └── Models/
│       ├── SimulationConfig.cs [NEW]
│       ├── SimulationResult.cs [NEW]
│       └── SimulationStatistics.cs [NEW]
├── GPS/
│   └── Classes/
│       ├── CSim.cs [MODIFIED - use SimulatorEngine]
│       ├── CGuidance.cs [MODIFIED - use IGuidanceContext]
│       └── FormGPSGuidanceContext.cs [NEW - adapter]
└── AgOpenGPS.SimulationTests/ [NEW PROJECT]
    ├── HeadlessSimulation.cs
    ├── SimulationRunner.cs
    ├── BasicGuidanceTests.cs
    ├── SectionControlTests.cs
    └── Helpers/
        ├── TestVehicles.cs
        └── TestFieldGenerator.cs
```

---

## Appendix B: Example Test Output

```
AgOpenGPS Simulation Test Results
==================================

Test: StraightABLine_ShouldFollowWithLowError
Duration: 2.3 seconds (simulated 180 seconds at 10x speed)
Result: PASS

Statistics:
  RMS Cross-Track Error: 0.014 m (1.4 cm)
  Max Cross-Track Error: 0.067 m (6.7 cm)
  Average Heading Error: 0.8°
  Total Distance: 498.2 m

Trajectory saved to: test_results/straight_ab_trajectory.csv
Coverage map saved to: test_results/straight_ab_coverage.png
```

---

## Next Steps

1. **Review this plan** with team
2. **Prioritize phases** - can we stop after Phase 4?
3. **Assign resources** - who will work on this?
4. **Create feature branch** - `feature/automated-simulation-testing`
5. **Start with Phase 1** - interfaces and DTOs
6. **Show early progress** - get buy-in from team

This refactoring will enable:
- ✅ Automated regression testing
- ✅ Faster development cycles
- ✅ Confidence in guidance algorithm changes
- ✅ Quantitative validation of accuracy
- ✅ CI/CD integration
- ✅ Reduced field testing requirements
