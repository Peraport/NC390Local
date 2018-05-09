using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Peraport.WidgetFinancial.Models
{
    public class DashModel
    {
        public string NAME { get; set; }
        public decimal BAKIYE { get; set; }
        public decimal LIMIT { get; set; }
        public decimal CURRENT_LIMIT { get; set; }
        public int ALL_ORDERS_QTY { get; set; }
        public decimal All_ORDERS_VAL { get; set; }
        public int NOT_YET_ORDERS_QTY { get; set; }
        public decimal NOT_YET_ORDERS_VAL { get; set; }
        public int SHIPPED_ORDERS_QTY { get; set; }
        public decimal SHIPPED_ORDERS_VAL { get; set; }

        public int ALL_DOCUMENTS_QTY { get; set; }
        public decimal ALL_DOCUMENTS_VAL { get; set; }
        public int NOT_PAID_DOCUMENTS_QTY { get; set; }
        public decimal NOT_PAID_DOCUMENTS_VAL { get; set; }
        public int DELAYED_DOCUMENTS_QTY { get; set; }
        public decimal DELAYED_DOCUMENTS_VAL { get; set; }

        public bool isAuth { get; set; }
    }
}
