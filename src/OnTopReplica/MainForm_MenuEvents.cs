using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using OnTopReplica.Properties;
using WindowsFormsAero.TaskDialog;
using OnTopReplica.SidePanels;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace OnTopReplica {
    partial class MainForm {

        private void Menu_opened(object sender, EventArgs e) {

        }

        private void Menu_opening(object sender, CancelEventArgs e) {
            //Cancel if currently in "fullscreen" mode or a side panel is open
            if (FullscreenManager.IsFullscreen || IsSidePanelOpen) {
                e.Cancel = true;
                return;
            }

            bool showing = _thumbnailPanel.IsShowingThumbnail;

            selectRegionToolStripMenuItem.Enabled = showing;
            switchToWindowToolStripMenuItem.Enabled = showing;
            resizeToolStripMenuItem.Enabled = showing;
            chromeToolStripMenuItem.Checked = IsChromeVisible;
            clickForwardingToolStripMenuItem.Checked = ClickForwardingEnabled;
            chromeToolStripMenuItem.Enabled = showing;
            clickThroughToolStripMenuItem.Enabled = showing;
            clickForwardingToolStripMenuItem.Enabled = showing;

            var settings = Settings.Default;
            alertTimeoutToolStripTextBox.Text = settings.ColorAlertTimeout.ToString();
            ColorAlertColor = settings.ColorAlertColor;

            ColorAlertRepeatCount = settings.ColorAlertRepeatCount;
            ColorAlertRepeatCountEnabled = settings.ColorAlertRepeatCountEnable;

            FuzzyEqualThreshold = settings.ColorAlertFuzzyEqualThreshold;
            ColorAlertFuzzyEqual = settings.ColorAlertFuzzyEqual;
        }

        private void Menu_Switch_click(object sender, EventArgs e) {
            if (CurrentThumbnailWindowHandle == null)
                return;

            Program.Platform.HideForm(this);
            Native.WindowManagerMethods.SetForegroundWindow(CurrentThumbnailWindowHandle.Handle);
        }

        private void Menu_Advanced_opening(object sender, EventArgs e) {
            restoreLastClonedWindowToolStripMenuItem.Checked = Settings.Default.RestoreLastWindow;
        }

        private void Menu_GroupSwitchMode_click(object sender, EventArgs e) {
            SetSidePanel(new SidePanels.GroupSwitchPanel());
        }

        private void Menu_RestoreLastWindow_click(object sender, EventArgs e) {
            Settings.Default.RestoreLastWindow = !Settings.Default.RestoreLastWindow;
        }

        private void Menu_ClickForwarding_click(object sender, EventArgs e) {
            ClickForwardingEnabled = !ClickForwardingEnabled;
        }

        private void Menu_ClickThrough_click(object sender, EventArgs e) {
            ClickThroughEnabled = true;
        }

        private void Menu_Opacity_opening(object sender, CancelEventArgs e) {
            ToolStripMenuItem[] items = {
                toolStripMenuItem1,
                toolStripMenuItem2,
                toolStripMenuItem3,
                toolStripMenuItem4
            };

            foreach (ToolStripMenuItem i in items) {
                if (((double)i.Tag) == this.Opacity)
                    i.Checked = true;
                else
                    i.Checked = false;
            }
        }

        private void Menu_Opacity_click(object sender, EventArgs e) {
            ToolStripMenuItem tsi = (ToolStripMenuItem)sender;

            if (this.Visible) {
                //Target opacity is stored in the item's tag
                this.Opacity = (double)tsi.Tag;
                Program.Platform.OnFormStateChange(this);
            }
        }

        private void Menu_Region_click(object sender, EventArgs e) {
            SetSidePanel(new OnTopReplica.SidePanels.RegionPanel());
        }

        private void Menu_Resize_opening(object sender, CancelEventArgs e) {
            if (!_thumbnailPanel.IsShowingThumbnail)
                e.Cancel = true;

            restorePositionAndSizeToolStripMenuItem.Checked = Settings.Default.RestoreSizeAndPosition;
        }

        private void Menu_Resize_Double(object sender, EventArgs e) {
            FitToThumbnail(2.0);
        }

        private void Menu_Resize_FitToWindow(object sender, EventArgs e) {
            FitToThumbnail(1.0);
        }

        private void Menu_Resize_Half(object sender, EventArgs e) {
            FitToThumbnail(0.5);
        }

        private void Menu_Resize_Quarter(object sender, EventArgs e) {
            FitToThumbnail(0.25);
        }

        private void Menu_Resize_Fullscreen(object sender, EventArgs e) {
            FullscreenManager.SwitchFullscreen();
        }

        private void Menu_Resize_RecallPosition_click(object sender, EventArgs e) {
            Settings.Default.RestoreSizeAndPosition = !Settings.Default.RestoreSizeAndPosition;
        }

        private void Menu_Position_Opening(object sender, EventArgs e) {
            disabledToolStripMenuItem.Checked = (PositionLock == null);
            topLeftToolStripMenuItem.Checked = (PositionLock == ScreenPosition.TopLeft);
            topRightToolStripMenuItem.Checked = (PositionLock == ScreenPosition.TopRight);
            centerToolStripMenuItem.Checked = (PositionLock == ScreenPosition.Center);
            bottomLeftToolStripMenuItem.Checked = (PositionLock == ScreenPosition.BottomLeft);
            bottomRightToolStripMenuItem.Checked = (PositionLock == ScreenPosition.BottomRight);
        }

        private void Menu_Position_Disable(object sender, EventArgs e) {
            PositionLock = null;
        }

        private void Menu_Position_TopLeft(object sender, EventArgs e) {
            PositionLock = ScreenPosition.TopLeft;
        }

        private void Menu_Position_TopRight(object sender, EventArgs e) {
            PositionLock = ScreenPosition.TopRight;
        }

        private void Menu_Position_Center(object sender, EventArgs e) {
            PositionLock = ScreenPosition.Center;
        }

        private void Menu_Position_BottomLeft(object sender, EventArgs e) {
            PositionLock = ScreenPosition.BottomLeft;
        }

        private void Menu_Position_BottomRight(object sender, EventArgs e) {
            PositionLock = ScreenPosition.BottomRight;
        }

        private void Menu_Reduce_click(object sender, EventArgs e) {
            //Hide form in a platform specific way
            Program.Platform.HideForm(this);
        }

        private void Menu_Chrome_click(object sender, EventArgs e) {
            IsChromeVisible = !IsChromeVisible;
        }

        private void Menu_Settings_click(object sender, EventArgs e) {
            this.SetSidePanel(new OptionsPanel());
        }

        private void Menu_About_click(object sender, EventArgs e) {
            this.SetSidePanel(new AboutPanel());
        }

        private void Menu_Close_click(object sender, EventArgs e) {
            this.Close();
        }

        private void Menu_Fullscreen_ExitFullscreen_click(object sender, EventArgs e) {
            FullscreenManager.SwitchBack();
        }

        private void Menu_Fullscreen_Mode_opening(object sender, EventArgs e) {
            var mode = Settings.Default.GetFullscreenMode();

            menuModeStandardToolStripMenuItem.Checked = (mode == FullscreenMode.Standard);
            menuModeFullscreenToolStripMenuItem.Checked = (mode == FullscreenMode.Fullscreen);
            menuModeAllScreensToolStripMenuItem.Checked = (mode == FullscreenMode.AllScreens);
        }

        private void Menu_Fullscreen_Mode_Standard_click(object sender, EventArgs e) {
            Settings.Default.SetFullscreenMode(FullscreenMode.Standard);
            FullscreenManager.SwitchFullscreen(FullscreenMode.Standard);
        }

        private void Menu_Fullscreen_Mode_Fullscreen_click(object sender, EventArgs e) {
            Settings.Default.SetFullscreenMode(FullscreenMode.Fullscreen);
            FullscreenManager.SwitchFullscreen(FullscreenMode.Fullscreen);
        }

        private void Menu_Fullscreen_Mode_AllScreens_click(object sender, EventArgs e) {
            Settings.Default.SetFullscreenMode(FullscreenMode.AllScreens);
            FullscreenManager.SwitchFullscreen(FullscreenMode.AllScreens);
        }

        private void Menu_Alert_Active_click(object sender, EventArgs e) {
            ColorAlertEnable = !ColorAlertEnable;
        }

        private void Menu_Alert_Color_click(object sender, EventArgs e) {
            var menu_item = (ToolStripMenuItem)sender;
            var base_menu = (ToolStripDropDownMenu)menu_item.GetCurrentParent();

            var base_menu_item = base_menu.OwnerItem;
            var base_menu_item_container = base_menu_item.Owner;

            var (width, height) = (Width, Height);
            var client_rect = ClientRectangle;
            var dx = width - client_rect.Width;
            var dy = height - client_rect.Height;

            var (dx2, dy2) = (dx / 2, dy / 2);

            var bounds = RectangleToScreen(ClientRectangle);

            var client_point = new Point(base_menu_item_container.Left - bounds.Left, base_menu_item_container.Top - bounds.Top);

            var bmp = new Bitmap(bounds.Width, bounds.Height);

            using(var g = Graphics.FromImage(bmp))
                g.CopyFromScreen(Left + dx2, Top + dy2, 0, 0, bmp.Size);

            var color = bmp.GetPixel(client_point.X - 1, client_point.Y - 1);
            ColorAlertColor = color;
        }

        private void SetMenuAlertColorIcon(Color color) {
            alertColorToolStripMenuItem.Text = $@"Color[a{color.A},r{color.R},g{color.G},b{color.B}]";

            var icon = new Bitmap(16, 16);

            using(var b = new SolidBrush(color))
            using(var g = Graphics.FromImage(icon))
                g.FillEllipse(b, 0, 0, 16, 16);

            alertColorToolStripMenuItem.Image = icon;
        }

        private DateTime _ColorAlert_LastTimeoutChangeTextTime = DateTime.MinValue;
        private async void alertTimeoutToolStripTextBox_KeyPress(object sender, KeyPressEventArgs e) {
            if(!char.IsDigit(e.KeyChar) && e.KeyChar != '\b') {
                e.Handled = true;
                return;
            }

            var now = DateTime.Now;
            _ColorAlert_LastTimeoutChangeTextTime = now;

            await Task.Delay(750);

            if(_ColorAlert_LastTimeoutChangeTextTime != now) return;

            var text_box = (ToolStripTextBox)sender;

            var text = text_box.Text;

            if(!int.TryParse(text_box.Text, out var timeout) || timeout <= 0) {
                text_box.Text = "200";
                text_box.SelectAll();
            }
            else 
                ColorAlertTimeout = timeout;
        }

        private void repeatCountToolStripMenuItem_Click(object sender, EventArgs e) {
            ColorAlertRepeatCountEnabled ^= true;
        }

        private void alertRepeatCountToolStripTextBox_KeyPressed(object sender, KeyPressEventArgs e) {
            if(!char.IsDigit(e.KeyChar) && e.KeyChar != '\b') {
                e.Handled = true;
                return;
            }

            if(sender is not ToolStripTextBox { Text: { Length: > 0 } text } || !int.TryParse(text, out var value)) {
                return;
            }

            ColorAlertRepeatCount = value;
        }

        private void fuzzyEqualToolStripMenuItem_Click(object sender, EventArgs e) {
            ColorAlertFuzzyEqual ^= true;
        }

        private void ColorAlertFuzzyEqualThresholdToolStripTextBox_KeyPress(object sender, KeyPressEventArgs e) {
            if(!char.IsDigit(e.KeyChar) && e.KeyChar != '\b') {
                e.Handled = true;
                return;
            }

            if(sender is not ToolStripTextBox { Text: { Length: > 0 } text } || !int.TryParse(text, out var value)) {
                return;
            }

            FuzzyEqualThreshold = value;
        }

        private void Menu_Alert_SoundSelection_click(object sender, EventArgs e) {
            var settings = Settings.Default;
            var dialog = new OpenFileDialog {
                Title = "Файл звукового оповещения",
                Filter = "Звуковые файлы (*.wav)|*.wav|Все файлы (*.*)|*.*",
                FileName = settings.ColorAlertSoundFile,
                RestoreDirectory = true,
                CheckFileExists = true,
            };

            if(dialog.ShowDialog() != DialogResult.OK)
                return;

            settings.ColorAlertSoundFile = dialog.FileName;
            settings.Save();
        }

        private void Menu_Alert_ColorSelectionDialog_click(object sender, EventArgs e) {
            var dialog = new ColorDialog {
                Color = ColorAlertColor,
                SolidColorOnly = true,
            };

            if(dialog.ShowDialog() != DialogResult.OK) return;

            ColorAlertColor = dialog.Color;
        }

        private bool _ColorAlertCheck;

        private void Menu_Alert_Check_click(object sender, EventArgs e) {
            if(ColorAlertEnable)
                _ColorAlertCheck = true;
        }

        private bool _ColorAlertCommit;

        private void Menu_Alert_Commit_click(object sender, EventArgs e) {
            if(ColorAlertEnable && _ColorAlertInActive)
                _ColorAlertCommit = true;
        }
    }
}
