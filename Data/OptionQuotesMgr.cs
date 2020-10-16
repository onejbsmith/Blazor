using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorTrader.Data
{
    public class OptionQuotesMgr
    {
        public OptionQuotesMgr()
        { }

        public TDAApiService OptionsService { get; set; }

        public async Task<TimeSpan> GetOptionQuotes()
        {
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            //await OptionsService.GetAuthentication();
            var nStrikes = TDAParameters.optionNumStrikes + TDAParameters.optionNumSkipStrikes + TDAParameters.optionNumSpreadStrikes;
            TDA.quotes = await OptionsService.GetSinglesAsync(TDAParameters.optionSymbol, TDAParameters.optionExpDate, 4 * nStrikes);

            //       TDANotifications.optionStatus = TDA.quotes.status;

            if (TDA.quotes == null)
            {
                TDANotifications.optionStatus = "INCOMPLETE";
            }
            else if (TDA.quotes.callExpDateMap.Count > 0)
            {
                /// TODO: Pull option contracts from Parameters dictionary
                foreach (var qtArray in TDA.quotes.callExpDateMap.First().Value.Values)
                {
                    var qt = qtArray[0];
                    if (TDAParameters.dictOptionContracts.ContainsKey(qt.description))
                    {
                        qt.contracts = TDAParameters.dictOptionContracts[qt.description];
                        if (TDAParameters.dictIsManualContracts.ContainsKey(qt.description))
                            qt.isManualContracts = TDAParameters.dictIsManualContracts[qt.description];
                    }
                }

                foreach (var qtArray in TDA.quotes.putExpDateMap.First().Value.Values)
                {
                    var qt = qtArray[0];
                    if (TDAParameters.dictOptionContracts.ContainsKey(qt.description))
                    {
                        qt.contracts = TDAParameters.dictOptionContracts[qt.description];
                        if (TDAParameters.dictIsManualContracts.ContainsKey(qt.description))
                            qt.isManualContracts = TDAParameters.dictIsManualContracts[qt.description];
                    }
                }

                if (TDAParameters.inTheMoney)
                {
                    TDA.callOptions = TDA.quotes.callExpDateMap.First().Value.Values.Where(t => t[0].inTheMoney == TDAParameters.inTheMoney)
                        .OrderByDescending(t => t[0].strikePrice)
                        .Skip(TDAParameters.optionNumSkipStrikes).ToArray();


                    TDA.otmCallOptions = TDA.quotes.callExpDateMap.First().Value.Values.Where(t => t[0].inTheMoney == !TDAParameters.inTheMoney)
                        .Skip(TDAParameters.optionNumSkipStrikes).ToArray();


                    TDA.putOptions = TDA.quotes.putExpDateMap.First().Value.Values.Where(t => t[0].inTheMoney == TDAParameters.inTheMoney)
                    .Skip(TDAParameters.optionNumSkipStrikes).ToArray();

                    TDA.otmPutOptions = TDA.quotes.putExpDateMap.First().Value.Values.Where(t => t[0].inTheMoney == !TDAParameters.inTheMoney)
                        .OrderByDescending(t => t[0].strikePrice)
                        .Skip(TDAParameters.optionNumSkipStrikes).ToArray();
                }
                else
                {
                    TDA.callOptions = TDA.quotes.callExpDateMap.First().Value.Values.Where(t => t[0].inTheMoney == TDAParameters.inTheMoney)
                        .Skip(TDAParameters.optionNumSkipStrikes).ToArray();

                    TDA.putOptions = TDA.quotes.putExpDateMap.First().Value.Values.Where(t => t[0].inTheMoney == TDAParameters.inTheMoney)
                        .OrderByDescending(t => t[0].strikePrice)
                        .Skip(TDAParameters.optionNumSkipStrikes).ToArray();
                }
            }

            stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopWatch.Elapsed;
            return ts;
        }

    }
}
