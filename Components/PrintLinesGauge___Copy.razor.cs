using Blazorise.Charts;
using tdaStreamHub.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace tdaStreamHub.Components
{
    public partial class PrintLinesGauge___Copy
    {
        DataItem[] rawGaugesCombined = new DataItem[] {
        new DataItem
        {
            Date = DateTime.Parse("2019-01-01"),
            Revenue = -7
        },
        new DataItem
        {
            Date = DateTime.Parse("2019-02-01"),
            Revenue = 3
        },
        new DataItem
        {
            Date = DateTime.Parse("2019-03-01"),
            Revenue = 4
        },
        new DataItem
        {
            Date = DateTime.Parse("2019-04-01"),
            Revenue = 7
        },
        new DataItem
        {
            Date = DateTime.Parse("2019-05-01"),
            Revenue = 5
        },
        new DataItem
        {
            Date = DateTime.Parse("2019-06-01"),
            Revenue = 2
        },
        new DataItem
        {
            Date = DateTime.Parse("2019-07-01"),
            Revenue = 274000
        },
        new DataItem
        {
            Date = DateTime.Parse("2019-08-01"),
            Revenue = -3
        },
        new DataItem
        {
            Date = DateTime.Parse("2019-09-01"),
            Revenue = 2
        },
        new DataItem
        {
            Date = DateTime.Parse("2019-10-01"),
            Revenue = 7
        },
        new DataItem
        {
            Date = DateTime.Parse("2019-11-01"),
            Revenue = 7
        },
        new DataItem
        {
            Date = DateTime.Parse("2019-12-01"),
            Revenue = 6
        }
    };
        DataItem[] average10min, movingAverage5min, movingAverage30sec,
            staticValue7, staticValue0, staticValueMinus7
            = new DataItem[]
        {
            new DataItem
            {
                Date = DateTime.Parse("2019-01-01"),
                Revenue = 0
            }
        };
        protected override async Task OnInitializedAsync()
        {
            TDAStreamerData.OnTimeSalesStatusChanged += getPrintsData;



            await Task.CompletedTask;
        }

        public void getPrintsData()
        {
            if (TDAStreamerData.gaugeValues.Count() == 0) return;

            rawGaugesCombined = TDAStreamerData.gaugeValues.Select(dict =>
            new DataItem()
            {
                Date = dict.Key,
                Revenue = dict.Value
            }
            ).ToArray();

            staticValue0 = staticValue(0);
            staticValue7 = staticValue(7);
            staticValueMinus7 = staticValue(-7);

            movingAverage5min = movingAverage(300);

            average10min = staticAverage(600);

            movingAverage30sec = movingAverage(30);

            StateHasChanged();
        }

        private DataItem[] staticAverage(int secs)
        {
            var maxDate = TDAStreamerData.gaugeValues.Keys.Max();
            var avg5min = TDAStreamerData.gaugeValues
                .Where(d => d.Key >= maxDate.AddSeconds(-secs))
                .Average(d => d.Value);

            return TDAStreamerData.gaugeValues.Select(dict =>
            new DataItem()
            {
                Date = dict.Key,
                Revenue = avg5min
            }
            ).ToArray();
        }
        private DataItem[] staticValue(double val)
        {
            return TDAStreamerData.gaugeValues.Select(dict =>
            new DataItem()
            {
                Date = dict.Key,
                Revenue = val
            }
            ).ToArray();
        }

        private DataItem[] movingAverage(int secs)
        {
            return TDAStreamerData.gaugeValues.Select(dict =>
                 new DataItem()
                 {
                     Date = dict.Key,
                     Revenue = TDAStreamerData.gaugeValues
                     .Where(d => d.Key <= dict.Key && d.Key >= dict.Key.AddSeconds(-secs))
                     .Select(d => d.Value).Average()
                 }
            ).ToArray();
        }

        class DataItem
        {
            public DateTime Date { get; set; }
            public double Revenue { get; set; }
        }
    }


}
