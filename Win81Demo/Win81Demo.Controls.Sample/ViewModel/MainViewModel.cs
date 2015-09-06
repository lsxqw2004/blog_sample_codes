using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Views;

namespace Win81Demo.Controls.Sample.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {

        private readonly INavigationService _navigationService;
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        private RelayCommand<object> _sampleClickCommand;


        public RelayCommand<object> SampleClickCommand
        {
            get
            {
                return _sampleClickCommand
                    ?? (_sampleClickCommand = new RelayCommand<object>(
                                          p =>
                                          {
                                              var destPage = string.Empty;
                                              switch (Convert.ToInt32(p))
                                              {
                                                  case 1:
                                                      destPage = "CircleMenu";
                                                      break;
                                              }

                                              _navigationService.NavigateTo(destPage);

                                          }));
            }
        }
    }
}