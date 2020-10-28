using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using tapeStream.Shared;
using static tapeStream.Shared.CONSTANTS;

namespace tdaStreamHub.Data
{
    public class TDAPrints
    {
        static public double GetPrintsGaugeScore(string symbol, ref Dictionary<int, DataItem[]> dictPies ) 
        {
            var value = 0;
            dictPies = new Dictionary<int, DataItem[]>();
            foreach (var seconds in CONSTANTS.printSeconds)
            {
                var slices = TDAPrints.GetPieSlices(symbol, seconds);
                dictPies.Add(seconds, slices);

                var reds = slices[0].Revenue + slices[2].Revenue;
                var greens = slices[3].Revenue + slices[4].Revenue;

                value += reds > greens ? -1
                        : greens > reds ? 1
                        : 0;
            }
            return value;
        }

        public static DataItem[] GetPieSlices(string symbol, int seconds)
        {
            long thisManySecondsAgo = (long)(DateTime.Now.ToUniversalTime().AddSeconds(-seconds) - new DateTime(1970, 1, 1)).TotalMilliseconds;

            var printsData = new DataItem[5];
            var timeSales = TDAStreamerData.timeSales[symbol];

            printsData[0].Revenue = timeSales.Where(t => t.level == 1 && t.time >= thisManySecondsAgo).Sum(t => t.size);
            printsData[1].Revenue = timeSales.Where(t => t.level == 2 && t.time >= thisManySecondsAgo).Sum(t => t.size);
            printsData[2].Revenue = timeSales.Where(t => t.level == 3 && t.time >= thisManySecondsAgo).Sum(t => t.size);
            printsData[3].Revenue = timeSales.Where(t => t.level == 4 && t.time >= thisManySecondsAgo).Sum(t => t.size);
            printsData[4].Revenue = timeSales.Where(t => t.level == 5 && t.time >= thisManySecondsAgo).Sum(t => t.size);

            return printsData;
        }


        private static void SaveToCsvFile(string svcName, string symbol, TimeSales_Content timeAndSales)
        {
            List<string> timeAndSalesFields = FilesManager.GetCSVHeader(new TimeSales_Content()).Split(',').ToList();

            var lstValues = new List<string>();
            foreach (string name in timeAndSalesFields)
            {
                lstValues.Add($"{timeAndSales[name]}");
            }
            string record = string.Join(',', lstValues) + "\n";

            string fileName = $"{svcName} {symbol} {DateTime.Now.ToString("MMM dd yyyy")}.csv";
            if (!System.IO.File.Exists(fileName))
            {
                System.IO.File.AppendAllText(fileName, string.Join(",", timeAndSalesFields) + "\n");

            }
            System.IO.File.AppendAllText(fileName, record);
        }
    }
}
