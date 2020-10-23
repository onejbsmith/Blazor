using Blazorise.Charts;
using BlazorTrader.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorTrader.Components
{
    public partial class PrintLinesGauge___Copy
    {

        LineChart<double> lineChart;

        LineChartDataset<double> buysChartDatasetData = new LineChartDataset<double>();
        //LineChartDataset<double> sellsChartDatasetData = new LineChartDataset<double>();

        //List<double> sellsData = new List<double>();
        List<double> buysData = new List<double>();

        static LineChartOptions lineOptions = new LineChartOptions();

        protected string chartOptions = System.Text.Json.JsonSerializer.Serialize<LineChartOptions>(lineOptions,

                            new System.Text.Json.JsonSerializerOptions() { WriteIndented = true });

        protected override async Task OnInitializedAsync()
        {
            TDAStreamerData.OnTimeSalesStatusChanged+= getPrintsData;
            StateHasChanged();
            buysChartDatasetData = GetBuyLineChartDataset(buysData);
            //sellsChartDatasetData = GetSellLineChartDataset(sellsData);
            await Task.CompletedTask;
            lineOptions.Tooltips = new Tooltips() { Enabled = false };
            lineOptions.Legend = new Legend() { Display = false };
            lineOptions.Scales = new Scales()
            { 
                YAxes = new List<Axis>(){ new Axis(){Display = false} },
                XAxes = new List<Axis>() { new Axis(){Display = false} }
            };
        }

        public void getPrintsData()
        {
            //if (isMovement)
            //    TDAStreamerData.getPrintsMovementBuysSellsData(ref sellsData, ref buysData, seconds, symbol);

            //else
            //    TDAStreamerData.getPrintsBuysSellsData(ref sellsData, ref buysData, seconds, isPrintsSize, symbol);
            if (!TDAStreamerData.values.ContainsKey(weight)) return;
             var buysData = TDAStreamerData.values[weight].Values.Select(val => new { value = val });

            HandleRedraw();
            StateHasChanged();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                HandleRedraw();
            }
        }

        void HandleRedraw()
        {
            buysChartDatasetData = GetBuyLineChartDataset(buysData);
            //sellsChartDatasetData = GetSellLineChartDataset(sellsData);
            lineChart.Clear();

            // lineChart.AddLabel(Labels);

            lineChart.AddDataSet(buysChartDatasetData);
            //lineChart.AddDataSet(sellsChartDatasetData);
            // lineChart.AddDataSet(GetTweenLineChartDataset(RandomizeData()));

            //lineChart.Options = new LineChartOptions() { Scales= new Scales() {  YAxes = new List<Axe>() {  } } };

            lineChart.Update();
        }

        LineChartDataset<double> GetBuyLineChartDataset(List<double> data)
        {
            return new LineChartDataset<double>
            {
                Data = data,
                BackgroundColor = buyBackgroundColors,
                BorderColor = buyBorderColors,
                Fill = true,
                PointRadius = 2,
                BorderDash = new List<int> { }
            };
        }

        //LineChartDataset<double> GetSellLineChartDataset(List<double> data)
        //{
        //    return new LineChartDataset<double>
        //    {
        //        Data = data,
        //        BackgroundColor = sellBackgroundColors,
        //        BorderColor = sellBorderColors,
        //        Fill = true,
        //        PointRadius = 2,
        //        BorderDash = new List<int> { }
        //    };
        //}

        //LineChartDataset<double> GetTweenLineChartDataset(List<double> data)
        //{
        //    return new LineChartDataset<double>
        //    {
        //        Data = data,
        //        BackgroundColor = tweenBackgroundColors,
        //        BorderColor = tweenBorderColors,
        //        Fill = true,
        //        PointRadius = 2,
        //        BorderDash = new List<int> { }
        //    };
        //}

        string[] Labels = { "Red", "Blue", "Yellow", "Green", "Purple", "Orange" };
        //List<string> sellBackgroundColors = new List<string> { ChartColor.FromRgba(255, 99, 132, 0.0f), ChartColor.FromRgba(54, 162, 235, 0.2f), ChartColor.FromRgba(255, 206, 86, 0.2f), ChartColor.FromRgba(75, 192, 192, 0.2f), ChartColor.FromRgba(153, 102, 255, 0.2f), ChartColor.FromRgba(255, 159, 64, 0.2f) };
        //List<string> sellBorderColors = new List<string> { ChartColor.FromRgba(255, 99, 132, 1f), ChartColor.FromRgba(54, 162, 235, 1f), ChartColor.FromRgba(255, 206, 86, 1f), ChartColor.FromRgba(75, 192, 192, 1f), ChartColor.FromRgba(153, 102, 255, 1f), ChartColor.FromRgba(255, 159, 64, 1f) };

        List<string> buyBackgroundColors = new List<string> { ChartColor.FromRgba(99, 255, 132, 0.0f), ChartColor.FromRgba(54, 162, 235, 0.2f), ChartColor.FromRgba(255, 206, 86, 0.2f), ChartColor.FromRgba(75, 192, 192, 0.2f), ChartColor.FromRgba(153, 102, 255, 0.2f), ChartColor.FromRgba(255, 159, 64, 0.2f) };
        List<string> buyBorderColors = new List<string> { ChartColor.FromRgba(99, 255, 132, 1f), ChartColor.FromRgba(54, 162, 235, 1f), ChartColor.FromRgba(255, 206, 86, 1f), ChartColor.FromRgba(75, 192, 192, 1f), ChartColor.FromRgba(153, 102, 255, 1f), ChartColor.FromRgba(255, 159, 64, 1f) };

        //List<string> tweenBackgroundColors = new List<string> { ChartColor.FromRgba(99, 132, 255, 0.0f), ChartColor.FromRgba(54, 162, 235, 0.2f), ChartColor.FromRgba(255, 206, 86, 0.2f), ChartColor.FromRgba(75, 192, 192, 0.2f), ChartColor.FromRgba(153, 102, 255, 0.2f), ChartColor.FromRgba(255, 159, 64, 0.2f) };
        //List<string> tweenBorderColors = new List<string> { ChartColor.FromRgba(99, 132, 255, 1f), ChartColor.FromRgba(54, 162, 235, 1f), ChartColor.FromRgba(255, 206, 86, 1f), ChartColor.FromRgba(75, 192, 192, 1f), ChartColor.FromRgba(153, 102, 255, 1f), ChartColor.FromRgba(255, 159, 64, 1f) };

        List<double> RandomizeData()
        {
            var r = new Random(DateTime.Now.Millisecond);

            return new List<double> { r.Next(3, 50) * r.NextDouble(), r.Next(3, 50) * r.NextDouble(), r.Next(3, 50) * r.NextDouble(), r.Next(3, 50) * r.NextDouble(), r.Next(3, 50) * r.NextDouble(), r.Next(3, 50) * r.NextDouble() };
        }

    }
}
