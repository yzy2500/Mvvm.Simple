using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Mvvm.Simple
{
    public class ViewModel : INotifyPropertyChanged
    {
        public ViewModel()
        {

        }

        /// <summary>
        /// ��Ӧ����ͼ
        /// </summary>
        public FrameworkElement View { get; private set; }
        /// <summary>
        /// �Ӽ���ͼ
        /// </summary>
        public FrameworkElement ActiveItem {  get; private set; }

        /// <summary>
        /// ���Ա��
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// ֪ͨ��ͼ���Է����ı�
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void NotifyProperty(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        /// <summary>
        /// ֪ͨ��ͼ���Է����ı�
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="property"></param>
        protected virtual void NotifyProperty<TProperty>(Expression<Func<TProperty>> property)
        {
            MemberExpression memberExpression;
            if (property.Body is UnaryExpression unaryExpression)
            {
                memberExpression = (MemberExpression)unaryExpression.Operand;
            }
            else
            {
                memberExpression = (MemberExpression)property.Body;
            }
            NotifyProperty(memberExpression.Member.Name);
        }

        /// <summary>
        /// ����ģ���Լ���ͼ
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T CreateInstanceAndView<T>() where T : ViewModel
        {
            var type = typeof(T);
            if (type is null || string.IsNullOrEmpty(type.FullName)) return null;
            if (type.Assembly.CreateInstance(type.FullName) is not T vm) return null;
            vm.InitializeView();
            return vm;
        }
        /// <summary>
        /// ������ͼ
        /// </summary>
        /// <returns></returns>
        public bool InitializeView()
        {
            if (View == null) 
            {
                var type = GetType();
                if (type is null || string.IsNullOrEmpty(type.FullName)) return false;
                var index = type.FullName.Length - 9;
                if (type.FullName[index..] != "ViewModel") return false;
                index += 4;
                if (type.Assembly.GetType(type.FullName[..index]) is Type t && !string.IsNullOrEmpty(t.FullName))
                {
                    View = t.Assembly.CreateInstance(t.FullName) as FrameworkElement;
                    if (View != null) View.DataContext = this;
                }

                if (View is not null) View.Loaded += OnViewFirstLoaded;
            }
            return View != null;
        }

        /// <summary>
        /// �����Ӽ�
        /// </summary>
        /// <param name="vm"></param>
        public void ActiveItemAsync(ViewModel vm)
        {
            if (vm == null) return;
            View?.Dispatcher.InvokeAsync(() =>
            {
                if (vm.View == null || !vm.InitializeView()) return;
                var parent = FindFrameworkElement<ContentControl>(View, "ActiveItem");
                if (parent != null)
                {
                    parent.Content = vm.View;
                    ActiveItem = vm.View;
                }
            });
        }

        /// <summary>
        /// �Ӽ���һ�μ������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnViewFirstLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement f) f.Loaded -= OnViewFirstLoaded;
            if (sender is Window win)
            {
                _ = new ListenWindowMessage(win, OnListenWindowMessage);
            }
        }

        /// <summary>
        /// ������Ϣ
        /// </summary>
        /// <param name="msg"></param>
        protected virtual void OnListenWindowMessage(string msg)
        {

        }

        /// <summary>
        /// �����Ӿ����е�ĳ���Ӽ�
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T FindFrameworkElement<T>(DependencyObject obj, string name) where T : FrameworkElement
        {
            if (obj == null) return null;
            if (obj is T r && (string.IsNullOrEmpty(name) || r.Name == name)) return r;
            int count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count; i++) 
            {
                var item = VisualTreeHelper.GetChild(obj, i);
                if (FindFrameworkElement<T>(item, name) is T res) return res;
            }
            return null;
        }
        /// <summary>
        /// �����Ӿ����е�ĳ������
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T FindParentView<T>(DependencyObject obj, string name = null) where T : FrameworkElement
        {
            while (obj != null)
            {
                if (obj is T r && (string.IsNullOrEmpty(name) || r.Name == name)) return r;
                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }

        /// <summary>
        /// ��ʾ����ʾ
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public void ShowToastTip(string message, TipLevel level = TipLevel.None)
        {
            if (string.IsNullOrWhiteSpace(message) || View == null) return;
            View.Dispatcher.InvokeAsync(() =>
            {
                var win = FindParentView<Window>(View);
                if (win == null) return;
                if (win.Content is not UIElement visual) return;
                var layer = AdornerLayer.GetAdornerLayer(visual);
                if (layer == null) return;
                if (layer.GetAdorners(visual)?.FirstOrDefault(x => x is ToastTipAdorner) is not ToastTipAdorner adorner)
                {
                    adorner = new ToastTipAdorner(visual)
                    {
                        IsHitTestVisible = false
                    };
                    layer.Add(adorner);
                }
                adorner.AddMessage(message, level);
            });
        }

        /// <summary>
        /// �����Ի���
        /// </summary>
        /// <param name="message">��Ϣ</param>
        /// <returns></returns>
        public bool? ShowMessageDialog(string message)
        {
            string title = View?.TryFindResource("Tip") as string ?? "��ʾ";
            return ShowMessageDialog(message, title);
        }
        /// <summary>
        /// �����Ի���
        /// </summary>
        /// <param name="message">��Ϣ</param>
        /// <param name="title">����</param>
        /// <returns></returns>
        public bool? ShowMessageDialog(string message, string title)
        {
            string confirm = View?.TryFindResource("Message_Confirm") as string ?? "ȷ��";
            string cancel = View?.TryFindResource("Message_Cancel") as string ?? "ȡ��";
            return ShowMessageDialog(message, title, confirm, cancel);
        }
        /// <summary>
        /// �����Ի���
        /// </summary>
        /// <param name="message">��Ϣ</param>
        /// <param name="title">����</param>
        /// <param name="confirm">��ȷѡ�������</param>
        /// <param name="cancel">����ѡ�������</param>
        /// <returns></returns>
        public bool? ShowMessageDialog(string message, string title, string confirm, string cancel)
        {
            if (View == null) return false;
            if (View.TryFindResource("MessageDialogStyle") is not Style)
            {
                ResourceDictionary resource = new()
                {
                    Source = new Uri(@"pack://application:,,,/Mvvm.Simple;component/MessageDialogStyle.xaml")
                };
                Application.Current.Resources.MergedDictionaries.Add(resource);
            }
            if (View.TryFindResource("MessageDialogStyle") is not Style style) return false;

            Window owner = FindParentView<Window>(View);
            int height = 200;
#pragma warning disable CS0618 // ���ͻ��Ա�ѹ�ʱ
            FormattedText output = new(message, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily(), FontStyles.Normal, FontWeights.Regular, FontStretches.Normal), 16, Brushes.White);
#pragma warning restore CS0618 // ���ͻ��Ա�ѹ�ʱ
            var h = (int)output.Height;
            if (h > 50) height += h - 50;

            Window box = new()
            {
                Owner = owner,
                WindowStartupLocation = owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
                Style = style,
                Height = height,
            };
            var data = new MessageBoxViewModel(box)
            {
                Title = title,
                Content = message,
                Cancel = cancel,
                Confirm = confirm,
            };
            box.DataContext = data;
            box.ShowDialog();

            return data.Result switch
            {
                2 => false,
                3 => true,
                _ => null,
            };
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<T> execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Action<T> execute, Func<bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            if (parameter is T t) _execute(t);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public enum TipLevel
    {
        None = 0,
        Success = 1,
        Error = 2,
    }

    class MessageBoxViewModel
    {
        readonly Window window;
        public MessageBoxViewModel(Window win)
        {
            window = win;
        }

        public string Title { get; set; }
        public string Content { get; set; }
        public string Cancel { get; set; }
        public string Confirm { get; set; }

        private int result = 0;
        public int Result
        {
            get { return result; }
            set
            {
                if (result != value)
                {
                    result = value;
                    window.Close();
                }
            }
        }
    }
}
