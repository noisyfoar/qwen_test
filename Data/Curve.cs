namespace NPFGEO.Data
{
    // Minimal curve implementation required by the dialog view-model.
    public sealed class Curve
    {
        private double _begin;
        private double _delta = 1.0;

        public string Caption { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Units { get; set; } = string.Empty;

        public Curve()
        {
        }

        public Curve(string caption, string units, string description)
        {
            Caption = caption ?? string.Empty;
            Units = units ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public void SetBegin(double begin)
        {
            _begin = begin;
        }

        public double GetBegin()
        {
            return _begin;
        }

        public void SetDelta(double delta)
        {
            _delta = delta;
        }

        public double GetDelta()
        {
            return _delta;
        }
    }
}
