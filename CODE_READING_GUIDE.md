# AgOpenGPS Code Reading Guide

## A Structured Path to Understanding the Codebase

This guide provides a learning path through the AgOpenGPS codebase, from high-level architecture down to implementation details.

---

## Overview: What You're Looking At

AgOpenGPS is precision agriculture software with ~100k+ lines of C# code. Key capabilities:
- **GPS-guided steering** (Stanley controller, Pure Pursuit)
- **Section control** (16-64 sections for implements)
- **Field mapping** (applied area tracking)
- **Auto headland turns** (U-turns at field edges)
- **AB line guidance** (straight lines, curves, contours)

**Primary Platform**: Windows Forms with OpenGL rendering

---

## Reading Path by Experience Level

### Path A: Beginner (New to Codebase)
**Goal**: Understand what the software does and basic structure

**Time**: 4-6 hours

1. Start with documentation and high-level overview
2. Explore data models and core concepts
3. Trace one simple feature end-to-end
4. Understand the main update loop

### Path B: Intermediate (Want to Contribute)
**Goal**: Understand specific subsystems to make changes

**Time**: 8-12 hours

1. Follow Beginner path first
2. Deep dive into guidance algorithms
3. Understand section control logic
4. Learn field file format and persistence

### Path C: Advanced (Major Refactoring/Architecture)
**Goal**: Understand entire system architecture

**Time**: 20-30 hours

1. Follow Intermediate path first
2. Study all major subsystems
3. Understand communication protocols
4. Map dependencies and coupling

---

## Phase 1: High-Level Orientation (1-2 hours)

### Step 1.1: Read Documentation First

**Start here** (in order):

```
📄 Read These Files:
1. [README.md](README.md)                           (10 min)
   └─> What is AgOpenGPS, how to build it

2. [TESTING_STRATEGY.md](TESTING_STRATEGY.md)       (15 min)
   └─> Understand testing approach

3. [CONTRIBUTING.md](CONTRIBUTING.md)               (10 min)
   └─> Development workflow

4. [AUTOMATED_TESTING_REFACTORING_PLAN.md](AUTOMATED_TESTING_REFACTORING_PLAN.md)  (30 min)
   └─> Current architecture analysis
   └─> Data flow diagrams
   └─> Component dependencies
```

**Key Takeaway**: You should now understand:
- ✓ What AgOpenGPS does (precision agriculture)
- ✓ Two main programs: AgOpenGPS + AgIO
- ✓ Main components: Guidance, Sections, Boundaries
- ✓ Current architecture challenges (tight coupling)

### Step 1.2: Explore Project Structure

**File**: [`SourceCode/AgOpenGPS.sln`](SourceCode/AgOpenGPS.sln)

Open in Visual Studio and examine Solution Explorer:

```
SourceCode/
├── 📁 GPS/                     ⭐ Main application (start here)
│   ├── Classes/               → Core business logic
│   ├── Forms/                 → UI and windows
│   └── Protocols/             → Communication
│
├── 📁 AgIO/                    ⭐ Communication hub
│   └── Source/                → Network I/O, serial ports
│
├── 📁 AgOpenGPS.Core/          ⭐ Shared library
│   ├── Models/                → Data structures
│   └── Translations/          → Localization
│
├── 📁 AgOpenGPS.WpfApp/        → WPF version (newer UI)
├── 📁 AgLibrary/               → Utility functions
├── 📁 ModSim/                  → GPS Simulator
└── 📁 *.Tests/                 → Unit tests
```

**Exercise**: Count how many projects are in the solution. Note which ones are tests vs. production code.

### Step 1.3: Find Key Entry Points

**Main Entry Points**:

```csharp
// Application entry point
📄 SourceCode/GPS/Program.cs
   └─> Application.Run(new FormGPS());

// Main form initialization
📄 SourceCode/GPS/Forms/FormGPS.cs:301
   └─> FormGPS() constructor
   └─> Initializes all subsystems
```

**Exercise**: Open [`SourceCode/GPS/Program.cs`](SourceCode/GPS/Program.cs). Read the `Main()` method. This is where the application starts.

---

## Phase 2: Core Concepts & Data Models (2-3 hours)

### Step 2.1: Understand Coordinate Systems

**Critical for understanding everything else!**

```csharp
📄 SourceCode/AgOpenGPS.Core/Models/Wgs84.cs
   └─> GPS coordinates (latitude, longitude)
   └─> Read the class definition

📄 SourceCode/AgOpenGPS.Core/Models/GeoCoord.cs
   └─> Local coordinates (easting, northing in meters)
   └─> Used for all calculations

📄 SourceCode/AgOpenGPS.Core/Models/LocalPlane.cs
   └─> Converts WGS84 ↔ Local flat plane
   └─> Read ConvertWgs84ToGeoCoord()
```

**Direct Links**:
- [Wgs84.cs](SourceCode/AgOpenGPS.Core/Models/Wgs84.cs)
- [GeoCoord.cs](SourceCode/AgOpenGPS.Core/Models/GeoCoord.cs)
- [LocalPlane.cs](SourceCode/AgOpenGPS.Core/Models/LocalPlane.cs)

**Why This Matters**: GPS gives lat/lon (WGS84), but all guidance calculations use local Cartesian coordinates (easting/northing).

**Exercise**:
1. Read the `Wgs84` struct - what fields does it have?
2. Find the method that calculates distance between two WGS84 points
3. Trace how GPS position becomes local coordinates

### Step 2.2: Core Data Structures

Read these classes to understand the domain:

```csharp
📄 SourceCode/GPS/Classes/CVehicle.cs (150 lines)
   └─> Vehicle configuration
   └─> Wheelbase, antenna position, steering limits
   └─> Stanley controller gains

📄 SourceCode/GPS/Classes/CSection.cs (76 lines)
   └─> Section state (on/off)
   └─> Position (left/right), width
   └─> Boundary detection

📄 SourceCode/GPS/Classes/CABLine.cs (~300 lines)
   └─> AB line definition
   └─> Origin point + heading
   └─> Parallel line calculation
```

**Direct Links**:
- [CVehicle.cs](SourceCode/GPS/Classes/CVehicle.cs)
- [CSection.cs](SourceCode/GPS/Classes/CSection.cs)
- [CABLine.cs](SourceCode/GPS/Classes/CABLine.cs)

**Reading Strategy**:
- Start with properties/fields (top of class)
- Skim method signatures (understand what it does)
- Read constructor to see initialization

**Exercise**: Draw a simple diagram showing:
- Vehicle with wheelbase, antenna, steering axle
- Section layout (left to right positions)
- AB line with origin and parallel lines

### Step 2.3: Main Form Structure

The god object - everything connects through here:

```csharp
📄 SourceCode/GPS/Forms/FormGPS.cs (lines 1-350)
   └─> FormGPS constructor (line 301)
   └─> Creates all subsystem instances

Key instances created:
   └─> camera = new Camera()          # OpenGL camera
   └─> vehicle = new CVehicle(this)   # Vehicle config
   └─> pn = new CNMEA(this)            # GPS data parser
   └─> gyd = new CGuidance(this)       # Guidance calculations
   └─> sim = new CSim(this)            # Simulator
   └─> section[] = new CSection[64]    # Section array
   └─> ABLine = new CABLine(this)      # AB line guidance
   └─> curve = new CABCurve(this)      # Curve guidance
   └─> ct = new CContour(this)         # Contour guidance
   └─> yt = new CYouTurn(this)         # Auto turn system
   └─> bnd = new CBoundary(this)       # Field boundaries
```

**Key Insight**: FormGPS is passed as `this` to every subsystem. This is why everything is tightly coupled!

**Exercise**: Open [FormGPS.cs](SourceCode/GPS/Forms/FormGPS.cs) and scroll through the constructor. Count how many subsystems are created. Notice how they all receive `this`.

---

## Phase 3: The Main Loop (2-3 hours)

### Step 3.1: Understand the Update Cycle

**This is the heart of AgOpenGPS!**

**Read**: [`SourceCode/GPS/Forms/Position.designer.cs:128`](SourceCode/GPS/Forms/Position.designer.cs#L128)

```csharp
   public void UpdateFixPosition()
   {
       // Called every time GPS data arrives (~10 Hz)

       1. Calculate heading from GPS fixes
       2. Apply offsets (antenna, roll correction)
       3. Update vehicle position
       4. Calculate guidance (steer angle)
       5. Update section states
       6. Record applied area
       7. Trigger rendering
   }
```

**Reading Path**:

```
Start: SourceCode/GPS/Forms/Position.designer.cs:128

1. Read UpdateFixPosition() structure (lines 128-800+)
   └─> Don't try to understand everything!
   └─> Just identify the major sections:

   Lines 147-151: Initialize first GPS positions
   Lines 190-550: Calculate heading from GPS
   Lines 552-750: Calculate guidance
   Lines 750-800: Update sections
```

**Visual Flow**:

```
GPS Data Arrives (NMEA sentence)
        ↓
CNMEA.ParseSentence()
        ↓
pn.fix (position) updated
        ↓
UpdateFixPosition() ← YOU ARE HERE
        ↓
    ┌───────────────────┐
    │ Calculate Heading │
    └─────────┬─────────┘
              ↓
    ┌─────────────────┐
    │ Guidance System │
    │ (steer angle)   │
    └─────────┬───────┘
              ↓
    ┌──────────────────┐
    │ Section Control  │
    └─────────┬────────┘
              ↓
    ┌─────────────────┐
    │ OpenGL Render   │
    └─────────────────┘
```

**Exercise**:
1. Put a bookmark at line 128 in Position.designer.cs
2. Identify the 5 major sections in the method
3. Find where guidance is calculated (hint: search for "gyd")

### Step 3.2: Follow GPS Data Flow

Trace data from NMEA string to vehicle position:

```csharp
📄 SourceCode/GPS/Forms/UDPComm.Designer.cs
   └─> ReceiveFromUDP()
   └─> Receives NMEA strings over network

   ↓

📄 SourceCode/GPS/Classes/CNMEA.cs
   └─> ParseNMEA() methods
   └─> Decodes "$GPGGA", "$GPVTG", etc.
   └─> Populates pn.fix, pn.speed, etc.

   ↓

📄 SourceCode/GPS/Forms/Position.designer.cs:128
   └─> UpdateFixPosition()
   └─> Uses pn.fix for guidance
```

**Read These Files** (in order):

1. **[CNMEA.cs](SourceCode/GPS/Classes/CNMEA.cs)** (lines 1-56)
   - Just the class definition and fields
   - See how GPS data is stored

2. **[UDPComm.Designer.cs](SourceCode/GPS/Forms/UDPComm.Designer.cs)** (~line 50)
   - Find `ReceiveFromUDP()`
   - See how NMEA strings arrive

3. **[Position.designer.cs](SourceCode/GPS/Forms/Position.designer.cs)** (lines 147-175)
   - See how GPS position is initialized

**Exercise**: Find where the GPS latitude/longitude is converted to local easting/northing coordinates. (Hint: Look for `ConvertWgs84ToGeoCoord`)

---

## Phase 4: Guidance System Deep Dive (3-4 hours)

This is where the "magic" happens - keeping the vehicle on the line!

### Step 4.1: Stanley Controller Fundamentals

**Start with the algorithm**:

**File**: [`SourceCode/GPS/Classes/CGuidance.cs`](SourceCode/GPS/Classes/CGuidance.cs)

```csharp
   Key Methods:
   1. StanleyGuidanceABLine()      (line 120)
      └─> Guidance for straight lines

   2. StanleyGuidanceCurve()       (line 202)
      └─> Guidance for curved paths

   3. DoSteerAngleCalc()           (line 42)
      └─> Stanley controller implementation
      └─> THIS IS THE CORE ALGORITHM! ⭐
```

**Reading Path**:

```
📄 SourceCode/GPS/Classes/CGuidance.cs:42 - DoSteerAngleCalc()

Read in this order:
1. Lines 42-109: Stanley controller math
   - Cross-track error correction
   - Heading error correction
   - Integral term (for steady-state error)

Key Variables:
- distanceFromCurrentLinePivot  → How far off the line
- steerHeadingError             → Heading difference from line
- inty                          → Integral accumulator
- steerAngleGu                  → OUTPUT: steering angle
```

**The Stanley Formula** (simplified):

```
steer_angle = atan(cross_track_error × gain / speed) + heading_error

Where:
- cross_track_error = perpendicular distance from line
- heading_error = difference between vehicle heading and line heading
- gain = tuning parameter (stanleyDistanceErrorGain)
```

**Exercise**:
1. Find the line where `steerAngleGu` is calculated (line 65)
2. Identify the cross-track correction term
3. Identify the heading error term
4. Find where the integral term is calculated

### Step 4.2: AB Line Guidance

How the vehicle follows a straight line:

**Read**: [`CGuidance.cs:120 - StanleyGuidanceABLine()`](SourceCode/GPS/Classes/CGuidance.cs#L120)

```csharp

Algorithm:
1. Calculate perpendicular distance from vehicle to line
   └─> distanceFromCurrentLinePivot

2. Find closest point on line
   └─> rEastPivot, rNorthPivot

3. Calculate heading error
   └─> steerHeadingError

4. Call DoSteerAngleCalc() to get steering angle
```

**Reading Strategy**:

Lines 120-145: Focus on the math
- Line 128: Distance calculation (perpendicular distance formula)
- Line 136-141: Closest point on line
- Line 178: Heading error calculation

**Exercise**:
1. On paper, draw:
   - An AB line (straight line)
   - Vehicle position off the line
   - Perpendicular distance
   - Closest point on line
2. Match your drawing to the math on line 128

### Step 4.3: Curve Guidance

More complex - follows a curved path:

**Read**: [`CGuidance.cs:202 - StanleyGuidanceCurve()`](SourceCode/GPS/Classes/CGuidance.cs#L202)

```csharp

Key Differences from AB Line:
1. Line is a list of points (curList)
2. Must find closest TWO points
3. Interpolate between them
4. Follow tangent to curve

Algorithm:
1. Find closest point in list (lines 211-243)
2. Find second closest (lines 263-291)
3. Calculate guidance between those points
4. Apply Stanley controller
```

**Reading Strategy**: Skim first, focus on structure:
- Lines 204-220: Find rough closest point (every 10th point)
- Lines 227-243: Refine to exact closest
- Lines 302-336: Calculate guidance

**Exercise**: Compare `StanleyGuidanceABLine` vs `StanleyGuidanceCurve`. What's similar? What's different?

---

## Phase 5: Section Control (2-3 hours)

### Step 5.1: Section Basics

**Read**: [`SourceCode/GPS/Classes/CSection.cs`](SourceCode/GPS/Classes/CSection.cs) (76 lines - entire file)

```csharp
   Read entire file - it's short!

   Key Fields:
   - isSectionOn           → Current state
   - positionLeft/Right    → Section boundaries (meters)
   - sectionWidth          → Width in meters
   - isInBoundary          → Inside field boundary?
   - sectionBtnState       → Off/Auto/On
```

**Exercise**: Draw the section layout. If `positionLeft = -4` and `positionRight = -2`, where is this section? (Hint: negative = left side)

### Step 5.2: Section Control Logic

**Read**: [`SourceCode/GPS/Forms/Sections.Designer.cs`](SourceCode/GPS/Forms/Sections.Designer.cs)

```csharp
Key Methods:
1. AllSectionsAndButtonsToState()     (line 144)
   └─> Turn all sections on/off/auto

2. btnSectionMasterAuto_Click()       (line 73)
   └─> Auto mode (sections controlled by boundaries)

3. btnSectionMasterManual_Click()     (line 40)
   └─> Manual mode (operator controls)
```

**States**:
```csharp
enum btnStates { Off, Auto, On }

Off  → Section always off
Auto → Software decides based on boundary
On   → Section always on (manual override)
```

**Exercise**: Find where section state is checked during the main loop. (Hint: search for "isSectionOn" in Position.designer.cs)

### Step 5.3: Applied Area Tracking

Where sections mark what's been covered:

**Read**: [`Position.designer.cs`](SourceCode/GPS/Forms/Position.designer.cs) (search for "triStrip")

```csharp

Key Concept: Sections paint triangles on the map

When section is ON:
1. Record corner positions
2. Create triangles
3. Add to triStrip list
4. Render as green patches
```

**Exercise**: Search for "triStrip" in Position.designer.cs. How many times is it referenced?

---

## Phase 6: Simulation System (1-2 hours)

Great for understanding the system without GPS hardware!

### Step 6.1: Simulator Core

**Read**: [`SourceCode/GPS/Classes/CSim.cs`](SourceCode/GPS/Classes/CSim.cs) (125 lines - entire file)

```csharp
   Read entire file!

   DoSimTick(double steerAngle)
   {
       1. Smooth steering angle (simulate hydraulics)
       2. Calculate heading change from steer angle
       3. Update position using heading and speed
       4. Convert to local coordinates
       5. Call UpdateFixPosition()
   }
```

**Key Physics**:

```csharp
Line 66: Heading change calculation
   headingChange = distance × tan(steerAngle) / wheelbase

Line 75: Position update
   newPosition = currentPosition.CalculateNewPositionFromBearingDistance()
```

**Exercise**:
1. Find where steer angle is smoothed (lines 35-62)
2. Why is smoothing needed? (Hint: real hydraulics have lag)
3. Find where heading is updated (line 67)

### Step 6.2: Simulator Loop

**Read**: [`OpenGL.Designer.cs`](SourceCode/GPS/Forms/OpenGL.Designer.cs) (search for "timerSim")

```csharp

   private void timerSim_Tick()
   {
       // Called every 100ms when simulator active

       1. Get desired steer angle from guidance
       2. Pass to sim.DoSimTick()
       3. Simulator updates position
       4. UpdateFixPosition() recalculates guidance
       5. Repeat
   }
```

**Closed Loop**:
```
Guidance calculates steer angle
         ↓
Simulator applies it
         ↓
Position changes
         ↓
Guidance sees new position
         ↓
Calculates new steer angle
         ↓
(loop continues)
```

**Exercise**: Put breakpoints in CSim.DoSimTick() and run the simulator. Watch the values change!

---

## Phase 7: Communication & I/O (2-3 hours)

### Step 7.1: Module Communication

**Read**: [`SourceCode/GPS/Classes/CModuleComm.cs`](SourceCode/GPS/Classes/CModuleComm.cs)

```csharp
   Handles communication with:
   - AutoSteer modules (steering control)
   - Section control modules
   - IMU/AHRS (heading, roll)
   - Rate controllers
```

**Key Concept**: AgOpenGPS sends UDP packets to external hardware modules.

**Exercise**: Search for "PGN" (Parameter Group Number) - the message IDs used.

### Step 7.2: NMEA Parsing

**Read**: [`CNMEA.cs`](SourceCode/GPS/Classes/CNMEA.cs) (search for "ParseNMEA")

```csharp

   Handles GPS sentences:
   - $GPGGA → Position, altitude, fix quality
   - $GPVTG → Speed and heading
   - $GPRMC → Position and time
   - $PAOGI → AgOpenGPS custom format
```

**Exercise**: Find the ParseGGA() method. What information does it extract?

---

## Phase 8: Field Management (2-3 hours)

### Step 8.1: Field Files

**Read**: [`SourceCode/GPS/Forms/SaveOpen.Designer.cs`](SourceCode/GPS/Forms/SaveOpen.Designer.cs)

```csharp
   Methods:
   - FileSaveEverything()      → Save current field
   - FileOpenField()           → Load existing field

   What's Saved:
   - Boundaries
   - AB lines
   - Applied area (triangles)
   - Headland
   - Flags
```

**Exercise**: Find where your field files are stored. Look at Properties.Settings.Default for the base directory.

### Step 8.2: Boundaries

**Read**: [`SourceCode/GPS/Classes/CBoundary.cs`](SourceCode/GPS/Classes/CBoundary.cs)

```csharp
   Boundary functions:
   - Draw boundary while driving
   - Import from KML/Shapefile
   - Check if point is inside
   - Calculate area
```

**Exercise**: Search for "IsPointInPolygon" - the algorithm that checks if a section is inside the boundary.

---

## Phase 9: OpenGL Rendering (Optional, 2-3 hours)

If you want to understand the 3D view:

**Read**: [`SourceCode/GPS/Forms/OpenGL.Designer.cs`](SourceCode/GPS/Forms/OpenGL.Designer.cs)

```csharp
   Key Methods:
   - oglMain_Paint()         → Main render loop
   - DrawFieldSurface()      → Draw ground plane
   - DrawTriangleStrips()    → Draw applied area
   - DrawVehicle()           → Draw tractor
```

**Note**: OpenGL code is complex. Skip unless working on rendering.

---

## Phase 10: Advanced Topics (For Deep Understanding)

### AutoSteer System
- [FormSteer.cs](SourceCode/GPS/Forms/Settings/FormSteer.cs) → Tuning UI
- [CAHRS.cs](SourceCode/GPS/Classes/CAHRS.cs) → IMU/heading management

### U-Turn System
- [CYouTurn.cs](SourceCode/GPS/Classes/CYouTurn.cs) → Auto headland turns
- [CHead.cs](SourceCode/GPS/Classes/CHead.cs) → Headland detection

### Contour Mode
- [CContour.cs](SourceCode/GPS/Classes/CContour.cs) → Follow terrain contours

### Recorded Paths
- [CRecordedPath.cs](SourceCode/GPS/Classes/CRecordedPath.cs) → Record and replay paths

---

## Reading Strategies & Tips

### Strategy 1: Top-Down
1. Start with high-level docs
2. Understand main classes
3. Dive into specific methods

### Strategy 2: Feature-Based
Pick a feature and trace it end-to-end:
- Example: "How does AB line guidance work?"
- Find UI button → Handler → Core logic → Rendering

### Strategy 3: Data-Flow
Follow data through the system:
- GPS sentence → Parse → Update → Guidance → Display

### Strategy 4: Debugging
- Set breakpoints in key methods
- Run the simulator
- Watch values change
- Understand execution flow

### Tools to Use

**Visual Studio Features**:
- **Go To Definition** (F12) - Jump to method/class
- **Find All References** (Shift+F12) - See where used
- **Call Hierarchy** (Ctrl+K, Ctrl+T) - See call graph
- **Code Map** (Architecture menu) - Visualize dependencies

**Recommended Extensions**:
- ReSharper - Code navigation
- CodeMaid - Code cleanup
- Productivity Power Tools

---

## Practice Exercises

### Exercise 1: Add Logging
Add `Console.WriteLine()` in key places:
- UpdateFixPosition() entry
- DoSteerAngleCalc() - log steer angle
- DoSimTick() - log position

Run simulator and watch console output.

### Exercise 2: Modify Simulator Speed
Find where sim speed is set. Change it. Observe effect.

### Exercise 3: Trace a Button Click
Pick any button in the UI. Find its Click handler. Trace what it does.

### Exercise 4: Read a Test
Open [`AgOpenGPS.Core.Tests/Models/Base/GeoCoordTests.cs`](SourceCode/AgOpenGPS.Core.Tests/Models/Base/GeoCoordTests.cs)
Understand how tests work. Write a similar test.

### Exercise 5: Change Stanley Gains
Find where `stanleyHeadingErrorGain` is used. Change the value. How does guidance change?

---

## Common Pitfalls

### Pitfall 1: Getting Lost in FormGPS
FormGPS is huge (1200+ lines). Don't try to read it all at once. Focus on specific methods.

### Pitfall 2: Ignoring Coordinate Systems
Everything breaks if you confuse WGS84 vs. local coordinates. Always check what coordinate system a method expects!

### Pitfall 3: OpenGL Confusion
Rendering code is complex. If not working on graphics, skip it.

### Pitfall 4: Following Too Deep
When reading, know when to stop. If a method calls 10 other methods, don't follow them all. Understand the high-level flow first.

---

## Checkpoints: Test Your Understanding

After each phase, ask yourself:

### After Phase 1-2:
- [ ] Can you explain what AgOpenGPS does to a farmer?
- [ ] Can you name the 5 major subsystems?
- [ ] Do you understand WGS84 vs. local coordinates?

### After Phase 3-4:
- [ ] Can you trace GPS data from NMEA to guidance?
- [ ] Can you explain the Stanley controller in simple terms?
- [ ] Do you understand the difference between AB line and curve guidance?

### After Phase 5-6:
- [ ] Can you explain how section control works?
- [ ] Can you describe what the simulator does?
- [ ] Do you understand the main update loop?

### After Phase 7-8:
- [ ] Can you name the types of NMEA sentences parsed?
- [ ] Do you know what's stored in a field file?
- [ ] Can you explain how boundaries work?

---

## Next Steps After Reading

### To Contribute Code:
1. Pick a `good-first-issue` from GitHub
2. Read the relevant code sections
3. Make your changes
4. Write a test
5. Submit PR

### To Add Tests:
1. Read COMMUNITY_TESTING_PLAN.md
2. Pick a test scenario
3. Follow the template
4. Submit PR with test only

### To Understand More:
1. Run the simulator with debugger
2. Modify values and observe changes
3. Ask questions in Discord/Forum
4. Review PRs to see how others work

---

## Resources

### While Reading Code:
- [C# Language Reference](https://docs.microsoft.com/en-us/dotnet/csharp/)
- [OpenTK Documentation](https://opentk.net/learn/index.html) (OpenGL wrapper)
- [AgOpenGPS Forum](https://discourse.agopengps.com/)

### Agricultural Context:
- [AgOpenGPS YouTube](https://www.youtube.com/c/agopengps) - See it in action!
- Forum posts from farmers - Real-world usage

### Related Projects:
- [Boards Repository](https://github.com/agopengps-official/Boards) - Hardware
- [Rate Control](https://github.com/agopengps-official/Rate_Control) - Variable rate

---

## Summary: Suggested Reading Order

### Day 1 (4-6 hours): Orientation
- ✅ Read all documentation files
- ✅ Explore project structure
- ✅ Read Phase 1-2 (coordinates, data models)
- ✅ Understand FormGPS constructor

### Day 2 (4-6 hours): Main Loop & Guidance
- ✅ Read Phase 3 (main update loop)
- ✅ Read Phase 4 (Stanley controller)
- ✅ Run simulator with debugger

### Day 3 (3-4 hours): Sections & Simulation
- ✅ Read Phase 5 (section control)
- ✅ Read Phase 6 (simulator)
- ✅ Modify and experiment

### Day 4+ (Optional): Advanced Topics
- ✅ Communication protocols (Phase 7)
- ✅ Field management (Phase 8)
- ✅ Your area of interest (Phase 9-10)

**Total Time Commitment**:
- **Basic Understanding**: 12-16 hours
- **Contributor Ready**: 20-30 hours
- **Architecture Expert**: 40-60 hours

---

## Final Advice

**Don't try to understand everything at once!**

Focus on what you need:
- Adding a test? → Read Phases 1-3, 6
- Fixing a bug? → Understand the affected subsystem
- Major refactoring? → Read everything systematically

**Use the debugger!** Reading code is good, but stepping through with real values is better.

**Ask questions!** The community is friendly and helpful.

**Happy Reading!** 🚜📚

---

*This guide was created to help new contributors understand AgOpenGPS. Feedback and improvements welcome!*
