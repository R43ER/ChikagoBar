using ChikagoBar;
using System;
using System.Diagnostics;
using System.Windows;

namespace ChicagoBar
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            btnExit.Click += BtnExit_Click;
            btnEndOfDay.Click += BtnEndOfDay_Click;
            btnOrder.Click += BtnOrder_Click;
            btnView.Click += BtnView_Click;
            btnReturn.Click += BtnReturn_Click;
            btnCash.Click += BtnCash_Click;
            this.Closing += MainWindow_Closing;
        }

        private void BtnOrder_Click(object sender, RoutedEventArgs e)
        {
            OrderWindow orderWindow = new OrderWindow();
            orderWindow.Show();
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            ViewWindow viewWindow = new ViewWindow();
            viewWindow.Show();
        }

        private void BtnReturn_Click(object sender, RoutedEventArgs e)
        {
            ReturnWindow returnWindow = new ReturnWindow();
            returnWindow.Show();
        }

        private void BtnCash_Click(object sender, RoutedEventArgs e)
        {
            CashWindow cashWindow = new CashWindow();
            cashWindow.Show();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnEndOfDay_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!ConfirmExit())
            {
                e.Cancel = true;
            }
        }

        private bool ConfirmExit()
        {
            MessageBoxResult result = MessageBox.Show("Вы уверены что хотите закрыть программу?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                return false;
            }

            MessageBoxResult shutdownResult = MessageBox.Show("Завершить работу компьютера?", "Выключение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (shutdownResult == MessageBoxResult.Yes)
            {
                ShutdownComputer();
            }
            else
            {
                Application.Current.Shutdown();
            }
            return true;
        }

        private void ShutdownComputer()
        {
            Process.Start("shutdown", "/s /t 0");
        }
    }
}
