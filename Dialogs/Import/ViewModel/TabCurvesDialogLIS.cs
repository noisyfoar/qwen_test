
using NPFGEO.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NPFGEO.ShellExtension.Formats.LIS.Dialogs.Import.ViewModel
{
    public class TabCurvesDialogLIS : ViewModelBase
    {
        public ObservableCollection<Curve> AvailableCurves { private set; get; } = new ObservableCollection<Curve>();
        public ObservableCollection<Curve> SelectedCurves { private set; get; } = new ObservableCollection<Curve>();

        ObservableCollection<Curve> _ignoredCurves;

        ObservableCollection<Curve> _sourceCurves;

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                CallPropertyChanged(nameof(SearchText));
                FilterCurves();
            }
        }
        public TabCurvesDialogLIS(ObservableCollection<Curve> source)
        {
            _sourceCurves = source;

            foreach (var curve in source)
                AvailableCurves.Add(curve);
        }

        public TabCurvesDialogLIS(IEnumerable<Curve> source, IEnumerable<Curve> ignoredItems)
        {
            if (ignoredItems.Any(i => !source.Contains(i)))
                throw new System.Exception($"Множество {nameof(ignoredItems)} должно быть подмножеством {nameof(source)}");

            foreach (var curve in source)
            {
                _sourceCurves.Add(curve);
                AvailableCurves.Add(curve);
            }

            foreach (var curve in ignoredItems)
            {
                _ignoredCurves.Add(curve);
            }

        }
        void FilterCurves()
        {
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                AvailableCurves.Clear();

                var found = _sourceCurves.Where(s => s.Mnemonics.ToLower().Contains(_searchText.ToLower()) &&
                                               !SelectedCurves.Any(z => z.Mnemonics.ToLower() == s.Mnemonics.ToLower()))
                                   .Select(i => i);

                foreach (var curve in found)
                {
                    AvailableCurves.Add(curve);
                }
            }
            else
            {
                AvailableCurves.Clear();

                var unselected = _sourceCurves.Except(SelectedCurves);

                foreach (var curve in unselected)
                {
                    AvailableCurves.Add(curve);
                }
            }
        }





        public bool CanSelectAll()
        {
            var result = AvailableCurves.Count > 0;
            return result;
        }

        public void SelectAll()
        {
            if (_ignoredCurves == null)
            {
                foreach (var curve in AvailableCurves)
                {
                    SelectedCurves.Add(curve);
                }

                AvailableCurves.Clear();
                return;
            }

            bool selectIgnored =
                AvailableCurves.Intersect(_ignoredCurves).Count() == AvailableCurves.Count();

            foreach (var curve in AvailableCurves)
            {
                if (selectIgnored || !_ignoredCurves.Contains(curve))
                    SelectedCurves.Add(curve);
            }

            AvailableCurves.Clear();

            if (!selectIgnored)
                foreach (var curve in _ignoredCurves)
                    AvailableCurves.Add(curve);
        }

        public bool CanUnselectAll()
        {
            var result = SelectedCurves.Count > 0;
            return result;
        }

        public void UnselectAll()
        {
            foreach (var curve in SelectedCurves)
            {
                AvailableCurves.Add(curve);
            }
            SelectedCurves.Clear();
        }

        public bool CanApply()
        {
            return SelectedCurves.Count > 0;
        }

        public void Up(Curve curve)
        {
            var index = SelectedCurves.IndexOf(curve);
            if (index > 0)
            {
                SelectedCurves.Move(index, index - 1);
            }
        }

        public void Down(Curve curve)
        {
            var index = SelectedCurves.IndexOf(curve);

            if (index + 1 < SelectedCurves.Count())
            {
                SelectedCurves.Move(index, index + 1);
            }
        }

        public void AddCurve(IEnumerable<Curve> items)
        {
            foreach (var curve in items)
            {
                SelectedCurves.Add(curve);
                AvailableCurves.Remove(curve);
            }
        }

        public void RemoveCurve(IEnumerable<Curve> items)
        {
            foreach (var curve in items)
            {
                SelectedCurves.Remove(curve);
                AvailableCurves.Add(curve);
            }
        }
    }

}
