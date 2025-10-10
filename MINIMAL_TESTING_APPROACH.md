# Minimal Viable Testing Approach - AgOpenGPS

## Philosophy: Test First, Refactor Later

**Problem**: The refactoring plan requires 10 weeks before we get any tests running.

**Solution**: Create tests that work with the *current* architecture, even if imperfect. Use these tests to *enable* safe refactoring.

---

## Quick Win Strategy (1-2 Weeks)

### Phase 0: Bare Minimum Test Infrastructure

**Goal**: Get 3-5 automated tests running by end of Week 1, without touching production code.

#### Approach: Test Through the UI (Yes, Really)

Since everything goes through `FormGPS`, we'll instantiate it in tests with minimal initialization.

---

## Implementation

### Step 1: Create Test Project (Day 1)

**File**: `SourceCode/AgOpenGPS.IntegrationTests/AgOpenGPS.IntegrationTests.csproj`

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
    <ProjectReference Include="..\GPS\GPS.csproj" />
  </ItemGroup>
</Project>
```

### Step 2: Create Minimal FormGPS Wrapper (Day 1-2)

**File**: `SourceCode/AgOpenGPS.IntegrationTests/TestableFormGPS.cs`

```csharp
using AgOpenGPS;
using System;
using System.Windows.Forms;

namespace AgOpenGPS.IntegrationTests
{
    /// <summary>
    /// Wrapper around FormGPS that allows headless testing
    /// Works with CURRENT architecture - no refactoring needed
    /// </summary>
    public class TestableFormGPS : IDisposable
    {
        private FormGPS form;
        private System.Windows.Forms.Timer simTimer;
        private bool isInitialized = false;

        public FormGPS Form => form;

        /// <summary>
        /// Trajectory recording for validation
        /// </summary>
        public TrajectoryRecorder Recorder { get; private set; }

        public TestableFormGPS()
        {
            // Create form WITHOUT showing it
            // This is the key - FormGPS can be instantiated without Display()
            form = new FormGPS();

            // Don't call Form.Show() or Application.Run()
            // Just initialize the components
            form.CreateControl(); // Force handle creation without showing

            Recorder = new TrajectoryRecorder(form);
        }

        /// <summary>
        /// Initialize for simulation testing
        /// </summary>
        public void InitializeForSimulation(TestScenario scenario)
        {
            if (isInitialized)
                throw new InvalidOperationException("Already initialized");

            // Set up starting position
            form.sim.CurrentLatLon = scenario.StartPosition;
            form.sim.headingTrue = scenario.StartHeading;

            // Set vehicle config
            form.vehicle.wheelbase = scenario.VehicleConfig.Wheelbase;
            form.vehicle.maxSteerAngle = scenario.VehicleConfig.MaxSteerAngle;
            form.vehicle.stanleyHeadingErrorGain = scenario.VehicleConfig.StanleyHeadingErrorGain;
            form.vehicle.stanleyDistanceErrorGain = scenario.VehicleConfig.StanleyDistanceErrorGain;
            form.vehicle.stanleyIntegralGainAB = scenario.VehicleConfig.StanleyIntegralGainAB;

            // Set up AB line
            if (scenario.GuidanceMode == GuidanceMode.ABLine)
            {
                SetupABLine(scenario.GuidanceLine);
            }

            // Enable simulator
            form.timerSim.Enabled = true;

            // Set speed
            form.sim.stepDistance = scenario.Speed / 36.0; // Convert km/h to m/100ms

            isInitialized = true;
        }

        /// <summary>
        /// Run simulation for specified duration (accelerated)
        /// </summary>
        public SimulationResult RunSimulation(double durationSeconds, double acceleration = 10.0)
        {
            if (!isInitialized)
                throw new InvalidOperationException("Not initialized");

            Recorder.StartRecording();

            // Calculate how many ticks we need
            double tickInterval = 0.1; // 100ms per tick
            int totalTicks = (int)(durationSeconds / tickInterval);

            // Enable autosteer
            form.isBtnAutoSteerOn = true;

            // Run simulation loop
            for (int i = 0; i < totalTicks; i++)
            {
                // This is the key: manually tick the simulation
                // Get steer angle from guidance
                double steerAngle = form.gyd.steerAngleGu;

                // Update simulator with that angle
                form.sim.DoSimTick(steerAngle);

                // Guidance will be recalculated in UpdateFixPosition()

                // Record state
                Recorder.RecordTick();

                // Optional: add small delay for debugging
                // System.Threading.Thread.Sleep(1);
            }

            Recorder.StopRecording();

            return Recorder.GetResults();
        }

        /// <summary>
        /// Set up AB line from point list
        /// </summary>
        private void SetupABLine(System.Collections.Generic.List<vec3> line)
        {
            if (line.Count < 2)
                throw new ArgumentException("Need at least 2 points for AB line");

            // Set AB line origin and heading
            vec3 ptA = line[0];
            vec3 ptB = line[line.Count - 1];

            form.ABLine.origin.easting = ptA.easting;
            form.ABLine.origin.northing = ptA.northing;

            double dx = ptB.easting - ptA.easting;
            double dy = ptB.northing - ptA.northing;
            form.ABLine.abHeading = Math.Atan2(dx, dy);

            form.ABLine.isABLineSet = true;
            form.ABLine.howManyPathsAway = 0; // Start on the line
        }

        public void Dispose()
        {
            if (simTimer != null)
            {
                simTimer.Enabled = false;
                simTimer.Dispose();
            }

            form?.Dispose();
        }
    }
}
```

### Step 3: Create Trajectory Recorder (Day 2)

**File**: `SourceCode/AgOpenGPS.IntegrationTests/TrajectoryRecorder.cs`

```csharp
using AgOpenGPS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AgOpenGPS.IntegrationTests
{
    /// <summary>
    /// Records vehicle trajectory during simulation for analysis
    /// </summary>
    public class TrajectoryRecorder
    {
        private readonly FormGPS form;
        private readonly List<TrajectoryPoint> trajectory = new List<TrajectoryPoint>();
        private bool isRecording = false;
        private DateTime startTime;

        public TrajectoryRecorder(FormGPS form)
        {
            this.form = form;
        }

        public void StartRecording()
        {
            trajectory.Clear();
            isRecording = true;
            startTime = DateTime.Now;
        }

        public void StopRecording()
        {
            isRecording = false;
        }

        public void RecordTick()
        {
            if (!isRecording) return;

            var point = new TrajectoryPoint
            {
                Time = (DateTime.Now - startTime).TotalSeconds,
                Easting = form.pn.fix.easting,
                Northing = form.pn.fix.northing,
                Heading = form.fixHeading,
                Speed = form.avgSpeed,
                SteerAngle = form.gyd.steerAngleGu,
                CrossTrackError = form.gyd.distanceFromCurrentLinePivot,
                HeadingError = form.vehicle.modeActualHeadingError
            };

            trajectory.Add(point);
        }

        public SimulationResult GetResults()
        {
            return new SimulationResult
            {
                Trajectory = trajectory.ToList(),
                Statistics = CalculateStatistics()
            };
        }

        private SimulationStatistics CalculateStatistics()
        {
            if (trajectory.Count == 0)
                return new SimulationStatistics();

            var errors = trajectory.Select(t => Math.Abs(t.CrossTrackError)).ToList();
            var headingErrors = trajectory.Select(t => Math.Abs(t.HeadingError)).ToList();

            // Calculate total distance
            double totalDistance = 0;
            for (int i = 1; i < trajectory.Count; i++)
            {
                double dx = trajectory[i].Easting - trajectory[i - 1].Easting;
                double dy = trajectory[i].Northing - trajectory[i - 1].Northing;
                totalDistance += Math.Sqrt(dx * dx + dy * dy);
            }

            return new SimulationStatistics
            {
                RMSCrossTrackError = Math.Sqrt(errors.Average(e => e * e)),
                MaxCrossTrackError = errors.Max(),
                AverageCrossTrackError = errors.Average(),
                AverageHeadingError = headingErrors.Average(),
                MaxHeadingError = headingErrors.Max(),
                TotalDistance = totalDistance,
                PointCount = trajectory.Count
            };
        }
    }

    public class TrajectoryPoint
    {
        public double Time { get; set; }
        public double Easting { get; set; }
        public double Northing { get; set; }
        public double Heading { get; set; }
        public double Speed { get; set; }
        public double SteerAngle { get; set; }
        public double CrossTrackError { get; set; }
        public double HeadingError { get; set; }
    }

    public class SimulationResult
    {
        public List<TrajectoryPoint> Trajectory { get; set; }
        public SimulationStatistics Statistics { get; set; }
    }

    public class SimulationStatistics
    {
        public double RMSCrossTrackError { get; set; }
        public double MaxCrossTrackError { get; set; }
        public double AverageCrossTrackError { get; set; }
        public double AverageHeadingError { get; set; }
        public double MaxHeadingError { get; set; }
        public double TotalDistance { get; set; }
        public int PointCount { get; set; }
    }
}
```

### Step 4: Create Test Helpers (Day 2)

**File**: `SourceCode/AgOpenGPS.IntegrationTests/TestScenario.cs`

```csharp
using AgOpenGPS.Core.Models;
using System;
using System.Collections.Generic;

namespace AgOpenGPS.IntegrationTests
{
    public enum GuidanceMode
    {
        ABLine,
        Curve,
        Contour
    }

    public class TestScenario
    {
        public Wgs84 StartPosition { get; set; }
        public double StartHeading { get; set; }
        public VehicleConfig VehicleConfig { get; set; }
        public GuidanceMode GuidanceMode { get; set; }
        public List<vec3> GuidanceLine { get; set; }
        public double Speed { get; set; } // km/h

        public static TestScenario CreateStraightABLine(double length = 500)
        {
            return new TestScenario
            {
                StartPosition = new Wgs84(39.0, -104.0),
                StartHeading = 0, // North
                VehicleConfig = VehicleConfig.StandardTractor(),
                GuidanceMode = GuidanceMode.ABLine,
                GuidanceLine = CreateStraightLine(length),
                Speed = 10.0
            };
        }

        private static List<vec3> CreateStraightLine(double length)
        {
            var line = new List<vec3>();
            line.Add(new vec3(0, 0, 0));
            line.Add(new vec3(0, length, 0));
            return line;
        }
    }

    public class VehicleConfig
    {
        public double Wheelbase { get; set; }
        public double MaxSteerAngle { get; set; }
        public double StanleyHeadingErrorGain { get; set; }
        public double StanleyDistanceErrorGain { get; set; }
        public double StanleyIntegralGainAB { get; set; }

        public static VehicleConfig StandardTractor()
        {
            return new VehicleConfig
            {
                Wheelbase = 2.5, // meters
                MaxSteerAngle = 45.0, // degrees
                StanleyHeadingErrorGain = 1.0,
                StanleyDistanceErrorGain = 1.0,
                StanleyIntegralGainAB = 1.0
            };
        }
    }
}
```

### Step 5: Write First Tests (Day 3)

**File**: `SourceCode/AgOpenGPS.IntegrationTests/BasicGuidanceTests.cs`

```csharp
using NUnit.Framework;
using System;

namespace AgOpenGPS.IntegrationTests
{
    [TestFixture]
    public class BasicGuidanceTests
    {
        [Test]
        public void Test_StraightABLine_500m_ShouldFollowWithLowError()
        {
            // Arrange
            var scenario = TestScenario.CreateStraightABLine(length: 500);

            using (var testForm = new TestableFormGPS())
            {
                testForm.InitializeForSimulation(scenario);

                // Act
                // Simulate 180 seconds (3 minutes at 10 km/h = ~500m)
                var result = testForm.RunSimulation(durationSeconds: 180, acceleration: 10.0);

                // Assert
                Console.WriteLine($"RMS Error: {result.Statistics.RMSCrossTrackError:F4} m");
                Console.WriteLine($"Max Error: {result.Statistics.MaxCrossTrackError:F4} m");
                Console.WriteLine($"Distance: {result.Statistics.TotalDistance:F1} m");

                Assert.That(result.Statistics.RMSCrossTrackError,
                           Is.LessThan(0.05),
                           $"RMS cross-track error should be < 5cm, was {result.Statistics.RMSCrossTrackError * 100:F2}cm");

                Assert.That(result.Statistics.MaxCrossTrackError,
                           Is.LessThan(0.20),
                           $"Max cross-track error should be < 20cm, was {result.Statistics.MaxCrossTrackError * 100:F2}cm");

                Assert.That(result.Statistics.TotalDistance,
                           Is.GreaterThan(450),
                           "Should travel at least 450m");
            }
        }

        [Test]
        public void Test_StanleyController_ShouldConvergeToLine()
        {
            // Start 2 meters off the line
            var scenario = TestScenario.CreateStraightABLine(length: 500);

            using (var testForm = new TestableFormGPS())
            {
                testForm.InitializeForSimulation(scenario);

                // Offset starting position by 2 meters
                testForm.Form.pn.fix.easting += 2.0;

                // Act
                var result = testForm.RunSimulation(durationSeconds: 60, acceleration: 10.0);

                // Assert
                // Should converge within 60 seconds
                var finalError = result.Trajectory[result.Trajectory.Count - 1].CrossTrackError;
                Assert.That(Math.Abs(finalError),
                           Is.LessThan(0.10),
                           $"Should converge to within 10cm, final error was {Math.Abs(finalError) * 100:F2}cm");
            }
        }

        [Test]
        public void Test_GuidanceIsStable_NoOscillation()
        {
            // Check that guidance doesn't oscillate wildly
            var scenario = TestScenario.CreateStraightABLine(length: 500);

            using (var testForm = new TestableFormGPS())
            {
                testForm.InitializeForSimulation(scenario);

                // Act
                var result = testForm.RunSimulation(durationSeconds: 180, acceleration: 10.0);

                // Assert
                // Check that steering angle doesn't change by more than 10 degrees between ticks
                int largeChangeCount = 0;
                for (int i = 1; i < result.Trajectory.Count; i++)
                {
                    double steerChange = Math.Abs(
                        result.Trajectory[i].SteerAngle - result.Trajectory[i - 1].SteerAngle);

                    if (steerChange > 10.0)
                        largeChangeCount++;
                }

                // Allow max 5% of ticks to have large changes (startup, etc)
                double percentLargeChanges = (double)largeChangeCount / result.Trajectory.Count * 100;
                Assert.That(percentLargeChanges,
                           Is.LessThan(5.0),
                           $"Steering should be stable, but {percentLargeChanges:F1}% of ticks had >10° changes");
            }
        }
    }
}
```

---

## Why This Works

### Advantages of This Approach

1. **Zero Production Code Changes**
   - Tests work with current architecture
   - No risk of breaking existing functionality

2. **Fast to Implement**
   - 3 days to first running test
   - 1 week to useful test suite

3. **Immediate Value**
   - Can test guidance accuracy NOW
   - Establishes baseline metrics
   - Catches regressions

4. **Enables Refactoring**
   - Once tests pass, refactor with confidence
   - Tests prove refactored code behaves same

### Limitations (Acceptable for Now)

1. **Tests are slower**
   - Still 10x faster than real-time
   - Good enough for development

2. **Not perfectly isolated**
   - FormGPS still exists in tests
   - But doesn't need to be shown

3. **Some UI dependencies**
   - Timer may need tweaking
   - But manageable

---

## Next Steps After Initial Tests Pass

### Week 2: Add More Test Scenarios

```csharp
[Test]
public void Test_45DegreeABLine_ShouldFollowAccurately()
{
    // Test diagonal lines
}

[Test]
public void Test_ReverseDirection_ShouldHandleCorrectly()
{
    // Test reverse guidance
}

[Test]
public void Test_SharpTurn_ShouldRespectMaxSteerAngle()
{
    // Test steering limits
}
```

### Week 3-4: Add Section Control Tests

```csharp
[Test]
public void Test_SectionControl_TurnsOnInBoundary()
{
    // Test section logic
}

[Test]
public void Test_AppliedArea_IsAccurate()
{
    // Test coverage calculations
}
```

### Week 5+: Begin Refactoring

Now that we have test coverage:
1. Extract interfaces (tests still pass)
2. Refactor CSim (tests still pass)
3. Refactor CGuidance (tests still pass)
4. Eventually migrate to full headless architecture

---

## Running the Tests

### Command Line
```bash
cd SourceCode
dotnet test AgOpenGPS.IntegrationTests/AgOpenGPS.IntegrationTests.csproj --logger "console;verbosity=detailed"
```

### Visual Studio
1. Open Test Explorer
2. Run All Tests
3. View results

### Expected Output
```
Test: Test_StraightABLine_500m_ShouldFollowWithLowError
RMS Error: 0.0234 m
Max Error: 0.0876 m
Distance: 492.3 m
Result: PASSED (2.8 seconds)
```

---

## Success Criteria - Week 1

- [ ] Test project compiles
- [ ] Can instantiate FormGPS in test
- [ ] Can run simulation loop
- [ ] 3+ tests passing
- [ ] Tests complete in < 30 seconds total
- [ ] CI integration (GitHub Actions)

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| FormGPS requires display | Use CreateControl() without Show() |
| Timer issues in test | Manually drive sim loop instead |
| OpenGL crashes | Disable rendering in test mode |
| State pollution between tests | Create fresh FormGPS per test |

---

## Cost-Benefit Analysis

### Traditional Refactor-First Approach
- **Time to first test**: 10 weeks
- **Risk**: High (big rewrite)
- **Value**: Better architecture

### This Minimal Approach
- **Time to first test**: 3 days
- **Risk**: Low (no code changes)
- **Value**: Tests enable safe refactoring

**Recommendation**: Start minimal, refactor incrementally with test coverage.

---

## Appendix: Quick Start Checklist

### Day 1
- [ ] Create AgOpenGPS.IntegrationTests project
- [ ] Add NUnit packages
- [ ] Add reference to GPS project
- [ ] Create TestableFormGPS.cs skeleton

### Day 2
- [ ] Implement TrajectoryRecorder
- [ ] Create TestScenario helpers
- [ ] Get FormGPS instantiating in test

### Day 3
- [ ] Write first test (straight AB line)
- [ ] Debug issues
- [ ] Get test passing

### Day 4-5
- [ ] Add 2-3 more test scenarios
- [ ] Set up CI pipeline
- [ ] Document findings

---

## After This Works

You'll have:
1. ✅ Automated tests protecting against regressions
2. ✅ Quantitative accuracy metrics
3. ✅ Confidence to refactor
4. ✅ CI/CD pipeline
5. ✅ Foundation for more tests

Then you can tackle the full refactoring plan incrementally, with tests verifying each step.
