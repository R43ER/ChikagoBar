using System;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ChikagoBar
{
    public partial class CashWindow : Window
    {
        private readonly string logFilePath;
        public ICommand ExitCommand { get; }

        public CashWindow()
        {
            InitializeComponent();

            this.PreviewKeyDown += CashWindow_PreviewKeyDown;
            ExitCommand = new RelayCommand(_ => btnExit_Click(this, null));
            this.DataContext = this;

            string logsDirectory = "logs";
            if (!Directory.Exists(logsDirectory))
                Directory.CreateDirectory(logsDirectory);

            logFilePath = Path.Combine(logsDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.log");

            LogAction("Окно Кассы открыто");

            ExecuteQuery("bar.db", "SELECT * FROM Cash WHERE CashNo = 1;", reader =>
            {
                startCash.Text = reader["Start"].ToString();
                curCash.Text = reader["Rest"].ToString();
            });
        }

        private void CashWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F2:
                    btnPlus_Click(sender, e);
                    break;
                case Key.F4:
                    btnMinus_Click(sender, e);
                    break;
                case Key.F8:
                    btnXReport_Click(sender, e);
                    break;
                case Key.F9:
                    btnZReport_Click(sender, e);
                    break;
            }
        }

        private void btnPlus_Click(object sender, RoutedEventArgs e)
        {
            InputBox summBox = new InputBox("Введите сумму", "Сколько нужно внести?");
            if (summBox.ShowDialog() == true)
            {
                bool res = int.TryParse(summBox.InputText, out var addSumm);
                if (res)
                {
                    ExecuteNonQuery("bar.db", "UPDATE Cash SET Rest = Rest + @add WHERE CashNo = 1", new SQLiteParameter("@add", addSumm));
                    ExecuteQuery("bar.db", "SELECT * FROM Cash WHERE CashNo = 1;", reader =>
                    {
                        curCash.Text = reader["Rest"].ToString();
                    });
                    LogAction($"Добавлено {addSumm} в кассу");
                }
            }
        }

        private void btnMinus_Click(object sender, RoutedEventArgs e)
        {
            InputBox summBox = new InputBox("Введите сумму", "Сколько нужно выдать?");
            if (summBox.ShowDialog() == true)
            {
                bool res = int.TryParse(summBox.InputText, out var addSumm);
                if (res)
                {
                    ExecuteNonQuery("bar.db", "UPDATE Cash SET Rest = Rest - @add WHERE CashNo = 1", new SQLiteParameter("@add", addSumm));
                    ExecuteQuery("bar.db", "SELECT * FROM Cash WHERE CashNo = 1;", reader =>
                    {
                        curCash.Text = reader["Rest"].ToString();
                    });
                    LogAction($"Из кассы выдано {addSumm}");
                }
            }
        }

        private void btnXReport_Click(object sender, RoutedEventArgs e)
        {
            LogAction("Создан X-отчет");
        }

        private void btnZReport_Click(object sender, RoutedEventArgs e)
        {
            LogAction("Создан Z-отчет");
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            LogAction("Выход из окна Кассы");
            this.Close();
        }

        private void LogAction(string action)
        {
            string logEntry = $"{DateTime.Now:HH:mm:ss} - {action}";
            File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
        }

        private void ExecuteNonQuery(string databaseFileName, string sql, params SQLiteParameter[] parameters)
        {
            string databasePath = Path.Combine("database", databaseFileName);
            if (!File.Exists(databasePath))
            {
                MessageBox.Show($"Файл базы данных {databaseFileName} не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters);

                    command.ExecuteNonQuery();
                }
            }
        }

        private void ExecuteQuery(string databaseFileName, string sql, Action<SQLiteDataReader> processRow, params SQLiteParameter[] parameters)
        {
            string databasePath = Path.Combine("database", databaseFileName);
            if (!File.Exists(databasePath))
            {
                MessageBox.Show($"Файл базы данных {databaseFileName} не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            processRow(reader);
                        }
                    }
                }
            }
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
}
