using CommunalController.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace CommunalController.SetCommunalInfo
{
    /// <summary>
    /// Логика взаимодействия для AddCommunalInfo.xaml
    /// </summary>
    /// 
    public partial class AddCommunalInfo : Window
    {
        private ObservableCollection<PaymentInfo> paymentInfos = new ObservableCollection<PaymentInfo>();
        public AddCommunalInfo(ObservableCollection<PaymentInfo> Info)
        {
            InitializeComponent();
            paymentInfos = Info;
        }

        private void Button_Save(object sender, RoutedEventArgs e)
        {
            try
            {
                paymentInfos.Add(new PaymentInfo { Accrued = double.Parse(accrued.Text), Rate = double.Parse(rate.Text), Size = int.Parse(size.Text), TypeOfPayment = type.Text });
                accrued.Text = rate.Text = size.Text = type.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Неверный формат", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
            }            
            
        }
    }
}
