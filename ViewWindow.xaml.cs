using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ChicagoBar;

namespace ChikagoBar
{
    /// <summary>
    /// Логика взаимодействия для ViewWindow.xaml
    /// </summary>
    public partial class ViewWindow : Window
    {
        public ICommand ExitCommand { get; }
        List<ZakazItem> zakazList = new List<ZakazItem>();
        List<AsortItem> basketList = new List<AsortItem>();
        List<VimirItem> vimirList = new List<VimirItem>();
        List<AsortItem> asortList = new List<AsortItem>();

        public ViewWindow()
        {
            InitializeComponent();
            this.ExitCommand = new RelayCommand(_ => btnExit_Click(this, null));
            LoadOrders();
        }

        private void LoadOrders()
        {

            DatabaseHelper.ExecuteQuery(DatabaseType.Data, "SELECT * FROM Asort", reader =>
            {
                while (reader.Read())
                {

                    asortList.Add(new AsortItem
                    {
                        ID = reader.GetInt32(0),
                        Name = reader.GetString(3),
                        VimirNo = reader.GetInt32(4),
                        Price = reader.GetFloat(5)
                    });
                }
            });
            DatabaseHelper.ExecuteQuery(DatabaseType.Data, "SELECT * FROM Vimir", reader =>
            {
                while (reader.Read())
                {

                    vimirList.Add(new VimirItem
                    {
                        VimirNo = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        NotFractal = Convert.ToBoolean(reader.GetValue(2))
                    });
                }
            });
            DatabaseHelper.ExecuteQuery(DatabaseType.Bar, "SELECT * FROM Zakaz;", reader =>
            {
                while (reader.Read())
                {
                    zakazList.Add(new ZakazItem
                    {
                        ZakazID = reader.GetInt32(0),
                        ZakazNo = reader.GetValue(2).ToString(),
                        Date = reader.GetDateTime(1).ToString("g")
                    }); ;
                }
            });
            zakazDataGrid.ItemsSource = zakazList;
        }

        private void zakazDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (zakazDataGrid.SelectedItem is ZakazItem selectedItem)
            {
                basketList.Clear();
                asortDataGrid.ItemsSource = null;
                DatabaseHelper.ExecuteQuery(DatabaseType.Bar, $"SELECT * FROM ZakazD WHERE ZakazID = {selectedItem.ZakazID};", delegate (SQLiteDataReader reader)
                {
                    float totalSumm = 0;
                    List<AsortItem> tempList = new List<AsortItem>();
                    while (reader.Read())
                    {
                        AsortItem tempAsortItem = asortList.FirstOrDefault(v => v.ID == reader.GetInt32(4));
                        totalSumm = totalSumm + reader.GetFloat(7);
                        VimirItem vimir = vimirList.FirstOrDefault(v => v.VimirNo == tempAsortItem.VimirNo);
                        tempList.Add(new AsortItem
                        {
                            ID = reader.GetInt32(4),
                            AsortCode = reader.GetString(5),
                            Quant = reader.GetFloat(6),
                            Summ = reader.GetFloat(7),
                            VimirNo = tempAsortItem.VimirNo,
                            Vimir = vimir.Name,
                            Name = tempAsortItem.Name,
                            Price = tempAsortItem.Price
                        });
                    }
                    basketList.Clear();
                    basketList.AddRange(tempList);
                    tempList.Clear();
                    asortDataGrid.ItemsSource = basketList;
                    zakazSumm.Text = totalSumm.ToString("F2");
                });
            }
        }

        private void btnDuplicate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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
