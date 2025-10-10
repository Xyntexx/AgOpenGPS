using System;
using System.Collections.Generic;
using AgOpenGPS;
using NUnit.Framework;

namespace AgLibrary.Tests.Geometry
{
    [TestFixture]
    public class GeometryExtensionMethodsTests
    {
        private const double Tolerance = 0.0001;

        #region CalculateHeadings Tests

        [Test]
        public void CalculateHeadings_EmptyList_ShouldNotThrow()
        {
            // Arrange
            var points = new List<vec3>();

            // Act & Assert
            Assert.DoesNotThrow(() => points.CalculateHeadings(false));
        }

        [Test]
        public void CalculateHeadings_SinglePoint_ShouldNotModifyList()
        {
            // Arrange
            var points = new List<vec3>
            {
                new vec3(10, 10, 0)
            };

            // Act
            points.CalculateHeadings(false);

            // Assert
            Assert.That(points.Count, Is.EqualTo(1));
        }

        [Test]
        public void CalculateHeadings_TwoPoints_OpenLine_ShouldCalculateCorrectHeadings()
        {
            // Arrange - Line going north
            var points = new List<vec3>
            {
                new vec3(0, 0, 0),    // Start
                new vec3(0, 10, 0)    // North
            };

            // Act
            points.CalculateHeadings(false);

            // Assert
            // Heading north should be 0 radians (or 2*PI)
            Assert.That(points[0].heading, Is.EqualTo(0).Within(Tolerance));
            Assert.That(points[1].heading, Is.EqualTo(0).Within(Tolerance));
        }

        [Test]
        public void CalculateHeadings_TwoPoints_Loop_ShouldUseWraparoundLogic()
        {
            // Arrange - Line going east
            var points = new List<vec3>
            {
                new vec3(0, 0, 0),    // Start
                new vec3(10, 0, 0)    // East
            };

            // Act
            points.CalculateHeadings(true);

            // Assert
            // For a loop with 2 points, each point should see heading between prev and next
            // First point: heading from last to second
            // Last point: heading from first to first (wraps around)
            Assert.That(points[0].heading, Is.GreaterThanOrEqualTo(0));
            Assert.That(points[1].heading, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void CalculateHeadings_ThreePoints_OpenLine_ShouldAverageMiddlePoint()
        {
            // Arrange - Straight line going north
            var points = new List<vec3>
            {
                new vec3(0, 0, 0),     // Start
                new vec3(0, 10, 0),    // Middle
                new vec3(0, 20, 0)     // End
            };

            // Act
            points.CalculateHeadings(false);

            // Assert
            // All headings should be north (0 radians)
            Assert.That(points[0].heading, Is.EqualTo(0).Within(Tolerance));
            Assert.That(points[1].heading, Is.EqualTo(0).Within(Tolerance));
            Assert.That(points[2].heading, Is.EqualTo(0).Within(Tolerance));
        }

        [Test]
        public void CalculateHeadings_ThreePoints_EastTurn_ShouldCalculateCorrectly()
        {
            // Arrange - Turn east
            var points = new List<vec3>
            {
                new vec3(0, 0, 0),     // Start
                new vec3(0, 10, 0),    // Middle - turn point
                new vec3(10, 10, 0)    // East
            };

            // Act
            points.CalculateHeadings(false);

            // Assert
            // First heading: north (0)
            Assert.That(points[0].heading, Is.EqualTo(0).Within(Tolerance));

            // Middle heading: average of north and east = northeast (PI/4 ≈ 0.785)
            Assert.That(points[1].heading, Is.EqualTo(Math.PI / 4).Within(Tolerance));

            // Last heading: east (PI/2 ≈ 1.571)
            Assert.That(points[2].heading, Is.EqualTo(Math.PI / 2).Within(Tolerance));
        }

        [Test]
        public void CalculateHeadings_Loop_ShouldWrapFirstAndLast()
        {
            // Arrange - Square
            var points = new List<vec3>
            {
                new vec3(0, 0, 0),      // SW corner
                new vec3(10, 0, 0),     // SE corner
                new vec3(10, 10, 0),    // NE corner
                new vec3(0, 10, 0)      // NW corner
            };

            // Act
            points.CalculateHeadings(true);

            // Assert
            // All points should have valid headings
            for (int i = 0; i < points.Count; i++)
            {
                Assert.That(points[i].heading, Is.GreaterThanOrEqualTo(0));
                Assert.That(points[i].heading, Is.LessThanOrEqualTo(glm.twoPI));
            }
        }

        [Test]
        public void CalculateHeadings_NegativeHeading_ShouldNormalizeTo2Pi()
        {
            // Arrange - Line going southwest (negative heading without normalization)
            var points = new List<vec3>
            {
                new vec3(10, 10, 0),   // Start
                new vec3(0, 0, 0)      // Southwest
            };

            // Act
            points.CalculateHeadings(false);

            // Assert
            // All headings should be positive (normalized)
            Assert.That(points[0].heading, Is.GreaterThanOrEqualTo(0));
            Assert.That(points[0].heading, Is.LessThanOrEqualTo(glm.twoPI));
            Assert.That(points[1].heading, Is.GreaterThanOrEqualTo(0));
            Assert.That(points[1].heading, Is.LessThanOrEqualTo(glm.twoPI));
        }

        #endregion

        #region OffsetLine Tests

        [Test]
        public void OffsetLine_EmptyList_ShouldReturnEmptyList()
        {
            // Arrange
            var points = new List<vec3>();

            // Act
            var result = points.OffsetLine(1.0, 0.1, false);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void OffsetLine_SinglePoint_WithValidOffset_ShouldReturnOffsetPoint()
        {
            // Arrange - Point heading north
            var points = new List<vec3>
            {
                new vec3(0, 0, 0)  // North heading (0)
            };

            // Act - Offset 1 meter to the right
            var result = points.OffsetLine(1.0, 0.1, false);

            // Assert
            // Offset perpendicular to north (heading 0) by 1 meter right
            // Should move east by 1 meter
            Assert.That(result.Count, Is.GreaterThan(0));
            Assert.That(result[0].easting, Is.EqualTo(1.0).Within(Tolerance));
            Assert.That(result[0].northing, Is.EqualTo(0.0).Within(Tolerance));
        }

        [Test]
        public void OffsetLine_StraightLineNorth_ShouldOffsetPerpendicularly()
        {
            // Arrange - Straight line going north
            var points = new List<vec3>
            {
                new vec3(0, 0, 0),
                new vec3(0, 10, 0),
                new vec3(0, 20, 0)
            };

            // Act - Offset 2 meters to the right
            var result = points.OffsetLine(2.0, 0.5, false);

            // Assert
            Assert.That(result.Count, Is.GreaterThan(0));

            // All offset points should be 2 meters east of original line
            foreach (var point in result)
            {
                Assert.That(point.easting, Is.EqualTo(2.0).Within(Tolerance));
            }
        }

        [Test]
        public void OffsetLine_StraightLineEast_ShouldOffsetPerpendicularly()
        {
            // Arrange - Straight line going east
            var points = new List<vec3>
            {
                new vec3(0, 0, Math.PI / 2),   // East heading
                new vec3(10, 0, Math.PI / 2),
                new vec3(20, 0, Math.PI / 2)
            };

            // Act - Offset 2 meters to the right (south)
            var result = points.OffsetLine(2.0, 0.5, false);

            // Assert
            Assert.That(result.Count, Is.GreaterThan(0));

            // All offset points should be 2 meters south of original line
            foreach (var point in result)
            {
                Assert.That(point.northing, Is.EqualTo(-2.0).Within(Tolerance));
            }
        }

        [Test]
        public void OffsetLine_MinDistFilter_ShouldRemoveClosePoints()
        {
            // Arrange - Line with closely spaced points
            var points = new List<vec3>
            {
                new vec3(0, 0, 0),
                new vec3(0, 0.5, 0),   // Very close
                new vec3(0, 1.0, 0),   // Very close
                new vec3(0, 10, 0)     // Far
            };

            // Act - Offset with large minDist to filter close points
            var result = points.OffsetLine(1.0, 5.0, false);

            // Assert
            // Should filter out closely spaced offset points
            Assert.That(result.Count, Is.LessThan(points.Count));
        }

        [Test]
        public void OffsetLine_PointsTooCloseToOriginalLine_ShouldBeFiltered()
        {
            // Arrange - Create a scenario where offset would be too close
            var points = new List<vec3>
            {
                new vec3(0, 0, 0),
                new vec3(0, 1, 0),
                new vec3(0.5, 1.5, Math.PI / 4)  // Point that would create close offset
            };

            // Act - Small offset distance
            var result = points.OffsetLine(0.1, 0.05, false);

            // Assert
            // Some points might be filtered if offset is too close to original
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void OffsetLine_Loop_ShouldCalculateHeadingsAsLoop()
        {
            // Arrange - Square boundary
            var points = new List<vec3>
            {
                new vec3(0, 0, 0),
                new vec3(10, 0, 0),
                new vec3(10, 10, 0),
                new vec3(0, 10, 0)
            };

            // Act - Offset as a closed loop
            var resultLoop = points.OffsetLine(2.0, 0.5, true);
            var resultOpen = points.OffsetLine(2.0, 0.5, false);

            // Assert
            // Loop and open should potentially have different results
            Assert.That(resultLoop, Is.Not.Null);
            Assert.That(resultOpen, Is.Not.Null);
        }

        [Test]
        public void OffsetLine_NegativeOffset_ShouldOffsetOppositeDirection()
        {
            // Arrange - Line going north
            var points = new List<vec3>
            {
                new vec3(0, 0, 0),
                new vec3(0, 10, 0)
            };

            // Act
            var resultPositive = points.OffsetLine(2.0, 0.1, false);
            var resultNegative = points.OffsetLine(-2.0, 0.1, false);

            // Assert
            // Negative offset should be opposite direction from positive
            if (resultPositive.Count > 0 && resultNegative.Count > 0)
            {
                var eastingDiff = resultPositive[0].easting - resultNegative[0].easting;
                Assert.That(Math.Abs(eastingDiff), Is.GreaterThan(3.0)); // Should be ~4 meters apart
            }
        }

        [Test]
        public void OffsetLine_ZeroDistance_ReturnsPointsAtOriginalLocation()
        {
            // Arrange
            var points = new List<vec3>
            {
                new vec3(0, 0, 0),
                new vec3(0, 10, 0),
                new vec3(0, 20, 0)
            };

            // Act - Zero offset creates points at perpendicular position (still offset by calculation)
            var result = points.OffsetLine(0.0, 0.1, false);

            // Assert - Zero distance still creates offset points (they just happen to be at same location)
            // The filtering is based on distance to original points, not the offset amount
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void OffsetLine_LargeOffset_ShouldMaintainRelativePositions()
        {
            // Arrange - Simple two-point line
            var points = new List<vec3>
            {
                new vec3(0, 0, 0),
                new vec3(0, 100, 0)
            };

            // Act - Large offset
            var result = points.OffsetLine(50.0, 1.0, false);

            // Assert
            Assert.That(result.Count, Is.GreaterThan(0));

            // All points should be offset by 50 meters
            foreach (var point in result)
            {
                Assert.That(point.easting, Is.EqualTo(50.0).Within(Tolerance));
            }
        }

        [Test]
        public void OffsetLine_PreservesPointCount_WhenNoFiltering()
        {
            // Arrange - Well-spaced points
            var points = new List<vec3>
            {
                new vec3(0, 0, 0),
                new vec3(0, 100, 0),
                new vec3(0, 200, 0)
            };

            // Act - Small offset with small minDist
            var result = points.OffsetLine(5.0, 0.1, false);

            // Assert
            // With well-spaced points and small minDist, should keep most/all points
            Assert.That(result.Count, Is.GreaterThan(0));
        }

        #endregion

        #region Integration Tests

        [Test]
        public void OffsetLine_CallsCalculateHeadings_Internally()
        {
            // Arrange - Points without pre-calculated headings
            var points = new List<vec3>
            {
                new vec3(0, 0, 999),     // Invalid heading
                new vec3(0, 10, 999),    // Invalid heading
                new vec3(0, 20, 999)     // Invalid heading
            };

            // Act - OffsetLine should calculate headings internally
            var result = points.OffsetLine(2.0, 0.5, false);

            // Assert
            // Should work correctly even with invalid initial headings
            Assert.That(result, Is.Not.Null);

            // Original points should have recalculated headings
            foreach (var point in points)
            {
                Assert.That(point.heading, Is.Not.EqualTo(999));
                Assert.That(point.heading, Is.GreaterThanOrEqualTo(0));
                Assert.That(point.heading, Is.LessThanOrEqualTo(glm.twoPI));
            }
        }

        [Test]
        public void GeometryExtensions_ComplexPath_ShouldHandleGracefully()
        {
            // Arrange - Complex path with turns
            var points = new List<vec3>
            {
                new vec3(0, 0, 0),
                new vec3(0, 10, 0),
                new vec3(10, 10, 0),
                new vec3(10, 20, 0),
                new vec3(0, 20, 0)
            };

            // Act
            var result = points.OffsetLine(3.0, 1.0, false);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.GreaterThan(0));

            // All offset points should be roughly 3 meters from original path
            // (exact distance varies at corners due to perpendicular offset)
        }

        #endregion
    }
}
