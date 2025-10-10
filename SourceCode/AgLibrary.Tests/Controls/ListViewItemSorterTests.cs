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
    public class ListViewItemSorterTests
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

        private void SimulateColumnClick(int columnIndex)
        {
            var method = typeof(ListView).GetMethod("OnColumnClick", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(_listView, new object[] { new ColumnClickEventArgs(columnIndex) });
        }

        [Test]
        public void Constructor_ShouldSetListViewItemSorter()
        {
            // Assert
            Assert.That(_listView.ListViewItemSorter, Is.EqualTo(_sorter));
        }

        [Test]
        public void ColumnClick_FirstClick_ShouldSortAscending()
        {
            // Arrange
            _listView.Items.Add(new ListViewItem(new[] { "Charlie", "30", "2023-03-03" }));
            _listView.Items.Add(new ListViewItem(new[] { "Alice", "10", "2023-01-01" }));
            _listView.Items.Add(new ListViewItem(new[] { "Bob", "20", "2023-02-02" }));

            // Act
            SimulateColumnClick(0);
            _listView.Sort();

            // Assert
            Assert.That(_listView.Items[0].Text, Is.EqualTo("Alice"));
            Assert.That(_listView.Items[1].Text, Is.EqualTo("Bob"));
            Assert.That(_listView.Items[2].Text, Is.EqualTo("Charlie"));
        }

        [Test]
        public void ColumnClick_SecondClick_ShouldSortDescending()
        {
            // Arrange
            _listView.Items.Add(new ListViewItem(new[] { "Charlie", "30", "2023-03-03" }));
            _listView.Items.Add(new ListViewItem(new[] { "Alice", "10", "2023-01-01" }));
            _listView.Items.Add(new ListViewItem(new[] { "Bob", "20", "2023-02-02" }));

            // Act - First click ascending, second click descending
            SimulateColumnClick(0);
            _listView.Sort();
            SimulateColumnClick(0);
            _listView.Sort();

            // Assert
            Assert.That(_listView.Items[0].Text, Is.EqualTo("Charlie"));
            Assert.That(_listView.Items[1].Text, Is.EqualTo("Bob"));
            Assert.That(_listView.Items[2].Text, Is.EqualTo("Alice"));
        }

        [Test]
        public void Compare_NumericColumn_ShouldSortNumerically()
        {
            // Arrange
            _listView.Items.Add(new ListViewItem(new[] { "Item1", "100", "2023-01-01" }));
            _listView.Items.Add(new ListViewItem(new[] { "Item2", "20", "2023-02-02" }));
            _listView.Items.Add(new ListViewItem(new[] { "Item3", "5", "2023-03-03" }));

            // Act - Sort by numeric column
            SimulateColumnClick(1);
            _listView.Sort();

            // Assert - Should be 5, 20, 100 (not 100, 20, 5 as string sort would be)
            Assert.That(_listView.Items[0].SubItems[1].Text, Is.EqualTo("5"));
            Assert.That(_listView.Items[1].SubItems[1].Text, Is.EqualTo("20"));
            Assert.That(_listView.Items[2].SubItems[1].Text, Is.EqualTo("100"));
        }

        [Test]
        public void Compare_DateColumn_ShouldSortByDate()
        {
            // Arrange
            _listView.Items.Add(new ListViewItem(new[] { "Item1", "10", "2023-12-25" }));
            _listView.Items.Add(new ListViewItem(new[] { "Item2", "20", "2023-01-01" }));
            _listView.Items.Add(new ListViewItem(new[] { "Item3", "30", "2023-06-15" }));

            // Act - Sort by date column
            SimulateColumnClick(2);
            _listView.Sort();

            // Assert - Dates are sorted in reverse (newest first based on implementation)
            Assert.That(_listView.Items[0].SubItems[2].Text, Is.EqualTo("2023-12-25"));
            Assert.That(_listView.Items[1].SubItems[2].Text, Is.EqualTo("2023-06-15"));
            Assert.That(_listView.Items[2].SubItems[2].Text, Is.EqualTo("2023-01-01"));
        }

        [Test]
        public void Compare_MixedColumn_ShouldPutNumbersFirst()
        {
            // Arrange
            _listView.Items.Add(new ListViewItem(new[] { "Text1", "abc", "2023-01-01" }));
            _listView.Items.Add(new ListViewItem(new[] { "Text2", "123", "2023-02-02" }));
            _listView.Items.Add(new ListViewItem(new[] { "Text3", "xyz", "2023-03-03" }));
            _listView.Items.Add(new ListViewItem(new[] { "Text4", "456", "2023-04-04" }));

            // Act - Sort by mixed column
            SimulateColumnClick(1);
            _listView.Sort();

            // Assert - Numbers should be sorted first, then text
            Assert.That(_listView.Items[0].SubItems[1].Text, Is.EqualTo("123"));
            Assert.That(_listView.Items[1].SubItems[1].Text, Is.EqualTo("456"));
            // Text items come after numbers
            var text1Index = Array.FindIndex(_listView.Items.Cast<ListViewItem>().ToArray(),
                item => item.SubItems[1].Text == "abc");
            var text2Index = Array.FindIndex(_listView.Items.Cast<ListViewItem>().ToArray(),
                item => item.SubItems[1].Text == "xyz");
            Assert.That(text1Index, Is.GreaterThan(1));
            Assert.That(text2Index, Is.GreaterThan(1));
        }

        [Test]
        public void Compare_StringColumn_ShouldSortCaseInsensitive()
        {
            // Arrange
            _listView.Items.Add(new ListViewItem(new[] { "charlie", "10", "2023-01-01" }));
            _listView.Items.Add(new ListViewItem(new[] { "ALICE", "20", "2023-02-02" }));
            _listView.Items.Add(new ListViewItem(new[] { "Bob", "30", "2023-03-03" }));

            // Act - Sort by string column
            SimulateColumnClick(0);
            _listView.Sort();

            // Assert - Case-insensitive alphabetical order
            Assert.That(_listView.Items[0].Text.ToLower(), Is.EqualTo("alice"));
            Assert.That(_listView.Items[1].Text.ToLower(), Is.EqualTo("bob"));
            Assert.That(_listView.Items[2].Text.ToLower(), Is.EqualTo("charlie"));
        }

        [Test]
        public void Compare_DecimalNumbers_ShouldSortCorrectly()
        {
            // Arrange
            _listView.Items.Add(new ListViewItem(new[] { "Item1", "10.5", "2023-01-01" }));
            _listView.Items.Add(new ListViewItem(new[] { "Item2", "2.75", "2023-02-02" }));
            _listView.Items.Add(new ListViewItem(new[] { "Item3", "100.25", "2023-03-03" }));

            // Act - Sort by decimal column
            SimulateColumnClick(1);
            _listView.Sort();

            // Assert
            Assert.That(_listView.Items[0].SubItems[1].Text, Is.EqualTo("2.75"));
            Assert.That(_listView.Items[1].SubItems[1].Text, Is.EqualTo("10.5"));
            Assert.That(_listView.Items[2].SubItems[1].Text, Is.EqualTo("100.25"));
        }

        [Test]
        public void Compare_NegativeNumbers_ShouldSortCorrectly()
        {
            // Arrange
            _listView.Items.Add(new ListViewItem(new[] { "Item1", "10", "2023-01-01" }));
            _listView.Items.Add(new ListViewItem(new[] { "Item2", "-5", "2023-02-02" }));
            _listView.Items.Add(new ListViewItem(new[] { "Item3", "0", "2023-03-03" }));

            // Act - Sort by numeric column
            SimulateColumnClick(1);
            _listView.Sort();

            // Assert
            Assert.That(_listView.Items[0].SubItems[1].Text, Is.EqualTo("-5"));
            Assert.That(_listView.Items[1].SubItems[1].Text, Is.EqualTo("0"));
            Assert.That(_listView.Items[2].SubItems[1].Text, Is.EqualTo("10"));
        }

        [Test]
        public void Compare_EmptyStrings_ShouldNotThrow()
        {
            // Arrange
            _listView.Items.Add(new ListViewItem(new[] { "", "10", "2023-01-01" }));
            _listView.Items.Add(new ListViewItem(new[] { "Test", "20", "2023-02-02" }));
            _listView.Items.Add(new ListViewItem(new[] { "", "30", "2023-03-03" }));

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                SimulateColumnClick(0);
                _listView.Sort();
            });
        }

        [Test]
        public void ColumnClick_DifferentColumn_ShouldResetToAscending()
        {
            // Arrange
            _listView.Items.Add(new ListViewItem(new[] { "Charlie", "30", "2023-03-03" }));
            _listView.Items.Add(new ListViewItem(new[] { "Alice", "10", "2023-01-01" }));
            _listView.Items.Add(new ListViewItem(new[] { "Bob", "20", "2023-02-02" }));

            // Act - Click column 0 twice (descending), then click column 1 (should be ascending)
            SimulateColumnClick(0);
            _listView.Sort();
            SimulateColumnClick(0);
            _listView.Sort();
            SimulateColumnClick(1);
            _listView.Sort();

            // Assert - Column 1 should be sorted ascending
            Assert.That(_listView.Items[0].SubItems[1].Text, Is.EqualTo("10"));
            Assert.That(_listView.Items[1].SubItems[1].Text, Is.EqualTo("20"));
            Assert.That(_listView.Items[2].SubItems[1].Text, Is.EqualTo("30"));
        }
    }
}
