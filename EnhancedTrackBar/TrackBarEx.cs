using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using static EnhancedTrackBar.WindowMessages;

namespace EnhancedTrackBar
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    [DefaultProperty("Value")]
    [DefaultEvent("Scroll")]
    [DefaultBindingProperty("Value")]
    [Designer("System.Windows.Forms.Design.TrackBarDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]

    public partial class TrackBarEx : System.Windows.Forms.TrackBar
    {
        private const int
            TBS_ENABLESELRANGE = 0x0020,
            TBS_FIXEDLENGTH = 0x0040,
            TBS_NOTHUMB = 0x0080,
            TBS_TOOLTIPS = 0x0100,
            TBS_NOTIFYBEFOREMOVE = 0x0800,
            TBS_TRANSPARENTBKGND = 0x1000;

        private const int
            TRBN_FIRST = -1501,
            TRBN_LAST = -1519,
            TRBN_THUMBPOSCHANGING = TRBN_FIRST - 1;

        private enum TrackBarMessages
        {
            TBM_GETTIC = WM_USER + 3,

            TBM_SETSEL = WM_USER + 10,
            TBM_SETSELSTART = WM_USER + 11,
            TBM_SETSELEND = WM_USER + 12,
            TBM_GETPTICS = WM_USER + 14,
            TBM_GETTICPOS = WM_USER + 15,
            TBM_GETNUMTICS = WM_USER + 16,

            TBM_CLEARSEL = WM_USER + 19,

            TBM_SETTHUMBLENGTH = WM_USER + 27,
            TBM_GETTHUMBLENGTH = WM_USER + 28,
            TBM_SETTOOLTIPS = WM_USER + 29,
            TBM_GETTOOLTIPS = WM_USER + 30,

            TBM_STPOSNOTIFY = WM_USER + 34,
        }

        private readonly CancelEventArgs cancelEventArgs = new CancelEventArgs();
        private bool transparentBackground;
        private bool showSelectionRange;
        private int selectionStart;
        private int selectionEnd;
        private int fixedThumbLength;
        private TrackBarThumbStyle thumbStyle;

        private static readonly object EVENT_PREVIEW_VALUE_CHANGE = new object();
        private static readonly object EVENT_NO_THUMB = new object();
        private static readonly object EVENT_SHOW_SELECTION_RANGE = new object();
        private static readonly object EVENT_SELECTION_START = new object();
        private static readonly object EVENT_SELECTION_END = new object();
        private static readonly object EVENT_SHOW_TOOLTIP = new object();

        public event EventHandler<PreviewValueChangeEventArgs> PreviewValueChange
        {
            add { Events.AddHandler(EVENT_PREVIEW_VALUE_CHANGE, value); }
            remove { Events.RemoveHandler(EVENT_PREVIEW_VALUE_CHANGE, value); }
        }

        public event EventHandler NoThumbChanged
        {
            add { Events.AddHandler(EVENT_NO_THUMB, value); }
            remove { Events.RemoveHandler(EVENT_NO_THUMB, value); }
        }

        public event EventHandler ShowSelectionRangeChanged
        {
            add { Events.AddHandler(EVENT_SHOW_SELECTION_RANGE, value); }
            remove { Events.RemoveHandler(EVENT_SHOW_SELECTION_RANGE, value); }
        }

        public event EventHandler SelectionStartChanged
        {
            add { Events.AddHandler(EVENT_SELECTION_START, value); }
            remove { Events.RemoveHandler(EVENT_SELECTION_START, value); }
        }

        public event EventHandler SelectionEndChanged
        {
            add { Events.AddHandler(EVENT_SELECTION_END, value); }
            remove { Events.RemoveHandler(EVENT_SELECTION_END, value); }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NMHDR
        {
            public IntPtr hwndFrom;
            public UIntPtr idFrom;
            public uint code;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NMTRBTHUMBPOSCHANGING
        {
            public NMHDR hdr;

            public uint dwPos;
            public int nReason;
        }
        private bool ProcessThumbPosChanging(uint dwPos, int nReason)
        {
            System.Diagnostics.Debug.WriteLine($"dwPos = {dwPos}, nReason={(Reason)nReason}, Value={Value}");

            return OnPreviewValueChange((int)dwPos);
        }

        private readonly PreviewValueChangeEventArgs previewValueChangeEventArgs = new PreviewValueChangeEventArgs();

        private bool OnPreviewValueChange(int newValue)
        {
            previewValueChangeEventArgs.Cancel = false;
            previewValueChangeEventArgs.NewValue = newValue;
            (Events[EVENT_PREVIEW_VALUE_CHANGE] as EventHandler<PreviewValueChangeEventArgs>)?.Invoke(this, previewValueChangeEventArgs);

            return previewValueChangeEventArgs.Cancel;
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_MOUSEWHEEL:
                case WM_MOUSEHWHEEL:
                    WmMouseWheel(ref m);
                    break;
                //return;
                case WM_REFLECT_NOTIFY:
                    var hdr = m.GetLParam<NMHDR>();
                    if ((int)hdr.code == TRBN_THUMBPOSCHANGING && this.Handle == hdr.hwndFrom)
                    {
                        var lpNMTrbThumbPosChanging = m.GetLParam<NMTRBTHUMBPOSCHANGING>();
                        if (ProcessThumbPosChanging(lpNMTrbThumbPosChanging.dwPos, lpNMTrbThumbPosChanging.nReason))
                        {
                            m.Result = (IntPtr)1;
                            return;
                        }
                    }

                    break;
            }
            base.WndProc(ref m);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var param = base.CreateParams;
                param.Style |= TBS_NOTIFYBEFOREMOVE;
                param.Style |= ThumbStyle == TrackBarThumbStyle.None ? TBS_NOTHUMB : 0;
                param.Style |= ThumbStyle == TrackBarThumbStyle.FixedLength ? TBS_FIXEDLENGTH : 0;
                param.Style |= ShowSelectionRange ? TBS_ENABLESELRANGE : 0;
                param.Style |= TransparentBackground ? TBS_TRANSPARENTBKGND : 0;
                return param;
            }
        }
        private void WmMouseWheel(ref Message m)
        {
            var (lo, hi) = LParam.GetLoHi(m.LParam);
            Point p = new Point((short)lo, hi);
            p = PointToClient(p);
            var (_, delta) = LParam.GetLoHi(m.WParam);
            HandledMouseEventArgs e = new HandledMouseEventArgs(MouseButtons.None,
                                                                0,
                                                                p.X,
                                                                p.Y,
                                                                delta);
            OnMouseWheel(e);
            m.Result = (IntPtr)(e.Handled ? 0 : 1);
            if (!e.Handled)
            {
                DefWndProc(ref m);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (e is HandledMouseEventArgs hme)
            {
                if (hme.Handled)
                {
                    return;
                }
                hme.Handled = true;
            }

            if ((ModifierKeys & (Keys.Shift | Keys.Alt)) != 0 || MouseButtons != MouseButtons.None)
            {
                return;
            }

            int wheelScrollLines = SystemInformation.MouseWheelScrollLines;
            if (wheelScrollLines == 0 || e.Delta == 0)
            {
                return;
            }

            var newValue = Value + (e.Delta < 0 ? -1 : 1);

            if (Minimum <= newValue && newValue <= Maximum)
            {
                if (OnPreviewValueChange(newValue))
                {
                    return;
                }

                Value = newValue;
            }
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (ShowSelectionRange)
            {
                SendMessage(TrackBarMessages.TBM_SETSELSTART, default, selectionStart);
                SendMessage(TrackBarMessages.TBM_SETSELEND, default, SelectionEnd);
            }

            if (fixedThumbLength != 0)
                SendMessage(TrackBarMessages.TBM_SETTHUMBLENGTH, (IntPtr)fixedThumbLength, default);
        }

        [Category("Appearance")]
        [DefaultValue(false)]
        public bool TransparentBackground
        {
            get => transparentBackground;
            set
            {
                if (transparentBackground != value)
                {
                    if (IsHandleCreated)
                    {
                        var style = NativeMethods.GetWindowLong(HandleRef(), GetWindowLongItemIndex.GWL_STYLE);
                        style = (style & ~TBS_TRANSPARENTBKGND) | (value ? TBS_TRANSPARENTBKGND : 0);
                        if (NativeMethods.SetWindowLong(HandleRef(), GetWindowLongItemIndex.GWL_STYLE, style) == 0)
                        {
                            throw new Win32Exception();
                        }
                    }
                    transparentBackground = value;
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool ShowSelectionRange
        {
            get => showSelectionRange;
            set
            {
                if (showSelectionRange != value)
                {
                    if (IsHandleCreated)
                    {
                        var style = NativeMethods.GetWindowLong(HandleRef(), GetWindowLongItemIndex.GWL_STYLE);
                        style = (style & ~TBS_ENABLESELRANGE) | (value ? TBS_ENABLESELRANGE : 0);
                        if (NativeMethods.SetWindowLong(HandleRef(), GetWindowLongItemIndex.GWL_STYLE, style) == 0)
                        {
                            throw new Win32Exception();
                        }
                        Invalidate();
                    }
                    showSelectionRange = value;
                    (Events[EVENT_SHOW_SELECTION_RANGE] as EventHandler)?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(0)]
        public int SelectionStart
        {
            get => selectionStart;
            set
            {
                if (selectionStart != value)
                {
                    SendMessage(TrackBarMessages.TBM_SETSELSTART, WParam.FromBool(true), value);
                    selectionStart = value;
                    (Events[EVENT_SELECTION_START] as EventHandler)?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [Category("Behavior")]
        [DefaultValue(0)]
        public int SelectionEnd
        {
            get => selectionEnd;
            set
            {
                if (selectionEnd != value)
                {
                    SendMessage(TrackBarMessages.TBM_SETSELEND, WParam.FromBool(true), value);
                    selectionEnd = value;
                    (Events[EVENT_SELECTION_END] as EventHandler)?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(TrackBarThumbStyle.Default)]
        public TrackBarThumbStyle ThumbStyle
        {
            get => thumbStyle;
            set
            {
                if (thumbStyle != value)
                {
                    if (IsHandleCreated)
                    {
                        var style = NativeMethods.GetWindowLong(HandleRef(), GetWindowLongItemIndex.GWL_STYLE);
                        style = (style & ~TBS_NOTHUMB) | (value == TrackBarThumbStyle.None ? TBS_NOTHUMB : 0);
                        style = (style & ~TBS_FIXEDLENGTH) | (value == TrackBarThumbStyle.FixedLength ? TBS_FIXEDLENGTH : 0);
                        if (NativeMethods.SetWindowLong(HandleRef(), GetWindowLongItemIndex.GWL_STYLE, style) == 0)
                        {
                            throw new Win32Exception();
                        }
                    }
                    thumbStyle = value;
                    (Events[EVENT_NO_THUMB] as EventHandler)?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(0)]
        public int FixedThumbLength
        {
            get => fixedThumbLength;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (fixedThumbLength != value)
                {
                    if (value != 0)
                        SendMessage(TrackBarMessages.TBM_SETTHUMBLENGTH, (IntPtr)value, default);
                    fixedThumbLength = value;
                }
            }
        }

        public void ClearSeleciton(bool redraw = true)
        {
            SendMessage(TrackBarMessages.TBM_CLEARSEL, WParam.FromBool(redraw), default);
        }

        private HandleRef HandleRef() => new HandleRef(this, Handle);

        private IntPtr SendMessage(TrackBarMessages msg, IntPtr wparam, int lparam)
        {
            if (IsHandleCreated)
            {
                return NativeMethods.SendMessage(HandleRef(), (int)msg, wparam, (IntPtr)lparam);
            }
            return default;
        }

        private bool useNativeTooltips;

        [DefaultValue(false)]
        public bool UseNativeTooltips
        {
            get => useNativeTooltips;
            set
            {
                if (useNativeTooltips != value)
                {
                    if (IsHandleCreated)
                    {
                        var style = NativeMethods.GetWindowLong(HandleRef(), GetWindowLongItemIndex.GWL_STYLE);
                        style = (style & ~TBS_TOOLTIPS) | (value ? TBS_TOOLTIPS : 0);
                        if (NativeMethods.SetWindowLong(HandleRef(), GetWindowLongItemIndex.GWL_STYLE, style) == 0)
                        {
                            throw new Win32Exception();
                        }
                    }
                    useNativeTooltips = value;
                    (Events[EVENT_SHOW_TOOLTIP] as EventHandler)?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [DefaultValue(null)]
        public NativeWindow NativeTooltips
        {
            get
            {
                return NativeWindow.FromHandle(SendMessage(TrackBarMessages.TBM_GETTOOLTIPS, default, default));
            }
            set
            {
                IntPtr handle = (value?.Handle) ?? default;
                SendMessage(TrackBarMessages.TBM_SETTOOLTIPS, handle, default);
            }
        }
    }

    public enum TrackBarThumbStyle
    {
        Default,
        None,
        FixedLength,
    }

    internal enum Reason
    {
        TB_LINEUP = 0,
        TB_LINEDOWN = 1,
        TB_PAGEUP = 2,
        TB_PAGEDOWN = 3,
        TB_THUMBPOSITION = 4,
        TB_THUMBTRACK = 5,
        TB_TOP = 6,
        TB_BOTTOM = 7,
        TB_ENDTRACK = 8,
    }
}
