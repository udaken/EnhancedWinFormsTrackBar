using System;
namespace EnhancedTrackBar
{
    public sealed class PreviewValueChangeEventArgs : EventArgs
    {
        public int NewValue { get; internal set; }
        public bool Cancel { get; set; }
    }

}
