using Mvvm.Simple;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Mvvm.Test
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var vm = ViewModel.CreateInstanceAndView<MainViewModel>();
            if (vm != null && vm.View is Window win) 
            {
                win.Show();
                MainWindow = win;
                var child = ViewModel.CreateInstanceAndView<TestViewModel>();
                if (child != null) 
                {
                    vm.ActiveItemAsync(child);
                    Task.Run(async () =>
                    {
                        await Task.Delay(5000);
                        child.Title = "已经完成";
                    });
                }
            }
        }
    }
}
