using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppFramework.UI.ViewModels.Commands;
using CFIT.AppLogger;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicLOD.AppConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows.Controls.Primitives;

namespace DynamicLOD.UI.Views.Profiles
{
    public partial class ModelProfiles : ModelBase<Config>
    {
        public virtual ViewModelSelector<SettingProfile, ModelProfileItem> ViewProfileSelector { get; }
        public virtual ModelProfileItem SelectedModel => ViewProfileSelector?.SelectedDisplayItem;
        public virtual SettingProfile SelectedProfile => ViewProfileSelector?.SelectedItem;
        public virtual SettingProfile LastProfile { get; protected set; } = null;
        public virtual ModelProfileCollection ProfileCollection { get; }
        public virtual ModelMatchingCollection MatchingCollection { get; }
        public virtual ViewModelSelector<string, string> ViewMatchingSelector { get; }
        public virtual ModelLevelCollection TlodCollection { get; }
        public virtual ViewModelSelector<KeyValuePair<int, int>, ModelLevelItem> ViewTlodSelector { get; }
        public virtual ModelLevelCollection OlodCollection { get; }
        public virtual ViewModelSelector<KeyValuePair<int, int>, ModelLevelItem> ViewOlodSelector { get; }
        [ObservableProperty]
        public virtual partial bool IsActivateVisible { get; set; } = false;
        [ObservableProperty]
        public virtual partial bool IsAddVisible { get; set; } = true;
        [ObservableProperty]
        public virtual partial bool IsEditAllowed { get; set; } = false;
        [ObservableProperty]
        public virtual partial bool IsDeleteProfileAllowed { get; set; } = false;
        public virtual CommandWrapper RemoveProfileCommand => ViewProfileSelector.RemoveCommand;
        protected virtual bool FirstLoad { get; set; } = true;

        public ModelProfiles(AppService source, Selector profileSelector, Selector matchingSelector, Selector tlodSelector, Selector olodSelector) : base(source.Config, source)
        {
            ProfileCollection = [];
            ViewProfileSelector = new(profileSelector, ProfileCollection, AppWindow.IconLoader)
            {
                ClearOnAddUpdate = false
            };
            ViewProfileSelector.OnSelectionChanged += OnProfileSelectionChanged;
            ViewProfileSelector.ClearInputs += OnProfileSelectionCleared;

            MatchingCollection = new(source?.SettingProfile);
            ViewMatchingSelector = new(matchingSelector, MatchingCollection, AppWindow.IconLoader);

            TlodCollection = new(source?.SettingProfile?.TlodLevels);
            ViewTlodSelector = new(tlodSelector, TlodCollection, AppWindow.IconLoader);

            OlodCollection = new(source?.SettingProfile?.OlodLevels);
            ViewOlodSelector = new(olodSelector, OlodCollection, AppWindow.IconLoader);

            AppService.ProfileChanged += OnAppProfileChanged;
            AppService.ProfileCollectionChanged += OnAppProfileCollectionChanged;
        }

        protected override void InitializeModel()
        {
            InitializeMessageService();
        }

        protected virtual void OnAppProfileChanged(SettingProfile settingProfile)
        {
            if (!FirstLoad)
                ViewProfileSelector.SelectedItem = settingProfile;
            else
                FirstLoad = false;
            NotifySelectionChange();
        }

        public virtual void OnProfileRemoved()
        {
            LastProfile = null;
        }

        protected virtual void OnAppProfileCollectionChanged()
        {
            ProfileCollection.NotifyCollectionChanged();
        }

        protected virtual void OnProfileSelectionChanged()
        {
            if (SelectedProfile != null)
                LastProfile = SelectedProfile;
            else if (SelectedProfile == null && LastProfile != null)
                ViewProfileSelector.SelectedItem = LastProfile;

            if (ViewProfileSelector.SelectedIndex != -1)
                IsAddVisible = false;
            else
                IsAddVisible = true;

            if (ViewProfileSelector?.SelectedItem != null)
            {
                MatchingCollection.ChangeProfile(ViewProfileSelector.SelectedItem);
                TlodCollection.ChangeSource(ViewProfileSelector.SelectedItem.TlodLevels);
                OlodCollection.ChangeSource(ViewProfileSelector.SelectedItem.OlodLevels);
            }
            else
            {
                MatchingCollection.Clear();
                ViewMatchingSelector.ClearSelection();

                TlodCollection.ChangeSource(new SortedDictionary<int, int>());
                ViewTlodSelector.ClearSelection();
                OlodCollection.ChangeSource(new SortedDictionary<int, int>());
                ViewOlodSelector.ClearSelection();
            }

            NotifySelectionChange();
        }

        protected virtual void OnProfileSelectionCleared()
        {
            LastProfile = null;
        }

        protected virtual void NotifySelectionChange()
        {
            NotifyPropertyChanged(string.Empty);
            IsActivateVisible = SelectedProfile != null && SelectedModel?.IsActive == false;
            IsEditAllowed = SelectedProfile != null;
            IsDeleteProfileAllowed = Source?.SettingProfiles?.Count > 1;
            NotifyPropertyChanged(nameof(IsAddVisible));
            NotifyPropertyChanged(nameof(IsActivateVisible));
            NotifyPropertyChanged(nameof(IsEditAllowed));
            NotifyPropertyChanged(nameof(IsDeleteProfileAllowed));
            NotifyPropertyChanged(nameof(SelectedModel));
            SelectedModel.NotifyPropertyChanged(string.Empty);
            NotifyPropertyChanged(nameof(SelectedProfile));
        }

        [RelayCommand]
        protected virtual void ImportProfile()
        {
            try
            {
                Logger.Debug($"Importing Profile from Clipboard ...");
                string json = ClipboardHelper.GetClipboard();
                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.Warning($"Clipboard Data is empty / wrong Type");
                    return;
                }

                Config.ImportProfile(json);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        [RelayCommand]
        protected virtual void ExportProfile()
        {
            try
            {
                Logger.Debug($"Exporting Profile to Clipboard ...");
                if (SelectedProfile == null)
                {
                    Logger.Warning($"Selected Profile is Null");
                    return;
                }

                string json = JsonSerializer.Serialize(SelectedProfile);
                ClipboardHelper.SetClipboard(json);
                Logger.Information($"Copied Profile '{SelectedProfile.Name}' to Clipboard");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        [RelayCommand]
        protected virtual void CloneProfile()
        {
            try
            {
                Logger.Debug($"Cloning Profile ...");
                if (SelectedProfile == null)
                {
                    Logger.Warning($"Selected Profile is Null");
                    return;
                }

                string json = JsonSerializer.Serialize(SelectedProfile);
                var clone = JsonSerializer.Deserialize<SettingProfile>(json);
                clone.Name = $"Clone of {SelectedProfile.Name}";

                if (Config.SettingProfiles.Any(p => p.Name.Equals(clone.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    Logger.Warning($"The Profile '{clone.Name}' is already configured");
                    return;
                }

                AppService.Instance.AddSettingProfile(clone);
                Logger.Information($"Cloned Profile '{SelectedProfile.Name}'");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
