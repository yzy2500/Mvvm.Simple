using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace Mvvm.Simple
{
    /// <summary>
    /// 监听窗口消息
    /// </summary>
    public class ListenWindowMessage
    {
        const int WM_COPYDATA = 0x004A;
        private Action<string> Receive { get; set; }
        private Window Target { get; set; }
        /// <summary>
        /// 监听窗口消息
        /// </summary>
        /// <param name="visual">目标窗口</param>
        /// <param name="result">消息回调</param>
        public ListenWindowMessage(Window visual, Action<string> result)
        {
            if (PresentationSource.FromVisual(visual) is HwndSource hwnd)
            {
                AllowWindowReceiveMessage(visual);
                var p = Process.GetCurrentProcess();
                hwnd.AddHook(new HwndSourceHook(WndProc));
                Receive = result;
                Target = visual;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_COPYDATA)
            {
                COPYDATASTRUCT cds = (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, typeof(COPYDATASTRUCT));
                Receive?.Invoke(cds.lpData);
            }
            else if (msg == 0x20)
            {
                if (lParam.ToInt32() == 0x201fffe && (Target?.OwnedWindows?.Count ?? 0) > 0)
                {
                    //使子窗口闪烁
                    foreach (var win in Target.OwnedWindows.Cast<Window>())
                        WindowJitterAnimation(win);
                }
            }

            return hwnd;
        }

        public enum ChangeFilterAction : uint
        {
            MSGFLT_RESET,
            MSGFLT_ALLOW,
            MSGFLT_DISALLOW
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct ChangeFilterStruct
        {
            public uint CbSize;
            public uint ExtStatus;
        }

        [DllImport("user32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1401:P/Invokes 应该是不可见的", Justification = "<挂起>")]
        public static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint message, ChangeFilterAction action, in ChangeFilterStruct pChangeFilterStruct);


        /// <summary>
        /// 允许窗口接收其他用户的消息消息
        /// </summary>
        /// <param name="window"></param>
        public static bool AllowWindowReceiveMessage(Window window)
        {
            var status = new ChangeFilterStruct() { CbSize = 8 };
            return ChangeWindowMessageFilterEx(new WindowInteropHelper(window).Handle, WM_COPYDATA,
                ChangeFilterAction.MSGFLT_ALLOW, in status);
        }

        private readonly List<Window> AnimationList = new();
        /// <summary>
        /// 窗口抖动动画
        /// </summary>
        /// <param name="win"></param>
        public void WindowJitterAnimation(Window win)
        {
            lock (AnimationList)
            {
                if (AnimationList.Contains(win)) return;
                AnimationList.Add(win);
            }

            var scaleXDoubleAnimation = new DoubleAnimationUsingKeyFrames();
            var scaleYDoubleAnimation = new DoubleAnimationUsingKeyFrames();

            scaleXDoubleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0)), Value = 1.0 });
            scaleXDoubleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(50)), Value = 0.95 });
            scaleXDoubleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100)), Value = 1.0 });
            scaleXDoubleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150)), Value = 0.95 });
            scaleXDoubleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)), Value = 1.0 });
            scaleXDoubleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250)), Value = 0.95 });
            scaleXDoubleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)), Value = 1.0 });

            scaleYDoubleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0)), Value = 1.0 });
            scaleYDoubleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(50)), Value = 0.95 });
            scaleYDoubleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100)), Value = 1.0 });
            scaleYDoubleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150)), Value = 0.95 });
            scaleYDoubleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)), Value = 1.0 });
            scaleYDoubleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250)), Value = 0.95 });
            scaleYDoubleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)), Value = 1.0 });

            var old = win.RenderTransform;
            var origin = win.RenderTransformOrigin;
            win.RenderTransformOrigin = new Point(0.5, 0.5);
            var matrix = new MatrixTransform(old.Value);
            var scale = new ScaleTransform();
            var group = new TransformGroup();
            group.Children.Add(scale);
            group.Children.Add(matrix);
            win.RenderTransform = group;

            var Storyboard = new Storyboard();
            DependencyProperty[] propertyChain = new DependencyProperty[]
            {
                FrameworkElement.RenderTransformProperty,
                TransformGroup.ChildrenProperty,
                ScaleTransform.ScaleXProperty,
                ScaleTransform.ScaleYProperty,
            };
            Storyboard.SetTarget(scaleXDoubleAnimation, win);
            Storyboard.SetTargetProperty(scaleXDoubleAnimation, new PropertyPath("(0).(1)[0].(2)", propertyChain));
            Storyboard.SetTarget(scaleYDoubleAnimation, win);
            Storyboard.SetTargetProperty(scaleYDoubleAnimation, new PropertyPath("(0).(1)[0].(3)", propertyChain));
            Storyboard.Children.Add(scaleXDoubleAnimation);
            Storyboard.Children.Add(scaleYDoubleAnimation);
            Storyboard.Completed += (s, e) =>
            {
                try
                {
                    Storyboard.Remove(win);
                    lock (AnimationList)
                    {
                        if (AnimationList.Contains(win))
                            AnimationList.Remove(win);
                    }
                    win.Dispatcher.InvokeAsync(() =>
                    {
                        win.RenderTransform = old;
                        win.RenderTransformOrigin = origin;
                    });
                }
                catch //(Exception ex)
                {
                    //LogHelper.LogError(ex.Message, ex);
                }
            };
            win.Activate();
            Storyboard.Begin();
        }
    }

    public struct COPYDATASTRUCT
    {
        public IntPtr dwData; // 任意值
        public int cbData;    // 指定lpData内存区域的字节数
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpData; // 发送给⽬标窗⼝所在进程的数据
    }
}
