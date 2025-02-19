using ChikagoBar;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace ChicagoBar
{
    public partial class MainWindow : Window
    {
        private readonly string logFilePath;

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

            string logsDirectory = "logs";
            if (!Directory.Exists(logsDirectory))
                Directory.CreateDirectory(logsDirectory);

            logFilePath = Path.Combine(logsDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.log");
        }

        private void LogAction(string action)
        {
            string logEntry = $"{DateTime.Now:HH:mm:ss} - {action}";
            File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
        }

        private void BtnOrder_Click(object sender, RoutedEventArgs e)
        {
            LogAction("Кнопка Заказ нажата");
            MessageBoxResult discountResult = MessageBox.Show("Есть дисконтная карта?", "Скидка", MessageBoxButton.YesNo, MessageBoxImage.Question);
            bool discount = false;
            string discountCard = null;

            if (discountResult == MessageBoxResult.Yes)
            {
                InputBox inputBox = new InputBox("Введите номер карты", "Скидка");
                if (inputBox.ShowDialog() == true)
                {
                    if (inputBox.InputText == "12345")
                    {
                        MessageBox.Show("Будет предоставлена скидка", "Скидка", MessageBoxButton.OK, MessageBoxImage.Information);
                        discount = true;
                        discountCard = "12345";
                        LogAction("Скидка применена");
                    }
                    else
                    {
                        MessageBox.Show("Скидка не предоставляется", "Скидка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        LogAction("Скидка не применена");
                    }
                }
            }

            OrderWindow orderWindow = new OrderWindow(discount, discountCard);
            orderWindow.Show();
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            LogAction("Кнопка Просмотр нажата");
            ViewWindow viewWindow = new ViewWindow();
            viewWindow.Show();
        }

        private void BtnReturn_Click(object sender, RoutedEventArgs e)
        {
            LogAction("Кнопка Возврат нажата");
            ReturnWindow returnWindow = new ReturnWindow();
            returnWindow.Show();
        }

        private void BtnCash_Click(object sender, RoutedEventArgs e)
        {
            LogAction("Кнопка Касса нажата");
            InputBox passwordBox = new InputBox("Введите пароль", "Авторизация");
            if (passwordBox.ShowDialog() == true && passwordBox.InputText == "12345678")
            {
                LogAction("Успешный вход в Кассу");
                CashWindow cashWindow = new CashWindow();
                cashWindow.Show();
            }
            else
            {
                LogAction("Неудачная попытка входа в Кассу");
                MessageBox.Show("Неверный пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            LogAction("Программа закрывается");
            Application.Current.Shutdown();
        }

        private void BtnEndOfDay_Click(object sender, RoutedEventArgs e)
        {
            LogAction("Конец дня");
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            LogAction("Попытка закрытия программы");
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
                LogAction("Закрытие программы отменено");
                return false;
            }

            MessageBoxResult shutdownResult = MessageBox.Show("Завершить работу компьютера?", "Выключение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (shutdownResult == MessageBoxResult.Yes)
            {
                LogAction("Компьютер выключается");
                ShutdownComputer();
            }
            else
            {
                LogAction("Программа закрыта без выключения компьютера");
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
