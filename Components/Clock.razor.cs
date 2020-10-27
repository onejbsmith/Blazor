using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace tdaStreamHub.Components
{
    public partial class Clock
    {
        string currentTime = "N/A";
        string buttonAction = "N/A";
        string currentCss = "clock-notset";
        Timer timer;

        DateTime dateTimeNow = DateTime.Now;


        protected override async Task OnInitializedAsync()
        {
            InitTimer();
            StartTimer();

            await Task.CompletedTask;
        }
        void startStop()
        {
            if (timer.Enabled)
            {
                StopTimer();
            }
            else
            {
                StartTimer();
            }
        }

        private void InitTimer()
        {
            timer = new Timer(1000);
            timer.Elapsed += async (sender, e) => await TimerTick();
        }
        private void StartTimer()
        {
            buttonAction = "STOP";
            timer.Start();
        }
        private void StopTimer()
        {
            buttonAction = "START";
            timer.Stop();
        }
        private Task TimerTick()
        {
            dateTimeNow = DateTime.Now;
            currentTime = dateTimeNow.ToLongTimeString();
            currentCss = "clock-working";
            this.StateHasChanged();
            return Task.CompletedTask;
        }  
    }
}
