﻿using ChainFX;

namespace ChainSMart
{
    /// <summary>
    /// A detail line of buy.
    /// </summary>
    public class BuyLn : IData, IKeyable<int>
    {
        public int wareid;

        public int itemid;

        public string name;

        public string unit; // basic unit

        public decimal unitx; // number of units per pack

        public decimal price;

        public decimal off;

        public decimal qty;

        public BuyLn()
        {
        }

        public BuyLn(int wareid, decimal qty)
        {
            this.wareid = wareid;
            this.qty = qty;
        }

        public BuyLn(int wareid, string[] comp)
        {
            this.wareid = wareid;

            itemid = int.Parse(comp[0]);
            name = comp[1];
            unit = comp[2];
            unitx = decimal.Parse(comp[3]);
            price = decimal.Parse(comp[4]);
            qty = decimal.Parse(comp[5]);
        }

        public void Read(ISource s, short msk = 0xff)
        {
            s.Get(nameof(wareid), ref wareid);
            s.Get(nameof(itemid), ref itemid);
            s.Get(nameof(name), ref name);
            s.Get(nameof(unit), ref unit);
            s.Get(nameof(unitx), ref unitx);
            s.Get(nameof(price), ref price);
            s.Get(nameof(off), ref off);
            s.Get(nameof(qty), ref qty);
        }

        public void Write(ISink s, short msk = 0xff)
        {
            s.Put(nameof(wareid), wareid);
            s.Put(nameof(name), name);
            s.Put(nameof(itemid), itemid);
            s.Put(nameof(unit), unit);
            s.Put(nameof(unitx), unitx);
            s.Put(nameof(price), price);
            s.Put(nameof(off), off);
            s.Put(nameof(qty), qty);
        }

        public int Key => wareid;

        public decimal RealPrice => price - off;

        public decimal SubTotal => decimal.Round(RealPrice * qty, 2);

        public short QtyX => (short) (qty / unitx);

        internal void Init(Ware w, bool discount)
        {
            name = w.name;
            itemid = w.itemid;
            unit = w.unit;
            unitx = w.unitx;
            price = w.price;

            if (discount)
            {
                off = w.off;
            }
        }
    }
}