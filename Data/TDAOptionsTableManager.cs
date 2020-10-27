
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace tdaStreamHub.Data
{
    public class TDAOptionsTableManager
    {
        #region Properties

        public int totalContracts = 0;
        public float totalCredit = 0.0f;
        public float totalCollateral = 0.0f;

        private TDAOptionQuote[][] _lstOptions;
        public TDAOptionQuote[][] lstOptions
        {
            get { return _lstOptions; }
            set { _lstOptions = value; }
        }
        public TDAOptionQuote[][] prevOptions { get; set; }

        public TDAOptionQuote[][] otmOptions { get; set; }

        #endregion

        #region Events

        public string classChanged(string fieldName, TDAOptionQuote prevOption, TDAOptionQuote option)
        {
            if (prevOption != null)
            {
                var prevAmt = prevOption[fieldName];
                var currAmt = option[fieldName];
                return classColor<dynamic>
        (ref prevAmt, ref currAmt);
            }
            else
                return "";
        }

        public string classColor<T>(ref T prevAmt, ref T currAmt)
        {
            string className = "";
            var comp = Comparer<T>
                .Default.Compare(prevAmt, currAmt);

            if (prevAmt != null && comp < 0)
                className = "green";
            else if (prevAmt != null && comp > 0)
                className = "red";

            return className;
        }

        public bool isChecked(int i)
        {
            var val = lstOptions.ElementAt(i);
            TDAOptionQuote option = val[0];
            return option.isChecked;
        }

        public string showChecked(TDAOptionQuote option, string val)
        {
            return option.isChecked ? val : " ";
        }

        #endregion

        #region Methods

        public void getOptionQuote(int i, ref TDAOptionQuote option, ref TDAOptionQuote longOption)
        {
            if (lstOptions[0][0].symbol.LastIndexOf("C") > lstOptions[0][0].symbol.LastIndexOf("P"))
                lstOptions = TDA.callOptions;
            else
                lstOptions = TDA.putOptions;

            if (i < lstOptions.Length - 1)
            {
                var next = lstOptions.ElementAt(i + 1);

                if (TDAParameters.inTheMoney)
                    if (i == 0)
                    {
                        if (otmOptions != null)
                            next = otmOptions.ElementAt(0);
                    }
                    else
                        next = lstOptions.ElementAt(i - 1);

                TDAOptionQuote nextOption = next[0];

                option.prem = option.bid - nextOption.ask;
            }

            if (i + TDAParameters.optionNumSpreadStrikes < lstOptions.Length)
            {
                var next2 = lstOptions.ElementAt(i + TDAParameters.optionNumSpreadStrikes);
                if (TDAParameters.inTheMoney)
                    if (i == TDAParameters.optionNumSpreadStrikes - 1)
                    {
                        if (otmOptions != null)
                            next2 = otmOptions.ElementAt(i + (TDAParameters.optionNumSpreadStrikes - 1));
                    }
                    else
                        if(i >= TDAParameters.optionNumSpreadStrikes)
                        next2 = lstOptions.ElementAt(i - TDAParameters.optionNumSpreadStrikes);

                longOption = next2[0];
                option.prem2 = option.bid - longOption.ask;

                option.buyOption = longOption.symbol;
                var sellOption = option.symbol;
                string longStrike = "";
                if (option.buyOption.LastIndexOf("C") > option.buyOption.LastIndexOf("P"))
                {
                    longStrike = Regex.Split(option.buyOption, "C").Last();
                    option.buyOption = "C" + longStrike;
                }
                else
                {
                    longStrike = Regex.Split(option.buyOption, "P").Last();
                    option.buyOption = "P" + longStrike;
                }
                option.maxLoss = Math.Abs(option.strikePrice - Convert.ToSingle(longStrike)) - option.prem2;
                option.breakeven = option.strikePrice + option.prem2;
                option.index = i;
                option.buyLongStrike = Convert.ToSingle(longStrike);
                var hasKey = TDAParameters.dictOptionCheckbox.ContainsKey(option.description);
                var isChecked = true;
                if (hasKey) isChecked = TDAParameters.dictOptionCheckbox[option.description];
                if (TDAParameters.sellOptionIndex != -1)
                    if (TDASpreadManager.isInDepthAndParity(i))
                        option.isChecked = isChecked;
                //var tokenTimeStampAsDateObj = new Date(userPrincipalsResponse.streamerInfo.tokenTimestamp);
                //var tokenTimeStampAsMs = tokenTimeStampAsDateObj.getTime();
                if (option.isChecked)
                {
                    //var contracts = (float)TDAParameters.contracts[i];
                    option.credit = option.contracts * 100 * option.prem2;
                    option.collateral = option.contracts * 100 * Math.Abs(option.strikePrice - longOption.strikePrice) - option.credit;

                    totalCollateral += option.collateral;
                    totalContracts += option.contracts;
                    totalCredit += option.credit;
                }
            }

        }

        #endregion

    }


}
