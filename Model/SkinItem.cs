using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace RustSkinScanner.Model
{
    public class SkinItem
    {
        [Key]
        public string MarketHashName { get; set; }
        public string Name { get; set; }
        public decimal CurrentBuyOrder { get; set; }
        public decimal CurrentLowestSell {  get; set; }
        public decimal HistoricalAvg7d { get; set; }
        public decimal HistoricalAvg30d { get; set; }
        public int Volume24h { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
