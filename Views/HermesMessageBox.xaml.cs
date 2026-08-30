using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Hermes_Executor.Views
{
    public partial class HermesMessageBox : Window
    {
        public enum NotificationType
        {
            Success,
            Error,
            Warning,
            Info
        }

        public bool Result { get; private set; } = false;

        public HermesMessageBox(string title, string message, NotificationType type = NotificationType.Success, bool isConfirm = false)
        {
            InitializeComponent();
            TxtTitle.Text = title;
            TxtMessage.Text = message;

            // Configure based on type
            switch (type)
            {
                case NotificationType.Success:
                    TxtIcon.Text = "✓";
                    break;
                case NotificationType.Error:
                    TxtIcon.Text = "✕";
                    break;
                case NotificationType.Warning:
                    TxtIcon.Text = "⚠";
                    break;
                case NotificationType.Info:
                    TxtIcon.Text = "ℹ";
                    break;
            }

            if (isConfirm)
            {
                BtnYes.Content = "YES";
                BtnNo.Visibility = Visibility.Visible;
            }

            // Allow dragging window
            MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
            
            // Allow ESC key to close
            KeyDown += (s, e) => { if (e.Key == Key.Escape) { Result = false; CloseWithAnimation(); } };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Resources["OpenAnimation"] is Storyboard sb)
            {
                sb.Begin();
            }
        }

        private void CloseWithAnimation()
        {
            var sb = new Storyboard();
            var fadeAnim = new DoubleAnimation(1, 0, new Duration(System.TimeSpan.FromMilliseconds(120)));
            Storyboard.SetTargetName(fadeAnim, "MainBorder");
            Storyboard.SetTargetProperty(fadeAnim, new PropertyPath(OpacityProperty));
            sb.Children.Add(fadeAnim);

            sb.Completed += (s, e) => Close();
            sb.Begin(this);
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            DialogResult = true;
            CloseWithAnimation();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            DialogResult = false;
            CloseWithAnimation();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            CloseWithAnimation();
        }

        public static bool Show(string title, string message, NotificationType type = NotificationType.Success, bool isConfirm = false)
        {
            var msgBox = new HermesMessageBox(title, message, type, isConfirm);
            msgBox.ShowDialog();
            return msgBox.Result;
        }
    }
}
