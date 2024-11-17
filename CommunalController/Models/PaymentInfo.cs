namespace CommunalController.Models
{
    public class PaymentInfo
    {
        public string TypeOfPayment { get; set; }
        public int Size { get; set; }
        public double Rate {  get; set; }
        public double Accrued { get; set; }
    }
}