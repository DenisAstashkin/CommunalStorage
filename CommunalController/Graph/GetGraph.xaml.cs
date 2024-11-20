using System.Globalization;
using System.Net;
using System.Windows;
using Microsoft.Data.Sqlite;
using ScottPlot;

namespace CommunalController.Graph
{
    /// <summary>
    /// Логика взаимодействия для GetGraph.xaml
    /// </summary>
    public partial class GetGraph : Window
    {
        private string _conBase;
        private string _sourceTable;
        private string _sourceColumn;

        public GetGraph(string conBase, string sourceTable, string sourceColumn)
        {
            InitializeComponent();
            _conBase = conBase;
            _sourceTable = sourceTable;
            _sourceColumn = sourceColumn;
        }

        private void BuildGraph(object sender, RoutedEventArgs e)
        {
            if (!Validate())
            {
                MessageBox.Show("Неверно заполненные поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
                return;
            }

            List<string> types = new List<string>();
            using (var connection = new SqliteConnection(_conBase))
            {
                connection.Open();

                SqliteCommand command = new SqliteCommand($"SELECT DISTINCT {_sourceColumn} FROM {_sourceTable}", connection);
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            types.Add(reader.GetString(0));
                        }
                    }
                }
                List<PieSlice> slices = new List<PieSlice>();
                Graph.Plot.Add.Palette = new ScottPlot.Palettes.OneHalfDark();
                for (int i = 0; i < types.Count; i++)
                {
                    command = new SqliteCommand($"SELECT SIZE FROM PaymentInfo WHERE \"Type\" = \"{types[i]}\" AND PayDate BETWEEN \"{ConvertDateToSql(FromDate.Text)[2]}-{ConvertDateToSql(FromDate.Text)[1]}-{ConvertDateToSql(FromDate.Text)[0]}\" AND \"{ConvertDateToSql(ToDate.Text)[2]}-{ConvertDateToSql(ToDate.Text)[1]}-{ConvertDateToSql(ToDate.Text)[0]}\"", connection);
                    int count = 0;
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                count += reader.GetInt32(0);
                            }
                        }
                    }
                    if (count != 0)
                        slices.Add(new PieSlice() { Value = count, Label = types[i] });

                }
                if (slices.Count != 0)
                {
                    Graph.Visibility = Visibility.Visible;
                    var pie = Graph.Plot.Add.Pie(slices);
                    pie.DonutFraction = .5;
                    Graph.Plot.ShowLegend();

                    Graph.Plot.Axes.Frameless();
                    Graph.Plot.HideGrid();

                    Graph.Refresh();
                }
                else
                {
                    Graph.Visibility = Visibility.Hidden;
                }
            }
        }

        private string[] ConvertDateToSql(string date) => date.Split('.');

        private bool Validate()
        {
            DateTime scheduleDate;
            if (
                DateTime.TryParseExact(FromDate.Text, "dd.MM.yyyy", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out scheduleDate) &&
                DateTime.TryParseExact(ToDate.Text, "dd.MM.yyyy", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out scheduleDate) &&
                CountCharInString(FromDate.Text, '.') == 2 &&
                CountCharInString(ToDate.Text, '.') == 2)
            {
                return true;
            }
            return false;
        }

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
