using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using Wpf.Control.Sample.Design;
using Wpf.Control.Sample.Model;

namespace Wpf.Control.Sample.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class ViewModelLocator
    {
        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            if (ViewModelBase.IsInDesignModeStatic)
            {
                SimpleIoc.Default.Register<IDataService, DesignDataService>();
            }
            else
            {
                SimpleIoc.Default.Register<IDataService, DataService>();
            }

            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<FlowSampleViewModel>();
            SimpleIoc.Default.Register<CircleMenuSampleViewModel>();
        }

        public MainViewModel Main { get { return ServiceLocator.Current.GetInstance<MainViewModel>(); } }
        public FlowSampleViewModel FlowSampleVm { get{return ServiceLocator.Current.GetInstance<FlowSampleViewModel>();} }
        public CircleMenuSampleViewModel CircleMenuSampleVm { get { return ServiceLocator.Current.GetInstance<CircleMenuSampleViewModel>();}} 

        /// <summary>
        /// Cleans up all the resources.
        /// </summary>
        public static void Cleanup()
        {
        }
    }
}