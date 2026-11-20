using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RustSkinScanner.Model;

namespace RustSkinScanner.Controller
{
    public class ProfitLaskin
    {
        public ProfitDeal Laske(SkinItem item)
        {
            decimal netSell = item.CurrentLowestSell * (1 - 0.15m);
            decimal profit = netSell - item.CurrentBuyOrder;
            decimal roi = item.CurrentBuyOrder > 0 ? (profit / item.CurrentBuyOrder) * 100 : 0;

            RiskLevel risk = item.Volume24h > 50 ? RiskLevel.Low : (item.Volume24h > 10 ? RiskLevel.Medium : RiskLevel.High);
            int sellTime = risk == RiskLevel.Low ? 1 : (risk == RiskLevel.Medium ? 3 : 7);

            return new ProfitDeal
            {
                MarketHashName = item.MarketHashName,
                NetProfit = profit,
                ROI = roi,
                Risk = risk,
                ArvioituSellTimeDays = sellTime,
                CalculatedAt = DateTime.Now
            };
        }
    }
}
