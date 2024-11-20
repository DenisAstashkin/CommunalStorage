using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunalController.Graph;
using CommunalController.Models;
using CommunalController.SetCommunalInfo;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;

namespace CommunalController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    class ViewId : INotifyPropertyChanged
    {
        private ObservableCollection<string> _ids;

        public ObservableCollection<string> Ids 
        {
            get => _ids;
            set
            {
                _ids = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string ProperyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ProperyName));
    }

    class ComView : INotifyPropertyChanged
    {
        private ObservableCollection<PaymentInfo> _comInfo;

        public ObservableCollection<PaymentInfo> ComInfo
        {
            get => _comInfo;
            set
            {
                _comInfo = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string ProperyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ProperyName));
    }

    public partial class MainWindow : Window
    {
        private ViewId IdReceipt;
        private ComView comView;

        private string conBase = "Data Source=";

        public MainWindow()
        {
            InitializeComponent();

            MessageBox.Show("Выберите путь к базеданных", "Информация", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None);
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "DataBase (*.db)|*.db";
            dialog.ShowDialog();
            
            if (!File.Exists(dialog.FileName))
            {
                MessageBox.Show("Данный путь ошибочный", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
                Close();
            }

            conBase += dialog.FileName;

            CreateTable();

            IdReceipt = new ViewId();
            comView = new ComView();
            comView.ComInfo = new ObservableCollection<PaymentInfo>();

            DataContext = IdReceipt;
            DataCommunal.ItemsSource = comView.ComInfo;

            IdReceipt.Ids = new ObservableCollection<string>();
            if (!GetIds())            
                MessageBox.Show("Ошибка получения УИН", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
        }

        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListViewItem;
            if (item != null && item.IsSelected)
            {
                comView.ComInfo.Clear();
                using (var connection = new SqliteConnection(conBase))
                {
                    connection.Open();

                    SqliteCommand command = new SqliteCommand("SELECT * FROM Person", connection);
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                if (item.Content.ToString() == reader.GetString(1))
                                {
                                    IdStorage.Text = reader.GetString(1);
                                    FullName.Text = reader.GetString(2);
                                    DateCreate.Text = ConvertDateToProgramm(reader.GetString(3))[2] + "." + ConvertDateToProgramm(reader.GetString(3))[1] + "." + ConvertDateToProgramm(reader.GetString(3))[0];
                                    Address.Text = reader.GetString(4);
                                    DatePay.Text = ConvertDateToProgramm(reader.GetString(5))[2] + "." + ConvertDateToProgramm(reader.GetString(5))[1] + "." + ConvertDateToProgramm(reader.GetString(5))[0];
                                    break;
                                }
                            }
                        }
                    }
                    command = new SqliteCommand("SELECT * FROM PaymentInfo", connection);
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            double totalcost = 0;
                            while (reader.Read())
                            {
                                if (item.Content.ToString() == reader.GetString(1))
                                {
                                    comView.ComInfo.Add(new PaymentInfo {TypeOfPayment=reader.GetString(2), Size=reader.GetInt32(3), Rate= reader.GetDouble(4), Accrued= reader.GetDouble(5)});
                                    totalcost += reader.GetDouble(5);
                                }
                            }
                            TotalCost.Text = totalcost.ToString();
                        }
                    }
                }
            }
        }

        private void Button_Add(object sender, RoutedEventArgs e)
        {
            if (!Validate() || IdReceipt.Ids.IndexOf(IdStorage.Text) >= 0)
            {
                MessageBox.Show("Неверно заполненные поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
                return;
            }

            IdReceipt.Ids.Add(IdStorage.Text);

            using (var connection = new SqliteConnection(conBase))
            {
                connection.Open();

                SqliteCommand command = connection.CreateCommand();
                command.Connection = connection;
                command.CommandText = $"INSERT INTO IdReceipts (receipts) VALUES (\"{IdStorage.Text}\");" +
                                      $"INSERT INTO Person (IdStorage, FullName, DateCreate, Address, PayData) VALUES (\"{IdStorage.Text}\", \"{FullName.Text}\", \"{ConvertDateToSql(DateCreate.Text)[2]}-{ConvertDateToSql(DateCreate.Text)[1]}-{ConvertDateToSql(DateCreate.Text)[0]}\", \"{Address.Text}\", \"{ConvertDateToSql(DatePay.Text)[2]}-{ConvertDateToSql(DatePay.Text)[1]}-{ConvertDateToSql(DatePay.Text)[0]}\");";
                
                command.ExecuteNonQuery();
                for (int i = 0; i < comView.ComInfo.Count; i++)
                {
                    command.CommandText = $"INSERT INTO PaymentInfo (IdStorage, Type, Size, Rate, Accrued, PayDate) VALUES (\"{IdStorage.Text}\", \"{comView.ComInfo[i].TypeOfPayment}\", {comView.ComInfo[i].Size}, {comView.ComInfo[i].Rate}, {comView.ComInfo[i].Accrued}, \"{ConvertDateToSql(DatePay.Text)[2]}-{ConvertDateToSql(DatePay.Text)[1]}-{ConvertDateToSql(DatePay.Text)[0]}\");";
                    command.ExecuteNonQuery();
                }
            }

            comView.ComInfo.Clear();
            FullName.Text = Address.Text = IdStorage.Text = DateCreate.Text = DatePay.Text = string.Empty;            
        }

        private void Button_AddTable(object sender, RoutedEventArgs e)
        {
            AddCommunalInfo communalInfo = new AddCommunalInfo(comView.ComInfo);
            communalInfo.Show();
        }

        private void Button_Delete(object sender, RoutedEventArgs e)
        {            
            string? id = IdStore.SelectedItem?.ToString();
            

            if (id == null)
                return;

            if (id.Equals(IdStorage.Text))
            {
                FullName.Text = Address.Text = IdStorage.Text = DateCreate.Text = DatePay.Text = TotalCost.Text = string.Empty;
                comView.ComInfo.Clear();
            }

            string sqlExpression = $"DELETE FROM Person WHERE IdStorage='{id}';" +
                                   $"DELETE FROM PaymentInfo WHERE IdStorage='{id}';" +
                                   $"DELETE FROM IdReceipts WHERE receipts='{id}';";
            using (var connection = new SqliteConnection(conBase))
            {
                connection.Open();

                SqliteCommand command = new SqliteCommand(sqlExpression, connection);

                int number = command.ExecuteNonQuery();
            }
            foreach (var i in IdReceipt.Ids)
            {
                if (i.Equals(id))
                {
                    IdReceipt.Ids.Remove(i);
                    break;
                }
            }
        }

        private void ShowStatic(object sender, RoutedEventArgs e)
        {
            GetGraph getGraph = new GetGraph(conBase, "PaymentInfo", "Type");
            getGraph.Show();
        }

        private bool GetIds()
        {
            try
            {
                using (var connection = new SqliteConnection(conBase))
                {
                    connection.Open();

                    SqliteCommand command = new SqliteCommand("SELECT * FROM IdReceipts", connection);
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                IdReceipt.Ids.Add(reader.GetString(1));
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    
        private void CreateTable()
        {
            using (var connection = new SqliteConnection(conBase))
            {
                connection.Open();

                SqliteCommand command = new SqliteCommand();
                command.Connection = connection;

                command.CommandText = "CREATE TABLE IF NOT EXISTS Person(id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE, IdStorage TEXT, FullName TEXT, DateCreate TEXT, Address TEXT, PayData TEXT);";
                command.ExecuteNonQuery();

                command.CommandText = "CREATE TABLE IF NOT EXISTS PaymentInfo( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE, IdStorage TEXT, Type TEXT, Size INTEGER, Rate REAL, Accrued REAL, PayDate TEXT);";
                command.ExecuteNonQuery();

                command.CommandText = "CREATE TABLE IF NOT EXISTS IdReceipts(id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE, receipts TEXT);";
                command.ExecuteNonQuery();

            }
        }
    
        private bool Validate()
        {
            DateTime scheduleDate;
            if (IdStorage.Text == string.Empty ||
                FullName.Text == string.Empty ||
                Address.Text == string.Empty ||
                DateCreate.Text == string.Empty ||
                DatePay.Text == string.Empty ||
                comView.ComInfo.Count == 0 ||
                !DateTime.TryParseExact(DatePay.Text, "dd.MM.yyyy", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out scheduleDate) ||
                !DateTime.TryParseExact(DateCreate.Text, "dd.MM.yyyy", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out scheduleDate) ||
                CountCharInString(DatePay.Text, '.') != 2 ||
                CountCharInString(DateCreate.Text, '.') != 2
                )
            {
                return false;
            }
            
            

            return true;
        }
    
        private string[] ConvertDateToSql(string date) => date.Split('.');

        private string[] ConvertDateToProgramm(string date) => date.Split('-');
        
        private int CountCharInString(string str, char symbol)
        {
            int count = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == symbol)
                    count++;
            }
            return count;
        }
    }
}