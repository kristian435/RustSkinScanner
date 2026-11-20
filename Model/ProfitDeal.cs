using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace RustSkinScanner.Model
{
    public enum RiskLevel { Low, Medium, High}
    public class ProfitDeal
    {
        [Key]
        public int Id { get; set; }
        public string MarketHashName { get; set; }
        public decimal NetProfit {  get; set; }
        public decimal ROI { get; set; }
        public RiskLevel Risk {  get; set; }
        public int ArvioituSellTimeDays { get; set; }
        public DateTime CalculatedAt { get; set; }
    }
}
