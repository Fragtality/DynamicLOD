using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppTools;
using DynamicLOD.AppConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DynamicLOD.UI.Views.Profiles
{
    public partial class ViewProfiles : UserControl, IView
    {
        protected virtual Config Config => AppService.Instance?.Config;
        protected virtual ModelProfiles ViewModel { get; }
        protected virtual ViewModelSelector<SettingProfile, ModelProfileItem> ViewProfileSelector { get; }
        protected virtual ViewModelSelector<string, string> ViewMatchingSelector { get; }
        protected virtual ViewModelSelector<KeyValuePair<int, int>, ModelLevelItem> ViewTlodSelector { get; }
        protected virtual ViewModelSelector<KeyValuePair<int, int>, ModelLevelItem> ViewOlodSelector { get; }

        public ViewProfiles()
        {
            InitializeComponent();

            ViewModel = new(AppService.Instance, SelectorProfiles, SelectorMatches, SelectorTlodLevels, SelectorOlodLevels);
            this.DataContext = ViewModel;

            ViewProfileSelector = ViewModel.ViewProfileSelector;
            ViewProfileSelector.BindAddUpdateButton(ButtonAddProfile, ImageAddProfile, GetProfile).Executed += OnAddExecuted;
            ViewProfileSelector.BindTextElement(InputName, nameof(ModelProfileItem.Name), "", null, true);
            InputName.UpdateSelectorOnLostFocus(ViewProfileSelector);

            ViewProfileSelector.BindMember(CheckboxVrMode, nameof(ModelProfileItem.IsVR), null, false);
            CheckboxVrMode.Click += (_, _) =>
            {
                ViewModel.SelectedModel.SetFeature(CheckboxVrMode?.IsChecked, nameof(ModelProfileItem.IsVR));
            };

            ViewProfileSelector.BindTextElement(InputTlodReset, nameof(ModelProfileItem.TlodReset), "", null, true);
            InputTlodReset.KeyUp += (sender, e) =>
            {
                if (Sys.IsEnter(e) && !string.IsNullOrWhiteSpace(InputTlodReset?.Text))
                    ViewProfileSelector.SelectedDisplayItem.TlodReset = InputTlodReset?.Text;
            };
            InputTlodReset.LostKeyboardFocus += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(InputTlodReset?.Text))
                    ViewProfileSelector.SelectedDisplayItem.TlodReset = InputTlodReset?.Text;
            };

            ViewProfileSelector.BindTextElement(InputOlodReset, nameof(ModelProfileItem.OlodReset), "", null, true);
            InputOlodReset.KeyUp += (sender, e) =>
            {
                if (Sys.IsEnter(e) && !string.IsNullOrWhiteSpace(InputOlodReset?.Text))
                    ViewProfileSelector.SelectedDisplayItem.OlodReset = InputOlodReset?.Text;
            };
            InputOlodReset.LostKeyboardFocus += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(InputOlodReset?.Text))
                    ViewProfileSelector.SelectedDisplayItem.OlodReset = InputOlodReset?.Text;
            };

            ViewMatchingSelector = ViewModel.ViewMatchingSelector;
            ViewMatchingSelector.BindAddUpdateButton(ButtonAddMatch, ImageAddMatching, GetMatching);
            ViewMatchingSelector.BindTextElement(InputMatchString, null, "", null, true);
            ViewMatchingSelector.AddUpdateCommand.Subscribe(InputMatchString);
            ViewMatchingSelector.BindRemoveButton(ButtonRemoveMatch);

            ViewProfileSelector.BindRemoveButton(ButtonRemoveProfile, () => ViewModel.IsDeleteProfileAllowed).Executed += () => ViewModel.OnProfileRemoved();
            ViewProfileSelector.AskConfirmation = true;
            ViewProfileSelector.ConfirmationFunc = () => MessageBox.Show("Delete the selected Profile?", "Delete Profile", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;


            ViewTlodSelector = ViewModel.ViewTlodSelector;
            ViewTlodSelector.BindAddUpdateButton(ButtonAddTlod, ImageAddTlod, GetTlod, () => ViewProfileSelector.HasSelection && GetTlod().Value != 0 && (ViewTlodSelector.IsUpdating || !ViewModel.TlodCollection.Contains(GetTlod())));
            InputTlodAlt.KeyUp += (sender, e) =>
            {
                if (Sys.IsEnter(e) && ViewTlodSelector.AddUpdateCommand.FuncCanExecute?.Invoke() == true)
                    ViewTlodSelector.AddUpdateCommand.Execute(null);
            };
            InputTlodValue.KeyUp += (sender, e) =>
            {
                if (Sys.IsEnter(e) && ViewTlodSelector.AddUpdateCommand.FuncCanExecute?.Invoke() == true)
                    ViewTlodSelector.AddUpdateCommand.Execute(null);
            };
            ViewTlodSelector.OnSelectionChanged += () => InputTlodAlt?.Text = ViewTlodSelector?.HasSelection == true ? ViewTlodSelector.SelectedItem.Key.ToString() : "";
            ViewTlodSelector.OnSelectionChanged += () => InputTlodValue?.Text = ViewTlodSelector?.HasSelection == true ? ViewTlodSelector.SelectedItem.Value.ToString() : "";
            ViewTlodSelector.AddUpdateCommand.Subscribe(InputTlodAlt);
            ViewTlodSelector.AddUpdateCommand.Subscribe(InputTlodValue);
            ViewTlodSelector.ClearInputs += () => InputTlodAlt?.Text = "";
            ViewTlodSelector.ClearInputs += () => InputTlodValue?.Text = "";
            ViewTlodSelector.BindRemoveButton(ButtonRemoveTlod, () => ViewTlodSelector.HasSelection && ViewModel.TlodCollection.Count > 1);


            ViewOlodSelector = ViewModel.ViewOlodSelector;
            ViewOlodSelector.BindAddUpdateButton(ButtonAddOlod, ImageAddOlod, GetOlod, () => ViewProfileSelector.HasSelection && GetOlod().Value != 0 && (ViewOlodSelector.IsUpdating || !ViewModel.OlodCollection.Contains(GetOlod())));
            InputOlodAlt.KeyUp += (sender, e) =>
            {
                if (Sys.IsEnter(e) && ViewOlodSelector.AddUpdateCommand.FuncCanExecute?.Invoke() == true)
                    ViewOlodSelector.AddUpdateCommand.Execute(null);
            };
            InputOlodValue.KeyUp += (sender, e) =>
            {
                if (Sys.IsEnter(e) && ViewOlodSelector.AddUpdateCommand.FuncCanExecute?.Invoke() == true)
                    ViewOlodSelector.AddUpdateCommand.Execute(null);
            };
            ViewOlodSelector.OnSelectionChanged += () => InputOlodAlt?.Text = ViewOlodSelector?.HasSelection == true ? ViewOlodSelector.SelectedItem.Key.ToString() : "";
            ViewOlodSelector.OnSelectionChanged += () => InputOlodValue?.Text = ViewOlodSelector?.HasSelection == true ? ViewOlodSelector.SelectedItem.Value.ToString() : "";
            ViewOlodSelector.AddUpdateCommand.Subscribe(InputOlodAlt);
            ViewOlodSelector.AddUpdateCommand.Subscribe(InputOlodValue);
            ViewOlodSelector.ClearInputs += () => InputOlodAlt?.Text = "";
            ViewOlodSelector.ClearInputs += () => InputOlodValue?.Text = "";
            ViewOlodSelector.BindRemoveButton(ButtonRemoveOlod, () => ViewOlodSelector.HasSelection && ViewModel.OlodCollection.Count > 1);
        }

        protected virtual void OnAddExecuted()
        {
            if (ViewModel.LastProfile == null && ViewModel.SelectedProfile == null)
            {
                var query = ViewModel.ProfileCollection.Source.Where((p) => p.Name.Equals(InputName?.Text, StringComparison.InvariantCultureIgnoreCase));
                if (query.Any())
                    ViewProfileSelector.SelectedItem = query.First();
            }
        }

        protected virtual SettingProfile GetProfile()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(InputName?.Text))
                    return new SettingProfile()
                    {
                        Name = InputName?.Text,
                        IsVR = CheckboxVrMode?.IsChecked == true,
                    };
                else
                    return null;
            }
            catch
            {
                return null;
            }
        }

        protected virtual string GetMatching()
        {
            try
            {
                return InputMatchString?.Text;
            }
            catch
            {
                return default;
            }
        }

        protected virtual KeyValuePair<int, int> GetTlod()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(InputTlodAlt?.Text) && !string.IsNullOrWhiteSpace(InputTlodValue?.Text)
                    && int.TryParse(InputTlodAlt?.Text, out int alt) && int.TryParse(InputTlodValue?.Text, out int value)
                    && alt >= 0 && alt <= 60000 && value >= Config.MinTlodValue && value <= Config.MaxTlodValue)
                    return new KeyValuePair<int, int>(alt, value);
                else
                    return new KeyValuePair<int, int>(0, 0);
            }
            catch
            {
                return new KeyValuePair<int, int>(0, 0);
            }
        }

        protected virtual KeyValuePair<int, int> GetOlod()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(InputOlodAlt?.Text) && !string.IsNullOrWhiteSpace(InputOlodValue?.Text)
                    && int.TryParse(InputOlodAlt?.Text, out int alt) && int.TryParse(InputOlodValue?.Text, out int value)
                    && alt >= 0 && alt <= 60000 && value >= Config.MinOlodValue && value <= Config.MaxOlodValue)
                    return new KeyValuePair<int, int>(alt, value);
                else
                    return new KeyValuePair<int, int>(0, 0);
            }
            catch
            {
                return new KeyValuePair<int, int>(0, 0);
            }
        }

        public virtual void Start()
        {

        }

        public virtual void Stop()
        {

        }
    }
}
