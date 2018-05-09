using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Peraport.WidgetFinancial.Models
{
    public class ShipTrackingModel
    {
        public string ProcessResult { get; set; }
        public int OrderId { get; set; }
        public string InvoiceCode { get; set; }
        public string OrderDate { get; set; }

        public string FirstBranch { get; set; }
        public string LastBranch { get; set; }

        public string SenderName { get; set; }
        public string CustomerName { get; set; }

        public string CustomerHandleName { get; set; }
        public string CustomerHandleDate { get; set; }

        public List<ShipTrackingRowModel> Rows;
        public ShipTrackingModel()
        {
            Rows = new List<ShipTrackingRowModel>();
        }
    }

    public class ShipTrackingRowModel
    {
        public string CargoBranch { get; set; }
        public string CargoMovementDate { get; set; }
        public string CargoMovementNote { get; set; }
        public string CargoStateNote { get; set; }

    }



}
