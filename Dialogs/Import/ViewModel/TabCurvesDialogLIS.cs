using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ShellExtension.Formats.LIS.Dialogs.Import.ViewModel
{
    public class TabCurvesDialogLIS : ViewModelBase
    {
        public class CurveInfo
        {
            public string Units { get; set; }
            public string Description { get; set; }
            public string Mnemonics { get; set; }
        }

        public class Item
        {
            public string Name { get; set; }
            public bool IsEnabled { get; set; }
            public bool CanEnabled { get; set; } = true;
            public CurveInfo Curve { get; set; } = new CurveInfo();
        }

        private readonly ObservableCollection<Item> _source = new ObservableCollection<Item>();
        private string _searchText;

        public ObservableCollection<Item> Available { get; } = new ObservableCollection<Item>();
        public ObservableCollection<Item> Selected { get; } = new ObservableCollection<Item>();

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                CallPropertyChanged(nameof(SearchText));
                RebuildAvailable();
            }
        }

        public bool CanApply()
        {
            return Selected.Count > 0;
        }

        public bool CanSelectAll()
        {
            return Available.Count > 0;
        }

        public void SelectAll()
        {
            foreach (var item in Available.ToArray())
            {
                if (!Selected.Contains(item))
                    Selected.Add(item);
            }

            RebuildAvailable();
        }

        public bool CanUnselectAll()
        {
            return Selected.Count > 0;
        }

        public void UnselectAll()
        {
            Selected.Clear();
            RebuildAvailable();
        }

        public void Up(Item item)
        {
            if (item == null)
                return;

            var index = Selected.IndexOf(item);
            if (index > 0)
                Selected.Move(index, index - 1);
        }

        public void Down(Item item)
        {
            if (item == null)
                return;

            var index = Selected.IndexOf(item);
            if (index >= 0 && index + 1 < Selected.Count)
                Selected.Move(index, index + 1);
        }

        public void AddCurve(IEnumerable<Item> items)
        {
            if (items == null)
                return;

            foreach (var item in items.ToArray())
            {
                if (item != null && !Selected.Contains(item))
                    Selected.Add(item);
            }

            RebuildAvailable();
        }

        public void RemoveCurve(IEnumerable<Item> items)
        {
            if (items == null)
                return;

            foreach (var item in items.ToArray())
            {
                if (item != null)
                    Selected.Remove(item);
            }

            RebuildAvailable();
        }

        public IReadOnlyList<Item> GetSelectedItems()
        {
            return Selected.ToArray();
        }

        private void RebuildAvailable()
        {
            Available.Clear();

            IEnumerable<Item> query = _source.Where(item => !Selected.Contains(item));

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var text = _searchText.ToLowerInvariant();
                query = query.Where(i =>
                    (i.Name ?? string.Empty).ToLowerInvariant().Contains(text)
                    || (i.Curve?.Mnemonics ?? string.Empty).ToLowerInvariant().Contains(text)
                    || (i.Curve?.Description ?? string.Empty).ToLowerInvariant().Contains(text));
            }

            foreach (var item in query)
                Available.Add(item);
        }
    }
}
