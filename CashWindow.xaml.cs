using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ChikagoBar
{
    public partial class CashWindow : Window
    {
        private readonly string logFilePath;
        List<CashItem> cashList = new List<CashItem>();
        public ICommand ExitCommand { get; }

        public CashWindow()
        {
            InitializeComponent();

            this.PreviewKeyDown += CashWindow_PreviewKeyDown;
            ExitCommand = new RelayCommand(_ => btnExit_Click(this, null));
            this.DataContext = this;

            logFilePath = Path.Combine("Logs", $"log_{DateTime.Now:yyyy-MM-dd}.log");

            LogAction("Окно Кассы открыто");
            DatabaseHelper.ExecuteQuery("SELECT * FROM Cash WHERE CashNo = 1;", reader =>
            {
                while (reader.Read())
                {
                    startCash.Text = reader.GetFloat(3).ToString("F2");
                    curCash.Text = reader.GetFloat(5).ToString("F2");
                    startTime.Text = reader.GetDateTime(2).ToString("dd.MM.yyyy HH:mm");
                }
            });
            DatabaseHelper.ExecuteQuery("SELECT * FROM Zakaz WHERE AsortNo IN (-2,-1, -7) ORDER BY Date ASC;", reader =>
            {
                while (reader.Read())
                {
                    startCash.Text = reader.GetFloat(3).ToString("F2");
                    curCash.Text = reader.GetFloat(5).ToString("F2");
                    startTime.Text = reader.GetDateTime(2).ToString("dd.MM.yyyy HH:mm");
                }
            });
            UpdateCashTable();
        }

        private void UpdateCashTable()
        {
            DatabaseHelper.ExecuteQuery("SELECT * FROM Zakaz WHERE AsortNo IN (-2,-1, -7) ORDER BY Date ASC;", reader =>
            {
                cashList.Clear();
                cashDataGrid.ItemsSource = null;
                List<CashItem> tempList = new List<CashItem>();
                while (reader.Read())
                {
                    string typeOper = "";
                    switch (reader.GetInt32(3))
                    {
                        case -7:
                            typeOper = "Обнуление Z-отчет";
                            break;
                        case -2:
                            typeOper = "Выдача наличных";
                            break;
                        case -1:
                            typeOper = "Внесение наличных";
                            break;
                    }
                    tempList.Add(new CashItem
                    {
                        Date = reader.GetDateTime(1).ToString("HH:mm dd.MM.yyyy"),
                        Type = typeOper,
                        Amount = reader.GetFloat(6).ToString("F2")
                    });
                }
                cashList.Clear();
                cashList.AddRange(tempList);
                tempList.Clear();
                cashDataGrid.ItemsSource = cashList;
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
                    var zakazDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    float Quantity = 0;
                    DatabaseHelper.BeginTransaction();
                    DatabaseHelper.ExecuteNonQuery("UPDATE Cash SET Rest = Rest + @add WHERE CashNo = 1;", new SQLiteParameter("@add", addSumm));
                    DatabaseHelper.ExecuteNonQuery("INSERT INTO Zakaz (Date, ZakazNo, AsortNo, AsortCode, Quantity, Amount, CashNo, OperNo, Release, PrintCheck, Discount, DiscountType, CardNo) VALUES (@Date, @ZakazNo, @AsortNo, @AsortCode, @Quantity, @Amount, @CashNo, @OperNo, @Release, @PrintCheck, @Discount, @DiscountType, @CardNo)",
                        new SQLiteParameter("@Date", zakazDate),
                        new SQLiteParameter("@ZakazNo", null),
                        new SQLiteParameter("@AsortNo", -1),
                        new SQLiteParameter("@AsortCode", -1),
                        new SQLiteParameter("@Quantity", Quantity),
                        new SQLiteParameter("@Amount", addSumm),
                        new SQLiteParameter("@CashNo", 1),
                        new SQLiteParameter("@OperNo", 1),
                        new SQLiteParameter("@Release", false),
                        new SQLiteParameter("@PrintCheck", false),
                        new SQLiteParameter("@Discount", false),
                        new SQLiteParameter("@DiscountType", false),
                        new SQLiteParameter("@CardNo", null));
                    DatabaseHelper.CommitTransaction();
                    DatabaseHelper.ExecuteQuery("SELECT * FROM Cash WHERE CashNo = 1;", reader =>
                    {
                        while (reader.Read())
                        {
                            curCash.Text = reader.GetFloat(5).ToString("F2");
                        }
                    });
                    UpdateCashTable();
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
                    var zakazDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    float Quantity = 0;
                    DatabaseHelper.BeginTransaction();
                    DatabaseHelper.ExecuteNonQuery("UPDATE Cash SET Rest = Rest - @add WHERE CashNo = 1;", new SQLiteParameter("@add", addSumm));
                    addSumm = addSumm * -1;
                    DatabaseHelper.ExecuteNonQuery("INSERT INTO Zakaz (Date, ZakazNo, AsortNo, AsortCode, Quantity, Amount, CashNo, OperNo, Release, PrintCheck, Discount, DiscountType, CardNo) VALUES (@Date, @ZakazNo, @AsortNo, @AsortCode, @Quantity, @Amount, @CashNo, @OperNo, @Release, @PrintCheck, @Discount, @DiscountType, @CardNo)",
                        new SQLiteParameter("@Date", zakazDate),
                        new SQLiteParameter("@ZakazNo", null),
                        new SQLiteParameter("@AsortNo", -1),
                        new SQLiteParameter("@AsortCode", -1),
                        new SQLiteParameter("@Quantity", Quantity),
                        new SQLiteParameter("@Amount", addSumm),
                        new SQLiteParameter("@CashNo", 1),
                        new SQLiteParameter("@OperNo", 1),
                        new SQLiteParameter("@Release", false),
                        new SQLiteParameter("@PrintCheck", false),
                        new SQLiteParameter("@Discount", false),
                        new SQLiteParameter("@DiscountType", false),
                        new SQLiteParameter("@CardNo", null));
                    DatabaseHelper.CommitTransaction();
                    LogAction($"Из кассы выдано {addSumm}");
                }
                DatabaseHelper.ExecuteQuery("SELECT * FROM Cash WHERE CashNo = 1;", reader =>
                {
                    while (reader.Read())
                    {
                        curCash.Text = reader.GetFloat(5).ToString("F2");
                    }
                });
                UpdateCashTable();
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

    public class CashItem
    {
        public string Date { get; set; }
        public string Type { get; set; }
        public string Amount { get; set; }
    }
}
