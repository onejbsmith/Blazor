using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace tdaStreamHub
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }

    /// DONE: Create API for Ameritrade to get Level 3 quote info
    /// Get Option Chains and History
    ///     - for a given series of symbols, 
    ///         e.g. QQQ, AMZN from a user settings store
    ///     - around the money, 20 deep on each side?
    ///     - so can figure out (plot?) spreads
    ///     
    /// TODO: Get Symbol History and Current Quotes
    /// TODO:     - so can calc stats like ATR
    /// TODO:     - so can figure range around at the money to buy sell
    /// 
    /// TODO: Create API for Robinhood to do trades and get account info
    /// Buy Option Spreads
    /// Cancel Optiom Spreads
    /// Close Option Spreads
    /// Get Cash Balances
    /// Get Open Orders
    /// Get Executed Orders
    /// Get Positions
    /// TODO: Multi-Tenant and can place and monitor trades across multiple accounts
    /// 
    /// 
    /// TODO: Create UI page for Settings
    /// TODONE: Add links to TDA Auth Pages
    ///     Allow user to input several symbols (space delimited)
}
