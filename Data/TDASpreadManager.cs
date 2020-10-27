
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace tdaStreamHub.Data
{
    public class TDASpreadManager
    {
        #region properties

        Dictionary<int, double> dictCredits = new Dictionary<int, double>();
        private TDAOptionQuote[][] _lstOptions;
        public TDAOptionQuote[][] lstOptions
        {
            get { return _lstOptions; }
            set { _lstOptions = value; }
        }
        #endregion

        #region Events
        public static void clickAllocate(Radzen.Blazor.RadzenSplitButtonItem item)
        {
            if (item != null)
            {
                TDAParameters.allocate = Convert.ToInt16(item.Value);
                TDAParameters.allocation = (item.Text);
            }
        }

        public void ChangeContracts(dynamic value)
        {
            switch (value)
            {
                case 11: // so will go from 1 to 10 and not 1 to 11
                    TDAParameters.optionNumContracts = 10;
                    break;
                case 0: // so will go from 10 to 1 and not 10 to 0
                    TDAParameters.optionNumContracts = 1;
                    break;
                default:
                    break;
            }


        }
        #endregion


        #region methods

        public TDASpreadManager(TDAOptionQuote[][] plstOptions)
        {
            lstOptions = plstOptions;
        }

        /// <summary>
        /// Determine # of TDAParameters.contracts at each strike before
        /// </summary>
        /// <remarks>
        /// Has to be recalced if Buys, Depth or Strikes changes, so just do at top of cycle
        /// </remarks>
        public void contractsAllocate()
        {
            /// First, build dictCredits based on 100*prem for list of checked strikes? e.g. 0:52, 2:26, 4:13
            dictCredits = new Dictionary<int, double>();

            Dictionary<int, double> dictMultiples = new Dictionary<int, double>();
            double parts = 0;
            for (int i = 0; i < lstOptions.Length - 1; i++)
            {
                if (TDASpreadManager.isInDepthAndParity(i))
                /// If strike indexes within the Depth
                {
                    var optionsList = lstOptions.ElementAt(i);
                    TDAOptionQuote option = optionsList[0];
                    var optionsLongList = lstOptions.ElementAt(i + 1);
                    TDAOptionQuote optionLong = optionsList[0];
                    var valPrem = 100 * (option.bid - optionLong.ask);

                    var hasKey = TDAParameters.dictOptionCheckbox.ContainsKey(option.description);
                    var isChecked = true;
                    if (hasKey) isChecked = TDAParameters.dictOptionCheckbox[option.description];
                    if (isChecked)
                    {
                        dictCredits.Add(i, valPrem);
                        /// iMult is inverted multiple of most e.g. 2 if you are 1/2 of most
                        double iMult = 1;
                        switch (TDAParameters.allocation)
                        {
                            case "Geometric":
                                iMult = Math.Round(dictCredits.Values.First() / valPrem, 0);
                                iMult *= iMult;
                                break;
                            case "Equal Contracts":
                                iMult = 1;
                                break;
                            case "Progressive":
                                iMult = Math.Round(dictCredits.Values.First() / valPrem, 0);
                                break;
                            case "Equal Credit":
                                iMult = dictCredits.Values.First() / valPrem;
                                break;
                            case "Average":
                                iMult = dictCredits.Values.First() / valPrem;
                                iMult = iMult * iMult + iMult;
                                break;
                            default:
                                break;
                        }

                        dictMultiples.Add(i, iMult);
                        parts += iMult;
                    }
                }
            }
            /// Now get the number of TDAParameters.contracts per part, the multiple
            double mult = Math.Floor(TDAParameters.optionNumContracts / parts);
            /// So dictCredits has values like [52,26,13]
            /// and dictMults has values like [1,2,4]
            /// and parts is = 7
            /// and mult is 50 / 7 = 7
            /// So contracts becomes 7, 14, 28 = 49 total
            for (var i = 0; i < dictCredits.Values.Count; i++)
            {
                var key = dictCredits.ElementAt(i).Key;
                if (dictMultiples.ContainsKey(key))
                {
                    var contracts = mult * dictMultiples[key];
                    var optionsList = lstOptions.ElementAt(key);
                    TDAOptionQuote option = optionsList[0];
                    try
                    {
                        var isManual = TDAParameters.dictIsManualContracts.ContainsKey(option.description)
                            && TDAParameters.dictIsManualContracts[option.description];

                        if (!isManual)
                        {
                            if (TDAParameters.dictOptionContracts.ContainsKey(option.description))
                                TDAParameters.dictOptionContracts[option.description] = Convert.ToInt32(contracts);
                            else
                                TDAParameters.dictOptionContracts.Add(option.description, Convert.ToInt32(contracts));
                            option.contracts = Convert.ToInt32(contracts);
                        }
                    }
                    catch (Exception exx)
                    {
                        Console.WriteLine(exx.Message);
                    }
                }
            }

        }

        public static bool isInDepthAndParity(int i)
        {
            int depth = TDAParameters.optionNumDepthStrikes;
            int spread = TDAParameters.optionNumSpreadStrikes;
            int index = i + 1;
            /// sellIndex is the index of the strike the user has clicked on, e.g. 6 for the seventh strike
            int sellIndex = TDAParameters.sellOptionIndex;  // 9
            int maxIndex = sellIndex + 1;                   // 10    

            int minIndex = maxIndex - spread * (2 * depth) + depth + 1;       // 10 - 3*(1 + 0) - 1    = 6 , so range is [6,7,8,9,10] or 10 - 3*(2 + 1) - 0 = 3 [1,2,3,4,5,6,7,8,9,10]
                                                                              //                            [6,  8,  10]                  [1,2,    5,6,    9,10]
            bool isInRange = index >= minIndex && index <= maxIndex;

            bool isRightParity = maxIndex % 2 == 0 ? sellIndex / depth % 2 == i / depth % 2 : maxIndex / depth % 2 == index / depth % 2;

            if (depth == 1)
                return index == maxIndex;
            else
                return isInRange && isRightParity;

            ///// i is the index of the strike being displayed
            ///// maxIndex is sellIndex + 1 (e.g. 7)
            ///// minIndex is sellIndex - depth*depth (e.g. 1)
            //int index = (int)Math.Ceiling((double)(i + 1) / TDAParameters.optionNumdepthStrikes);
            //int sellIndex = (int)Math.Ceiling(((double)TDAParameters.sellOptionIndex + 1) / TDAParameters.optionNumdepthStrikes);
            //int maxIndex = index + 2 * (TDAParameters.optionNumDepthStrikes - 1);
            //int minIndex = i;
            //return
            ////sellIndex >= minIndex && sellIndex <= maxIndex
            ////&& 
            //sellIndex % 2 == index % 2; // within 2*Depth and same parity (mod 2 value) as sellOptionIndex
        }
        #endregion

    }


}

