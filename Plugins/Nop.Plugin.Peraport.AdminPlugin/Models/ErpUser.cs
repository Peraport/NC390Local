using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Peraport.AdminPlugin.Models
{
    public class ErpUserx
    {
        public Int32 ID { get; set; }
        public string CODE { get; set; }
        public string NAME { get; set; }
        public string EMAIL { get; set; }
        public string PASSWORD { get; set; }
        public string ADDRESS { get; set; }
        public string ADDRESS2 { get; set; }
        public string CITY { get; set; }
        public string COUNTRY { get; set; }
        public string NOTE { get; set; }
        public decimal LIMIT { get; set; }
        public string SPECODE { get; set; }
        public string SPECODE2 { get; set; }
        public string SPECODE3 { get; set; }
        public string SPECODE4 { get; set; }
        public string SPECODE5 { get; set; }

    }
    public class ErpProductx
    {
        public Int32 ID { get; set; }
        public string CODE { get; set; }
        public string NAME { get; set; }
        public string BARCODE { get; set; }
        public string MANUFACTURE { get; set; }
        public string BRAND { get; set; }
        public string CATEGORY1 { get; set; }
        public string CATEGORY2 { get; set; }
        public string CATEGORY3 { get; set; }
        public string NOTE { get; set; }
        public int STOK { get; set; }
        public int KDV { get; set; }
        public decimal PRICE { get; set; }
        public int CURRENCY { get; set; }
        public DateTime BEGDATE { get; set; }
        public DateTime ENDDATE { get; set; }
        public DateTime MODIFIEDDATE { get; set; }

    }
}
