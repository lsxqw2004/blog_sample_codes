using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Views;
using Win81Demo.Controls.CircleMenuControl;

namespace Win81Demo.Controls.Sample.ViewModel
{
    public class CircleMenuSampleViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;

        public CircleMenuSampleViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            SubMenuItems = new ObservableCollection<CircleMenuItem>(
                 new List<CircleMenuItem>()
                 {
                        new CircleMenuItem() {Id = 1, Title = "一"},
                        new CircleMenuItem() {Id = 2, Title = "城"},
                        new CircleMenuItem() {Id = 3, Title = "山"},
                        new CircleMenuItem() {Id = 4, Title = "色"},
                        new CircleMenuItem() {Id = 5, Title = "半"},
                        new CircleMenuItem() {Id = 6, Title = "城"},
                        new CircleMenuItem() {Id = 7, Title = "湖"},
                 });
        }

        private ObservableCollection<CircleMenuItem> _subMenuItems;

        public ObservableCollection<CircleMenuItem> SubMenuItems
        {

            get
            {
                if (_subMenuItems == null) _subMenuItems = new ObservableCollection<CircleMenuItem>();
                return _subMenuItems;
            }

            set { Set(() => SubMenuItems, ref _subMenuItems, value); }

        }

        public Action<int> SubMenuClickCommand
        {
            get
            {
                return subMenuId =>
                {
                    _dialogService.ShowMessage(subMenuId.ToString(), "子菜单编号");
                };
            }
        }
    }
}
