using BasicallyMe.RobinhoodNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhAutoHSOTP.classes
{
    public class Trades
    {
        //public bool paper { get; set; }
        public int Id { get; set; }
        public string symbol { get; set; }
        public decimal strike { get; set; }
        private decimal _buyAtCost;

        public decimal buyAtCost
        {
            get { return _buyAtCost; }
            set
            {
                _buyAtCost = value;
                Cost = contracts * value*100;
                buyAtTime = DateTime.Now;
            }
        }

        private decimal _sellAtPrice;

        public decimal sellAtPrice
        {
            get { return _sellAtPrice; }
            set
            {
                _sellAtPrice = value;
                Price = contracts * value*100;
                sellAtTime = DateTime.Now;
                ProfitLoss = Price - Cost;
                Total += ProfitLoss;
                Balance += ProfitLoss;
            }
        }
        private DateTime TradingDate { get; set; }
        public int contracts { get; set; }
        public decimal Cost { get; set; }
        public decimal Price { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal Total { get; set; }
        public decimal Balance { get; set; }



        public DateTime buyAtTime { get; set; }
        public DateTime sellAtTime { get; set; }
        public String BuyReason { get; set; }
        public String SellReason { get; set; }

        public Trades(int id, string symbolIn, decimal strikeIn, int contractsIn, decimal buyAtCostIn, decimal total, decimal balance)
        {
            Id = id;
            symbol = symbolIn;
            strike = strikeIn;
            contracts = contractsIn;
            buyAtCost = buyAtCostIn;
            Total = total;
            Balance = balance;
            BuyReason = "";
            SellReason = "";
        }

    }
}
