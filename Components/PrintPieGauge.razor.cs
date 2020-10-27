using tdaStreamHub.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace tdaStreamHub.Components
{
    public partial class PrintPieGauge
    {
        string total = "0";
        string bids;

        string badgeClass = "btn btn-circle";
        public int[] printSeconds { get; set; } = new int[] { 5, 10, 30, 60, 120, 240, 600 };

        DataItem[] printsData = new DataItem[] {
         new DataItem { Quarter = "Price < Bid", Revenue = 100 }, // darkred
         new DataItem { Quarter = "Price = Bid", Revenue = 200 }, // red
         new DataItem { Quarter = "Price Between Bid & Ask", Revenue = 300 }, // cyan
         new DataItem { Quarter = "Price = Ask", Revenue = 400 }, // green
         new DataItem { Quarter = "Price > Ask", Revenue = 501 } }; // lime


        protected override async Task OnInitializedAsync()
        {
            TDAStreamerData.OnTimeSalesStatusChanged += getPrintsData;
            StateHasChanged();
            await Task.CompletedTask;
        }

        public void getPrintsData()
        {
            TDAStreamerData.getPrintsData(ref printsData, seconds, isPrintsSize, symbol);
            total = printsData.Sum(t => t.Revenue).ToString("n0");

            long oneMinuteAgo = (long)(DateTime.Now.ToUniversalTime().AddSeconds(-seconds) - new DateTime(1970, 1, 1)).TotalMilliseconds;
            var reds = TDAStreamerData.timeSales[symbol].Where(t => t.level == 1 && t.time >= oneMinuteAgo).Sum(t => t.size)
                       + TDAStreamerData.timeSales[symbol].Where(t => t.level == 2 && t.time >= oneMinuteAgo).Sum(t => t.size);

            var greens = TDAStreamerData.timeSales[symbol].Where(t => t.level == 4 && t.time >= oneMinuteAgo).Sum(t => t.size)
            + TDAStreamerData.timeSales[symbol].Where(t => t.level == 5 && t.time >= oneMinuteAgo).Sum(t => t.size);
            float nBids = 1;
            if (reds == 0 && greens == 0) nBids = 0;
            if (reds == 0) reds = (1 / 11) * greens;
            nBids = 100 * ((greens / reds) - 1);
            bids = nBids.ToString("n0");

            if (nBids < -10)
                badgeClass = "btn-danger btn btn-circle";
            else if (nBids > 10)
                badgeClass = "btn-success btn btn-circle";
            else
                badgeClass = " btn btn-circle";

            StateHasChanged();
        }

    }
}
