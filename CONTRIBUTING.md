# Contributing to AgOpenGPS

Thank you for your interest in contributing to AgOpenGPS! This document will help you get started.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [How Can I Contribute?](#how-can-i-contribute)
- [Development Setup](#development-setup)
- [Submitting Changes](#submitting-changes)
- [Testing](#testing)
- [Coding Guidelines](#coding-guidelines)
- [Community](#community)

---

## Code of Conduct

AgOpenGPS is a community project serving farmers and agricultural professionals worldwide. We expect all contributors to:

- Be respectful and inclusive
- Welcome newcomers and help them learn
- Focus on what's best for the farming community
- Show empathy towards other community members
- Accept constructive criticism gracefully

---

## Getting Started

### First Time Contributors

New to open source? Welcome! Here's how to start:

1. **Browse issues** labeled [`good-first-issue`](../../labels/good-first-issue)
2. **Read the documentation** in the `/docs` folder
3. **Join our community** (see [Community](#community) section)
4. **Ask questions** - we're here to help!

### Finding Something to Work On

Look for issues labeled:
- [`good-first-issue`](../../labels/good-first-issue) - Great for newcomers
- [`help-wanted`](../../labels/help-wanted) - Community assistance needed
- [`bug`](../../labels/bug) - Something isn't working
- [`enhancement`](../../labels/enhancement) - New feature or request
- [`test-needed`](../../labels/test-needed) - Needs automated testing
- [`documentation`](../../labels/documentation) - Improvements to docs

---

## How Can I Contribute?

### 🐛 Reporting Bugs

**Before submitting a bug report:**
1. Check if it's already reported in [Issues](../../issues)
2. Try to reproduce on the latest `develop` branch
3. Gather relevant information (version, OS, steps to reproduce)

**Bug Report Template:**

```markdown
**Describe the bug**
A clear description of what the bug is.

**To Reproduce**
Steps to reproduce the behavior:
1. Go to '...'
2. Click on '...'
3. See error

**Expected behavior**
What you expected to happen.

**Screenshots/Logs**
If applicable, add screenshots or log files.

**Environment:**
- AgOpenGPS Version: [e.g. v5.7.0]
- OS: [e.g. Windows 10]
- Hardware: [e.g. Tablet make/model]

**Additional context**
Any other information about the problem.
```

### 💡 Suggesting Enhancements

**Before suggesting an enhancement:**
1. Check if it already exists in [Issues](../../issues)
2. Discuss on the [forum](https://discourse.agopengps.com/) first for major changes
3. Consider if it benefits the farming community

**Enhancement Template:**

```markdown
**Is your feature request related to a problem?**
A clear description of the problem. Ex. I'm always frustrated when [...]

**Describe the solution you'd like**
A clear description of what you want to happen.

**Describe alternatives you've considered**
Other solutions or features you've considered.

**Additional context**
Mockups, diagrams, or examples.
```

### 🧪 Adding Tests

We're building automated test coverage! Tests are especially welcome because they:
- Help prevent bugs
- Make refactoring safer
- Don't risk breaking existing functionality

See [COMMUNITY_TESTING_PLAN.md](COMMUNITY_TESTING_PLAN.md) for details.

**Quick Start for Test Contributors:**

1. Find a [`test-needed`](../../labels/test-needed) issue
2. Comment "I'll take this!" to claim it
3. Follow template in `SourceCode/AgOpenGPS.IntegrationTests/`
4. Submit PR with just the test (no production changes)

Example test:
```csharp
[Test]
[Description("Tests AB line guidance at 90 degrees - contributed by @yourname")]
public void StraightABLine_90Degrees_ShouldFollowAccurately()
{
    var scenario = TestScenario.CreateStraightLine(500);
    scenario.Heading = Math.PI / 2; // 90 degrees (East)

    using var testForm = new TestableFormGPS();
    testForm.Configure(scenario);

    var result = testForm.RunSimulation(180);

    Assert.That(result.Statistics.RMSCrossTrackError, Is.LessThan(0.10));
}
```

### 📝 Improving Documentation

Documentation improvements are always welcome:
- Fix typos or unclear explanations
- Add examples and tutorials
- Translate to other languages
- Create video guides

Documentation lives in:
- `/docs` - General documentation
- `README.md` - Project overview
- Code comments - Inline documentation
- Wiki - Community knowledge base

### 🌍 Translations

AgOpenGPS serves farmers worldwide. Help translate to your language:

1. Visit [AgOpenGPS on Weblate](https://hosted.weblate.org/engage/agopengps)
2. Select your language (or add a new one)
3. Translate strings in the web interface
4. Translations are automatically submitted as PRs

---

## Development Setup

### Prerequisites

- Windows 10/11 (primary platform)
- Visual Studio 2022 Community Edition (free) or JetBrains Rider
- .NET Framework 4.8 SDK
- Git

### Getting the Code

```bash
# Fork the repository on GitHub, then clone your fork
git clone https://github.com/YOUR-USERNAME/AgOpenGPS.git
cd AgOpenGPS

# Add upstream remote
git remote add upstream https://github.com/agopengps-official/AgOpenGPS.git

# Create a branch for your work
git checkout -b feature/your-feature-name develop
```

### Building the Project

```bash
cd SourceCode
dotnet restore AgOpenGPS.sln
dotnet build AgOpenGPS.sln
```

Or open `SourceCode/AgOpenGPS.sln` in Visual Studio and build from IDE.

### Running Tests

```bash
cd SourceCode
dotnet test AgOpenGPS.IntegrationTests/AgOpenGPS.IntegrationTests.csproj
```

Or use Visual Studio Test Explorer.

---

## Submitting Changes

### Branch Strategy

- `master` - Stable releases only
- `develop` - Active development (target your PRs here)
- `feature/*` - New features
- `fix/*` - Bug fixes
- `test/*` - Adding/improving tests

### Pull Request Process

1. **Claim the issue** - Comment on the issue you're working on
2. **Create a branch** from `develop`
   ```bash
   git checkout -b feature/your-feature develop
   ```
3. **Make your changes** - Keep commits logical and focused
4. **Test thoroughly** - Run existing tests, add new ones
5. **Update documentation** - README, code comments, etc.
6. **Commit with clear messages**
   ```bash
   git commit -m "Add 90-degree AB line test

   Tests guidance accuracy on East-facing AB lines.
   Validates RMS error stays below 10cm threshold.

   Closes #123"
   ```
7. **Push to your fork**
   ```bash
   git push origin feature/your-feature
   ```
8. **Open a Pull Request** to `develop` branch
9. **Respond to feedback** - Address review comments
10. **Celebrate!** 🎉 Once merged, you're an AgOpenGPS contributor!

### Pull Request Guidelines

**DO:**
- ✅ Keep PRs focused on a single issue
- ✅ Write clear PR descriptions
- ✅ Add tests for new functionality
- ✅ Update documentation
- ✅ Follow existing code style
- ✅ Be patient with reviews (maintainers are volunteers)

**DON'T:**
- ❌ Mix unrelated changes in one PR
- ❌ Include large reformatting changes
- ❌ Submit PRs without testing
- ❌ Change files unnecessarily
- ❌ Force push after someone has reviewed

### Commit Message Format

```
Short summary (50 chars or less)

More detailed explanation if needed. Wrap at 72 characters.
Explain WHAT changed and WHY, not HOW (code shows that).

- Bullet points are okay
- Use imperative mood ("Add feature" not "Added feature")

Fixes #123
Closes #456
See also #789
```

### PR Title Format

- `feat: Add support for curved AB lines`
- `fix: Resolve section control timing issue`
- `test: Add 45-degree AB line validation`
- `docs: Update installation instructions`
- `refactor: Extract simulation state class`

---

## Testing

### Why Tests Matter

Tests help ensure AgOpenGPS works reliably in the field. A bug in guidance software could:
- Waste expensive inputs (seed, fertilizer, chemicals)
- Damage crops
- Cost farmers money and time

Good test coverage gives farmers confidence in the software.

### Types of Tests

1. **Unit Tests** - Test individual functions
   - Location: `AgOpenGPS.Core.Tests/`
   - Example: Geometry calculations, coordinate transformations

2. **Integration Tests** - Test system behavior
   - Location: `AgOpenGPS.IntegrationTests/`
   - Example: Guidance accuracy, section control

3. **Manual Tests** - Real-world validation
   - Location: Field testing by community
   - Example: Running AgOpenGPS on actual equipment

### Writing Good Tests

```csharp
[TestFixture]
public class GuidanceTests
{
    [Test]
    public void TestName_Scenario_ExpectedBehavior()
    {
        // Arrange - Set up test conditions
        var config = new TestConfig();

        // Act - Perform the action
        var result = PerformAction(config);

        // Assert - Verify expectations
        Assert.That(result, Is.EqualTo(expected));
    }
}
```

**Test Best Practices:**
- Tests should be deterministic (same result every time)
- Each test should test one thing
- Use descriptive names
- Include helpful failure messages
- Clean up resources (use `using` statements)

### Test Coverage Goals

- Core guidance algorithms: >80%
- Safety-critical features: 100%
- UI code: Best effort (harder to test)

---

## Coding Guidelines

### General Principles

1. **Clarity over cleverness** - Code is read more than written
2. **Consistency** - Follow existing patterns
3. **Comments for why, not what** - Code shows what, comments explain why
4. **Don't break existing functionality** - Backwards compatibility matters

### C# Style Guide

```csharp
// Naming Conventions
public class MyClass                    // PascalCase for classes
{
    private readonly int maxSpeed;      // camelCase for private fields
    public int MaxSpeed { get; set; }   // PascalCase for properties

    public void CalculateSpeed()        // PascalCase for methods
    {
        int localVar = 0;               // camelCase for local variables
        const int MAX_RETRIES = 3;      // UPPER_CASE for constants
    }
}

// Bracing - Opening brace on new line (Allman style)
if (condition)
{
    DoSomething();
}
else
{
    DoSomethingElse();
}

// Comments
// Single line comments for brief explanations

/// <summary>
/// XML comments for public APIs
/// </summary>
public void PublicMethod()
{
}

// Spacing
public void Method(int param1, int param2)  // Space after commas
{
    int result = param1 + param2;           // Spaces around operators

    if (result > 10)                        // Space before opening brace
    {
        Console.WriteLine(result);
    }
}
```

### Code Organization

```
SourceCode/
├── GPS/                    # Main application
│   ├── Classes/           # Core business logic
│   ├── Forms/             # UI forms
│   ├── Properties/        # Settings & resources
│   └── Protocols/         # Communication protocols
├── AgOpenGPS.Core/        # Shared core library
│   ├── Models/           # Data models
│   ├── Interfaces/       # Abstractions
│   └── Translations/     # Localization
└── AgOpenGPS.*.Tests/    # Test projects
```

### Performance Considerations

AgOpenGPS runs on tablets and older hardware:
- Avoid allocations in hot loops
- Cache calculations when possible
- Profile before optimizing
- Consider memory pressure

### Platform Compatibility

- Primary: Windows 10/11
- Target: .NET Framework 4.8
- Hardware: Range from low-end tablets to gaming laptops
- Consider touch interfaces in UI design

---

## Community

### Where to Connect

- **Forum**: [discourse.agopengps.com](https://discourse.agopengps.com/) - Main discussion
- **GitHub Discussions**: Questions, ideas, show-and-tell
- **GitHub Issues**: Bug reports, feature requests
- **Discord**: Real-time chat (link in forum)

### Getting Help

**For coding questions:**
1. Search existing issues and forum posts
2. Ask in GitHub Discussions or Discord
3. Tag your question appropriately
4. Include relevant code/error messages

**For agricultural questions:**
1. Ask in the forum (farmers and experts there)
2. Be specific about your equipment/crop/region
3. Share photos if helpful

### Helpful Resources

- [AgOpenGPS Documentation](https://docs.agopengps.com/)
- [Forum - Getting Started](https://discourse.agopengps.com/c/getting-started)
- [YouTube Channel](https://www.youtube.com/c/agopengps) - Tutorials
- [PCB Repository](https://github.com/agopengps-official/Boards) - Hardware

---

## Recognition

Contributors are recognized in:
- Release notes
- CONTRIBUTORS.md file
- GitHub contribution graph
- Community shoutouts on forum

Every contribution, no matter how small, makes a difference to farmers using AgOpenGPS!

---

## Additional Notes

### Backward Compatibility

AgOpenGPS is used on working farms during planting and harvest seasons. Breaking changes can:
- Disrupt critical operations
- Require emergency rollbacks
- Lose farmer trust

**Guidelines:**
- Maintain compatibility with existing field files
- Preserve settings across versions
- Deprecate features, don't remove them abruptly
- Test upgrades from older versions

### Safety-Critical Code

Some code is safety-critical (guidance, section control):
- Requires extra scrutiny in review
- Must have test coverage
- Changes need field validation
- Document assumptions clearly

### Seasonal Considerations

Development activity varies with farming seasons:
- **Planting season** (Spring) - Critical usage period, avoid breaking changes
- **Growing season** (Summer) - Active feature development
- **Harvest season** (Fall) - Critical usage period, bug fixes only
- **Winter** - Major refactoring, experimental features

### License

By contributing, you agree that your contributions will be licensed under the GNU GPLv3 License.

Key points:
- Your code will be freely available
- Others can modify and redistribute
- Must remain open source
- You retain copyright on your contributions

---

## Questions?

Don't hesitate to ask! Some of our best contributors started by asking "newbie" questions.

- **General questions**: [GitHub Discussions](../../discussions)
- **Bug reports**: [GitHub Issues](../../issues)
- **Feature ideas**: [Forum](https://discourse.agopengps.com/)
- **Quick help**: Discord (link in forum)

---

## Thank You!

AgOpenGPS exists because of contributors like you. Every line of code, every bug report, every translation, and every forum post helps farmers around the world.

**Happy Contributing!** 🚜

---

## Quick Links

- [Code of Conduct](CODE_OF_CONDUCT.md) *(to be created)*
- [Development Setup](#development-setup)
- [Testing Guide](COMMUNITY_TESTING_PLAN.md)
- [Architecture Overview](docs/ARCHITECTURE.md) *(to be created)*
- [API Documentation](docs/API.md) *(to be created)*
- [FAQ](docs/FAQ.md) *(to be created)*

---

*Last updated: 2025-01-09*
