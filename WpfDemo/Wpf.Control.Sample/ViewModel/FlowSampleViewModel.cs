using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Wpf.Control.Flow;
using Wpf.Control.Sample.Model;

namespace Wpf.Control.Sample.ViewModel
{
    public class FlowSampleViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        public FlowSampleViewModel(IDataService dataService)
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

                    Nodes = new ObservableCollection<FlowItem>(

                        new List<FlowItem>()
                        {
                            new FlowItem() {Id = 1, OffsetRate = 0, Title = "接到报修"},
                            new FlowItem() {Id = 2, OffsetRate = 0.5, Title = "派工完成"},
                            new FlowItem() {Id = 3, OffsetRate = 0.75, Title = "维修完成"},
                            new FlowItem() {Id = 3, OffsetRate = 1, Title = "客户确认(我是特别长的标题)"},
                        }
                        );
                });
        }

        private ObservableCollection<FlowItem> _nodes;

        public ObservableCollection<FlowItem> Nodes
        {
            get { return _nodes; }
            set { Set(() => Nodes, ref _nodes, value); }
        }

        private RelayCommand<RoutedEventArgs> _nodeClickCommand;

        /// <summary>
        /// Gets the NodeClickCommand.
        /// </summary>
        public RelayCommand<RoutedEventArgs> NodeClickCommand
        {
            get
            {
                return _nodeClickCommand
                    ?? (_nodeClickCommand = new RelayCommand<RoutedEventArgs>(
                                          p =>
                                          {
                                              var aa = p;
                                              MessageBox.Show(((FlowNodeControl)aa.OriginalSource).NodeTitle);
                                          }));
            }
        }
    }
}