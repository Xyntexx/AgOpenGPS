using System;
using System.Collections;
using System.Windows.Forms;

namespace AgLibrary.Controls
{
    /// <summary>
    /// Smart ListView sorter that handles numeric, date, and string columns.
    /// Automatically detects column types and sorts appropriately.
    /// </summary>
    public class ListViewItemSorter : IComparer
    {
        private readonly CaseInsensitiveComparer objectCompare;
        private readonly ListView listView;

        public ListViewItemSorter(ListView lv)
        {
            listView = lv;
            lv.ListViewItemSorter = this;
            lv.ColumnClick += ListView_ColumnClick;

            SortColumn = 0;
            Order = SortOrder.Ascending;
            objectCompare = new CaseInsensitiveComparer();
        }

        private int SortColumn { get; set; }
        private SortOrder Order { get; set; }

        private void ListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ReverseSortOrderAndSort(e.Column, (ListView)sender);
        }

        public int Compare(object x, object y)
        {
            int compareResult;

            ListViewItem listviewX = (ListViewItem)x;
            ListViewItem listviewY = (ListViewItem)y;

            string textX = listviewX.SubItems[SortColumn].Text;
            string textY = listviewY.SubItems[SortColumn].Text;

            // Try numeric comparison first
            if (decimal.TryParse(textX, out decimal dx) && decimal.TryParse(textY, out decimal dy))
            {
                compareResult = decimal.Compare(dx, dy);
            }
            // Try date comparison
            else if (DateTime.TryParse(textX, out DateTime dtx) && DateTime.TryParse(textY, out DateTime dty))
            {
                compareResult = -DateTime.Compare(dtx, dty);
            }
            // When X is a number but Y is not, put numbers on top
            else if (decimal.TryParse(textX, out _))
            {
                compareResult = -1;
            }
            // When Y is a number but X is not, put numbers on top
            else if (decimal.TryParse(textY, out _))
            {
                compareResult = 1;
            }
            // Default to string comparison
            else
            {
                compareResult = objectCompare.Compare(textX, textY);
            }

            // Apply sort order
            if (Order == SortOrder.Ascending)
                return compareResult;
            else if (Order == SortOrder.Descending)
                return -compareResult;
            else
                return 0;
        }

        private void ReverseSortOrderAndSort(int column, ListView lv)
        {
            if (column == SortColumn)
            {
                // Reverse the current sort direction
                Order = Order == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                // New column, default to ascending
                SortColumn = column;
                Order = SortOrder.Ascending;
            }

            lv.Sort();
        }
    }
}
