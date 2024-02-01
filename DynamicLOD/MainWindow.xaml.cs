using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace DynamicLOD
{
    public partial class MainWindow : Window
    {
        protected NotifyIconViewModel notifyModel;
        protected ServiceModel serviceModel;
        protected DispatcherTimer timer;

        protected int editPairTLOD = -1;
        protected int editPairOLOD = -1;

        public MainWindow(NotifyIconViewModel notifyModel, ServiceModel serviceModel)
        {
            InitializeComponent();
            this.notifyModel = notifyModel;
            this.serviceModel = serviceModel;
            
            string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            assemblyVersion = assemblyVersion[0..assemblyVersion.LastIndexOf('.')];
            Title += " (" + assemblyVersion + ")";

            FillIndices(dgTlodPairs);
            FillIndices(dgOlodPairs);

            for (int i = 0; i < ServiceModel.maxProfile; i++)
                cbProfile.Items.Add($"{i + 1}");

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += OnTick;
        }

        protected void LoadSettings()
        {
            chkOpenWindow.IsChecked = serviceModel.OpenWindow;
            chkUseTargetFPS.IsChecked = serviceModel.UseTargetFPS;
            cbProfile.SelectedIndex = serviceModel.SelectedProfile;
            dgTlodPairs.ItemsSource = serviceModel.PairsTLOD[serviceModel.SelectedProfile].ToDictionary(x => x.Item1, x => x.Item2);
            dgOlodPairs.ItemsSource = serviceModel.PairsOLOD[serviceModel.SelectedProfile].ToDictionary(x => x.Item1, x => x.Item2);
            chkProfileIsVr.IsChecked = serviceModel.IsVrProfile;
            txtTargetFPS.Text = Convert.ToString(serviceModel.TargetFPS, CultureInfo.CurrentUICulture);
            txtDecreaseTlod.Text = Convert.ToString(serviceModel.DecreaseTLOD, CultureInfo.CurrentUICulture);
            txtDecreaseOlod.Text = Convert.ToString(serviceModel.DecreaseOLOD, CultureInfo.CurrentUICulture);
            txtMinLod.Text = Convert.ToString(serviceModel.MinLOD, CultureInfo.CurrentUICulture);
            txtConstraintTicks.Text = Convert.ToString(serviceModel.ConstraintTicks, CultureInfo.CurrentUICulture);
            txtTargetFpsIndex.Text = Convert.ToString(serviceModel.TargetFPSIndex, CultureInfo.CurrentUICulture);
            txtTlodDefault.Text = Convert.ToString(serviceModel.DefaultTLOD, CultureInfo.CurrentUICulture);
            txtOlodDefault.Text = Convert.ToString(serviceModel.DefaultOLOD, CultureInfo.CurrentUICulture);
        }

        protected static void FillIndices(DataGrid dataGrid)
        {
            DataGridTextColumn column0 = new()
            {
                Header = "#",
                Width = 16
            };

            Binding bindingColumn0 = new()
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1),
                Converter = new RowToIndexConvertor()
            };

            column0.Binding = bindingColumn0;

            dataGrid.Columns.Add(column0);
        }

        protected void UpdateStatus()
        {
            if (serviceModel.IsSimRunning)
                lblConnStatMSFS.Foreground = new SolidColorBrush(Colors.DarkGreen);
            else
                lblConnStatMSFS.Foreground = new SolidColorBrush(Colors.Red);

            if (IPCManager.SimConnect != null && IPCManager.SimConnect.IsReady)
                lblConnStatSimConnect.Foreground = new SolidColorBrush(Colors.DarkGreen);
            else
                lblConnStatSimConnect.Foreground = new SolidColorBrush(Colors.Red);

            if (serviceModel.IsSessionRunning)
                lblConnStatSession.Foreground = new SolidColorBrush(Colors.DarkGreen);
            else
                lblConnStatSession.Foreground = new SolidColorBrush(Colors.Red);
        }

        protected void UpdateLiveValues()
        {
            if (IPCManager.SimConnect != null && IPCManager.SimConnect.IsConnected)
                lblSimFPS.Content = IPCManager.SimConnect.GetAverageFPS().ToString("F2");
            else
                lblSimFPS.Content = "0";

            if (serviceModel.MemoryAccess != null)
            {
                if (!serviceModel.IsVrModeActive)
                {
                    lblSimTLOD.Content = "<" + serviceModel.MemoryAccess.GetTLOD_PC().ToString("F0") + "> / " + serviceModel.MemoryAccess.GetTLOD_VR().ToString("F0");
                    lblSimOLOD.Content = "<" + serviceModel.MemoryAccess.GetOLOD_PC().ToString("F0") + "> / " + serviceModel.MemoryAccess.GetOLOD_VR().ToString("F0");
                }
                else
                {
                    lblSimTLOD.Content = serviceModel.MemoryAccess.GetTLOD_PC().ToString("F0") + " / <" + serviceModel.MemoryAccess.GetTLOD_VR().ToString("F0") + ">";
                    lblSimOLOD.Content = serviceModel.MemoryAccess.GetOLOD_PC().ToString("F0") + " / <" + serviceModel.MemoryAccess.GetOLOD_VR().ToString("F0") + ">";
                }
            }
            else
            {
                lblSimTLOD.Content = "0 / 0";
                lblSimOLOD.Content = "0 / 0";
            }

            if (serviceModel.UseTargetFPS && serviceModel.IsSessionRunning)
            {
                if (IPCManager.SimConnect.GetAverageFPS() < serviceModel.TargetFPS)
                    lblSimFPS.Foreground = new SolidColorBrush(Colors.Red);
                else
                    lblSimFPS.Foreground = new SolidColorBrush(Colors.DarkGreen);
            }
            else
            {
                lblSimFPS.Foreground = new SolidColorBrush(Colors.Black);
            }

            if (serviceModel.fpsMode)
            {
                lblSimTLOD.Foreground = new SolidColorBrush(Colors.Red);
                lblSimOLOD.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                lblSimTLOD.Foreground = new SolidColorBrush(Colors.Black);
                lblSimOLOD.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        protected void UpdateAircraftValues()
        {
            if (IPCManager.SimConnect != null && IPCManager.SimConnect.IsConnected)
            {
                var simConnect = IPCManager.SimConnect;
                if (!serviceModel.OnGround)
                    lblPlaneAGL.Content = simConnect.AltAboveGround(false).ToString("F0");
                else
                    lblPlaneAGL.Content = "0";

                lblPlaneVS.Content = (simConnect.ReadSimVar("VERTICAL SPEED", "feet per second") * 60.0f).ToString("F0");
                if (serviceModel.OnGround)
                    lblVSTrend.Content = "Ground";
                else if (serviceModel.VerticalTrend > 0)
                    lblVSTrend.Content = "Climb";
                else if (serviceModel.VerticalTrend < 0)
                    lblVSTrend.Content = "Descent";
                else
                    lblVSTrend.Content = "Cruise";
            }
            else
            {
                lblPlaneAGL.Content = "0";
                lblPlaneVS.Content = "0";
                lblVSTrend.Content = "0";
            }
        }

        protected static void UpdateIndex(DataGrid grid, List<(float, float)> pairs, int index)
        {
            if (index >= 0 && index < pairs.Count)
                grid.SelectedIndex = index;
        }

        protected void OnTick(object sender, EventArgs e)
        {
            if (serviceModel.ProfileSelectionChanged)
            {
                LoadSettings();
                serviceModel.ProfileSelectionChanged = false;
            }
            
            UpdateStatus();
            UpdateLiveValues();
            UpdateAircraftValues();

            if (serviceModel.IsSessionRunning)
            {
                UpdateIndex(dgTlodPairs, serviceModel.PairsTLOD[serviceModel.SelectedProfile], serviceModel.CurrentPairTLOD);
                UpdateIndex(dgOlodPairs, serviceModel.PairsOLOD[serviceModel.SelectedProfile], serviceModel.CurrentPairOLOD);
            }
        }

        protected void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible)
            {
                notifyModel.CanExecuteHideWindow = false;
                notifyModel.CanExecuteShowWindow = true;
                timer.Stop();
            }
            else
            {
                LoadSettings();
                timer.Start();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void chkUseTargetFPS_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("useTargetFps", chkUseTargetFPS.IsChecked.ToString().ToLower());
            LoadSettings();
        }

        private void chkOpenWindow_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("openWindow", chkOpenWindow.IsChecked.ToString().ToLower());
            LoadSettings();
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox_SetSetting(sender as TextBox);
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || e.Key != Key.Return)
                return;

            TextBox_SetSetting(sender as TextBox);
        }

        private void TextBox_SetSetting(TextBox sender)
        {
            if (sender == null || string.IsNullOrWhiteSpace(sender.Text))
                return;

            string key;
            bool intValue = false;
            bool notNegative = true;
            switch (sender.Name)
            {
                case "txtTargetFPS":
                    key = "targetFps";
                    intValue = true;
                    break;
                case "txtDecreaseTlod":
                    key = "decreaseTlod";
                    break;
                case "txtDecreaseOlod":
                    key = "decreaseOlod";
                    break;
                case "txtConstraintTicks":
                    key = "constraintTicks";
                    intValue = true;
                    break;
                case "txtTargetFpsIndex":
                    key = "targetFpsIndex";
                    intValue = true;
                    break;
                case "txtMinLod":
                    key = "minLod";
                    break;
                case "txtTlodDefault":
                    key = "defaultTlod";
                    break;
                case "txtOlodDefault":
                    key = "defaultOlod";
                    break;
                default:
                    key = "";
                    break;
            }

            if (key == "")
                return;

            if (intValue && int.TryParse(sender.Text, CultureInfo.InvariantCulture, out int iValue))
            {
                if (notNegative)
                    iValue = Math.Abs(iValue);
                serviceModel.SetSetting(key, Convert.ToString(iValue, CultureInfo.InvariantCulture));
            }

            if (!intValue && float.TryParse(sender.Text, new RealInvariantFormat(sender.Text), out float fValue))
            {
                if (notNegative)
                    fValue = Math.Abs(fValue);
                serviceModel.SetSetting(key, Convert.ToString(fValue, CultureInfo.InvariantCulture));
            }

            LoadSettings();
        }

        private static void SetPairTextBox(DataGrid sender, TextBox alt, TextBox value, ref int index)
        {
            if (sender.SelectedIndex == -1 || sender.SelectedItem == null)
                return;

            var item = (KeyValuePair<float, float>)sender.SelectedItem;
            alt.Text = Convert.ToString((int)item.Key, CultureInfo.CurrentUICulture);
            value.Text = Convert.ToString(item.Value, CultureInfo.CurrentUICulture);
            index = sender.SelectedIndex;
        }

        private void dgTlodPairs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetPairTextBox(dgTlodPairs, txtTlodAlt, txtTlodValue, ref editPairTLOD);
        }

        private void dgOlodPairs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetPairTextBox(dgOlodPairs, txtOlodAlt, txtOlodValue, ref editPairOLOD);
        }

        private void ChangeLodPair(ref int pairIndex, TextBox alt, TextBox value, List<(float, float)> pairs)
        {
            if (pairIndex == -1)
                return;

            if (pairIndex == 0 && alt.Text != "0")
                alt.Text = "0";

            if (int.TryParse(alt.Text, CultureInfo.InvariantCulture, out int agl) && float.TryParse(value.Text, new RealInvariantFormat(value.Text), out float lod)
                && pairIndex < pairs.Count && agl >= 0 && lod >= serviceModel.SimMinLOD)
            {
                var oldPair = pairs[pairIndex];
                pairs[pairIndex] = (agl, lod);
                if (pairs.Count(pair => pair.Item1 == agl) > 1)
                    pairs[pairIndex] = oldPair;
                serviceModel.SavePairs();
            }

            LoadSettings();
            alt.Text = "";
            value.Text = "";
            pairIndex = -1;
        }

        private void btnTlodChange_Click(object sender, RoutedEventArgs e)
        {
            ChangeLodPair(ref editPairTLOD, txtTlodAlt, txtTlodValue, serviceModel.PairsTLOD[serviceModel.SelectedProfile]);
        }

        private void btnOlodChange_Click(object sender, RoutedEventArgs e)
        {
            ChangeLodPair(ref editPairOLOD, txtOlodAlt, txtOlodValue, serviceModel.PairsOLOD[serviceModel.SelectedProfile]);
        }

        private void AddLodPair(ref int pairIndex, TextBox alt, TextBox value, List<(float, float)> pairs)
        {
            if (int.TryParse(alt.Text, CultureInfo.InvariantCulture, out int agl) && float.TryParse(value.Text, new RealInvariantFormat(value.Text), out float lod)
                && agl >= 0 && lod >= serviceModel.SimMinLOD
                && !pairs.Any(pair => pair.Item1 == agl))
            {
                pairs.Add((agl, lod));
                ServiceModel.SortTupleList(pairs);
                serviceModel.SavePairs();
            }

            LoadSettings();
            alt.Text = "";
            value.Text = "";
            pairIndex = -1;
        }

        private void btnTlodAdd_Click(object sender, RoutedEventArgs e)
        {
            AddLodPair(ref editPairTLOD, txtTlodAlt, txtTlodValue, serviceModel.PairsTLOD[serviceModel.SelectedProfile]);
        }

        private void btnOlodAdd_Click(object sender, RoutedEventArgs e)
        {
            AddLodPair(ref editPairOLOD, txtOlodAlt, txtOlodValue, serviceModel.PairsOLOD[serviceModel.SelectedProfile]);
        }

        private void RemoveLoadPair(ref int pairIndex, TextBox alt, TextBox value, List<(float, float)> pairs)
        {
            if (pairIndex < 1 || pairIndex >= pairs.Count)
                return;

            pairs.RemoveAt(pairIndex);
            ServiceModel.SortTupleList(pairs);
            serviceModel.SavePairs();
            LoadSettings();
            alt.Text = "";
            value.Text = "";
            pairIndex = -1;
        }

        private void btnTlodRemove_Click(object sender, RoutedEventArgs e)
        {
            RemoveLoadPair(ref editPairTLOD, txtTlodAlt, txtTlodValue, serviceModel.PairsTLOD[serviceModel.SelectedProfile]);
        }

        private void btnOlodRemove_Click(object sender, RoutedEventArgs e)
        {
            RemoveLoadPair(ref editPairOLOD, txtOlodAlt, txtOlodValue, serviceModel.PairsOLOD[serviceModel.SelectedProfile]);
        }

        private void cbProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbProfile.SelectedIndex >= 0 && cbProfile.SelectedIndex <= ServiceModel.maxProfile)
            {
                serviceModel.SetSetting("selectedProfile", cbProfile.SelectedIndex.ToString());
                LoadSettings();
            }
        }

        private void chkProfileIsVr_Click(object sender, RoutedEventArgs e)
        {
            if (cbProfile.SelectedIndex >= 0 && cbProfile.SelectedIndex <= ServiceModel.maxProfile)
            {
                serviceModel.SetSetting($"isVr{serviceModel.SelectedProfile}", chkProfileIsVr.IsChecked.ToString().ToLower());
                LoadSettings();
            }
        }
    }

    public class RowToIndexConvertor : MarkupExtension, IValueConverter
    {
        static RowToIndexConvertor convertor;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is DataGridRow row)
            {
                return row.GetIndex();
            }
            else
            {
                return -1;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            convertor ??= new RowToIndexConvertor();

            return convertor;
        }


        public RowToIndexConvertor()
        {

        }
    }
}
