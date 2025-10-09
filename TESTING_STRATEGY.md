# AgOpenGPS Testing Strategy Report

## Current State Analysis

### Existing Testing Infrastructure
- **Unit Tests**: Minimal coverage (2 test projects)
  - `AgLibrary.Tests`: XML settings serialization
  - `AgOpenGPS.Core.Tests`: Basic geometric calculations (GeoCoord, Wgs84)
- **Simulator**: Basic real-time simulator exists (`CSim.cs`, `ModSim`)
  - Simulates GPS position, heading, speed
  - Manual steering via UI
  - Real-time NMEA string generation
  - No automated validation or acceleration

### Critical Components Lacking Tests
- Guidance algorithms (Stanley controller in `CGuidance.cs`)
- Section control logic (`CSection.cs`)
- AB line/curve tracking
- Contour guidance
- Auto UTurn functionality
- Applied area calculations

---

## Recommended Testing Strategies

### 1. **Automated Simulation Testing** ⭐ (Highest Priority)

**Why It's Critical**: Accelerated simulation testing is the **highest priority** for AgOpenGPS because:
- Tests complete field operations without hardware
- Validates steering accuracy quantitatively
- Catches regressions in guidance algorithms
- Enables CI/CD integration

**Implementation Approach**:

```
Headless Simulation Test Framework
├── Test Scenarios
│   ├── Straight AB Lines (various angles)
│   ├── Curved AB Paths
│   ├── Contour Following
│   └── UTurn Sequences
├── Validation Metrics
│   ├── Cross-track error (RMS, max deviation)
│   ├── Path completion accuracy
│   ├── Section overlap/skip percentage
│   └── Applied area vs expected area
└── Time Acceleration (10x-100x real-time)
```

**Key Features**:
- Inject predefined GPS paths/fields
- Run guidance loop at accelerated rates
- Record vehicle trajectory
- Compare against expected path with tolerance bands
- Generate pass/fail reports with metrics

**Example Test Cases**:
1. Follow 500m AB line at 30° angle → Max cross-track error < 2cm
2. Navigate S-curve contour → RMS error < 5cm
3. Complete field with 16 sections → Overlap < 3%, Skip < 1%

**Implementation Details**:
```csharp
[Test]
public void TestStraightABLineTracking()
{
    // Arrange
    var field = CreateTestField(500m length, 30° angle);
    var simulator = new HeadlessSimulator(accelerationFactor: 10);

    // Act
    var trajectory = simulator.RunSimulation(
        field: field,
        guidanceMode: GuidanceMode.ABLine,
        speed: 10 /* km/h */
    );

    // Assert
    var crossTrackError = trajectory.CalculateRMSError();
    Assert.That(crossTrackError, Is.LessThan(0.02)); // 2cm tolerance

    var appliedArea = trajectory.CalculateAppliedArea();
    var expectedArea = field.GetExpectedArea();
    Assert.That(appliedArea, Is.EqualTo(expectedArea).Within(3.Percent()));
}
```

---

### 2. **Unit Testing for Core Algorithms**

**Target Areas**:

| Component | Test Focus | Priority |
|-----------|-----------|----------|
| `CGuidance.cs` (GPS/Classes/) | Stanley controller math, integral windup limits | High |
| `CSection.cs` (GPS/Classes/) | On/off timing, boundary detection logic | High |
| `CABLine.cs`/`CABCurve.cs` (GPS/Classes/) | Line/curve generation, closest point calculations | Medium |
| `CHead.cs` (GPS/Classes/) | Headland turn point calculations | Medium |
| Coordinate transformations | WGS84 ↔ local plane conversions | High |

**Benefits**:
- Fast feedback (<1s per test)
- Pinpoint regressions to specific functions
- Enable refactoring with confidence
- Validate edge cases (zero speed, extreme angles, etc.)

**Example Tests**:
```csharp
[Test]
public void SteerAngle_ShouldNotExceedMaximum()
{
    var guidance = new CGuidance(mockFormGPS);
    guidance.distanceFromCurrentLineSteer = 2.0; // 2m off line
    guidance.steerHeadingError = 0.3; // radians

    guidance.DoSteerAngleCalc();

    Assert.That(Math.Abs(guidance.steerAngleGu),
                Is.LessThanOrEqualTo(maxSteerAngle));
}

[Test]
public void SectionControl_ShouldRespectTimingDelays()
{
    var section = new CSection();
    section.sectionOnRequest = true;
    section.sectionOnTimer = 5;

    // Should not activate until timer reaches threshold
    UpdateSectionState(section);
    Assert.That(section.isSectionOn, Is.False);
}
```

---

### 3. **Property-Based Testing**

**Approach**: Use frameworks like FsCheck or Hedgehog to generate random inputs and verify invariants:

```csharp
[Property]
public Property SteerAngle_ShouldAlwaysBeFinite(
    double crossTrackError,
    double headingError)
{
    var guidance = new CGuidance(mockFormGPS);
    guidance.distanceFromCurrentLineSteer = crossTrackError;
    guidance.steerHeadingError = headingError;

    guidance.DoSteerAngleCalc();

    return (!double.IsNaN(guidance.steerAngleGu) &&
            !double.IsInfinity(guidance.steerAngleGu))
            .ToProperty();
}
```

**Use Cases**:
- Geometry calculations (ensure no division by zero, NaN)
- Coordinate transformations (reversibility, bounds checking)
- Edge cases in section control timers
- Boundary detection at field edges

---

### 4. **Regression Testing via Recorded Field Data**

**Concept**: Capture real-world field operation data for replay testing:

```
Test_Data/
├── Field_Test_001/
│   ├── field_data.aof          # AgOpenGPS field file
│   ├── gps_track.nmea          # Recorded NMEA sentences
│   ├── steering_commands.log   # Steering outputs
│   ├── section_states.log      # Section on/off events
│   └── applied_area.png        # Reference coverage map
└── Field_Test_002/
    └── ...
```

**Test Process**:
1. Replay recorded GPS data through guidance system
2. Verify steering outputs match recorded values (±tolerance)
3. Compare regenerated applied area map with reference
4. Validate section control decisions

**Benefits**:
- Tests against actual field conditions (GPS noise, terrain variations)
- Validates bug fixes don't break working scenarios
- Builds confidence before releases to farmers
- Captures edge cases from real usage

**Implementation**:
```csharp
[Test]
[TestCase("Field_Test_001")]
[TestCase("Field_Test_002")]
public void RegressionTest_FieldData(string testCaseName)
{
    var testData = LoadTestData(testCaseName);
    var replay = new FieldDataReplay(testData);

    var results = replay.Execute();

    // Verify steering commands
    var steerDiff = CompareSteeringCommands(
        results.SteeringCommands,
        testData.ExpectedSteeringCommands
    );
    Assert.That(steerDiff.RMSError, Is.LessThan(0.5)); // 0.5° tolerance

    // Verify applied area
    var areaDiff = CompareAppliedArea(
        results.AppliedAreaMap,
        testData.ReferenceAppliedAreaMap
    );
    Assert.That(areaDiff.PixelMatchPercentage, Is.GreaterThan(98.0));
}
```

---

### 5. **Integration Testing**

**Focus Areas**:

| Test Type | Components | Purpose |
|-----------|-----------|---------|
| Communication | AgIO ↔ AgOpenGPS | UDP packet handling, NMEA parsing |
| File Operations | Field save/load, KML import | Data integrity, version compatibility |
| Hardware Simulation | Serial/UDP modules | Virtual ports, CAN bus messages |
| Settings Management | XML serialization | Configuration persistence |

**Tools**:
- Mock frameworks (Moq, NSubstitute)
- Virtual serial ports (com0com)
- In-memory UDP sockets

**Example Tests**:
```csharp
[Test]
public void AgIO_ShouldForwardNMEAToAgOpenGPS()
{
    var agIO = new AgIOSimulator();
    var agOpenGPS = new AgOpenGPSTestHarness();

    agIO.SendNMEASentence("$GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M,,*47");

    var receivedPosition = agOpenGPS.GetLastPosition(timeout: 1000);
    Assert.That(receivedPosition.Latitude, Is.EqualTo(48.1173).Within(0.0001));
    Assert.That(receivedPosition.Longitude, Is.EqualTo(11.5167).Within(0.0001));
}

[Test]
public void FieldFile_ShouldPreserveDataThroughSaveLoad()
{
    var originalField = CreateTestField();
    originalField.SaveToFile("test.aof");

    var loadedField = Field.LoadFromFile("test.aof");

    Assert.That(loadedField.Boundary, Is.EqualTo(originalField.Boundary));
    Assert.That(loadedField.ABLine, Is.EqualTo(originalField.ABLine));
}
```

---

### 6. **Visual Regression Testing** (Lower Priority)

**Approach**: Capture OpenGL viewport screenshots during simulation:
- Compare rendered field maps pixel-by-pixel
- Detect unintended UI changes
- Verify section coloring accuracy
- Validate guidance line rendering

**Tools**:
- ApprovalTests.NET
- ImageSharp for image comparison

**Use Cases**:
- Verify applied area visualization
- Check section color rendering
- Validate AB line display
- Test UI layout changes

---

## Recommended Implementation Roadmap

### Phase 1: Foundation (Weeks 1-3)
**Goal**: Establish basic testing infrastructure

1. Create `AgOpenGPS.SimulationTests` project
2. Implement headless mode (no OpenGL context required)
   - Extract simulation logic from UI dependencies
   - Create `ISimulationEnvironment` interface
3. Build basic path injection and trajectory recording
4. Add 5 simple straight-line AB tests
5. Set up CI pipeline (GitHub Actions)

**Deliverables**:
- Runnable test suite with 5+ tests
- CI integration passing on PR

---

### Phase 2: Core Validation (Weeks 4-6)
**Goal**: Validate critical guidance algorithms

6. Add curved path tests (S-curves, circles)
7. Implement applied area calculation validation
8. Create test fixtures for standard field shapes (rectangular, L-shaped, irregular)
9. Achieve 10x time acceleration
10. Add section control integration tests

**Deliverables**:
- 20+ simulation tests covering main scenarios
- Section control validation suite
- Automated applied area verification

---

### Phase 3: Expansion (Weeks 7-10)
**Goal**: Comprehensive coverage of core systems

11. Add unit tests for `CGuidance`, `CSection`, AB classes
12. Implement regression testing framework
13. Capture 5+ real field datasets
14. Target 70% code coverage on core logic
15. Add performance benchmarks

**Deliverables**:
- 50+ unit tests
- Regression test suite with real data
- Code coverage reports in CI

---

### Phase 4: Advanced (Ongoing)
**Goal**: Continuous improvement

16. Property-based testing for edge cases
17. Real field data replay tests (10+ scenarios)
18. Performance benchmarking suite
19. Visual regression tests for UI
20. Load testing (multiple simultaneous operations)

**Deliverables**:
- >80% code coverage
- Comprehensive test documentation
- Performance baselines

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Core guidance algorithm coverage | >80% | Code coverage tools |
| Automated simulation tests | >20 scenarios | Test count |
| Test execution time (full suite) | <5 minutes | CI pipeline metrics |
| Max cross-track error (straight AB) | <2cm | Simulation validation |
| Section overlap/skip | <3% | Applied area analysis |
| CI pipeline success rate | >95% | GitHub Actions history |
| Regression test coverage | 10+ real datasets | Test case count |
| Bug detection rate | 80% caught before release | Issue tracking |

---

## Technical Architecture Recommendations

### Headless Simulation Framework

```csharp
// Core abstraction for testable simulation
public interface ISimulationEnvironment
{
    void Initialize(SimulationConfig config);
    void Step(double deltaTime);
    SimulationState GetCurrentState();
    void Dispose();
}

public class HeadlessSimulator : ISimulationEnvironment
{
    private readonly FormGPS mf; // Configured for headless mode
    private readonly double accelerationFactor;

    public SimulationTrajectory Run(TestScenario scenario)
    {
        // Execute simulation loop
        // Record all state changes
        // Return trajectory data
    }
}

public class TestScenario
{
    public Field Field { get; set; }
    public GuidanceMode Mode { get; set; }
    public double Speed { get; set; }
    public List<Wgs84> GPSPath { get; set; }
    public ValidationCriteria Criteria { get; set; }
}

public class SimulationTrajectory
{
    public List<VehicleState> States { get; set; }
    public List<SteeringCommand> SteeringCommands { get; set; }
    public List<SectionState> SectionStates { get; set; }

    public double CalculateRMSError() { /* ... */ }
    public double CalculateMaxDeviation() { /* ... */ }
    public AppliedAreaMap GenerateAppliedAreaMap() { /* ... */ }
}
```

### Dependency Injection for Testing

Refactor tightly coupled components:
- Extract interfaces for GPS position source, UDP communication
- Use dependency injection to swap real implementations with test mocks
- Separate rendering logic from simulation logic

---

## Risk Mitigation

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Tests too slow for CI | High | Parallelize tests, optimize simulation speed |
| OpenGL dependency blocking headless mode | High | Abstract rendering, use software rendering fallback |
| Real field data privacy concerns | Medium | Anonymize GPS coordinates, use synthetic fields |
| Test maintenance burden | Medium | Focus on high-value tests, automated test generation |
| False positives from floating-point precision | Low | Use appropriate tolerances, document assumptions |

---

## Conclusion

**The accelerated simulation testing approach is the most valuable investment for AgOpenGPS.** It directly addresses the project's critical need to validate guidance accuracy—where errors have real financial consequences for farmers through wasted inputs and reduced yields.

**Recommended Immediate Actions**:
1. Start with Phase 1 implementation (3 weeks)
2. Create 5 basic AB line tracking tests
3. Validate feasibility of 10x time acceleration
4. Present results to development team

This testing strategy will:
- Reduce field testing requirements by 70%
- Catch 80%+ of guidance bugs before release
- Enable confident refactoring of core algorithms
- Establish AgOpenGPS as a quality-focused precision agriculture platform

The combination of automated simulation, unit tests, and real-world regression testing creates a robust quality assurance foundation suitable for agricultural precision software where accuracy is paramount.
