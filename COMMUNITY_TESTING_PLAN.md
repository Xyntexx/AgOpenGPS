# Community-Driven Testing Plan - AgOpenGPS

## Adapted for Open Source Community Project

### Key Considerations for Community Projects

1. **Contributors have varying skill levels** - from beginners to experts
2. **Part-time/volunteer effort** - people work when they can
3. **Need to onboard new contributors** - clear documentation essential
4. **Multiple people working in parallel** - avoid stepping on toes
5. **Changes must be reviewable** - small, focused PRs
6. **Consensus needed** - community buy-in for architectural changes
7. **Backwards compatibility critical** - can't break existing users
8. **Limited review bandwidth** - maintainers are busy

---

## Revised Strategy: Incremental & Parallelizable

### Philosophy: Small PRs, Clear Value, Easy Review

Each phase should be:
- ✅ **Completable by one person in 1-2 weeks** (part-time)
- ✅ **Independent** - can be worked on in parallel
- ✅ **Reviewable** - small enough for maintainers to review quickly
- ✅ **Valuable standalone** - provides benefit even if later phases don't happen
- ✅ **Documented** - clear for next contributor to pick up

---

## Phase-Based Approach (Each = Separate PR)

### Phase 0: Foundation & Documentation (Anyone can do)

**Goal**: Set up project structure and document testing approach

**Effort**: 4-8 hours, beginner-friendly

**Files Changed**: None (just new files)

**Deliverable**: PR #1

```
New Files:
├── .github/workflows/test.yml (skeleton)
├── SourceCode/AgOpenGPS.IntegrationTests/
│   ├── AgOpenGPS.IntegrationTests.csproj
│   ├── README.md (how to add tests)
│   └── .gitignore
└── docs/
    ├── TESTING_GUIDE.md
    └── HOW_TO_WRITE_TESTS.md
```

**File**: `SourceCode/AgOpenGPS.IntegrationTests/README.md`

```markdown
# AgOpenGPS Integration Tests

## Purpose
Automated tests to validate guidance accuracy without field testing.

## Status
🚧 Under development - Community contributions welcome!

## How to Run Tests
```bash
cd SourceCode
dotnet test AgOpenGPS.IntegrationTests
```

## How to Add a Test
See docs/HOW_TO_WRITE_TESTS.md

## Current Coverage
- [ ] Straight AB line guidance
- [ ] Curved path guidance
- [ ] Section control
- [ ] Applied area calculation

## Contributing
1. Pick an unclaimed test scenario from issues labeled `test-needed`
2. Write test following the template
3. Submit PR with test only (no production code changes)
```

**Review Criteria**: Documentation clear? Project structure good?

**Community Benefit**: Clear roadmap, shows testing is a priority

---

### Phase 1A: Test Infrastructure (Intermediate, 1 week)

**Goal**: Create minimal test harness that works with current code

**Effort**: 1-2 weeks part-time for one person

**Files Changed**: 0 production files

**Deliverable**: PR #2

```
New Files:
├── AgOpenGPS.IntegrationTests/
│   ├── Infrastructure/
│   │   ├── TestableFormGPS.cs
│   │   ├── TrajectoryRecorder.cs
│   │   └── TestScenario.cs
│   └── BasicGuidanceTests.cs (1 test)
```

**Key Point**: No production code changes, so easy to review!

**File**: `TestableFormGPS.cs`

```csharp
/// <summary>
/// Wrapper around FormGPS for testing
/// DOES NOT MODIFY PRODUCTION CODE
/// </summary>
public class TestableFormGPS : IDisposable
{
    private FormGPS form;
    public TrajectoryRecorder Recorder { get; private set; }

    public TestableFormGPS()
    {
        // Create FormGPS without showing window
        form = new FormGPS();
        form.CreateControl();
        Recorder = new TrajectoryRecorder(form);
    }

    public void Configure(TestScenario scenario)
    {
        // Set up simulation from scenario
        form.sim.CurrentLatLon = scenario.StartPosition;
        form.vehicle.wheelbase = scenario.Vehicle.Wheelbase;
        // etc...
    }

    public SimulationResult RunSimulation(double durationSeconds)
    {
        Recorder.StartRecording();

        int ticks = (int)(durationSeconds / 0.1);
        for (int i = 0; i < ticks; i++)
        {
            double steerAngle = form.gyd.steerAngleGu;
            form.sim.DoSimTick(steerAngle);
            Recorder.RecordTick();
        }

        return Recorder.GetResults();
    }

    public void Dispose() => form?.Dispose();
}
```

**First Test**:

```csharp
[TestFixture]
public class BasicGuidanceTests
{
    [Test]
    public void StraightABLine_ShouldFollowWithReasonableAccuracy()
    {
        // Arrange
        var scenario = TestScenario.CreateStraightLine(500);
        using var testForm = new TestableFormGPS();
        testForm.Configure(scenario);

        // Act
        var result = testForm.RunSimulation(180);

        // Assert - be generous initially
        Assert.That(result.Statistics.RMSCrossTrackError,
                   Is.LessThan(0.10), // 10cm - will tighten later
                   $"RMS error was {result.Statistics.RMSCrossTrackError * 100:F2}cm");

        Console.WriteLine($"✓ RMS Error: {result.Statistics.RMSCrossTrackError * 100:F2}cm");
        Console.WriteLine($"✓ Max Error: {result.Statistics.MaxCrossTrackError * 100:F2}cm");
    }
}
```

**Review Criteria**:
- Does test run?
- Does it fail if guidance broken?
- Is code documented?

**Community Benefit**:
- First automated test!
- Template for others to follow
- Proves concept works

**Who Can Do This**: Intermediate C# developer familiar with NUnit

---

### Phase 1B: Add More Test Scenarios (Beginner-friendly, parallel)

**Goal**: Community members add test scenarios independently

**Effort**: 2-4 hours per test

**Files Changed**: 0 production files (just add tests)

**Deliverable**: Multiple small PRs (PR #3, #4, #5...)

**Template for Contributors**:

```csharp
[Test]
[Category("Community")] // Mark as community contribution
[Description("Tests 45-degree AB line guidance - contributed by @username")]
public void StraightABLine_45Degrees_ShouldFollowAccurately()
{
    // Arrange
    var scenario = TestScenario.CreateStraightLine(500);
    scenario.Heading = Math.PI / 4; // 45 degrees

    using var testForm = new TestableFormGPS();
    testForm.Configure(scenario);

    // Act
    var result = testForm.RunSimulation(180);

    // Assert
    Assert.That(result.Statistics.RMSCrossTrackError,
               Is.LessThan(0.10),
               $"45° line: RMS error was {result.Statistics.RMSCrossTrackError * 100:F2}cm");
}
```

**Good First Issues**:

Create GitHub issues like:

```
Title: Add test for 90-degree AB line
Labels: good-first-issue, test-needed, help-wanted

Description:
We need a test that validates guidance on a 90-degree (East) AB line.

Template: See BasicGuidanceTests.cs
Acceptance: Test passes with RMS error < 10cm
Estimated effort: 2 hours
```

**Test Scenarios to Add** (each = separate PR):
1. 90-degree AB line
2. 135-degree AB line
3. 180-degree AB line (South)
4. Very short line (100m)
5. Very long line (1000m)
6. Slow speed (5 km/h)
7. Fast speed (20 km/h)
8. Large offset start (2m off line)

**Review Criteria**:
- Does test follow template?
- Does it pass?
- Is it documented?

**Community Benefit**:
- Easy entry point for new contributors
- Builds test coverage organically
- Multiple people can contribute in parallel

**Who Can Do This**: Anyone who can write a basic C# test

---

### Phase 2: Extract Simulation State (Intermediate, 1 week)

**Goal**: Make test code cleaner without breaking production

**Effort**: 1-2 weeks part-time

**Files Changed**: 1 (CSim.cs) - small, safe change

**Deliverable**: PR #6

```
Modified:
└── SourceCode/GPS/Classes/CSim.cs (+50 lines)

New:
└── SourceCode/AgOpenGPS.Core/Simulation/SimulationState.cs
```

**Change to CSim.cs**:

```csharp
public class CSim
{
    private readonly FormGPS mf;

    // NEW: Expose state for testing
    public SimulationState State { get; private set; }

    public CSim(FormGPS _f)
    {
        mf = _f;
        State = new SimulationState(); // NEW
        // ... existing code unchanged
    }

    public void DoSimTick(double _st)
    {
        // ... all existing code exactly the same ...

        // NEW: Update state at end (non-breaking addition)
        State.CurrentLatLon = CurrentLatLon;
        State.Heading = headingTrue;
        State.Speed = mf.avgSpeed;
        State.SteerAngle = steerangleAve;
        State.CrossTrackError = mf.gyd.distanceFromCurrentLinePivot;
    }
}
```

**Review Criteria**:
- Does existing simulator still work?
- Are tests updated to use State?
- Is change minimal and clear?

**Testing Before Merge**:
```csharp
[Test]
public void SimulationState_IsPopulatedCorrectly()
{
    using var testForm = new TestableFormGPS();
    var scenario = TestScenario.CreateStraightLine(100);
    testForm.Configure(scenario);

    testForm.RunSimulation(10); // Short run

    // Verify state is populated
    var state = testForm.Form.sim.State;
    Assert.That(state.Speed, Is.GreaterThan(0));
    Assert.That(state.Heading, Is.InRange(0, Math.PI * 2));
}
```

**Community Benefit**:
- Tests become cleaner
- Establishes pattern for future changes
- Minimal risk (just adds property)

**Discussion Period**:
- Open PR as draft
- Discuss on forum/Discord
- Get feedback before marking ready

**Who Can Do This**: Experienced contributor who understands CSim.cs

---

### Phase 3A: Guidance Interface (Advanced, 2 weeks)

**Goal**: Decouple guidance from FormGPS

**Effort**: 2-3 weeks part-time

**Files Changed**: 1 (CGuidance.cs) - backward compatible

**Deliverable**: PR #7

⚠️ **Important**: This is a bigger change, needs community discussion first!

**Pre-PR Steps**:
1. Create discussion issue: "Proposal: Decouple CGuidance for testability"
2. Share design document showing interfaces
3. Get feedback and consensus
4. Only then start implementation

**File Structure**:
```
New:
├── AgOpenGPS.Core/Interfaces/
│   └── IGuidanceContext.cs
└── GPS/Classes/
    ├── FormGPSGuidanceAdapter.cs
    └── TestGuidanceContext.cs

Modified:
└── GPS/Classes/CGuidance.cs (~100 lines changed)
```

**Key: Backward Compatible**

```csharp
public class CGuidance
{
    private readonly IGuidanceContext context;

    // OLD constructor - STILL WORKS! No breaking change
    public CGuidance(FormGPS _f)
        : this(new FormGPSGuidanceAdapter(_f))
    {
    }

    // NEW constructor for testing
    public CGuidance(IGuidanceContext context)
    {
        this.context = context;
    }

    private void DoSteerAngleCalc()
    {
        // OLD: if (mf.isReverse)
        // NEW: if (context.IsReverse)
        if (context.IsReverse) steerHeadingError *= -1;
        // ... rest of changes similar
    }
}
```

**Review Process**:
1. Open as draft PR
2. Request reviews from 3+ maintainers
3. Address feedback
4. Extensive testing period (1 week)
5. Merge only after consensus

**Testing**:
- All existing tests still pass
- New tests verify guidance works independently
- Manual testing in UI confirms no regressions

**Community Benefit**:
- Big step toward testability
- Demonstrates careful refactoring approach
- Sets precedent for future changes

**Who Can Do This**:
- Core contributor
- Requires deep understanding of guidance system
- Requires community trust

---

### Phase 3B: Standalone Simulation Engine (Advanced, 2-3 weeks)

**Goal**: Fast simulation without FormGPS

**Effort**: 2-3 weeks part-time

**Files Changed**: 0 production files

**Deliverable**: PR #8

```
New:
├── AgOpenGPS.Core/Simulation/
│   ├── SimulationEngine.cs
│   └── VehiclePhysics.cs
└── AgOpenGPS.IntegrationTests/
    ├── FastSimulationRunner.cs
    └── FastGuidanceTests.cs
```

**Key Point**: This is OPTIONAL optimization, doesn't change production code!

**Community Benefit**:
- Tests run 10-100x faster
- But can skip this if Phase 1 tests are good enough
- Community can decide priority

**Who Can Do This**:
- Advanced contributor
- Good understanding of simulation and guidance

---

### Phase 4: Validation & Tooling (Anyone, parallel)

**Goal**: Make tests more useful for contributors

**Effort**: Various small tasks

**Deliverable**: Multiple PRs

Tasks (each = separate PR):

1. **Test result visualization** (Beginner)
   - Export trajectory to CSV
   - Generate plots with Python/gnuplot
   - File: `scripts/plot_trajectory.py`

2. **Baseline comparison** (Intermediate)
   - Save baseline results
   - Compare runs to detect regressions
   - File: `Tests/BaselineManager.cs`

3. **Performance benchmarks** (Intermediate)
   - Track test execution time
   - Alert if tests slow down
   - File: `Tests/PerformanceBenchmarks.cs`

4. **Documentation** (Anyone)
   - Add more examples
   - Create troubleshooting guide
   - Record video tutorials

5. **CI improvements** (DevOps experience)
   - Run tests on PR
   - Post results as comment
   - Block merge if tests fail

**Community Benefit**:
- Incremental improvements
- Multiple people can contribute
- Each adds standalone value

---

## Community Workflow

### For New Contributors

1. **Find an issue** labeled `good-first-issue` or `test-needed`
2. **Comment** "I'd like to work on this"
3. **Fork** the repository
4. **Write test** following the template
5. **Submit PR** with just the test (no production changes)
6. **Respond** to review feedback
7. **Celebrate** when merged! 🎉

### For Experienced Contributors

1. **Discuss** major changes first (open issue/forum post)
2. **Get consensus** before starting work
3. **Work in draft PR** for transparency
4. **Request reviews** from multiple people
5. **Be patient** - review bandwidth is limited

### For Maintainers

1. **Triage** test-related issues, label appropriately
2. **Review** small test PRs quickly (encourage contributors!)
3. **Discuss** larger changes thoroughly before approval
4. **Merge** confidently knowing tests protect against regressions

---

## Timeline (Community Pace)

### Month 1: Foundation
- Week 1-2: Phase 0 (Documentation & structure)
- Week 3-4: Phase 1A (Test infrastructure)
- **Milestone**: First test running

### Month 2-3: Community Growth
- Phase 1B: Community adds 10+ test scenarios (parallel)
- **Milestone**: Decent test coverage

### Month 4-5: Strategic Refactoring (Optional)
- Phase 2: Extract simulation state (if community wants it)
- Discussion and consensus on Phase 3
- **Milestone**: Cleaner architecture

### Month 6+: Ongoing
- Phase 3: Advanced refactoring (if valuable)
- Phase 4: Tooling improvements (continuous)
- **Milestone**: Mature test infrastructure

---

## Communication & Coordination

### Discord/Forum Channels

Create dedicated channels:
- `#testing-general` - Discussion
- `#testing-help` - Support for contributors
- `#testing-pr-reviews` - Coordinate reviews

### Regular Sync (Optional)

- Monthly "Testing Office Hours" video call
- Anyone can join, ask questions, demo work
- Record and post for those who can't attend

### Documentation

Keep living documents:
- `TEST_STATUS.md` - Current state, what's needed
- `TEST_ROADMAP.md` - Future plans
- `TEST_FAQ.md` - Common questions

---

## Risk Management for Community Projects

### Risk: Contributor Abandons Work

**Mitigation**:
- Keep PRs small (1-2 weeks max)
- Document work clearly
- Another contributor can pick up

### Risk: Conflicts Between Contributors

**Mitigation**:
- Claim issues before starting (comment on issue)
- Communicate in Discord/forum
- Merge quickly to avoid conflicts

### Risk: Breaking Changes

**Mitigation**:
- No breaking changes without community consensus
- Long discussion period for big changes
- Feature flags to enable/disable new code

### Risk: Review Bottleneck

**Mitigation**:
- Empower trusted contributors to approve test PRs
- Keep production changes separate from test additions
- Auto-merge passing test additions (controversial, discuss first)

---

## Success Metrics (Community-Focused)

### Short Term (3 months)
- [ ] 10+ contributors added tests
- [ ] 20+ test scenarios covered
- [ ] Tests run in CI on every PR
- [ ] 5+ community members understand testing system

### Long Term (6-12 months)
- [ ] Tests catch real bugs before field testing
- [ ] Contributors feel confident making changes
- [ ] New contributors can add tests easily
- [ ] Testing seen as community asset, not burden

---

## Principles for Community Testing

### 1. **Value Incremental Progress**
Every small test added is valuable, even if "perfect" architecture takes time.

### 2. **Lower Barriers to Entry**
Make it easy for anyone to contribute a test. Template code, good docs, helpful reviews.

### 3. **Celebrate Contributors**
Acknowledge every PR, thank contributors, showcase their work.

### 4. **Respect Maintainer Time**
Keep PRs small and focused. Don't require deep review for test additions.

### 5. **Be Patient**
Open source moves at community pace. That's okay!

### 6. **Communicate Openly**
Discuss plans publicly. Build consensus before big changes.

### 7. **Document Everything**
Next contributor shouldn't have to reverse-engineer your work.

---

## Call to Action

### For Project Leads

- [ ] Create `test-needed` label on GitHub
- [ ] Pin testing roadmap issue
- [ ] Announce testing initiative in forum/Discord
- [ ] Identify 3-5 initial "good first issue" tests

### For Contributors

- [ ] Read this plan
- [ ] Pick a test scenario
- [ ] Join Discord `#testing` channel
- [ ] Submit your first test PR!

### For Community

- [ ] Discuss and refine this plan
- [ ] Decide priorities (some phases optional)
- [ ] Build consensus on approach
- [ ] Start contributing!

---

## Questions for Community Discussion

Before starting, discuss:

1. **Priority**: Is automated testing a priority right now?
2. **Scope**: Start with minimal (Phase 1) or aim for full refactor (Phase 3)?
3. **Pace**: Push hard for 3 months, or slow-and-steady over 12 months?
4. **Ownership**: Who coordinates testing efforts?
5. **Standards**: What's the review process for test PRs vs. production changes?

**Recommendation**: Start small (Phase 0-1), grow organically based on community interest.

---

## Example: Good First Issue Template

```markdown
Title: Add test for South-facing (180°) AB line

Labels: good-first-issue, test-needed, help-wanted

**Description**
We need a test that validates straight-line guidance when heading South (180 degrees).

**Acceptance Criteria**
- Test follows template in BasicGuidanceTests.cs
- Uses heading of 180 degrees (π radians)
- Asserts RMS error < 10cm
- Includes descriptive console output

**Template Code**
```csharp
[Test]
public void StraightABLine_180Degrees_ShouldFollowAccurately()
{
    // TODO: Implement following template in BasicGuidanceTests.cs
}
```

**Resources**
- Testing guide: docs/HOW_TO_WRITE_TESTS.md
- Example test: BasicGuidanceTests.cs line 15
- Ask questions in: Discord #testing-help

**Estimated Effort**: 2 hours

**Claim This Issue**
Comment "I'll take this!" to claim.
```

---

## Conclusion

This community-adapted plan emphasizes:

✅ **Small, reviewable changes**
✅ **Parallel work by multiple contributors**
✅ **Low barrier to entry**
✅ **Incremental value delivery**
✅ **Respect for volunteer time**
✅ **Community consensus on big changes**
✅ **Documentation & communication**

The plan is flexible - community decides pace and scope. Even implementing just Phase 0-1B would be a huge win!
