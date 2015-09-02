using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Wpf.Control.Flow;
using Wpf.Control.Sample.Model;
using Wpf.Control.CircleMenu;

namespace Wpf.Control.Sample.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class CircleMenuSampleViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        /// <summary>
        /// Initializes a new instance of the CircleMenuSampleViewModel class.
        /// </summary>
        public CircleMenuSampleViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _dataService.GetData(
                (item, error) =>
                {
                    if (error != null)
                    {
                        // Report error here
                        return;
                    }

                    SubMenuItems = new ObservableCollection<CircleMenuItem>(
                        new List<CircleMenuItem>()
                        {
                            new CircleMenuItem() {Id = 1, Title = "衣"},
                            new CircleMenuItem() {Id = 2, Title = "带"},
                            new CircleMenuItem() {Id = 3, Title = "渐"},
                            new CircleMenuItem() {Id = 4, Title = "宽"},
                            new CircleMenuItem() {Id = 5, Title = "终"},
                            new CircleMenuItem() {Id = 6, Title = "不"},
                            new CircleMenuItem() {Id = 7, Title = "悔"},
                            new CircleMenuItem() {Id = 8, Title = "为"},
                            new CircleMenuItem() {Id = 9, Title = "伊"},
                            new CircleMenuItem() {Id = 10, Title = "消"},
                            new CircleMenuItem() {Id = 11, Title = "得"},
                            new CircleMenuItem() {Id = 12, Title = "人"},
                            new CircleMenuItem() {Id = 13, Title = "憔"},
                            new CircleMenuItem() {Id = 14, Title = "悴"}
                        });
                });

        }

        private ObservableCollection<CircleMenuItem> _subMenuItems;

        public ObservableCollection<CircleMenuItem> SubMenuItems
        {
            get { return _subMenuItems; }
            set { Set(() => SubMenuItems, ref _subMenuItems, value); }
        }

        private RelayCommand<RoutedEventArgs> _nodeClickCommand;

        public RelayCommand<RoutedEventArgs> NodeClickCommand
        {
            get
            {
                return _nodeClickCommand
                    ?? (_nodeClickCommand = new RelayCommand<RoutedEventArgs>(
                                          p =>
                                          {
                                              var dataItem = ((FrameworkElement)p.OriginalSource).DataContext;
                                              MessageBox.Show(((CircleMenuItem)dataItem).Id.ToString());

                                              var circleCtrl = (CircleMenuControl)p.Source;
                                              var suc = VisualStateManager.GoToState(circleCtrl, CircleMenuControl.VisualStateCollapsed, false);
                                              var bb = 1;
                                          }));
            }
        }
    }
}