﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ChainFx;
using ChainFx.Web;
using static ChainFx.Entity;
using static ChainFx.Web.Modal;
using static ChainFx.Fabric.Nodality;
using static ChainFx.Web.ToolAttribute;

namespace ChainMart
{
    public abstract class WareWork<V> : WebWork where V : WareVarWork, new()
    {
        protected override void OnCreate()
        {
            CreateVarWork<V>();
        }
    }

    public class PublyWareWork : WareWork<PublyWareVarWork>
    {
        public async Task @default(WebContext wc)
        {
            int shpid = wc[0];
            var shp = GrabObject<int, Org>(shpid);

            using var dc = NewDbContext();
            dc.Sql("SELECT ").collst(Ware.Empty).T(" FROM wares_vw WHERE shpid = @1 AND status = 4 ORDER BY id DESC");
            var arr = await dc.QueryAsync<Ware>(p => p.Set(shp.id));

            wc.GivePage(200, h =>
            {
                if (arr == null)
                {
                    h.ALERT("暂无商品");
                    return;
                }

                decimal fprice = 0;

                h.FORM_(oninput: $"pay.value = {fprice} * parseInt(unitx.value) * parseInt(qty.value);");

                h.MAINGRID(arr, o =>
                {
                    h.SECTION_("uk-card-body uk-flex");

                    // the cclickable icon
                    //
                    h.ADIALOG_(o.Key, "/", MOD_SHOW, false, css: "uk-display-contents");
                    if (o.icon)
                    {
                        h.PIC_("uk-width-1-5").T(MainApp.WwwUrl).T("/ware/").T(o.id).T("/icon")._PIC();
                    }
                    else
                    {
                        h.PIC("/void.webp", css: "uk-width-1-5");
                    }
                    h._A();

                    h.ASIDE_();

                    h.HEADER_().H4(o.name);
                    if (o.unitx != 1)
                    {
                        h.SP().SMALL_().T(o.unitx).T(o.unit).T("件")._SMALL();
                    }
                    // top right corner span
                    h.SPAN_(css: "uk-badge");
                    // ran mark
                    short rank = 0;
                    if (o.itemid > 0)
                    {
                        var item = GrabObject<int, Item>(o.itemid);
                        if (item.state > 0)
                        {
                            rank = item.state;
                        }
                    }
                    h.MARK(Item.States[rank], "state", rank);
                    h._SPAN();
                    h._HEADER();

                    h.Q(o.tip, "uk-width-expand");

                    // FOOTER: price and qty select & detail
                    h.T($"<footer cookie= \"vip\" onfix=\"fillPriceAndQtySelect(this,event,{o.price},{o.off},{o.min},{o.max},{o.AvailX});\">"); // pricing portion
                    h.SPAN_("uk-width-1-4").T("<output class=\"rmb fprice\"></output>&nbsp;<sub>").T(o.unit).T("</sub>")._SPAN();
                    h.SELECT_(o.id, onchange: $"sumQtyDetails(this,{o.unitx});", css: "uk-width-1-5 qtyselect ").OPTION((short) 0, "0 件")._SELECT();
                    h.SPAN_("qtydetail uk-invisible").T("&nbsp;<output class=\"qtyx\"></output>&nbsp;").T(o.unit).T("<output class=\"rmb subtotal uk-width-expand uk-text-end\"></output>")._SPAN();
                    h._FOOTER();

                    h._ASIDE();

                    h._SECTION();
                });

                var topay = 0.00M;

                h.BOTTOMBAR_(large: true);

                h.DIV_("uk-col");
                h.T("<output class=\"nametel\" name=\"nametel\" cookie=\"nametel\"></output>");
                h.T("<input type=\"text\" name=\"addr\" class=\"uk-input\" placeholder=\"请填收货地址（限离市场２公里内）\" maxlength=\"30\" minlength=\"4\" local=\"addr\" required>");
                h._DIV();

                h.BUTTON_(nameof(buy), css: "uk-button-danger uk-width-medium uk-height-1-1", onclick: "return call_buy(this);").CNYOUTPUT(nameof(topay), topay)._BUTTON();

                h._BOTTOMBAR();

                h._FORM();
            }, true, 300, title: shp.name, onload: "fixAll();");
        }

        public async Task buy(WebContext wc, int cmd)
        {
            int shpid = wc[-1];
            var shp = GrabObject<int, Org>(shpid);
            var prin = (User) wc.Principal;

            var f = await wc.ReadAsync<Form>();
            string addr = f[nameof(addr)];

            // detail list
            var details = new List<BuyDetail>();
            for (int i = 0; i < f.Count; i++)
            {
                var ety = f.EntryAt(i);
                int id = ety.Key.ToInt();
                short qty = ety.Value;

                if (id <= 0 || qty <= 0) // filter out the non-selected (but submitted)
                {
                    continue;
                }

                details.Add(new BuyDetail
                {
                    wareid = id,
                    qty = qty
                });
            }

            using var dc = NewDbContext(IsolationLevel.ReadCommitted);
            try
            {
                dc.Sql("SELECT ").collst(Ware.Empty).T(" FROM wares WHERE shpid = @1 AND id ")._IN_(details);
                var map = await dc.QueryAsync<int, Ware>(p => p.Set(shpid).SetForIn(details));

                for (int i = 0; i < details.Count; i++)
                {
                    var dtl = details[i];
                    var ware = map[dtl.wareid];
                    if (ware != null)
                    {
                        dtl.InitByWare(ware, offed: prin.vip?.Contains(shpid) ?? false);
                    }
                }

                var m = new Buy
                {
                    typ = Buy.TYP_ONLINE,
                    name = shp.Name,
                    created = DateTime.Now,
                    creator = prin.name,
                    shpid = shp.id,
                    mktid = shp.MarketId,
                    details = details.ToArray(),
                    uid = prin.id,
                    uname = prin.name,
                    utel = prin.tel,
                    uim = prin.im,
                    uaddr = addr,
                };
                m.SetToPay();

                // NOTE single unsubmitted record
                const short msk = MSK_BORN | MSK_EDIT;
                dc.Sql("INSERT INTO buys ").colset(Buy.Empty, msk)._VALUES_(Buy.Empty, msk).T(" ON CONFLICT (shpid, status) WHERE status = 0 DO UPDATE ")._SET_(Buy.Empty, msk).T(" RETURNING id, topay");
                await dc.QueryTopAsync(p => m.Write(p, msk));
                dc.Let(out int buyid);
                dc.Let(out decimal topay);

                // // call WeChatPay to prepare order there
                string trade_no = Buy.GetOutTradeNo(buyid, topay);
                var (prepay_id, err_code) = await WeixinUtility.PostUnifiedOrderAsync(sup: false,
                    trade_no,
                    topay,
                    prin.im, // the payer
                    wc.RemoteIpAddress.ToString(),
                    MainApp.WwwUrl + "/" + nameof(WwwService.onpay),
                    m.ToString()
                );
                if (prepay_id != null)
                {
                    wc.Give(200, WeixinUtility.BuildPrepayContent(prepay_id));
                }
                else
                {
                    dc.Rollback();
                    wc.Give(500);
                }
            }
            catch (Exception e)
            {
                dc.Rollback();
                Application.Err(e.Message);
                wc.Give(500);
            }
        }
    }

    [OrglyAuthorize(Org.TYP_SHP, 1)]
    [Ui("商品管理", "商户")]
    public class ShplyWareWork : WareWork<ShplyWareVarWork>
    {
        protected static void MainGrid(HtmlBuilder h, Ware[] arr)
        {
            h.MAINGRID(arr, o =>
            {
                h.ADIALOG_(o.Key, "/", MOD_OPEN, false, tip: o.name, css: "uk-card-body uk-flex");
                if (o.icon)
                {
                    h.PIC_("uk-width-1-5").T(MainApp.WwwUrl).T("/ware/").T(o.id).T("/icon")._PIC();
                }
                else
                    h.PIC("/void.webp", css: "uk-width-1-5");

                h.ASIDE_();
                h.HEADER_().H4(o.name);
                if (o.unitx != 1)
                {
                    h.SP().SMALL_().T(o.unitx).T(o.unit).T("件")._SMALL();
                }
                h.SPAN(Ware.Statuses[o.status], "uk-badge");
                h._HEADER();

                h.Q(o.tip, "uk-width-expand");
                h.FOOTER_().SPAN_("uk-margin-auto-left").CNY(o.price)._SPAN()._FOOTER();
                h._ASIDE();

                h._A();
            });
        }

        [Ui("上线商品", group: 1), Tool(Anchor)]
        public async Task @default(WebContext wc)
        {
            var src = wc[-1].As<Org>();

            using var dc = NewDbContext();
            dc.Sql("SELECT ").collst(Ware.Empty).T(" FROM wares_vw WHERE shpid = @1 AND status = 4 ORDER BY oked DESC");
            var arr = await dc.QueryAsync<Ware>(p => p.Set(src.id));

            wc.GivePage(200, h =>
            {
                h.TOOLBAR(subscript: STA_FINE);

                if (arr == null)
                {
                    h.ALERT("尚无上线商品");
                    return;
                }
                MainGrid(h, arr);
            }, false, 4);
        }

        [Ui(tip: "下线商品", icon: "cloud-download", group: 2), Tool(Anchor)]
        public async Task offln(WebContext wc)
        {
            var src = wc[-1].As<Org>();

            using var dc = NewDbContext();
            dc.Sql("SELECT ").collst(Ware.Empty).T(" FROM wares_vw WHERE shpid = @1 AND status BETWEEN 1 AND 2 ORDER BY adapted DESC");
            var arr = await dc.QueryAsync<Ware>(p => p.Set(src.id));

            wc.GivePage(200, h =>
            {
                h.TOOLBAR(subscript: STA_VOID);
                if (arr == null)
                {
                    h.ALERT("尚无下线商品");
                    return;
                }
                MainGrid(h, arr);
            }, false, 4);
        }

        [Ui(tip: "作废商品", icon: "trash", group: 8), Tool(Anchor)]
        public async Task aborted(WebContext wc)
        {
            var src = wc[-1].As<Org>();

            using var dc = NewDbContext();
            dc.Sql("SELECT ").collst(Ware.Empty).T(" FROM wares_vw WHERE shpid = @1 AND status = 8 ORDER BY adapted DESC");
            var arr = await dc.QueryAsync<Ware>(p => p.Set(src.id));

            wc.GivePage(200, h =>
            {
                h.TOOLBAR(subscript: STA_VOID);
                if (arr == null)
                {
                    h.ALERT("尚无作废商品");
                    return;
                }
                MainGrid(h, arr);
            }, false, 4);
        }

        [OrglyAuthorize(Org.TYP_SHP, User.ROL_MGT)]
        [Ui("自建", "自建其它来源商品", icon: "plus", group: 2), Tool(ButtonOpen)]
        public async Task def(WebContext wc, int state)
        {
            var org = wc[-1].As<Org>();

            var prin = (User) wc.Principal;
            var cats = Grab<short, Cat>();

            if (wc.IsGet)
            {
                var o = new Ware
                {
                    created = DateTime.Now,
                    state = (short) state,
                };
                wc.GivePane(200, h =>
                {
                    h.FORM_().FIELDSUL_("商品信息");

                    h.LI_().TEXT("商品名", nameof(o.name), o.name, max: 12).SELECT("类别", nameof(o.typ), o.typ, cats, required: true)._LI();
                    h.LI_().TEXTAREA("简介", nameof(o.tip), o.tip, max: 40)._LI();
                    h.LI_().TEXT("计价单位", nameof(o.unit), o.unit, min: 1, max: 4, required: true).NUMBER("每件含量", nameof(o.unitx), o.unitx, min: 1, money: false)._LI();
                    h.LI_().NUMBER("单价", nameof(o.price), o.price, min: 0.00M, max: 99999.99M).NUMBER("大客户立减", nameof(o.off), o.off, min: 0.00M, max: 99999.99M)._LI();
                    h.LI_().NUMBER("起订件数", nameof(o.min), o.min).NUMBER("限订件数", nameof(o.max), o.max, min: 1, max: 1000)._LI();

                    h._FIELDSUL().BOTTOM_BUTTON("确认", nameof(def))._FORM();
                });
            }
            else // POST
            {
                const short msk = MSK_BORN | MSK_EDIT;
                // populate 
                var m = await wc.ReadObjectAsync(msk, new Ware
                {
                    shpid = org.id,
                    created = DateTime.Now,
                    creator = prin.name,
                });

                // insert
                using var dc = NewDbContext();
                dc.Sql("INSERT INTO wares ").colset(Ware.Empty, msk)._VALUES_(Ware.Empty, msk);
                await dc.ExecuteAsync(p => m.Write(p, msk));

                wc.GivePane(200); // close dialog
            }
        }

        [Ui("引入", "引入供应链产品", icon: "plus-circle", group: 2), Tool(ButtonOpen)]
        public async Task imp(WebContext wc, int state)
        {
            var org = wc[-1].As<Org>();
            var prin = (User) wc.Principal;

            if (wc.IsGet)
            {
                using var dc = NewDbContext();
                dc.Sql("SELECT DISTINCT itemid, concat(srcname, ' ', name), id FROM books WHERE shpid = @1 AND status = 4 ORDER BY id DESC LIMIT 50");
                await dc.QueryAsync(p => p.Set(org.id));
                var map = dc.ToIntMap();

                var o = new Ware
                {
                    created = DateTime.Now,
                    creator = prin.name,
                    unitx = 1.0M,
                    min = 1, max = 30,
                };

                wc.GivePane(200, h =>
                {
                    h.FORM_().FIELDSUL_("产品和销售信息");

                    h.LI_().SELECT("供应链产品", nameof(o.itemid), o.itemid, map, required: true)._LI();
                    h.LI_().TEXT("基本单位", nameof(o.unit), o.unit, min: 1, max: 4, required: true).NUMBER("每件含量", nameof(o.unitx), o.unitx, min: 1, money: false)._LI();
                    h.LI_().NUMBER("单价", nameof(o.price), o.price, min: 0.00M, max: 99999.99M).NUMBER("大客户立减", nameof(o.off), o.off, min: 0.00M, max: 99999.99M)._LI();
                    h.LI_().NUMBER("起订件数", nameof(o.min), o.min).NUMBER("限订件数", nameof(o.max), o.max, min: 1, max: 1000)._LI();

                    h._FIELDSUL().BOTTOM_BUTTON("确认", nameof(imp))._FORM();
                });
            }
            else // POST
            {
                const short msk = MSK_BORN | MSK_EDIT;
                // populate 
                var m = await wc.ReadObjectAsync(msk, new Ware
                {
                    shpid = org.id,
                    created = DateTime.Now,
                    creator = prin.name,
                });
                var item = GrabObject<int, Item>(m.itemid);
                m.typ = item.typ;
                m.name = item.name;
                m.tip = item.tip;

                // insert
                using var dc = NewDbContext(IsolationLevel.ReadCommitted);

                dc.Sql("INSERT INTO wares ").colset(Ware.Empty, msk)._VALUES_(Ware.Empty, msk).T(" RETURNING id");
                var wareid = (int) await dc.ScalarAsync(p => m.Write(p, msk));

                dc.Sql("UPDATE wares SET (icon, pic) = (SELECT icon, pic FROM items WHERE id = @1) WHERE id = @2");
                await dc.ExecuteAsync(p => p.Set(m.itemid).Set(wareid));

                wc.GivePane(200); // close dialog
            }
        }
    }
}