using MathCore.WinAPI.Windows;

using OnTopReplica.Native;
using OnTopReplica.Properties;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsAero.TaskDialog;

using Screen = MathCore.WinAPI.Windows.Screen;
using Timer = System.Windows.Forms.Timer;

namespace OnTopReplica {
    //Contains some feature implementations of MainForm
    partial class MainForm {

        #region Click forwarding

        public bool ClickForwardingEnabled {
            get {
                return _thumbnailPanel.ReportThumbnailClicks;
            }
            set {
                if (value && Settings.Default.FirstTimeClickForwarding) {
                    TaskDialog dlg = new TaskDialog(Strings.InfoClickForwarding, Strings.InfoClickForwardingTitle, Strings.InfoClickForwardingContent) {
                        CommonButtons = CommonButton.Yes | CommonButton.No
                    };
                    if (dlg.Show(this).CommonButton == CommonButtonResult.No)
                        return;

                    Settings.Default.FirstTimeClickForwarding = false;
                }

                _thumbnailPanel.ReportThumbnailClicks = value;
            }
        }

        #endregion

        #region Click-through

        bool _clickThrough = false;

        readonly Color DefaultNonClickTransparencyKey;

        public bool ClickThroughEnabled {
            get {
                return _clickThrough;
            }
            set {
                TransparencyKey = (value) ? Color.Black : DefaultNonClickTransparencyKey;
                if (value) {
                    //Re-force as top most (always helps in some cases)
                    TopMost = false;
                    this.Activate();
                    TopMost = true;
                }

                _clickThrough = value;
            }
        }

        //Must NOT be equal to any other valid opacity value
        const double ClickThroughHoverOpacity = 0.6;

        Timer _clickThroughComeBackTimer = null;
        long _clickThroughComeBackTicks;
        const int ClickThroughComeBackTimerInterval = 1000;

        /// <summary>
        /// When the mouse hovers over a fully opaque click-through form,
        /// this fades the form to semi-transparency
        /// and starts a timeout to get back to full opacity.
        /// </summary>
        private void RefreshClickThroughComeBack() {
            if (this.Opacity == 1.0) {
                this.Opacity = ClickThroughHoverOpacity;
            }

            if (_clickThroughComeBackTimer == null) {
                _clickThroughComeBackTimer = new Timer();
                _clickThroughComeBackTimer.Tick += _clickThroughComeBackTimer_Tick;
                _clickThroughComeBackTimer.Interval = ClickThroughComeBackTimerInterval;
            }
            _clickThroughComeBackTicks = DateTime.UtcNow.Ticks;
            _clickThroughComeBackTimer.Start();
        }

        void _clickThroughComeBackTimer_Tick(object sender, EventArgs e) {
            var diff = DateTime.UtcNow.Subtract(new DateTime(_clickThroughComeBackTicks));
            if (diff.TotalSeconds > 2) {
                var mousePointer = WindowMethods.GetCursorPos();

                if (!this.ContainsMousePointer(mousePointer)) {
                    if (this.Opacity == ClickThroughHoverOpacity) {
                        this.Opacity = 1.0;
                    }
                    _clickThroughComeBackTimer.Stop();
                }
            }
        }

        #endregion

        #region Chrome

        readonly FormBorderStyle DefaultBorderStyle; // = FormBorderStyle.Sizable; // FormBorderStyle.SizableToolWindow;

        public bool IsChromeVisible {
            get {
                return (FormBorderStyle == DefaultBorderStyle);
            }
            set {
                //Cancel hiding chrome if no thumbnail is shown
                if (!value && !_thumbnailPanel.IsShowingThumbnail)
                    return;

                if (!value) {
                    Location = new Point {
                        X = Location.X + SystemInformation.FrameBorderSize.Width,
                        Y = Location.Y + SystemInformation.FrameBorderSize.Height
                    };
                    FormBorderStyle = FormBorderStyle.None;
                }
                else if(value) {
                    Location = new Point {
                        X = Location.X - SystemInformation.FrameBorderSize.Width,
                        Y = Location.Y - SystemInformation.FrameBorderSize.Height
                    };
                    FormBorderStyle = DefaultBorderStyle;
                }

                Program.Platform.OnFormStateChange(this);
                Invalidate();
            }
        }

        #endregion

        #region Position lock

        ScreenPosition? _positionLock = null;

        /// <summary>
        /// Gets or sets the screen position where the window is currently locked in.
        /// </summary>
        public ScreenPosition? PositionLock {
            get {
                return _positionLock;
            }
            set {
                if (value != null)
                    this.SetScreenPosition(value.Value);

                _positionLock = value;
            }
        }

        /// <summary>
        /// Refreshes window position if in lock mode.
        /// </summary>
        private void RefreshScreenLock() {
            //If locked in position, move accordingly
            if (PositionLock.HasValue) {
                this.SetScreenPosition(PositionLock.Value);
            }
        }

        #endregion

        #region Color alert

        private bool _ColorAlertEnable;

        public bool ColorAlertEnable {
            get => _ColorAlertEnable;
            set {
                if(value == _ColorAlertEnable) return;
                _ColorAlertEnable = value;

                _ColoeAlertCancelation?.Cancel();

                if(!value) return;

                var cancellation = new CancellationTokenSource();
                _ColoeAlertCancelation = cancellation;

                var t = ColorAlertWatchAsync(cancellation.Token);

                t.OnCancelled(() => Debug.WriteLine("Capture screen process stoped"));
            }
        }

        public int ColorAlertTimeout { get; set; } = 150;

        public Color ColorAlertColor { get; set; } = Color.Red;

        private CancellationTokenSource _ColoeAlertCancelation;

        private async Task ColorAlertWatchAsync(CancellationToken Cancel) {

            var screen = Screen.FromHandle(this.Handle);

            Debug.WriteLine("Capture screen process started");

            var client_rect = ClientRectangle;

            byte[] pixels = null;
            var (width, height) = (Width, Height);
            var dx = width - client_rect.Width;
            var dy = height - client_rect.Height;

            var (dx2, dy2) = (dx / 2, dy / 2);

            var bmp = new Bitmap(width - dx, height - dy);

            while(!Cancel.IsCancellationRequested) {
                await Task.Delay(ColorAlertTimeout).ConfigureAwait(false);

                (width, height) = (Width - dx, Height - dy);
                if(width != bmp.Width || height != bmp.Height) {
                    bmp?.Dispose();
                    bmp = null;

                    Debug.WriteLine("Reset buffer img");
                }

                if(bmp is null) {
                    bmp = new Bitmap(width, height);

                    Debug.WriteLine("Update buffer image {0}x{1}", width, height);
                }

                using(var g = Graphics.FromImage(bmp))
                    g.CopyFromScreen(Left + dx2, Top + dy2, 0, 0, bmp.Size);

                TestForm.Instance.View(bmp);

                Debug.WriteLine("Capture screen at {0},{1} -> {1}x{2}", Left, Top, width, height);

                var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, bmp.PixelFormat);
                try {
                    var bytes_count = data.Stride * height;
                    if(pixels is null || pixels.Length != bytes_count)
                        pixels = new byte[bytes_count];

                    Marshal.Copy(data.Scan0, pixels, 0, bytes_count);
                }
                finally {
                    bmp.UnlockBits(data);
                }

                if(!CheckImage(pixels, ColorAlertColor)) continue;

                MessageBox.Show("Find!");
            }

            Cancel.ThrowIfCancellationRequested();
        }

        private static void CopyPixels(Bitmap bmp, ref byte[] pixels) {
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            try {
                var bytes_count = data.Stride * bmp.Height;
                if(pixels is null || pixels.Length != bytes_count)
                    pixels = new byte[bytes_count];

                Marshal.Copy(data.Scan0, pixels, 0, bytes_count);
            }
            finally {
                bmp.UnlockBits(data);
            }
        } 

        private static bool CheckImage(byte[] pixels, Color color) =>
            MemoryMarshal.Cast<byte, int>(pixels).IndexOf(color.ToArgb()) >= 0;

        #endregion
    }
}
