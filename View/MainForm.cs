using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RustSkinScanner.Controller;
using RustSkinScanner.Model;

namespace RustSkinScanner.View
{
    public partial class MainForm : Form
    {
        private readonly SteamMarketLogiikka _logiikka;
        private readonly ProfitLaskin _laskin = new();
        private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        private List<string> _skinList = new()
        {
            "Whiteout Facemask",
            "Heat Seeker LR",
            "Heat Seeker Mp5",
            "No Mercy Tactical Gloves",
            "Heat Seeker SKS"
        };

        public MainForm()
        {
            InitializeComponent();
            _logiikka = new SteamMarketLogiikka(_cache);

            SetupDataGridColumns();

            // Timer asetukset
            timerScan.Interval = 30000; // 30 sekuntia
            timerScan.Tick += TimerScan_Tick;
            timerScan.Enabled = true;

            // Alusta grid heti
            RefreshGrid();
        }

        private void SetupDataGridColumns()
        {
            dgvDeals.AutoGenerateColumns = false;
            dgvDeals.Columns.Clear();

            dgvDeals.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MarketHashName",
                DataPropertyName = "MarketHashName",
                HeaderText = "Skin",
                Width = 220
            });
            dgvDeals.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "NetProfit",
                DataPropertyName = "NetProfit",
                HeaderText = "Profit €",
                DefaultCellStyle = { Format = "N2" },
                Width = 90
            });
            dgvDeals.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ROI",
                DataPropertyName = "ROI",
                HeaderText = "ROI %",
                DefaultCellStyle = { Format = "P2" },
                Width = 80
            });
            dgvDeals.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Risk",
                DataPropertyName = "Risk",
                HeaderText = "Risk",
                Width = 70
            });
        }

        private void RefreshGrid()
        {
            try
            {
                using var db = new SovellusDbContext();

                var deals = db.ProfitDeals
                    .Where(d => d.NetProfit > 0)
                    .OrderByDescending(d => d.ROI)
                    .ToList();

                dgvDeals.DataSource = deals;
                dgvDeals.Refresh();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Grid refresh virhe: {ex.Message}");
            }
        }

        private async void TimerScan_Tick(object sender, EventArgs e)
        {
            await ScanAsync();
        }

        private async Task ScanAsync()
        {
            btnScan.Enabled = false;

            try
            {
                using var db = new SovellusDbContext();

                foreach (var hashName in _skinList)
                {
                    Debug.WriteLine($"=== Käsitellään: {hashName} ===");

                    // 1. Hae dynaaminen itemNameId
                    long itemNameId = await _logiikka.GetItemNameIdAsync(hashName);
                    if (itemNameId == 0)
                    {
                        Debug.WriteLine($"Ei löytynyt itemNameId:tä → ohitetaan");
                        continue;
                    }

                    // 2. Hae histogram (buy/sell)
                    var (buyOrder, lowestSell) = await _logiikka.GetHistogramAsync(hashName, itemNameId);
                    Debug.WriteLine($"Buy: {buyOrder:F2} € | Sell: {lowestSell:F2} €");

                    // 3. Hae price history
                    var (avg7d, avg30d, vol24h) = await _logiikka.GetPriceHistoryAsync(hashName);
                    Debug.WriteLine($"Avg7d: {avg7d:F2} | Vol24h: {vol24h}");

                    // Volume-raja (voit muuttaa tätä)
                    if (vol24h < 5)
                    {
                        Debug.WriteLine($"Volume liian pieni ({vol24h}) → ohitetaan");
                        continue;
                    }

                    // 4. Luo/päivitä SkinItem
                    var item = new SkinItem
                    {
                        MarketHashName = hashName,
                        Name = hashName,
                        CurrentBuyOrder = buyOrder,
                        CurrentLowestSell = lowestSell,
                        HistoricalAvg7d = avg7d,
                        HistoricalAvg30d = avg30d,
                        Volume24h = vol24h,
                        LastUpdated = DateTime.Now
                    };

                    var existing = await db.SkinItems.FindAsync(hashName);
                    if (existing == null)
                    {
                        db.SkinItems.Add(item);
                        Debug.WriteLine("Lisätään uusi SkinItem");
                    }
                    else
                    {
                        db.Entry(existing).CurrentValues.SetValues(item);
                        Debug.WriteLine("Päivitetään olemassa olevaa");
                    }

                    await db.SaveChangesAsync();

                    // 5. Laske deal
                    var deal = _laskin.Laske(item);
                    Debug.WriteLine($"Deal: Profit={deal.NetProfit:F2} € | ROI={deal.ROI:F2}%");

                    // 6. Lisää vain jos kelvollinen (voit muuttaa ehtoa)
                    if (deal.NetProfit > 0 && deal.ROI > 3)
                    {
                        db.ProfitDeals.Add(deal);
                        await db.SaveChangesAsync();
                        Debug.WriteLine("Deal lisätty tietokantaan");
                    }
                    else
                    {
                        Debug.WriteLine("Ei kelvollinen deal → ei lisätä");
                    }
                }

                // Päivitä grid lopuksi
                RefreshGrid();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ScanAsync virhe: {ex.Message}");
                MessageBox.Show($"Virhe skannauksessa: {ex.Message}");
            }
            finally
            {
                btnScan.Enabled = true;
            }
        }

        private async void btnScan_Click(object sender, EventArgs e)
        {
            await ScanAsync();
        }
    }
}