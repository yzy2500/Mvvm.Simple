using Mvvm.Simple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Mvvm.Test
{
    public class TestViewModel : ViewModel
    {
        private string title = "测试一下";
        public string Title
        {
            get { return title; }
            set { title = value; NotifyProperty(() => Title); }
        }

        public RelayCommand ExtToastTip => new(OnToastTip);

        private void OnToastTip()
        {
            ShowToastTip("这是轻提示");
        }

        public RelayCommand ExtMsgDialog => new(OnMsgDialog);

        private void OnMsgDialog()
        {
            ShowMessageDialog("这个使对话框");
        }
    }
}
