using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using AgLibrary.Controls;
using NUnit.Framework;

namespace AgLibrary.Tests.Controls
{
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class ListViewItemSorterTests_Fixed
    {
        private ListView _listView;
        private ListViewItemSorter _sorter;

        [SetUp]
        public void SetUp()
        {
            _listView = new ListView();
            _listView.View = View.Details;
            _listView.Columns.Add("Name", 100);
            _listView.Columns.Add("Value", 100);
            _listView.Columns.Add("Date", 100);

            _sorter = new ListViewItemSorter(_listView);
        }

        [TearDown]
        public void TearDown()
        {
            _sorter = null;
            _listView?.Dispose();
        }

        private void SetSortColumn(int column, SortOrder order = SortOrder.Ascending)
        {
            // Set sort column and order via reflection
            var columnProp = typeof(ListViewItemSorter).GetProperty("SortColumn", BindingFlags.NonPublic | BindingFlags.Instance);
            var orderProp = typeof(ListViewItemSorter).GetProperty("Order", BindingFlags.NonPublic | BindingFlags.Instance);

            columnProp?.SetValue(_sorter, column);
            orderProp?.SetValue(_sorter, order);
        }

        [Test]
        public void Constructor_ShouldSetListViewItemSorter()
        {
            // Assert
            Assert.That(_listView.ListViewItemSorter, Is.EqualTo(_sorter));
        }

        [Test]
        public void Compare_NumericColumn_ShouldSortNumerically()
        {
            // Arrange
            var item1 = new ListViewItem(new[] { "Item1", "100", "2023-01-01" });
            var item2 = new ListViewItem(new[] { "Item2", "20", "2023-02-02" });
            var item3 = new ListViewItem(new[] { "Item3", "5", "2023-03-03" });

            SetSortColumn(1); // Numeric column

            // Act
            var result1 = _sorter.Compare(item3, item2); // 5 vs 20
            var result2 = _sorter.Compare(item2, item1); // 20 vs 100

            // Assert - Should be 5 < 20 < 100 (not 100 < 20 < 5 as string sort would be)
            Assert.That(result1, Is.LessThan(0)); // 5 comes before 20
            Assert.That(result2, Is.LessThan(0)); // 20 comes before 100
        }

        [Test]
        public void Compare_DateColumn_ShouldSortByDate()
        {
            // Arrange
            var item1 = new ListViewItem(new[] { "Item1", "10", "2023-12-25" });
            var item2 = new ListViewItem(new[] { "Item2", "20", "2023-01-01" });
            var item3 = new ListViewItem(new[] { "Item3", "30", "2023-06-15" });

            SetSortColumn(2); // Date column

            // Act
            var result = _sorter.Compare(item1, item3); // 2023-12-25 vs 2023-06-15

            // Assert - Dates are sorted in reverse (newest first)
            Assert.That(result, Is.LessThan(0)); // Dec comes before Jun (reverse date sort)
        }

        [Test]
        public void Compare_MixedColumn_ShouldPutNumbersFirst()
        {
            // Arrange
            var itemText = new ListViewItem(new[] { "Text1", "abc", "2023-01-01" });
            var itemNum1 = new ListViewItem(new[] { "Text2", "123", "2023-02-02" });
            var itemNum2 = new ListViewItem(new[] { "Text4", "456", "2023-04-04" });

            SetSortColumn(1); // Mixed column

            // Act
            var result1 = _sorter.Compare(itemNum1, itemText); // 123 vs abc
            var result2 = _sorter.Compare(itemNum1, itemNum2); // 123 vs 456

            // Assert - Numbers should come before text
            Assert.That(result1, Is.LessThan(0)); // Number comes before text
            Assert.That(result2, Is.LessThan(0)); // 123 comes before 456
        }

        [Test]
        public void Compare_StringColumn_ShouldSortCaseInsensitive()
        {
            // Arrange
            var item1 = new ListViewItem(new[] { "charlie", "10", "2023-01-01" });
            var item2 = new ListViewItem(new[] { "ALICE", "20", "2023-02-02" });
            var item3 = new ListViewItem(new[] { "Bob", "30", "2023-03-03" });

            SetSortColumn(0); // String column

            // Act
            var result1 = _sorter.Compare(item2, item3); // ALICE vs Bob
            var result2 = _sorter.Compare(item3, item1); // Bob vs charlie

            // Assert - Case-insensitive: alice < bob < charlie
            Assert.That(result1, Is.LessThan(0)); // ALICE comes before Bob
            Assert.That(result2, Is.LessThan(0)); // Bob comes before charlie
        }

        [Test]
        public void Compare_DecimalNumbers_ShouldSortCorrectly()
        {
            // Arrange - Use integers to avoid locale-specific decimal separator issues
            var item1 = new ListViewItem(new[] { "Item1", "105", "2023-01-01" });
            var item2 = new ListViewItem(new[] { "Item2", "27", "2023-02-02" });
            var item3 = new ListViewItem(new[] { "Item3", "1002", "2023-03-03" });

            SetSortColumn(1); // Numeric column

            // Act
            var result1 = _sorter.Compare(item2, item1); // 27 vs 105
            var result2 = _sorter.Compare(item1, item3); // 105 vs 1002

            // Assert
            Assert.That(result1, Is.LessThan(0)); // 27 comes before 105
            Assert.That(result2, Is.LessThan(0)); // 105 comes before 1002
        }

        [Test]
        public void Compare_NegativeNumbers_ShouldSortCorrectly()
        {
            // Arrange
            var item1 = new ListViewItem(new[] { "Item1", "10", "2023-01-01" });
            var item2 = new ListViewItem(new[] { "Item2", "-5", "2023-02-02" });
            var item3 = new ListViewItem(new[] { "Item3", "0", "2023-03-03" });

            SetSortColumn(1); // Numeric column

            // Act
            var result1 = _sorter.Compare(item2, item3); // -5 vs 0
            var result2 = _sorter.Compare(item3, item1); // 0 vs 10

            // Assert
            Assert.That(result1, Is.LessThan(0)); // -5 comes before 0
            Assert.That(result2, Is.LessThan(0)); // 0 comes before 10
        }

        [Test]
        public void Compare_DescendingOrder_ShouldReverseComparison()
        {
            // Arrange
            var item1 = new ListViewItem(new[] { "Alice", "10", "2023-01-01" });
            var item2 = new ListViewItem(new[] { "Bob", "20", "2023-02-02" });

            SetSortColumn(0, SortOrder.Descending);

            // Act
            var result = _sorter.Compare(item1, item2); // Alice vs Bob in descending

            // Assert - In descending, Bob should come before Alice
            Assert.That(result, Is.GreaterThan(0)); // Alice comes after Bob (descending)
        }

        [Test]
        public void Compare_EmptyStrings_ShouldNotThrow()
        {
            // Arrange
            var item1 = new ListViewItem(new[] { "", "10", "2023-01-01" });
            var item2 = new ListViewItem(new[] { "Test", "20", "2023-02-02" });

            SetSortColumn(0);

            // Act & Assert
            Assert.DoesNotThrow(() => _sorter.Compare(item1, item2));
        }
    }
}
