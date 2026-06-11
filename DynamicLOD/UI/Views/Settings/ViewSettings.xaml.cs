using CFIT.AppFramework.UI.Validations;
using System.Windows.Controls;

namespace DynamicLOD.UI.Views.Settings
{
    public partial class ViewSettings : UserControl, IView
    {
        protected virtual ModelSettings ViewModel { get; }
        public ViewSettings()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;

            ViewModel.BindStringNumber(nameof(ViewModel.FeetPerSecondThreshold), InputVsThreshold, "4", new ValidationRuleRange<int>(1, 18));
            ViewModel.BindStringInteger(nameof(ViewModel.TrendAvgPoints), InputTrendPoints, "30", new ValidationRuleRange<int>(2, 10));
        }

        public virtual void Start()
        {

        }

        public virtual void Stop()
        {

        }
    }
}
