using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
