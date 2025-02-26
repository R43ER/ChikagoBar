using System.Windows;
using System.Windows.Controls;

namespace ChikagoBar
{
    /// <summary>
    /// Логика взаимодействия для PromtWindow.xaml
    /// </summary>
    public partial class PromtWindow : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.No;
        public PromtWindow(string title, string content, MessageBoxButton buttons)
        {
            InitializeComponent();
            this.Title = title;
            TextBlock.Text = content;

            if (buttons == MessageBoxButton.OK)
            {
                btnYes.Content = "OK";
                btnNo.Visibility = Visibility.Collapsed;
            }
            else if (buttons == MessageBoxButton.YesNo)
            {
                btnYes.Content = "Да";
                btnNo.Content = "Нет";
            }
        }
        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            this.DialogResult = true;
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            this.DialogResult = false;
        }
        public static class PromtWindowBoxHelper
        {
            public static MessageBoxResult Show(string message, string title = "Сообщение", MessageBoxButton buttons = MessageBoxButton.YesNo)
            {
                PromtWindow msgBox = new PromtWindow(message, title, buttons);
                bool? result = msgBox.ShowDialog();
                return result == true ? msgBox.Result : MessageBoxResult.No;
            }
        }
    }
}
