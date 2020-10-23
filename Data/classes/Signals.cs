using System;
using System.Collections.Generic;
using System.Security.Cryptography.Xml;
using System.Text;

namespace RhAutoHSOTP.classes
{
    public class Signals
    {
        public int dirCount { get; set; }
        public int baCount { get; set; }        
        public  int upCount { get; set; }
        public  int dnCount { get; set; }
        public  int bidCount { get; set; }
        public  int askCount { get; set; }

        public decimal spread { get; set; }

        //public Signals(int ups, int dns, int bids, int asks)
        //{

        //}
    }

    public class signal
    {
        public int count { get; set; }
        public DateTime when { get; set; }
        public decimal? price { get; set; }

        public signal(int c, DateTime t, decimal? p)
        {
            count = c;
            when = t;
            price = p;
        }
    }

    public class lstSignals
    {
        public List<signal> signs { get; set; }
    }

}
