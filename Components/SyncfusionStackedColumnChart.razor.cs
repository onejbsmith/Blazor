using Syncfusion.Blazor.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tdaStreamHub.Data;

namespace tdaStreamHub.Components
{
    public partial class SyncfusionStackedColumnChart
    {
        public double maxY = 50000;

        public double intervalY = 10000;

        public class StackedColumnChartData2
        {
            public double x { get; set; }
            public Int32 y { get; set; }
            public Int32 y1 { get; set; }
            public Int32 y2 { get; set; }
            public Int32 y3 { get; set; }
            public Int32 y4 { get; set; }
        }

        public List<StackedColumnChartData2> DataSource = new List<StackedColumnChartData2>
{
        new StackedColumnChartData2 { x= 180.20, y= 0, y1= 7600 , y2= 6600 , y3= 3400, y4=2000},
        new StackedColumnChartData2 { x= 180.21, y= 0, y1= 9900 , y2= 7900 , y3= 3800, y4=0},
        new StackedColumnChartData2 { x= 180.22, y= 2000, y1= 12100, y2= 9100 , y3= 4400, y4=0},
        new StackedColumnChartData2 { x= 180.24, y= 0, y1= 14200, y2= 10200, y3= 5100, y4=0},
         new StackedColumnChartData2 { x= 180.25, y= 0, y1= 7600 , y2= 6600 , y3= 3400, y4=2000},
        new StackedColumnChartData2 { x= 180.27, y= 0, y1= 9900 , y2= 7900 , y3= 3800, y4=0},
        new StackedColumnChartData2 { x= 180.32, y= 2000, y1= 12100, y2= 9100 , y3= 4400, y4=0},
        new StackedColumnChartData2 { x= 180.36, y= 0, y1= 14200, y2= 10200, y3= 5100, y4=0},
        new StackedColumnChartData2 { x= 180.4, y= 0, y1= 7600 , y2= 6600 , y3= 3400, y4=2000},
        new StackedColumnChartData2 { x= 180.5, y= 0, y1= 9900 , y2= 7900 , y3= 3800, y4=0},
        new StackedColumnChartData2 { x= 180.6, y= 2000, y1= 12100, y2= 9100 , y3= 4400, y4=0},
        new StackedColumnChartData2 { x= 180.7, y= 0, y1= 14200, y2= 10200, y3= 5100, y4=0},
   };



        public String[] palettes = new String[] { "#800000", "#FF0000", "#808080", "#008000", "#00FF00" };
        public class StackedColumnChartData
        {
            public string x { get; set; }
            public double y { get; set; }
            public double y1 { get; set; }
            public double y2 { get; set; }
            public double y3 { get; set; }
            public double y4 { get; set; }
        }
        public List<StackedColumnChartData> DataSource2 = new List<StackedColumnChartData>
{
        new StackedColumnChartData { x= "2014", y= 111.1, y1= 76.9 , y2= 66.1 , y3= 34.1, y4=20},
        new StackedColumnChartData { x= "2015", y= 127.3, y1= 99.5 , y2= 79.3 , y3= 38.2, y4=20},
        new StackedColumnChartData { x= "2016", y= 143.4, y1= 121.7, y2= 91.3 , y3= 44.0, y4=20},
        new StackedColumnChartData { x= "2017", y= 159.9, y1= 142.5, y2= 102.4, y3= 51.6, y4=20},
    };
        string CurrentUri;
        protected override async Task OnInitializedAsync()
        {
            TDAStreamerData.OnTimeSalesStatusChanged += getTimeSalesData;
        }

        private void getTimeSalesData()
        {
            var timeSeconds = new TimeSpan(0, 0, seconds);
            var timeSalesSmoothed = TDAStreamerData.timeSales[symbol]
                .Select(tss => new
                { 
                    price = Math.Round(tss.price,3),
                    level = tss.level,
                    size = tss.size,
                    timeDate = tss.TimeDate                
                });

            var timeSalesGroupedSummed = timeSalesSmoothed
                .Where(ts => DateTime.Now.Subtract(ts.timeDate) <= timeSeconds)
                .GroupBy(ts => new
                {
                    ts.price,
                    ts.level
                })
                .Select(gts => new
                {
                    price = gts.Key.price,
                    level = gts.Key.level,
                    y = gts.Where(ts => ts.level == 1).Sum(ts => ts.size),
                    y1 = gts.Where(ts => ts.level == 2).Sum(ts => ts.size),
                    y2 = gts.Where(ts => ts.level == 3).Sum(ts => ts.size),
                    y3 = gts.Where(ts => ts.level == 4).Sum(ts => ts.size),
                    y4 = gts.Where(ts => ts.level == 5).Sum(ts => ts.size),
                    yTotal = gts.Sum(ts => ts.size)

                });

            /// Now rest the chart data
            /// 
            maxY = 0;
            DataSource = new List<StackedColumnChartData2>();
            foreach (var plv in timeSalesGroupedSummed)
            {
                DataSource.Add(new StackedColumnChartData2
                {
                    x = plv.price,
                    y = Convert.ToInt32(plv.y),
                    y1 = Convert.ToInt32(plv.y1),
                    y2 = Convert.ToInt32(plv.y2),
                    y3 = Convert.ToInt32(plv.y3),
                    y4 = Convert.ToInt32(plv.y4),
                });

                maxY = Math.Max(maxY, plv.yTotal);
            }
            maxY = 1000 * Math.Floor((maxY + 1000) / 1000);
            intervalY = maxY / 5;
            StateHasChanged();

        }

        void ChartLoad(ILoadedEventArgs Args)
        {
            return;
            //CurrentUri = NavigationManager.Uri;
            if (CurrentUri.IndexOf("material") > -1)
            {
                Args.Theme = ChartTheme.Material;
            }
            else if (CurrentUri.IndexOf("fabric") > -1)
            {
                Args.Theme = ChartTheme.Fabric;
            }
            else if (CurrentUri.IndexOf("bootstrap4") > -1)
            {
                Args.Theme = ChartTheme.Bootstrap4;
            }
            else if (CurrentUri.IndexOf("bootstrap") > -1)
            {
                Args.Theme = ChartTheme.Bootstrap;
            }
            else if (CurrentUri.IndexOf("highcontrast") > -1)
            {
                Args.Theme = ChartTheme.HighContrast;
            }
            else
            {
                Args.Theme = ChartTheme.Bootstrap4;
            }
        }

    }
}
