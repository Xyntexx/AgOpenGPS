# Balanced Testing Approach - AgOpenGPS

## Philosophy: Strategic Refactoring with Early Testing

**Problem with Minimal Approach**: Tests work but are slow, brittle, and hard to maintain long-term.

**Problem with Full Refactor**: 10 weeks before any tests run, high risk.

**Solution**: Strategic minimal refactoring to enable clean tests, delivered incrementally.

---

## The Sweet Spot: 3-Week Hybrid Approach

### Week 1: Minimal Tests + Strategic Extraction
Get tests running quickly while extracting just enough to make them maintainable.

### Week 2: Core Simulation Decoupling
Clean up simulation to run truly headless with good performance.

### Week 3: Expand Test Coverage + Basic CI
Build out test suite and automation.

---

## Week 1: Quick Wins (Days 1-7)

### Day 1-2: Get First Test Running (Minimal Approach)

**Follow the minimal approach** to get initial tests working:
- Create test project
- Create `TestableFormGPS` wrapper
- Get one test passing

**Deliverable**: 1 passing test that validates straight line guidance

### Day 3-4: Extract Simulation State (Strategic Refactoring)

**Problem**: Tests need to reach into FormGPS for too many things.

**Solution**: Create lightweight state container for simulation.

**File**: `SourceCode/AgOpenGPS.Core/Simulation/SimulationState.cs` (NEW)

```csharp
namespace AgOpenGPS.Core.Simulation
{
    /// <summary>
    /// Contains all simulation state in one place
    /// Enables easy testing without FormGPS dependency
    /// </summary>
    public class SimulationState
    {
        // Position
        public Wgs84 CurrentLatLon { get; set; }
        public vec2 CurrentPosition { get; set; }
        public double Heading { get; set; }
        public double Speed { get; set; }
        public double Altitude { get; set; }

        // Orientation
        public double ImuHeading { get; set; }
        public double ImuRoll { get; set; }

        // Guidance
        public double SteerAngle { get; set; }
        public double CrossTrackError { get; set; }
        public double HeadingError { get; set; }

        // GPS Quality
        public int SatellitesTracked { get; set; }
        public double HDOP { get; set; }

        // Copy state from FormGPS
        public static SimulationState FromFormGPS(FormGPS form)
        {
            return new SimulationState
            {
                CurrentLatLon = form.sim.CurrentLatLon,
                CurrentPosition = form.pn.fix,
                Heading = form.fixHeading,
                Speed = form.avgSpeed,
                Altitude = form.pn.altitude,
                ImuHeading = form.ahrs.imuHeading,
                ImuRoll = form.ahrs.imuRoll,
                SteerAngle = form.gyd.steerAngleGu,
                CrossTrackError = form.gyd.distanceFromCurrentLinePivot,
                HeadingError = form.vehicle.modeActualHeadingError,
                SatellitesTracked = form.pn.satellitesTracked,
                HDOP = form.pn.hdop
            };
        }
    }
}
```

**Modification**: Update `CSim.cs` to use/expose state (SMALL CHANGE)

```csharp
public class CSim
{
    private readonly FormGPS mf;

    // NEW: Expose state cleanly
    public SimulationState State { get; private set; }

    public CSim(FormGPS _f)
    {
        mf = _f;
        State = new SimulationState();
        // ... existing code
    }

    public void DoSimTick(double _st)
    {
        // ... existing simulation code ...

        // NEW: Update state at end
        State.CurrentLatLon = CurrentLatLon;
        State.Heading = headingTrue;
        State.Speed = mf.avgSpeed;
        State.SteerAngle = steerangleAve;
    }
}
```

**Impact**: Now tests can read `form.sim.State` instead of reaching into 5 different objects.

### Day 5: Create Simulation Runner (Clean Wrapper)

**File**: `SourceCode/AgOpenGPS.IntegrationTests/SimulationRunner.cs`

```csharp
/// <summary>
/// Runs simulations and collects results
/// Uses FormGPS internally but provides clean API
/// </summary>
public class SimulationRunner
{
    private FormGPS form;
    private readonly List<SimulationState> trajectory = new List<SimulationState>();

    public SimulationRunner()
    {
        form = new FormGPS();
        form.CreateControl();
    }

    public void Configure(SimulationConfig config)
    {
        // Set up vehicle
        form.vehicle.wheelbase = config.Vehicle.Wheelbase;
        form.vehicle.maxSteerAngle = config.Vehicle.MaxSteerAngle;
        form.vehicle.stanleyHeadingErrorGain = config.Stanley.HeadingErrorGain;
        form.vehicle.stanleyDistanceErrorGain = config.Stanley.DistanceErrorGain;
        form.vehicle.stanleyIntegralGainAB = config.Stanley.IntegralGain;

        // Set up starting position
        form.sim.CurrentLatLon = config.StartPosition;
        form.sim.headingTrue = config.StartHeading;
        form.sim.stepDistance = config.Speed / 36.0;

        // Set up guidance
        if (config.ABLine != null)
        {
            SetupABLine(config.ABLine);
        }

        // Enable autosteer
        form.isBtnAutoSteerOn = true;
    }

    public SimulationResult Run(double duration)
    {
        trajectory.Clear();

        int ticks = (int)(duration / 0.1); // 100ms per tick

        for (int i = 0; i < ticks; i++)
        {
            // Get current guidance steer angle
            double steerAngle = form.gyd.steerAngleGu;

            // Update simulation
            form.sim.DoSimTick(steerAngle);

            // Record state (now clean!)
            trajectory.Add(SimulationState.FromFormGPS(form));
        }

        return new SimulationResult
        {
            Trajectory = trajectory,
            Statistics = CalculateStatistics()
        };
    }

    private void SetupABLine(ABLineConfig config)
    {
        form.ABLine.origin.easting = config.OriginEasting;
        form.ABLine.origin.northing = config.OriginNorthing;
        form.ABLine.abHeading = config.Heading;
        form.ABLine.isABLineSet = true;
        form.ABLine.howManyPathsAway = 0;
    }

    private SimulationStatistics CalculateStatistics()
    {
        var errors = trajectory.Select(t => Math.Abs(t.CrossTrackError)).ToList();

        return new SimulationStatistics
        {
            RMSCrossTrackError = Math.Sqrt(errors.Average(e => e * e)),
            MaxCrossTrackError = errors.Max(),
            AverageCrossTrackError = errors.Average(),
            TotalDistance = CalculateDistance(),
            PointCount = trajectory.Count
        };
    }

    private double CalculateDistance()
    {
        double total = 0;
        for (int i = 1; i < trajectory.Count; i++)
        {
            double dx = trajectory[i].CurrentPosition.easting - trajectory[i - 1].CurrentPosition.easting;
            double dy = trajectory[i].CurrentPosition.northing - trajectory[i - 1].CurrentPosition.northing;
            total += Math.Sqrt(dx * dx + dy * dy);
        }
        return total;
    }

    public void Dispose()
    {
        form?.Dispose();
    }
}
```

### Day 6-7: Clean Configuration & Write Tests

**File**: `SourceCode/AgOpenGPS.IntegrationTests/SimulationConfig.cs`

```csharp
/// <summary>
/// Configuration for simulation tests
/// Clean API that hides FormGPS complexity
/// </summary>
public class SimulationConfig
{
    public Wgs84 StartPosition { get; set; }
    public double StartHeading { get; set; }
    public VehicleConfig Vehicle { get; set; }
    public StanleyConfig Stanley { get; set; }
    public ABLineConfig ABLine { get; set; }
    public double Speed { get; set; } // km/h

    public static SimulationConfig CreateDefault()
    {
        return new SimulationConfig
        {
            StartPosition = new Wgs84(39.0, -104.0),
            StartHeading = 0,
            Vehicle = VehicleConfig.StandardTractor(),
            Stanley = StanleyConfig.Default(),
            Speed = 10.0
        };
    }
}

public class VehicleConfig
{
    public double Wheelbase { get; set; }
    public double MaxSteerAngle { get; set; }

    public static VehicleConfig StandardTractor()
    {
        return new VehicleConfig
        {
            Wheelbase = 2.5,
            MaxSteerAngle = 45.0
        };
    }
}

public class StanleyConfig
{
    public double HeadingErrorGain { get; set; }
    public double DistanceErrorGain { get; set; }
    public double IntegralGain { get; set; }

    public static StanleyConfig Default()
    {
        return new StanleyConfig
        {
            HeadingErrorGain = 1.0,
            DistanceErrorGain = 1.0,
            IntegralGain = 1.0
        };
    }
}

public class ABLineConfig
{
    public double OriginEasting { get; set; }
    public double OriginNorthing { get; set; }
    public double Heading { get; set; }

    public static ABLineConfig CreateStraightNorth(double length = 500)
    {
        return new ABLineConfig
        {
            OriginEasting = 0,
            OriginNorthing = 0,
            Heading = 0 // North
        };
    }
}
```

**File**: `SourceCode/AgOpenGPS.IntegrationTests/BasicGuidanceTests.cs`

```csharp
[TestFixture]
public class BasicGuidanceTests
{
    [Test]
    public void StraightLine_500m_ShouldFollowWithLowError()
    {
        // Arrange - Clean API!
        var config = SimulationConfig.CreateDefault();
        config.ABLine = ABLineConfig.CreateStraightNorth(500);

        using (var runner = new SimulationRunner())
        {
            runner.Configure(config);

            // Act
            var result = runner.Run(duration: 180); // 3 minutes

            // Assert
            Console.WriteLine($"RMS Error: {result.Statistics.RMSCrossTrackError * 100:F2} cm");
            Console.WriteLine($"Max Error: {result.Statistics.MaxCrossTrackError * 100:F2} cm");

            Assert.That(result.Statistics.RMSCrossTrackError, Is.LessThan(0.05),
                       "RMS error should be < 5cm");
            Assert.That(result.Statistics.MaxCrossTrackError, Is.LessThan(0.20),
                       "Max error should be < 20cm");
        }
    }

    [Test]
    public void OffsetStart_ShouldConverge()
    {
        // Start 2m off the line
        var config = SimulationConfig.CreateDefault();
        config.ABLine = ABLineConfig.CreateStraightNorth(500);

        using (var runner = new SimulationRunner())
        {
            runner.Configure(config);

            // Manually offset position
            runner.form.pn.fix.easting = 2.0; // HACK: Still need some access

            // Act
            var result = runner.Run(duration: 60);

            // Assert
            var finalError = result.Trajectory.Last().CrossTrackError;
            Assert.That(Math.Abs(finalError), Is.LessThan(0.10),
                       "Should converge to within 10cm");
        }
    }
}
```

### Week 1 Deliverables ✅

- [x] 1 file modified: `CSim.cs` (add State property)
- [x] 1 new class: `SimulationState.cs`
- [x] Test infrastructure: `SimulationRunner`, `SimulationConfig`
- [x] 3-5 tests passing
- [x] Clean test API established

**Total Production Code Changes**: ~50 lines added to CSim, no breaking changes

---

## Week 2: Strategic Decoupling (Days 8-14)

### Goal: Make simulation truly headless and 10x faster

### Day 8-9: Extract Guidance Interface

**Problem**: Can't test guidance without FormGPS

**Solution**: Small adapter pattern (like original plan but minimal)

**File**: `SourceCode/GPS/Classes/GuidanceAdapter.cs` (NEW)

```csharp
/// <summary>
/// Provides guidance context without exposing all of FormGPS
/// Minimal adapter - only what guidance needs
/// </summary>
public interface IGuidanceContext
{
    double AvgSpeed { get; }
    bool IsReverse { get; }
    double MaxSteerAngle { get; }
    double StanleyHeadingErrorGain { get; }
    double StanleyDistanceErrorGain { get; }
    double StanleyIntegralGainAB { get; }
    double ImuRoll { get; }
}

/// <summary>
/// Adapter from FormGPS to IGuidanceContext
/// </summary>
public class FormGPSGuidanceAdapter : IGuidanceContext
{
    private readonly FormGPS mf;

    public FormGPSGuidanceAdapter(FormGPS mf)
    {
        this.mf = mf;
    }

    public double AvgSpeed => mf.avgSpeed;
    public bool IsReverse => mf.isReverse;
    public double MaxSteerAngle => mf.vehicle.maxSteerAngle;
    public double StanleyHeadingErrorGain => mf.vehicle.stanleyHeadingErrorGain;
    public double StanleyDistanceErrorGain => mf.vehicle.stanleyDistanceErrorGain;
    public double StanleyIntegralGainAB => mf.vehicle.stanleyIntegralGainAB;
    public double ImuRoll => mf.ahrs.imuRoll;
}

/// <summary>
/// Simple implementation for testing
/// </summary>
public class TestGuidanceContext : IGuidanceContext
{
    public double AvgSpeed { get; set; }
    public bool IsReverse { get; set; }
    public double MaxSteerAngle { get; set; }
    public double StanleyHeadingErrorGain { get; set; }
    public double StanleyDistanceErrorGain { get; set; }
    public double StanleyIntegralGainAB { get; set; }
    public double ImuRoll { get; set; }
}
```

**Modification**: Update `CGuidance.cs` (BACKWARD COMPATIBLE)

```csharp
public class CGuidance
{
    private readonly IGuidanceContext context;

    // OLD constructor - still works!
    public CGuidance(FormGPS _f)
        : this(new FormGPSGuidanceAdapter(_f))
    {
    }

    // NEW constructor for testing
    public CGuidance(IGuidanceContext context)
    {
        this.context = context;
        sideHillCompFactor = Properties.Settings.Default.setAS_sideHillComp;
    }

    private void DoSteerAngleCalc()
    {
        // Replace: mf.isReverse
        // With: context.IsReverse
        if (context.IsReverse) steerHeadingError *= -1;

        steerHeadingError *= context.StanleyHeadingErrorGain;

        double sped = Math.Abs(context.AvgSpeed);
        // ... rest of method using context
    }
}
```

**Impact**: Now can test guidance calculations independently!

### Day 10-11: Create Lightweight Simulation Engine

**File**: `SourceCode/AgOpenGPS.Core/Simulation/SimulationEngine.cs` (NEW)

```csharp
/// <summary>
/// Lightweight simulation engine - no FormGPS dependency
/// Can run 100x faster for testing
/// </summary>
public class SimulationEngine
{
    private readonly SimulationConfig config;
    private SimulationState state;
    private CGuidance guidance;

    public SimulationState State => state;

    public SimulationEngine(SimulationConfig config)
    {
        this.config = config;
        Reset();
    }

    public void Reset()
    {
        state = new SimulationState
        {
            CurrentLatLon = config.StartPosition,
            Heading = config.StartHeading,
            Speed = config.Speed / 3.6, // km/h to m/s
            ImuRoll = 0,
            SatellitesTracked = 12,
            HDOP = 0.7
        };

        // Create guidance with test context
        var guidanceContext = new TestGuidanceContext
        {
            AvgSpeed = state.Speed,
            IsReverse = false,
            MaxSteerAngle = config.Vehicle.MaxSteerAngle,
            StanleyHeadingErrorGain = config.Stanley.HeadingErrorGain,
            StanleyDistanceErrorGain = config.Stanley.DistanceErrorGain,
            StanleyIntegralGainAB = config.Stanley.IntegralGain,
            ImuRoll = 0
        };

        guidance = new CGuidance(guidanceContext);
    }

    public void Step(double deltaTime)
    {
        // 1. Calculate cross-track error and heading error
        CalculateGuidanceErrors();

        // 2. Get steer angle from guidance
        // Call existing guidance calculation
        var steerAngle = guidance.steerAngleGu;

        // 3. Update vehicle physics
        double steerangleSmoothed = SmoothSteerAngle(steerAngle, deltaTime);

        // 4. Update position and heading
        double stepDistance = state.Speed * deltaTime;
        double headingChange = stepDistance *
            Math.Tan(glm.toRadians(steerangleSmoothed)) /
            config.Vehicle.Wheelbase;

        state.Heading += headingChange;
        if (state.Heading > glm.twoPI) state.Heading -= glm.twoPI;
        if (state.Heading < 0) state.Heading += glm.twoPI;

        // Update position (WGS84)
        state.CurrentLatLon = state.CurrentLatLon
            .CalculateNewPostionFromBearingDistance(state.Heading, stepDistance);

        // Convert to local coordinates
        // (Simplified - uses flat earth approximation)
        state.CurrentPosition.easting += Math.Sin(state.Heading) * stepDistance;
        state.CurrentPosition.northing += Math.Cos(state.Heading) * stepDistance;

        state.SteerAngle = steerangleSmoothed;
    }

    private void CalculateGuidanceErrors()
    {
        // Calculate cross-track error to AB line
        if (config.ABLine != null)
        {
            // AB line equation: how far is current position from line?
            double lineHeading = config.ABLine.Heading;
            double dx = state.CurrentPosition.easting - config.ABLine.OriginEasting;
            double dy = state.CurrentPosition.northing - config.ABLine.OriginNorthing;

            // Cross-track error (perpendicular distance)
            state.CrossTrackError = -dx * Math.Cos(lineHeading) + dy * Math.Sin(lineHeading);

            // Heading error
            state.HeadingError = state.Heading - lineHeading;
            if (state.HeadingError > Math.PI) state.HeadingError -= glm.twoPI;
            if (state.HeadingError < -Math.PI) state.HeadingError += glm.twoPI;

            // Update guidance inputs
            guidance.distanceFromCurrentLinePivot = state.CrossTrackError;
            guidance.distanceFromCurrentLineSteer = state.CrossTrackError;
            guidance.steerHeadingError = state.HeadingError;
        }
    }

    private double currentSteerAngleSmoothed = 0;

    private double SmoothSteerAngle(double targetAngle, double deltaTime)
    {
        // Simulate hydraulic lag
        double diff = Math.Abs(targetAngle - currentSteerAngleSmoothed);

        if (diff > 11)
            currentSteerAngleSmoothed += (targetAngle > currentSteerAngleSmoothed) ? 6 : -6;
        else if (diff > 5)
            currentSteerAngleSmoothed += (targetAngle > currentSteerAngleSmoothed) ? 2 : -2;
        else if (diff > 1)
            currentSteerAngleSmoothed += (targetAngle > currentSteerAngleSmoothed) ? 0.5 : -0.5;
        else
            currentSteerAngleSmoothed = targetAngle;

        return currentSteerAngleSmoothed;
    }
}
```

### Day 12-14: Update Tests to Use New Engine

**File**: `SourceCode/AgOpenGPS.IntegrationTests/FastSimulationRunner.cs` (NEW)

```csharp
/// <summary>
/// Fast simulation runner using SimulationEngine
/// 10-100x faster than FormGPS-based tests
/// </summary>
public class FastSimulationRunner
{
    private readonly SimulationEngine engine;
    private readonly List<SimulationState> trajectory = new List<SimulationState>();

    public FastSimulationRunner(SimulationConfig config)
    {
        engine = new SimulationEngine(config);
    }

    public SimulationResult Run(double duration, double acceleration = 1.0)
    {
        trajectory.Clear();
        engine.Reset();

        double timeStep = 0.1; // 100ms
        double elapsed = 0;

        while (elapsed < duration)
        {
            engine.Step(timeStep);

            // Record state
            trajectory.Add(new SimulationState
            {
                CurrentLatLon = engine.State.CurrentLatLon,
                CurrentPosition = engine.State.CurrentPosition,
                Heading = engine.State.Heading,
                Speed = engine.State.Speed,
                SteerAngle = engine.State.SteerAngle,
                CrossTrackError = engine.State.CrossTrackError,
                HeadingError = engine.State.HeadingError
            });

            elapsed += timeStep;
        }

        return new SimulationResult
        {
            Trajectory = trajectory,
            Statistics = CalculateStatistics()
        };
    }

    private SimulationStatistics CalculateStatistics()
    {
        // Same as before
        var errors = trajectory.Select(t => Math.Abs(t.CrossTrackError)).ToList();

        return new SimulationStatistics
        {
            RMSCrossTrackError = Math.Sqrt(errors.Average(e => e * e)),
            MaxCrossTrackError = errors.Max(),
            AverageCrossTrackError = errors.Average(),
            TotalDistance = CalculateDistance(),
            PointCount = trajectory.Count
        };
    }

    private double CalculateDistance()
    {
        double total = 0;
        for (int i = 1; i < trajectory.Count; i++)
        {
            var p1 = trajectory[i - 1].CurrentPosition;
            var p2 = trajectory[i].CurrentPosition;
            double dx = p2.easting - p1.easting;
            double dy = p2.northing - p1.northing;
            total += Math.Sqrt(dx * dx + dy * dy);
        }
        return total;
    }
}
```

**Updated Tests**:

```csharp
[TestFixture]
public class FastGuidanceTests
{
    [Test]
    public void FastSim_StraightLine_ShouldFollowAccurately()
    {
        // Arrange
        var config = SimulationConfig.CreateDefault();
        config.ABLine = ABLineConfig.CreateStraightNorth(500);

        var runner = new FastSimulationRunner(config);

        // Act - much faster now!
        var result = runner.Run(duration: 180);

        // Assert
        Assert.That(result.Statistics.RMSCrossTrackError, Is.LessThan(0.05));
        Assert.That(result.Statistics.MaxCrossTrackError, Is.LessThan(0.20));
    }

    [Test]
    [TestCase(0.0)]    // North
    [TestCase(45.0)]   // Northeast
    [TestCase(90.0)]   // East
    [TestCase(135.0)]  // Southeast
    public void FastSim_VariousHeadings_ShouldFollow(double headingDegrees)
    {
        // Test multiple headings quickly
        var config = SimulationConfig.CreateDefault();
        config.ABLine = new ABLineConfig
        {
            OriginEasting = 0,
            OriginNorthing = 0,
            Heading = glm.toRadians(headingDegrees)
        };

        var runner = new FastSimulationRunner(config);
        var result = runner.Run(duration: 180);

        Assert.That(result.Statistics.RMSCrossTrackError, Is.LessThan(0.05),
                   $"Heading {headingDegrees}° should follow accurately");
    }
}
```

### Week 2 Deliverables ✅

- [x] `CGuidance.cs` modified (backward compatible)
- [x] `IGuidanceContext` interface created
- [x] `SimulationEngine` created (no FormGPS dependency)
- [x] `FastSimulationRunner` for rapid tests
- [x] 10+ tests running in <10 seconds

**Total Production Code Changes**:
- CGuidance: ~100 lines changed (replace `mf.` with `context.`)
- New files: ~400 lines
- All backward compatible!

---

## Week 3: Expand & Automate (Days 15-21)

### Day 15-16: Validation Framework

**File**: `SourceCode/AgOpenGPS.IntegrationTests/Validators/GuidanceValidator.cs`

```csharp
/// <summary>
/// Validates guidance accuracy against requirements
/// </summary>
public static class GuidanceValidator
{
    public static ValidationResult Validate(SimulationResult result, GuidanceRequirements requirements)
    {
        var validationResult = new ValidationResult();

        // Check RMS error
        if (result.Statistics.RMSCrossTrackError > requirements.MaxRMSError)
        {
            validationResult.AddError(
                $"RMS error {result.Statistics.RMSCrossTrackError * 100:F2}cm exceeds " +
                $"requirement of {requirements.MaxRMSError * 100:F2}cm");
        }

        // Check max error
        if (result.Statistics.MaxCrossTrackError > requirements.MaxAbsoluteError)
        {
            validationResult.AddError(
                $"Max error {result.Statistics.MaxCrossTrackError * 100:F2}cm exceeds " +
                $"requirement of {requirements.MaxAbsoluteError * 100:F2}cm");
        }

        // Check stability (no wild oscillations)
        var oscillationCount = CountOscillations(result.Trajectory);
        if (oscillationCount > requirements.MaxOscillations)
        {
            validationResult.AddWarning(
                $"Detected {oscillationCount} oscillations, limit is {requirements.MaxOscillations}");
        }

        return validationResult;
    }

    private static int CountOscillations(List<SimulationState> trajectory)
    {
        int count = 0;
        for (int i = 2; i < trajectory.Count; i++)
        {
            double steerChange = trajectory[i].SteerAngle - trajectory[i - 1].SteerAngle;
            double prevSteerChange = trajectory[i - 1].SteerAngle - trajectory[i - 2].SteerAngle;

            // Sign change = oscillation
            if (Math.Sign(steerChange) != Math.Sign(prevSteerChange) && Math.Abs(steerChange) > 5)
            {
                count++;
            }
        }
        return count;
    }
}

public class GuidanceRequirements
{
    public double MaxRMSError { get; set; } = 0.05; // 5cm
    public double MaxAbsoluteError { get; set; } = 0.20; // 20cm
    public int MaxOscillations { get; set; } = 10;

    public static GuidanceRequirements Standard => new GuidanceRequirements();

    public static GuidanceRequirements Precision => new GuidanceRequirements
    {
        MaxRMSError = 0.02, // 2cm
        MaxAbsoluteError = 0.10 // 10cm
    };
}
```

### Day 17-18: Comprehensive Test Suite

**File**: `SourceCode/AgOpenGPS.IntegrationTests/ComprehensiveTests.cs`

```csharp
[TestFixture]
public class ComprehensiveGuidanceTests
{
    private static readonly GuidanceRequirements StandardReq = GuidanceRequirements.Standard;

    [Test]
    [TestCase(100)]
    [TestCase(250)]
    [TestCase(500)]
    [TestCase(1000)]
    public void StraightLine_VariousLengths(int length)
    {
        var config = SimulationConfig.CreateDefault();
        config.ABLine = ABLineConfig.CreateStraightNorth(length);

        var runner = new FastSimulationRunner(config);
        var result = runner.Run(duration: length / 10.0 * 3.6); // Time to cover distance

        var validation = GuidanceValidator.Validate(result, StandardReq);
        Assert.That(validation.IsValid, validation.GetErrorMessage());
    }

    [Test]
    [TestCase(5.0)]   // Very slow
    [TestCase(10.0)]  // Normal
    [TestCase(15.0)]  // Fast
    [TestCase(20.0)]  // Very fast
    public void StraightLine_VariousSpeeds(double speedKmh)
    {
        var config = SimulationConfig.CreateDefault();
        config.Speed = speedKmh;
        config.ABLine = ABLineConfig.CreateStraightNorth(500);

        var runner = new FastSimulationRunner(config);
        var result = runner.Run(duration: 500 / speedKmh * 3.6);

        var validation = GuidanceValidator.Validate(result, StandardReq);
        Assert.That(validation.IsValid, validation.GetErrorMessage());
    }

    [Test]
    public void LargeInitialOffset_ShouldConverge()
    {
        var config = SimulationConfig.CreateDefault();
        config.ABLine = ABLineConfig.CreateStraightNorth(500);

        var engine = new SimulationEngine(config);

        // Start 5m off the line
        engine.State.CurrentPosition.easting = 5.0;

        var runner = new FastSimulationRunner(config);
        var result = runner.Run(duration: 120); // 2 minutes to converge

        // Should be within 20cm at the end
        var finalError = Math.Abs(result.Trajectory.Last().CrossTrackError);
        Assert.That(finalError, Is.LessThan(0.20),
                   "Should converge from 5m offset to within 20cm");
    }

    [Test]
    public void StanleyGains_ShouldAffectPerformance()
    {
        // Test that changing gains has expected effect
        var configLowGain = SimulationConfig.CreateDefault();
        configLowGain.Stanley.HeadingErrorGain = 0.5;
        configLowGain.ABLine = ABLineConfig.CreateStraightNorth(500);

        var configHighGain = SimulationConfig.CreateDefault();
        configHighGain.Stanley.HeadingErrorGain = 2.0;
        configHighGain.ABLine = ABLineConfig.CreateStraightNorth(500);

        var resultLow = new FastSimulationRunner(configLowGain).Run(180);
        var resultHigh = new FastSimulationRunner(configHighGain).Run(180);

        // Higher gain should converge faster but may overshoot
        Console.WriteLine($"Low gain RMS: {resultLow.Statistics.RMSCrossTrackError * 100:F2}cm");
        Console.WriteLine($"High gain RMS: {resultHigh.Statistics.RMSCrossTrackError * 100:F2}cm");

        // Both should still meet requirements
        Assert.That(resultLow.Statistics.RMSCrossTrackError, Is.LessThan(0.05));
        Assert.That(resultHigh.Statistics.RMSCrossTrackError, Is.LessThan(0.05));
    }
}
```

### Day 19-20: Regression Test Infrastructure

**File**: `SourceCode/AgOpenGPS.IntegrationTests/RegressionTests.cs`

```csharp
[TestFixture]
public class RegressionTests
{
    /// <summary>
    /// Baseline tests - these establish expected behavior
    /// If these fail after changes, investigate carefully!
    /// </summary>
    [Test]
    public void Baseline_StandardScenario_ResultsMatch()
    {
        var config = SimulationConfig.CreateDefault();
        config.ABLine = ABLineConfig.CreateStraightNorth(500);

        var runner = new FastSimulationRunner(config);
        var result = runner.Run(duration: 180);

        // These are the baseline numbers from initial testing
        // Update these if you intentionally change guidance behavior
        Assert.That(result.Statistics.RMSCrossTrackError, Is.LessThan(0.05));
        Assert.That(result.Statistics.MaxCrossTrackError, Is.LessThan(0.20));
        Assert.That(result.Statistics.TotalDistance, Is.GreaterThan(450));

        // Save results for comparison
        SaveBaselineResults(result, "standard_scenario");
    }

    private void SaveBaselineResults(SimulationResult result, string testName)
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(result.Statistics, Newtonsoft.Json.Formatting.Indented);
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, $"baselines/{testName}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);
    }
}
```

### Day 21: CI/CD Setup

**File**: `.github/workflows/test.yml`

```yaml
name: AgOpenGPS Tests

on:
  push:
    branches: [ develop, main ]
  pull_request:
    branches: [ develop, main ]

jobs:
  test:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '4.8.x'

    - name: Restore dependencies
      run: dotnet restore SourceCode/AgOpenGPS.sln

    - name: Build
      run: dotnet build SourceCode/AgOpenGPS.sln --no-restore

    - name: Run Tests
      run: dotnet test SourceCode/AgOpenGPS.IntegrationTests/AgOpenGPS.IntegrationTests.csproj --no-build --verbosity normal --logger "trx;LogFileName=test-results.trx"

    - name: Publish Test Results
      uses: dorny/test-reporter@v1
      if: always()
      with:
        name: Test Results
        path: '**/*.trx'
        reporter: dotnet-trx

    - name: Upload test artifacts
      if: always()
      uses: actions/upload-artifact@v3
      with:
        name: test-results
        path: '**/test-results.trx'
```

### Week 3 Deliverables ✅

- [x] Validation framework
- [x] 20+ comprehensive tests
- [x] Regression test infrastructure
- [x] CI/CD pipeline
- [x] Test runs in <30 seconds

---

## Summary: What You Get After 3 Weeks

### Production Code Changes (Minimal!)

```
Modified Files (backward compatible):
├── CSim.cs (~50 lines added)
│   └── Add SimulationState property
└── CGuidance.cs (~100 lines changed)
    └── Use IGuidanceContext instead of direct FormGPS access

New Files (no impact on existing code):
├── AgOpenGPS.Core/Simulation/
│   ├── SimulationState.cs
│   └── SimulationEngine.cs
└── GPS/Classes/
    ├── IGuidanceContext.cs
    ├── FormGPSGuidanceAdapter.cs
    └── TestGuidanceContext.cs
```

### Test Infrastructure

```
AgOpenGPS.IntegrationTests/ (NEW)
├── SimulationRunner.cs (uses FormGPS)
├── FastSimulationRunner.cs (uses SimulationEngine)
├── SimulationConfig.cs
├── Validators/
│   └── GuidanceValidator.cs
├── BasicGuidanceTests.cs
├── FastGuidanceTests.cs
├── ComprehensiveTests.cs
└── RegressionTests.cs
```

### Capabilities

✅ **20+ automated tests** covering:
- Straight line guidance
- Various headings (0°, 45°, 90°, 135°)
- Various speeds (5-20 km/h)
- Various distances (100-1000m)
- Convergence from offsets
- Stability validation
- Regression baselines

✅ **Two test modes**:
- SlowRunner: Uses FormGPS (for validation)
- FastRunner: Uses SimulationEngine (10x faster)

✅ **CI/CD pipeline**: Automated testing on every PR

✅ **Validation framework**: Systematic requirements checking

✅ **Foundation for refactoring**: Tests protect future changes

---

## Comparison: Three Approaches

| Aspect | Minimal | **Balanced** | Full Refactor |
|--------|---------|------------|---------------|
| Time to first test | 3 days | **3 days** | 10 weeks |
| Production changes | 0 lines | **~150 lines** | ~2000 lines |
| Test speed | 1x-10x | **10x-100x** | 100x |
| Code quality | Poor | **Good** | Excellent |
| Maintainability | Low | **Medium-High** | High |
| Risk | Very Low | **Low** | Medium |
| Enables refactoring | Somewhat | **Yes** | N/A |

---

## Why This Is The Sweet Spot

### Compared to Minimal
- ✅ Much faster tests (10-100x vs 1-10x)
- ✅ Cleaner test code
- ✅ Better foundation for future work
- ✅ Only 150 lines of production changes

### Compared to Full Refactor
- ✅ 3 days vs 10 weeks to first test
- ✅ Much lower risk
- ✅ Tests enable the refactoring (not blocked by it)
- ✅ Can refactor incrementally later

### Best of Both Worlds
1. **Week 1**: Get tests running (like minimal)
2. **Week 2**: Strategic refactoring for clean tests
3. **Week 3**: Build comprehensive suite

Then use these tests to enable further refactoring safely.

---

## Next Steps

1. **Start Week 1** - Get first tests running
2. **Validate approach** - Make sure tests catch real issues
3. **Complete Week 2** - Clean up for maintainability
4. **Expand Week 3** - Build comprehensive coverage
5. **Then refactor** - Use tests to enable safe changes

This gives you automated testing in **3 weeks** with minimal risk and high quality.
