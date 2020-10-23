using ChartJs.Blazor.ChartJS.BarChart;
using ChartJs.Blazor.Charts;
using ChartJs.Blazor.ChartJS.BarChart.Axes;
using ChartJs.Blazor.ChartJS.Common.Properties;
using System;
using System.Collections.Generic;
using ChartJs.Blazor.ChartJS.Common.Axes;
using ChartJs.Blazor.ChartJS.Common.Enums;
using System.Drawing;
using ChartJs.Blazor.Util;
using System.Collections.ObjectModel;

namespace BlazorTrader.Components
{
    public partial class PrintsGroupedStackedColumnCart
    {

        private const int InitalCount = 7;
        private BarConfig _config;
        private Random _rng = new Random();
        private ChartBase<BarConfig> _chart;

        protected override void OnInitialized()
        {
            _config = new BarConfig
            {
                Options = new BarOptions
                {
                    Responsive = true,
                    Title = new OptionsTitle
                    {
                        Display = true,
                        Text = "Chart.js Bar Chart - Stacked"
                    },
                    //Tooltips = new Tooltips
                    //{
                    //    Mode = InteractionMode.Index,
                    //    Intersect = false
                    //},
                    Scales = new BarScales
                    {
                        XAxes = new List<CartesianAxis>
                    {
                        new BarCategoryAxis
                        {
                            Stacked = true
                        }
                    },
                        YAxes = new List<CartesianAxis>
                    {
                        new BarLinearCartesianAxis
                        {
                            Stacked = true
                        }
                    }
                    }
                }
            };


            //BarDataset<BlazorTrader.Data.TimeSales_Content> dataset1 = new BarDataset<BlazorTrader.Data.TimeSales_Content>();
            //BarDataset<BlazorTrader.Data.TimeSales_Content> dataset2 = new BarDataset<BlazorTrader.Data.TimeSales_Content>();
            //BarDataset<BlazorTrader.Data.TimeSales_Content> dataset3 = new BarDataset<BlazorTrader.Data.TimeSales_Content>();
            //BarDataset<BlazorTrader.Data.TimeSales_Content> dataset4 = new BarDataset<BlazorTrader.Data.TimeSales_Content>();

            BarDataset<List<BarData>> dataset1 = new BarDataset<List<BarData>>()
            {
                Label = "Dataset 1",
                BackgroundColor =  ColorUtil.FromDrawingColor(SampleUtils.ChartColors.Red)
            };

            BarDataset<List<BarData>> dataset2 = new BarDataset<List<BarData>>()
            {
                Label = "Dataset 2",
                BackgroundColor = ColorUtil.FromDrawingColor(SampleUtils.ChartColors.Blue)
            };

            BarDataset<List<BarData>> dataset3 = new BarDataset<List<BarData>>()
            {
                Label = "Dataset 3",
                BackgroundColor = ColorUtil.FromDrawingColor(SampleUtils.ChartColors.Green)
            };

            _config.Data.Labels.AddRange(SampleUtils.Months);
            _config.Data.Datasets.Add(dataset1);
            _config.Data.Datasets.Add(dataset2);
            _config.Data.Datasets.Add(dataset3);
        }

        private void RandomizeData()
        {
            foreach (IDataset<int> dataset in _config.Data.Datasets)
            {
                int count = dataset.Count;
                dataset.Clear();
                var range = BlazorTrader.Data.SampleUtils.RandomScalingFactor(count);
                foreach (var it in range)
                    dataset.Add(it);
            }

            _chart.Update();
        }
    }

    /// <summary>
    /// Represents a dataset with an id and a type.
    /// </summary>
    public interface IDataset
    {
        /// <summary>
        /// Gets the ID of this dataset. Used to keep track of the datasets
        /// across the .NET &lt;-&gt; JavaScript boundary.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the <see cref="ChartType"/> this dataset is for.
        /// Important to set in mixed charts.
        /// </summary>
        ChartType Type { get; }
    }

    /// <summary>
    /// Represents a strongly typed dataset that holds data of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of data this dataset contains.</typeparam>
    public interface IDataset<T> : IDataset, IList<T>
    {
        /// <summary>
        /// Gets the data contained in this dataset. This property is read-only.
        /// This is in addition to implementing <see cref="IList{T}"/>.
        /// </summary>
        IReadOnlyList<T> Data { get; }
    }

    public static class SampleUtils
    {
        private static readonly Random _rng = new Random();

        public static class ChartColors
        {
            private static readonly Lazy<IReadOnlyList<Color>> _all = new Lazy<IReadOnlyList<Color>>(() => new Color[7]
            {
                Red, Orange, Yellow, Green, Blue, Purple, Grey
            });

            public static IReadOnlyList<Color> All => _all.Value;

            public static readonly Color Red = Color.FromArgb(255, 99, 132);
            public static readonly Color Orange = Color.FromArgb(255, 159, 64);
            public static readonly Color Yellow = Color.FromArgb(255, 205, 86);
            public static readonly Color Green = Color.FromArgb(75, 192, 192);
            public static readonly Color Blue = Color.FromArgb(54, 162, 235);
            public static readonly Color Purple = Color.FromArgb(153, 102, 255);
            public static readonly Color Grey = Color.FromArgb(201, 203, 207);
        }

        public static IReadOnlyList<string> Months { get; } = new ReadOnlyCollection<string>(new[]
        {
            "January", "February", "March", "April", "May", "June", "July"
        });

        private static int RandomScalingFactorThreadUnsafe() => _rng.Next(-100, 100);

        public static int RandomScalingFactor()
        {
            lock (_rng)
            {
                return RandomScalingFactorThreadUnsafe();
            }
        }

        public static IEnumerable<int> RandomScalingFactor(int count)
        {
            int[] factors = new int[count];
            lock (_rng)
            {
                for (int i = 0; i < count; i++)
                {
                    factors[i] = RandomScalingFactorThreadUnsafe();
                }
            }

            return factors;
        }
    }
}
