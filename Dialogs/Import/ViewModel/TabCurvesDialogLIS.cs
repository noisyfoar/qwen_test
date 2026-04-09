using NPFGEO.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.ViewModel
{
    public class TabCurvesDialogLIS : ViewModelBase
    {
        private readonly List<Curve> _sourceCurves = new List<Curve>();
        private string _searchText;

        // Имена свойств совпадают с текущими биндингами View.
        public ObservableCollection<Curve> Available { get; } = new ObservableCollection<Curve>();
        public ObservableCollection<Curve> Selected { get; } = new ObservableCollection<Curve>();

        // Совместимость с уже существующим кодом.
        public ObservableCollection<Curve> AvailableCurves => Available;
        public ObservableCollection<Curve> SelectedCurves => Selected;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (string.Equals(_searchText, value, StringComparison.Ordinal))
                    return;

                _searchText = value;
                CallPropertyChanged(nameof(SearchText));
                RebuildAvailable();
            }
        }

        public TabCurvesDialogLIS(Curves source)
            : this(source, null)
        {
        }

        public TabCurvesDialogLIS(Curves source, Curves preselected)
        {
            if (source != null)
                _sourceCurves.AddRange(source);

            if (preselected != null)
            {
                foreach (var curve in preselected)
                {
                    var sourceCurve = ResolveSourceCurve(curve);
                    if (sourceCurve != null && !Selected.Contains(sourceCurve))
                        Selected.Add(sourceCurve);
                }
            }

            RebuildAvailable();
        }

        public Curves GetSelectedSourceCurves()
        {
            var result = new Curves();
            result.AddRange(Selected);
            return result;
        }

        public bool CanSelectAll()
        {
            return Available.Count > 0;
        }

        public void SelectAll()
        {
            foreach (var curve in Available.ToArray())
            {
                if (!Selected.Contains(curve))
                    Selected.Add(curve);
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

        public bool CanApply()
        {
            return Selected.Count > 0;
        }

        public void Up(Curve curve)
        {
            if (curve == null)
                return;

            var index = Selected.IndexOf(curve);
            if (index > 0)
                Selected.Move(index, index - 1);
        }

        public void Down(Curve curve)
        {
            if (curve == null)
                return;

            var index = Selected.IndexOf(curve);
            if (index >= 0 && index + 1 < Selected.Count)
                Selected.Move(index, index + 1);
        }

        public void AddCurve(IEnumerable<Curve> items)
        {
            if (items == null)
                return;

            foreach (var curve in items.ToArray())
            {
                if (curve != null && !Selected.Contains(curve))
                    Selected.Add(curve);
            }

            RebuildAvailable();
        }

        public void RemoveCurve(IEnumerable<Curve> items)
        {
            if (items == null)
                return;

            foreach (var curve in items.ToArray())
            {
                if (curve != null)
                    Selected.Remove(curve);
            }

            RebuildAvailable();
        }

        private void RebuildAvailable()
        {
            Available.Clear();

            IEnumerable<Curve> query = _sourceCurves.Where(curve => !Selected.Contains(curve));

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var text = _searchText.Trim();
                query = query.Where(curve => MatchCurve(curve, text));
            }

            foreach (var curve in query)
                Available.Add(curve);
        }

        private Curve ResolveSourceCurve(Curve curve)
        {
            if (curve == null)
                return null;

            if (_sourceCurves.Contains(curve))
                return curve;

            // Фоллбэк для случаев, когда в preselected пришли клоны.
            return _sourceCurves.FirstOrDefault(c =>
                string.Equals(c?.Mnemonics, curve.Mnemonics, StringComparison.OrdinalIgnoreCase));
        }

        private static bool MatchCurve(Curve curve, string text)
        {
            if (curve == null)
                return false;

            return ContainsIgnoreCase(curve.Mnemonics, text)
                   || ContainsIgnoreCase(curve.Caption, text)
                   || ContainsIgnoreCase(curve.Name, text)
                   || ContainsIgnoreCase(curve.Description, text);
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
                return false;

            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
