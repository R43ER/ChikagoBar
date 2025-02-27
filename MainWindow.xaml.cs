using Ionic.Zip;
using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ChikagoBar
{
    public partial class MainWindow : Window
    {
        public string logFilePath;
        private bool openShift;
        public bool connectKKT;
        public ICommand ExitCommand { get; }

        public MainWindow()
        {
            InitializeComponent();
            ExitCommand = new RelayCommand(_ => BtnExit_Click(this, null));
            DataContext = this;
            KeyDown += MainWindow_KeyDown;
            btnExit.Click += BtnExit_Click;
            btnEndOfDay.Click += BtnEndOfDay_Click;
            btnOrder.Click += BtnOrder_Click;
            btnView.Click += BtnView_Click;
            btnReturn.Click += BtnReturn_Click;
            btnCash.Click += BtnCash_Click;
            Closing += MainWindow_Closing;
            Loaded += MainWindow_Loaded;

            CheckFolders();
        }

        private void CheckFolders()
        {
            string logsDirectory = "Logs";
            if (!Directory.Exists(logsDirectory))
                Directory.CreateDirectory(logsDirectory);
            logFilePath = Path.Combine(logsDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.log");
            string archiveDirectory = "Archive";
            if (!Directory.Exists(archiveDirectory))
                Directory.CreateDirectory(archiveDirectory);
        }

        public void LogAction(string action)
        {
            string logEntry = $"{DateTime.Now:HH:mm:ss} - {action}";
            File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            openShift = Properties.Settings.Default.openShift;
            if (!openShift)
            {
                MessageBoxResult shiftResult = MessageBox.Show("Смена закрыта. Хотите открыть?", "Смена", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
                if (shiftResult == MessageBoxResult.Yes)
                {
                    var dateStart = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    DatabaseHelper.ExecuteNonQuery("UPDATE Cash SET DateStart = @DateStart WHERE CashNo = 1", new SQLiteParameter("@DateStart", dateStart));
                    Properties.Settings.Default.openShift = true;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F2:
                    BtnOrder_Click(sender, e);
                    break;
                case Key.F3:
                    BtnView_Click(sender, e);
                    break;
                case Key.F4:
                    BtnReturn_Click(sender, e);
                    break;
                case Key.F5:
                    BtnCash_Click(sender, e);
                    break;
                case Key.F7:
                    BtnEndOfDay_Click(sender, e);
                    break;
            }
        }

        private void BtnOrder_Click(object sender, RoutedEventArgs e)
        {
            LogAction("Кнопка Заказ нажата");
            MessageBoxResult discountResult = MessageBox.Show("Есть дисконтная карта?", "Скидка", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            bool discount = false;
            string discountCard = null;

            if (discountResult == MessageBoxResult.Yes)
            {
                InputBox inputBox = new InputBox("Введите номер карты", "Скидка");
                if (inputBox.ShowDialog() == true)
                {
                    if (inputBox.InputText == "123")
                    {
                        MessageBox.Show("Будет предоставлена скидка", "Скидка", MessageBoxButton.OK, MessageBoxImage.Information);
                        discount = true;
                        discountCard = inputBox.InputText;
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
            MessageBoxResult discountResult = MessageBox.Show("Есть дисконтная карта?", "Скидка", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            bool discount = false;
            string discountCard = null;

            if (discountResult == MessageBoxResult.Yes)
            {
                InputBox inputBox = new InputBox("Введите номер карты", "Скидка");
                if (inputBox.ShowDialog() == true)
                {
                    if (inputBox.InputText == "123")
                    {
                        MessageBox.Show("Будет предоставлена скидка", "Скидка", MessageBoxButton.OK, MessageBoxImage.Information);
                        discount = true;
                        discountCard = inputBox.InputText;
                        LogAction("Скидка применена");
                    }
                    else
                    {
                        MessageBox.Show("Скидка не предоставляется", "Скидка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        LogAction("Скидка не применена");
                    }
                }
            }
            ReturnWindow returnWindow = new ReturnWindow(discount, discountCard);
            returnWindow.Show();
        }

        private void BtnCash_Click(object sender, RoutedEventArgs e)
        {
            LogAction("Кнопка Касса нажата");
            InputBox passwordBox = new InputBox("Введите пароль", "Авторизация");
            if (passwordBox.ShowDialog() == true && passwordBox.InputText == "123")
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
            MessageBoxResult endDayResult = MessageBox.Show("Вы точно хотите завершить день?", "Закрытие дня", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (endDayResult == MessageBoxResult.Yes)
            {
                LogAction("Конец дня");
                string curTime = DateTime.Now.Ticks.ToString();
                string archiveDirectory = "Archive";
                string databaseDirectory = "Database";
                string logsDirectory = "Logs";
                string archivePath = Path.Combine(archiveDirectory, $"{curTime}.zip");
                using (var zip = new ZipFile())
                {
                    AddFilesToZip(zip, databaseDirectory);
                    AddFilesToZip(zip, logsDirectory);
                    zip.Save(archivePath); // Сохраняем ZIP-файл
                }
                LogAction($"Архив создан: {archivePath}");
                DatabaseHelper.ExecuteNonQuery("DELETE FROM Zakaz;");
                Properties.Settings.Default.curOrderNo = 1;
                Properties.Settings.Default.openShift = false;
                Properties.Settings.Default.Save();
                LogAction("День закрыт. Таблица очищена.");
            }
        }

        static void AddFilesToZip(ZipFile zip, string sourceDirectory)
        {
            if (!Directory.Exists(sourceDirectory))
                return; // Если папки нет, пропускаем

            string[] files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                zip.AddFile(file).FileName = Path.GetFileName(file);
            }
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
                //ShutdownComputer();
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

        public class RelayCommand : ICommand
        {
            private readonly Action<object> execute;
            private readonly Predicate<object> canExecute;

            public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
            {
                this.execute = execute;
                this.canExecute = canExecute;
            }

            public bool CanExecute(object parameter) => canExecute?.Invoke(parameter) ?? true;

            public void Execute(object parameter) => execute(parameter);

            public event EventHandler CanExecuteChanged;
        }
    }

    public class ZakazItem
    {
        public int ZakazID { get; set; }
        public string ZakazNo { get; set; }
        public DateTime Date { get; set; }
        public string FormattedDate { get; set; }
    }

    public class GrpProdItem
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

    public class AsortItem
    {
        public int ID { get; set; }
        public string AsortCode { get; set; }
        public string Name { get; set; }
        public int VimirNo { get; set; }
        public string Vimir { get; set; }
        public float Price { get; set; }
        public float Quant { get; set; }
        public float Summ { get; set; }
    }

    public class VimirItem
    {
        public int VimirNo { get; set; }
        public string Name { get; set; }
        public bool NotFractal { get; set; }
    }
}
