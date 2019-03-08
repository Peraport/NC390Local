using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Peraport.WidgetFinancial.Models
{
    public class FiModel
    {
        public string CODE { get; set; }
        public string NOP_CODE { get; set; }
        public string NAME { get; set; }
        public decimal BAKIYE { get; set; }
        public decimal LIMIT { get; set; }
        public decimal CURRENT_LIMIT { get; set; }
        public bool isAuth { get; set; }
        public string NOTE { get; set; }
    }

    public class FiModelFatura
    {
        public long ID { get; set; }
        public decimal TUTAR { get; set; }
    }

    public class EFaturaModel
    {
        public long ID { get; set; }
        public decimal TUTAR { get; set; }
    }

}
