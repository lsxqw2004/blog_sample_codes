using System;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace Wpf.Control.Sample.ViewModel
{
    public class MainViewModel : ViewModelBase
    {

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {

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
                                                      destPage = "Views/FlowSample.xaml";
                                                      break;
                                                  case 2:
                                                      destPage = "Views/CircleMenuSample.xaml";
                                                      break;
                                              }

                                              var frame = ((Frame)((FrameworkElement)Application.Current.MainWindow.Content).FindName("SampleContainer"));
                                              frame.Navigate(new Uri(destPage, UriKind.Relative));

                                          }));
            }
        }
    }
}