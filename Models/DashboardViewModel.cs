using System.Data;

namespace IMSMIS.Models
{
    public class DashboardViewModel
    {
        public DataTable DataTable1 { get; set; }
        public DataTable DataTable2 { get; set; }
        public DataTable DataTable3 { get; set; }
		public List<Data> ListData { get; set; }
        public List<Data1> ListData1 { get; set; }
    }
}
