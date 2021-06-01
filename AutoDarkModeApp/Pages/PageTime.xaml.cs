using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using Windows.Devices.Geolocation;
using Windows.System.Power;
using AutoDarkModeApp.Properties;
using System.Diagnostics;
using AutoDarkModeSvc.Config;
using System.Globalization;
using System.Threading.Tasks;
using AutoDarkModeSvc.Communication;
using AutoDarkModeApp.Communication;
using AutoDarkModeApp.Handlers;

namespace AutoDarkModeApp.Pages
{
    /// <summary>
    /// Interaction logic for PageTime.xaml
    /// </summary>
    public partial class PageTime : Page
    {
        readonly AdmConfigBuilder builder = AdmConfigBuilder.Instance();
        private readonly bool init = true;
        readonly ICommandClient messagingClient = new ZeroMQClient(Command.DefaultPort);

        public PageTime()
        {
            try
            {
                builder.Load();
                builder.LoadLocationData();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
            InitializeComponent();
            if (builder.Config.AutoThemeSwitchingEnabled)
            {
                autoCheckBox.IsChecked = true;
            }
            if (builder.Config.Location.Enabled)
            {
                ActivateLocationMode();
                RadioButtonLocationTimes.IsChecked = true;
            }
            else
            {
                applyButton.IsEnabled = false;
                if (Properties.Settings.Default.AlterTime)
                {
                    darkStartBox.Text = builder.Config.Sunset.ToString("hh", CultureInfo.InvariantCulture);
                    lightStartBox.Text = builder.Config.Sunrise.ToString("hh", CultureInfo.InvariantCulture);
                }
                else
                {
                    darkStartBox.Text = builder.Config.Sunset.ToString("HH", CultureInfo.InvariantCulture);
                    lightStartBox.Text = builder.Config.Sunrise.ToString("HH", CultureInfo.InvariantCulture);
                }
                DarkStartMinutesBox.Text = builder.Config.Sunset.ToString("mm", CultureInfo.InvariantCulture);
                LightStartMinutesBox.Text = builder.Config.Sunrise.ToString("mm", CultureInfo.InvariantCulture);
            }
            InitOffset();
            if (Settings.Default.AlterTime) AlterTime(true);
            init = false;
        }

        //offset for sunrise and sunset hours
        private void PopulateOffsetFields(int offsetDark, int offsetLight)
        {
            if (offsetLight < 0)
            {
                OffsetLightModeButton.Content = "-";
                OffsetLightBox.Text = Convert.ToString(-offsetLight);
            }
            else
            {
                OffsetLightBox.Text = Convert.ToString(offsetLight);
            }
            if (offsetDark < 0)
            {
                OffsetDarkModeButton.Content = "-";
                OffsetDarkBox.Text = Convert.ToString(-offsetDark);
            }
            else
            {
                OffsetDarkBox.Text = Convert.ToString(offsetDark);
            }
        }
        private void InitOffset()
        {
            PopulateOffsetFields(builder.Config.Location.SunsetOffsetMin, builder.Config.Location.SunriseOffsetMin);
        }
        //+ and - button
        private void OffsetModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Content.ToString() == "+")
                {
                    button.Content = "-";
                }
                else
                {
                    button.Content = "+";
                }
                OffsetButton.IsEnabled = true;
            }
        }
        //apply offset
        private void OffsetButton_Click(object sender, RoutedEventArgs e)
        {
            int offsetDark;
            int offsetLight;

            //get values from TextBox
            try
            {
                offsetDark = int.Parse(OffsetDarkBox.Text);
                offsetLight = int.Parse(OffsetLightBox.Text);
            }
            catch
            {
                userFeedback.Text = Properties.Resources.errorNumberInput;
                return;
            }

            PopulateOffsetFields(offsetDark, offsetLight);

            if (OffsetLightModeButton.Content.ToString() == "+")
            {
                builder.Config.Location.SunriseOffsetMin = offsetLight;
            }
            else
            {
                builder.Config.Location.SunriseOffsetMin = -offsetLight;
            }

            if (OffsetDarkModeButton.Content.ToString() == "+")
            {
                builder.Config.Location.SunsetOffsetMin = offsetDark;
            }
            else
            {
                builder.Config.Location.SunsetOffsetMin = -offsetDark;
            }
            UpdateSuntimes();
            OffsetButton.IsEnabled = false;
            ApplyTheme();
        }


        //apply theme
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            int darkStart;
            int darkStartMinutes;
            int lightStart;
            int lightStartMinutes;

            //get values from TextBox
            try
            {
                darkStart = int.Parse(darkStartBox.Text);
                darkStartMinutes = int.Parse(DarkStartMinutesBox.Text);
                lightStart = int.Parse(lightStartBox.Text);
                lightStartMinutes = int.Parse(LightStartMinutesBox.Text);
            }
            catch
            {
                userFeedback.Text = Properties.Resources.errorNumberInput;
                return;
            }

            //check values from TextBox
            //hours with 24 hour time
            if (!Settings.Default.AlterTime)
            {
                if (darkStart >= 24)
                {
                    darkStart = 23;
                    darkStartMinutes = 59;
                }
                if (lightStart < 0)
                {
                    lightStart = 6;
                    darkStart = 17;
                }
            }
            //hours with 12 hour time
            else
            {
                if (darkStart >= 12)
                {
                    darkStart = 11;
                    darkStartMinutes = 59;
                }
                if(darkStart == 0)
                {
                    darkStart = 1;
                }
                if (lightStart >= 13)
                {
                    lightStart = 12;
                }
            }

            //minutes
            if (lightStartMinutes > 59)
            {
                lightStartMinutes = 59;
            }
            if (darkStartMinutes > 59)
            {
                darkStartMinutes = 59;
            }

            //display edited hour values for the user
            darkStartBox.Text = Convert.ToString(darkStart);
            lightStartBox.Text = Convert.ToString(lightStart);

            //display minute values + more beautiful display of minutes, if they are under 10
            if (lightStartMinutes < 10)
            {
                LightStartMinutesBox.Text = "0" + Convert.ToString(lightStartMinutes);
            }
            else
            {
                LightStartMinutesBox.Text = Convert.ToString(lightStartMinutes);
            }
            if (darkStartMinutes < 10)
            {
                DarkStartMinutesBox.Text = "0" + Convert.ToString(darkStartMinutes);
            }
            else
            {
                DarkStartMinutesBox.Text = Convert.ToString(darkStartMinutes);
            }

            //Apply Theme
            if (Properties.Settings.Default.AlterTime)
            {
                darkStart += 12;
            }
            builder.Config.Sunset = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, darkStart, darkStartMinutes, 0);
            builder.Config.Sunrise = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, lightStart, lightStartMinutes, 0);
            ApplyTheme();

            //ui
            applyButton.IsEnabled = false;
        }

        private async void ApplyTheme()
        {
            //show warning for notebook on battery with enabled battery saver
            if (PowerManager.EnergySaverStatus == EnergySaverStatus.On)
            {
                userFeedback.Text = Properties.Resources.msgChangesSaved + "\n\n" + Properties.Resources.msgBatterySaver;
                applyButton.IsEnabled = true;
            }
            else
            {
                userFeedback.Text = Properties.Resources.msgChangesSaved;//changes were saved!
            }

            if (builder.Config.Location.Enabled)
            {
                ActivateLocationMode();
            }
            else
            {
                try
                {
                    builder.Save();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex);
                }
            }
            try
            {
                string result = await messagingClient.SendMessageAndGetReplyAsync(Command.Switch);
                if (result == Response.Err)
                {
                    throw new SwitchThemeException();
                }
            } 
            catch (Exception ex)
            {
                ErrorWhileApplyingTheme($"ZMQ message is {Response.Err}", ex.ToString());
            }

        }
        //if something went wrong while applying the settings :(
        private void ErrorWhileApplyingTheme(string erroDescription, string exception)
        {
            userFeedback.Text = Properties.Resources.msgErrorOcc;
            string error = string.Format(Properties.Resources.errorThemeApply, Properties.Resources.cbSettingsMultiUserImprovements) + "\n\n" + erroDescription + "\n\n" + exception;
            MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno");
            msg.Owner = Window.GetWindow(this);
            msg.ShowDialog();
            var result = msg.DialogResult;
            if (result == true)
            {
                StartProcessByProcessInfo("https://github.com/Armin2208/Windows-Auto-Night-Mode/issues/44");
            }
        }

        // set starttime based on user location
        public async void ActivateLocationMode()
        {
            //ui
            StackPanelTimePicker.Visibility = Visibility.Collapsed;
            TextBlockDark.Visibility = Visibility.Collapsed;
            TextBlockLight.Visibility = Visibility.Collapsed;
            StackPanelLocationTime.Visibility = Visibility.Visible;
            SetOffsetVisibility(Visibility.Visible);
            locationBlock.Visibility = Visibility.Visible;
            locationBlock.Text = Properties.Resources.msgSearchLoc;//Searching your location...
            userFeedback.Text = Properties.Resources.msgSearchLoc;

            if (!init)
            {
                try
                {
                    builder.Save();
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex);
                }
            }

            int timeout = 2;
            bool loaded = false;
            for (int i = 0; i < timeout; i++)
            {
                if (builder.LocationData.LastUpdate == DateTime.MinValue)
                {
                    try
                    {
                        var result = await messagingClient.SendMessageAndGetReplyAsync(Command.Location);
                        if (result == Response.NoLocAccess)
                        {
                            NoLocationAccess();
                            break;
                        }
                        builder.LoadLocationData();
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage(ex);
                        loaded = true;
                        break;
                    }
                    await Task.Delay(1000);
                }
                else
                {
                    loaded = true;
                    break;
                }                
            }

            LocationHandler locationHandler = new LocationHandler();
            var accesStatus = await Geolocator.RequestAccessAsync();
            switch (accesStatus)
            {
                case GeolocationAccessStatus.Allowed:
                    //locate user + get sunrise & sunset times
                    locationBlock.Text = Properties.Resources.lblCity + ": " + await locationHandler.GetCityName();
                    break;

                case GeolocationAccessStatus.Denied:
                    if (Geolocator.DefaultGeoposition.HasValue)
                    {
                        //locate user + get sunrise & sunset times
                        locationBlock.Text = Properties.Resources.lblCity + ": " + await locationHandler.GetCityName();
                    }
                    else
                    {
                        NoLocationAccess();
                        loaded = false;
                    }
                    break;

                case GeolocationAccessStatus.Unspecified:
                    NoLocationAccess();
                    loaded = false;
                    break;
            }

            if (!loaded)
            {
                ShowErrorMessage(new TimeoutException("waiting for location data timed out"));
            }

            UpdateSuntimes();

            // ui controls
            lightStartBox.IsEnabled = false;
            LightStartMinutesBox.IsEnabled = false;
            darkStartBox.IsEnabled = false;
            DarkStartMinutesBox.IsEnabled = false;
            userFeedback.Text = Properties.Resources.msgChangesSaved;

            return;
        }
        private async void NoLocationAccess()
        {
            locationBlock.Text = Properties.Resources.msgLocPerm;//The App needs permission to location
            userFeedback.Text = Properties.Resources.msgLocPerm;
            locationBlock.Visibility = Visibility.Visible;
            TextBlockDarkTime.Text = null;
            TextBlockLightTime.Text = null;
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-location"));
        }

        private void DisableLocationMode()
        {
            lightStartBox.IsEnabled = true;
            LightStartMinutesBox.IsEnabled = true;
            darkStartBox.IsEnabled = true;
            DarkStartMinutesBox.IsEnabled = true;
            applyButton.Visibility = Visibility.Visible;
            applyButton.IsEnabled = true;
            locationBlock.Visibility = Visibility.Collapsed;
            StackPanelLocationTime.Visibility = Visibility.Collapsed;
            StackPanelTimePicker.Visibility = Visibility.Visible;
            TextBlockDark.Visibility = Visibility.Visible;
            TextBlockLight.Visibility = Visibility.Visible;
            SetOffsetVisibility(Visibility.Collapsed);

            builder.Config.Location.Enabled = false;
            userFeedback.Text = Properties.Resources.msgClickApply;//Click on apply to save changes
        }

        //automatic theme switch checkbox
        private async void AutoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            StackPanelRadioHolder.IsEnabled = true;
            RadioButtonCustomTimes.IsChecked = true;
            applyButton.IsEnabled = true;
            darkStartBox.IsEnabled = true;
            DarkStartMinutesBox.IsEnabled = true;
            lightStartBox.IsEnabled = true;
            LightStartMinutesBox.IsEnabled = true;
            userFeedback.Text = Properties.Resources.msgClickApply;//Click on apply to save changes
            //this setting enables all the configuration possibilities of auto dark mode
            builder.Config.AutoThemeSwitchingEnabled = true;
            if (!init)
            {
                try
                {
                    builder.Save();
                    var result = await messagingClient.SendMessageAndGetReplyAsync(Command.AddAutostart);
                    if (result != Response.Ok)
                    {
                        throw new AddAutoStartException($"ZMQ command {result}", "AutoCheckBox_Checked");
                    }
                }
                catch (AddAutoStartException aex)
                {
                    ShowErrorMessage(aex);
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex);
                }
            }

        }
        private async void AutoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            //remove all tasks + autostart
            if (e != null && !init)
            {
                builder.Config.AutoThemeSwitchingEnabled = false;
                try
                {
                    builder.Save();
                    var result = await messagingClient.SendMessageAndGetReplyAsync(Command.RemoveAutostart);
                    if (result != Response.Ok)
                    {
                        throw new AddAutoStartException($"ZMQ command {result}", "AutoCheckBox_Checked");
                    }
                }
                catch (AddAutoStartException aex)
                {
                    ShowErrorMessage(aex);
                }
                catch (Exception ex)
                {
                    ShowErrorMessage(ex);
                }
            }

            //ui
            StackPanelRadioHolder.IsEnabled = false;
            RadioButtonCustomTimes.IsChecked = true;
            DisableLocationMode();
            applyButton.IsEnabled = false;
            darkStartBox.IsEnabled = false;
            DarkStartMinutesBox.IsEnabled = false;
            lightStartBox.IsEnabled = false;
            LightStartMinutesBox.IsEnabled = false;
            userFeedback.Text = Properties.Resources.welcomeText; //Activate the checkbox to enable automatic theme switching
        }

        //12 hour times
        private void AlterTime(bool enable)
        {
            if (enable)
            {
                Properties.Settings.Default.AlterTime = true;
                amTextBlock.Text = "am";
                pmTextBlock.Text = "pm";
                TextBlockDark.Margin = new Thickness(113,0,0,0);
                int darkTime = Convert.ToInt32(darkStartBox.Text) - 12;
                if (darkTime < 1)
                {
                    darkTime = 7;
                }
                darkStartBox.Text = Convert.ToString(darkTime);

                int lightTime = Convert.ToInt32(lightStartBox.Text);
                if (lightTime > 12)
                {
                    lightTime = 7;
                }
                lightStartBox.Text = Convert.ToString(lightTime);
            }
            else
            {
                Properties.Settings.Default.AlterTime = false;
                amTextBlock.Text = "";
                pmTextBlock.Text = "";
                applyButton.Margin = new Thickness(184, 25, 0, 0);
                int darkTime = Convert.ToInt32(darkStartBox.Text) + 12;
                if (darkTime > 24)
                {
                    darkTime = 19;
                }
                if (darkTime == 24)
                {
                    darkTime = 23;
                }
                darkStartBox.Text = Convert.ToString(darkTime);
            }

        }
        private void SetOffsetVisibility(Visibility value)
        {
            OffsetLbl.Visibility = value;
            OffsetDarkLbl.Visibility = value;
            OffsetDarkModeButton.Visibility = value;
            OffsetLightLbl.Visibility = value;
            OffsetLightModeButton.Visibility = value;
            OffsetLightBox.Visibility = value;
            OffsetDarkBox.Visibility = value;
            OffsetDarkDot.Visibility = value;
            OffsetLightDot.Visibility = value;
            OffsetButton.Visibility = value;
        }

        private void RadioButtonCustomTimes_Click(object sender, RoutedEventArgs e)
        {
            DisableLocationMode();
        }

        private void RadioButtonLocationTimes_Click(object sender, RoutedEventArgs e)
        {
            builder.Config.Location.Enabled = true;
            ApplyTheme();
        }

        //textbox event handlers
        private void TextBox_BlockChars_TextInput(object sender, TextCompositionEventArgs e)
        {
            applyButton.IsEnabled = true;
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void TextBox_BlockChars_TextInput_Offset(object sender, TextCompositionEventArgs e)
        {
            OffsetButton.IsEnabled = true;
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void TextBox_BlockCopyPaste_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }
        private void TexttBox_SelectAll_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var textBox = ((System.Windows.Controls.TextBox)sender);
            textBox.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                textBox.SelectAll();
            }));
        }
        private void TextBox_TabNext_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (((TextBox)sender).MaxLength == ((TextBox)sender).Text.Length)
            {
                var ue = e.OriginalSource as FrameworkElement;
                e.Handled = true;
                ue.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        private void TextBlockHelpWiki_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StartProcessByProcessInfo("https://github.com/Armin2208/Windows-Auto-Night-Mode/wiki/Troubleshooting");
        }

        private void UpdateSuntimes()
        {
            LocationHandler.GetSunTimesWithOffset(builder, out DateTime SunriseWithOffset, out DateTime SunsetWithOffset);
            if (Settings.Default.AlterTime)
            {
                TextBlockLightTime.Text = Properties.Resources.lblLight + ": " + SunriseWithOffset.ToString("hh:mm tt", CultureInfo.InvariantCulture); //textblock1
                TextBlockDarkTime.Text = Properties.Resources.lblDark + ": " + SunsetWithOffset.ToString("hh:mm tt", CultureInfo.InvariantCulture); //textblock2
            }
            else
            {
                TextBlockLightTime.Text = Properties.Resources.lblLight + ": " + SunriseWithOffset.ToString("HH:mm", CultureInfo.InvariantCulture); //textblock1
                TextBlockDarkTime.Text = Properties.Resources.lblDark + ": " + SunsetWithOffset.ToString("HH:mm", CultureInfo.InvariantCulture); //textblock2
            }
        }

        private void ShowErrorMessage(Exception ex)
        {
            string error = Properties.Resources.errorThemeApply + "\n\nError ocurred in: " + ex.Source + "\n\n" + ex.Message;
            MsgBox msg = new MsgBox(error, Properties.Resources.errorOcurredTitle, "error", "yesno")
            {
                Owner = Window.GetWindow(this)
            };
            msg.ShowDialog();
            var result = msg.DialogResult;
            if (result == true)
            {
                string issueUri = @"https://github.com/Armin2208/Windows-Auto-Night-Mode/issues";
                Process.Start(new ProcessStartInfo(issueUri)
                {
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            return;
        }

        private void StartProcessByProcessInfo(string message)
        {
            Process.Start(new ProcessStartInfo(message)
            {
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }
}
