using BasicallyMe.RobinhoodNet;
using RhAutoHSOTP.classes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

namespace RhAutoHSOTP
{
    public class RhAutoProcs
    {

        public static string _token;

        public static dynamic _results;
        public static string _resultsLabel;

        public static OrderSnapshot _order;

        public static decimal _balance { get; set; }

        public static Quote _quote;
        public static OptionQuote[] _options;

        public static async Task GetStockQuote(string symbol)
        {
            var rh = new RobinhoodClient(_token);
            _quote = await rh.DownloadQuote(symbol);
        }

        public static void AttemptLogin()
        {

            try
            {

                var rh = new RobinhoodClient();


                //Utils.MessageBoxModal("Attempting Authentication...");
                RhLogins.authenticate(rh).Wait();
                _token = rh.AuthToken;
                _resultsLabel = "Authentication";
                if (rh.AuthToken == null)
                {
                    Utils.MessageBoxModal("No Authentication token.");
                    return;
                }
                else
                {
                    //Utils.MessageBoxModal("Authentication succeeded.");
                }
            }
            catch (Exception exc)
            {
                Utils.MessageBoxModal(exc.ToString());
            }
        }

        internal static async Task<Quote> AttemptToGetQuote(string symbol)
        {
            try
            {
                Utils.MessageBoxModal($"Attempting to get Quote for {symbol}...");

                var rh = new RobinhoodClient(_token);

                _results = await rh.DownloadQuote(symbol).ConfigureAwait(continueOnCapturedContext: false);
                _resultsLabel = symbol;

                Utils.MessageBoxModal("Quote succeeded.");
            }
            catch (Exception exc)
            {
                Utils.MessageBoxModal(exc.ToString());
            }
            return null;
        }


        internal static async Task<Quote> GetQuote(string symbol)
        {
            try
            {
                var rh = new RobinhoodClient(_token);

                _quote = await rh.DownloadQuote(symbol).ConfigureAwait(continueOnCapturedContext: false);
           

            }
            catch (Exception exc)
            {
                Utils.MessageBoxModal(exc.ToString());
            }
            return null;
        }
        internal static async Task<OptionQuote[]> AttemptToGetOptionQuote(string symbol, string expires, decimal strikePrice)
        {

            try
            {
                _resultsLabel = $"{symbol} {expires} {strikePrice}";
                Utils.MessageBoxModal($"Attempting to get Option Quote for {_resultsLabel}...");

                var rh = new RobinhoodClient(_token);

                _results = await rh.DownloadOptionQuotes(new string[] { symbol, expires, strikePrice.ToString(), "2" })
                    .ConfigureAwait(continueOnCapturedContext: false);

                Utils.MessageBoxModal("Option Quote succeeded.");

            }
            catch (Exception exc)
            {
                Utils.MessageBoxModal(exc.ToString());
            }
            return null;
        }

        internal static async Task<OptionQuote[]> GetOptionQuote(string symbol, string expires, decimal strikePrice, int nRange)
        {

            try
            {
                _resultsLabel = $"{symbol} {expires} {strikePrice}";

                var rh = new RobinhoodClient(_token);

                _options = await rh.DownloadOptionQuotes(new string[] { symbol, expires, strikePrice.ToString(), nRange.ToString() })
                    .ConfigureAwait(continueOnCapturedContext: false);

            }
            catch { }

            //catch (Exception exc)
            //{
            //    Utils.MessageBoxModal(exc.ToString());
            //}
            return new OptionQuote[1];
        }

        internal static async Task<OptionQuote[]> AttemptToGetAccountData()
        {

            try
            {
                _resultsLabel = $"Account Data";
                Utils.MessageBoxModal($"Attempting to get Account Data ...");

                var rh = new RobinhoodClient(_token);

                Account account = rh.DownloadAllAccounts().Result.First();

                _results = account;

                Utils.MessageBoxModal("Account Data succeeded.");

            }
            catch (Exception exc)
            {
                Utils.MessageBoxModal(exc.ToString());
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">
        /// args = new string[] { "QQQ", "2019-04-29", "call", "293", "1", "0.01" };
        /// </param>
        private async Task<OrderSnapshot> PlaceOptionOrderAsync(string[] args)
        {
            Debug.Print(System.Reflection.MethodBase.GetCurrentMethod().Name);
            var rh = new RobinhoodClient(_token);

            Account account = rh.DownloadAllAccounts().Result.First();

            JToken instrumentData = null;
            while (instrumentData == null)
            {
                try
                {
                    // Console.Write("Symbol: ");
                    rh.DownloadOptionStrikeInstrument(args).Wait();

                    instrumentData = rh.results;

                    //Console.WriteLine(instrument.Name);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message + "\n\nInstrument problem. Try again.");
                    System.Threading.Thread.Sleep(1000);
                }
            }

            int qty = Convert.ToInt32(args[4]);

            decimal price = Convert.ToDecimal(args[5]);

            TimeInForce tif = TimeInForce.GoodTillCancel;

            OptionInstrument instrument = new OptionInstrument()
            {
                InstrumentUrl = new Url<OptionInstrument>(instrumentData.SelectToken("url").ToString()),
                Symbol = instrumentData.SelectToken("chain_symbol").ToString()
            };

            var newOptionOrderSingle = new NewOptionOrderSingle(instrument);

            newOptionOrderSingle.AccountUrl = account.AccountUrl;
            newOptionOrderSingle.Quantity = Math.Abs(qty);
            newOptionOrderSingle.Side = qty > 0 ? Side.Buy : Side.Sell;
            newOptionOrderSingle.TimeInForce = tif;
            newOptionOrderSingle.OrderType = OrderType.Limit;
            newOptionOrderSingle.Price = price;

            var order = await rh.PlaceOptionOrder(newOptionOrderSingle);

            //if (!String.IsNullOrEmpty(order.ErrorMessage))
            //    //MessageBox.Show(order.ErrorMessage);
            //else
            //    //MessageBox.Show(string.Format("{0}\t{1}\t{2} x {3}\t{4}",
            //                        //order.Side,
            //                        //instrument.Symbol,
            //                        //order.Quantity,
            //                        //order.Price.HasValue ? order.Price.ToString() : "mkt",
            //                        //order.State));

            //if (!String.IsNullOrEmpty(order.RejectReason))
            //{
            //    //MessageBox.Show(order.RejectReason);
            //}

            return order;


        }


        internal static async Task AttemptToPlaceOrder(string expires, string side, string strike)
        {
            var context = SynchronizationContext.Current;

            Utils.MessageBoxModal($"Attempting to place Order ...");

            string[] args = new string[] { "QQQ", expires, side.ToLower(), strike, "10", "0.01" };

            //            System.Threading.ThreadPool.QueueUserWorkItem(async (w) => await PlaceOrder(args), context);
            await PlaceOrder(args).ConfigureAwait(false);
            Debug.Print("1:" + DateTime.Now);
            _results = _order;

            if (RobinhoodClient._ErrorMessage.Length > 0)
            {
                _resultsLabel = RobinhoodClient._ErrorMessage;
                Utils.MessageBoxModal("Place Order failed.");
            }
            else
            {
                _resultsLabel = "Option Order";

                Utils.MessageBoxModal("Place Order succeeded.");
            }

        }

        internal static async Task AttemptToCancelOrder()
        {

            Utils.MessageBoxModal($"Attempting to cancel Order ...");

            //            System.Threading.ThreadPool.QueueUserWorkItem(async (w) => await PlaceOrder(args), context);
            var result = await CancelOrder(_order).ConfigureAwait(false);

            if (result == true)

                Utils.MessageBoxModal("Cancel Order succeeded.");
            else
                Utils.MessageBoxModal("Cancel Order failed.");



        }

        public static async Task PlaceOrder(string[] args)
        {
            Debug.Print(System.Reflection.MethodBase.GetCurrentMethod().Name);
            var rh = new RobinhoodClient(_token);

            //Account account = rh.DownloadAllAccounts().Result.First();

            //JToken instrumentData = null;
            //while (instrumentData == null)
            //{
            //    try
            //    {
            //        Console.Write("Symbol: ");
            //        rh.DownloadOptionStrikeInstrument(args).Wait();

            //        instrumentData = rh.results;

            //        MessageBox.Show(instrument.Name);
            //    }
            //    catch (Exception ex)
            //    {
            //        Utils.MessageBoxModal("Problem with initial order.\n\nWill try again 1 second after you hit OK.\n\nIf expiry date is today or wrong, no order will be places, /n/nsince no option instrument can be found ");
            //        System.Threading.Thread.Sleep(1000);

            //    }
            //}

            int qty = Convert.ToInt32(args[4]);

            decimal price = Convert.ToDecimal(args[5]);

            TimeInForce tif = TimeInForce.GoodTillCancel;

            //OptionInstrument instrument = new OptionInstrument()
            //{
            //    InstrumentUrl = "https://api.robinhood.com/options/instruments/5ac9f2ac-6d01-4d2d-94de-e18ae7602a81/", //new Url<OptionInstrument>(instrumentData.SelectToken("url").ToString()),
            //    Symbol = "c277b118-58d9-4060-8dc5-a3b5898955cb"  //
            //    //instrumentData.SelectToken("chain_symbol").ToString()

            //};


            OptionInstrument instrument = new OptionInstrument()
            {
                InstrumentUrl = new Url<OptionInstrument>("https://api.robinhood.com/options/instruments/5ac9f2ac-6d01-4d2d-94de-e18ae7602a81/"), //new Url<OptionInstrument>(instrumentData.SelectToken("url").ToString()),
                Symbol = "c277b118-58d9-4060-8dc5-a3b5898955cb"  //
                //instrumentData.SelectToken("chain_symbol").ToString()

            };

            var newOptionOrderSingle = new NewOptionOrderSingle(instrument);
            newOptionOrderSingle.AccountUrl =  // account.AccountUrl;
            new Url<Account>("https://api.robinhood.com/accounts/475573473/");
            //
            newOptionOrderSingle.Quantity = Math.Abs(qty);
            newOptionOrderSingle.Side = qty > 0 ? Side.Buy : Side.Sell;

            newOptionOrderSingle.TimeInForce = tif;
            if (price == 0)
            {
                newOptionOrderSingle.OrderType = OrderType.Market;
            }
            else
            {
                newOptionOrderSingle.OrderType = OrderType.Limit;
                newOptionOrderSingle.Price = price;
            }

            _order = rh.PlaceOptionOrder(newOptionOrderSingle).Result;
            Debug.Print("2:" + DateTime.Now);

            _resultsLabel = RobinhoodClient._ErrorMessage;
           // _resultsLabel = rh._


            /// update lastMatch.buyAtPrice
            /// 
            /// await the order response
            /// 
            /// /// get reversals after
            /// 
            //bool isPending = order.State != null && order.State == "unconfirmed";  /// does this say pending or is there a better way
            //                                                                       /// if the order is pending or the order is partial
            //if (isPending)
            //{
            //    ///     cancel the order 
            //    rh.CancelOrder(order.CancelUrl).Wait();

            //    /// get the order status 
            //    /// 
            //    /// replace the order
            //}

            ///     if new reversal 
            ///         if  not partial
            ///             remove order from lstMatch // so a resumption will create a new turn up (this means our reversal stats are not complete)
            ///             set lastMatch to null
            ///         exit stage left
            ///         
            ///         
            ///     else 
            ///         if  partial
            ///             record the partial order
            ///             reset contracts
            ///         re-order
            /// else // order was filled

            ///     log the order


            //using (TradeLogTableAdapter ta = new TradeLogTableAdapter())
            //{
            //    ta.Insert(args[6], Convert.ToDateTime(args[7]), order.jsonText);
            //}

            /// 
            /// Analyse the current status
            /// Is the ask > buyAtPrice 
            /// Is it more than a penny?
            /// Is the ask now lower than buyAtPrice i.e. the price is falling
            /// Has there been a reversal?
            /// Is BuyPrice >= Bid and <= Ask?



            //if (!String.IsNullOrEmpty(order.ErrorMessage))
            //    MessageBox.Show(order.ErrorMessage);
            //else
            //{
            //    MessageBox.Show(string.Format("{0}\t{1}\t{2} x {3}\t{4}",
            //                        order.Side,
            //                        instrument.Symbol,
            //                        order.Quantity,
            //                        order.Price.HasValue ? order.Price.ToString() : "mkt",
            //                        order.State));

            //}
            //if (!String.IsNullOrEmpty(order.RejectReason))
            //{
            //    MessageBox.Show(order.RejectReason);
            //}


            //object state = SynchronizationContext.Current;
            //var context = (SynchronizationContext)state;
            //state = new object();
            //context.Post(RHCockpit.frmBot.WorkerDone, state);

        }

        public static async Task<bool> CancelOrder(OrderSnapshot order)

        {
            if (order != null)
            {
                bool isPending = order.State != null && order.State == "unconfirmed";  /// does this say pending or is there a better way
                                                                                       /// if the order is pending or the order is partial
                if (isPending)
                {
                    var rh = new RobinhoodClient(_token);

                    ///     cancel the order 
                    await rh.CancelOrder(order.CancelUrl).ConfigureAwait(false);

                    return true;
                }
            }

            return false;

        }
    }
}
