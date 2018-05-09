using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Peraport.PerapayLimit.Models
{
    public class PerapayLimitModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Note { get; set; }
        public decimal Limit { get; set; }
        public decimal ALimit { get; set; }
        public decimal Amount { get; set; }
        public int State { get; set; }
    }
}
