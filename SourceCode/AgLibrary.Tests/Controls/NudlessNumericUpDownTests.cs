using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using AgLibrary.Controls;
using NUnit.Framework;

namespace AgLibrary.Tests.Controls
{
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class NudlessNumericUpDownTests
    {
        private NudlessNumericUpDown _control;
        private CultureInfo _originalCulture;

        [SetUp]
        public void SetUp()
        {
            // Save original culture and set to invariant for consistent test results
            _originalCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            _control = new NudlessNumericUpDown();
        }

        [TearDown]
        public void TearDown()
        {
            _control?.Dispose();

            // Restore original culture
            Thread.CurrentThread.CurrentCulture = _originalCulture;
        }

        [Test]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Assert
            Assert.That(_control.Minimum, Is.EqualTo(0.0));
            Assert.That(_control.Maximum, Is.EqualTo(100.0));
            Assert.That(_control.DecimalPlaces, Is.EqualTo(0));
            Assert.That(_control.Mode, Is.EqualTo(UnitMode.None));
            Assert.That(_control.GetDisplayConversionFactor, Is.Not.Null);
            Assert.That(_control.GetStorageConversionFactor, Is.Not.Null);
        }

        [Test]
        public void Value_ShouldClampToMinimum()
        {
            // Arrange
            _control.Minimum = 10;
            _control.Maximum = 100;

            // Act
            _control.Value = 5;

            // Assert
            Assert.That(_control.Value, Is.EqualTo(10));
        }

        [Test]
        public void Value_ShouldClampToMaximum()
        {
            // Arrange
            _control.Minimum = 0;
            _control.Maximum = 50;

            // Act
            _control.Value = 100;

            // Assert
            Assert.That(_control.Value, Is.EqualTo(50));
        }

        [Test]
        public void Value_WithinRange_ShouldSetCorrectly()
        {
            // Arrange
            _control.Minimum = 0;
            _control.Maximum = 100;

            // Act
            _control.Value = 42.5;

            // Assert
            Assert.That(_control.Value, Is.EqualTo(42.5));
        }

        [Test]
        public void Minimum_GreaterThanMaximum_ShouldAdjustMaximum()
        {
            // Arrange
            _control.Maximum = 50;

            // Act
            _control.Minimum = 75;

            // Assert
            Assert.That(_control.Minimum, Is.EqualTo(75));
            Assert.That(_control.Maximum, Is.EqualTo(75));
        }

        [Test]
        public void Maximum_LessThanMinimum_ShouldAdjustMinimum()
        {
            // Arrange
            _control.Minimum = 50;

            // Act
            _control.Maximum = 25;

            // Assert
            Assert.That(_control.Maximum, Is.EqualTo(25));
            Assert.That(_control.Minimum, Is.EqualTo(25));
        }

        [Test]
        public void DecimalPlaces_ShouldFormatValueCorrectly()
        {
            // Arrange
            using (var form = new Form())
            {
                form.Controls.Add(_control);
                form.Show();

                _control.Value = 42.6789;

                // Act
                _control.DecimalPlaces = 2;
                Application.DoEvents();

                // Assert
                Assert.That(_control.Text, Is.EqualTo("42.68"));
            }
        }

        [Test]
        public void DecimalPlaces_Zero_ShouldFormatAsInteger()
        {
            // Arrange
            using (var form = new Form())
            {
                form.Controls.Add(_control);
                form.Show();

                _control.Value = 42.6789;

                // Act
                _control.DecimalPlaces = 0;
                Application.DoEvents();

                // Assert
                Assert.That(_control.Text, Is.EqualTo("43"));
            }
        }

        [Test]
        public void Mode_ShouldApplyDisplayConversionFactor()
        {
            // Arrange
            using (var form = new Form())
            {
                form.Controls.Add(_control);
                form.Show();

                _control.Value = 10.0;
                _control.DecimalPlaces = 2;
                _control.GetDisplayConversionFactor = mode => mode == UnitMode.Large ? 3.28084 : 1.0;

                // Act - Setting mode should trigger UpdateEditText which uses the conversion factor
                _control.Mode = UnitMode.Large;
                Application.DoEvents();

                // Assert - 10 meters = 32.81 feet (but need to trigger update)
                // Mode change alone doesn't update text, need to set value or decimal places after
                _control.DecimalPlaces = 2; // Trigger update
                Application.DoEvents();

                Assert.That(_control.Text, Is.EqualTo("32.81"));
            }
        }

        [Test]
        public void ValueChanged_EventCanBeSubscribed()
        {
            // Arrange
            bool eventFired = false;
            _control.ValueChanged += (s, e) => eventFired = true;

            // Act - ValueChanged only fires via OnClick dialog, not direct property set
            // Just verify event subscription works
            _control.Value = 42;

            // Assert - Event doesn't fire on direct property set by design
            Assert.That(eventFired, Is.False);
        }

        [Test]
        public void GetDisplayConversionFactor_DefaultValue_ShouldReturnOne()
        {
            // Act
            var factor = _control.GetDisplayConversionFactor(UnitMode.Large);

            // Assert
            Assert.That(factor, Is.EqualTo(1.0));
        }

        [Test]
        public void GetStorageConversionFactor_DefaultValue_ShouldReturnOne()
        {
            // Act
            var factor = _control.GetStorageConversionFactor(UnitMode.Large);

            // Assert
            Assert.That(factor, Is.EqualTo(1.0));
        }

        [Test]
        public void GetDisplayConversionFactor_CustomValue_ShouldBeUsed()
        {
            // Arrange
            _control.GetDisplayConversionFactor = mode => mode == UnitMode.Small ? 2.54 : 1.0;

            // Act
            var factor = _control.GetDisplayConversionFactor(UnitMode.Small);

            // Assert
            Assert.That(factor, Is.EqualTo(2.54));
        }

        [Test]
        public void ToString_ShouldIncludeMinimumAndMaximum()
        {
            // Arrange
            _control.Minimum = 10.5;
            _control.Maximum = 99.5;

            // Act
            var result = _control.ToString();

            // Assert - ToString includes Minimum and Maximum labels
            Assert.That(result, Does.Contain("Minimum"));
            Assert.That(result, Does.Contain("Maximum"));
            Assert.That(result, Does.Contain("10"));
            Assert.That(result, Does.Contain("99"));
        }

        [Test]
        public void Mode_AllValues_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _control.Mode = UnitMode.None);
            Assert.DoesNotThrow(() => _control.Mode = UnitMode.Large);
            Assert.DoesNotThrow(() => _control.Mode = UnitMode.Small);
            Assert.DoesNotThrow(() => _control.Mode = UnitMode.Speed);
            Assert.DoesNotThrow(() => _control.Mode = UnitMode.Area);
            Assert.DoesNotThrow(() => _control.Mode = UnitMode.Distance);
            Assert.DoesNotThrow(() => _control.Mode = UnitMode.Temperature);
        }
    }
}
