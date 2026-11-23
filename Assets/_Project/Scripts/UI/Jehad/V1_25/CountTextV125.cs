namespace Echoheart.UI
{
    /// <summary>One-entry text cache for the fixed-wording UI counters; rebuilds when the number changes.</summary>
    public sealed class CountTextV125
    {
        private readonly string _prefix, _suffix;
        private readonly bool _hideNonPositive;
        private bool _ready;
        private int _value;
        private string _text = string.Empty;

        public CountTextV125(string prefix, string suffix, bool hideNonPositive)
        {
            _prefix = prefix ?? string.Empty;
            _suffix = suffix ?? string.Empty;
            _hideNonPositive = hideNonPositive;
        }

        public string Format(int value)
        {
            if (_ready && _value == value) return _text;
            _ready = true;
            _value = value;
            _text = _hideNonPositive && value <= 0 ? string.Empty : _prefix + value + _suffix;
            return _text;
        }
    }
}
