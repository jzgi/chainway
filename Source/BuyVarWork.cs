using System;
using System.Data;
using System.Threading.Tasks;
using ChainFx;
using ChainFx.Web;
using static ChainFx.Web.Modal;
using static ChainSmart.WeixinUtility;
using static ChainFx.Application;
using static ChainFx.Nodal.Nodality;

namespace ChainSmart;

public abstract class BuyVarWork : WebWork
{
    public async Task @default(WebContext wc)
    {
        int id = wc[0];

        using var dc = NewDbContext();
        dc.Sql("SELECT ").collst(Buy.Empty).T(" FROM buys WHERE id = @1");
        var o = await dc.QueryTopAsync<Buy>(p => p.Set(id));

        wc.GivePane(200, h =>
        {
            h.UL_("uk-list uk-list-divider");
            h.LI_().LABEL("时间").SPAN_("uk-static").T(o.created, time: 2)._SPAN()._LI();
            if (o.IsOnNet)
            {
                h.LI_().LABEL("买家").SPAN_("uk-static").T(o.uname).SP().A_TEL(o.utel, o.utel)._SPAN()._LI();
                h.LI_().LABEL(string.Empty).SPAN_("uk-static").T(o.ucom).T('-').T(o.uaddr)._SPAN()._LI();
                h.LI_().FIELD("金额", o.topay, true).FIELD("派送费", o.fee, true)._LI();
                h.LI_().FIELD("状态", Buy.Statuses[o.status]).FIELD("创建", o.creator)._LI();
                h.LI_().FIELD("集合", o.adapter).FIELD(o.IsVoid ? "撤销" : "派发", o.oker)._LI();
            }
            else
            {
                h.LI_().FIELD("金额", o.topay, true).FIELD("创建", o.creator)._LI();
            }
            h._UL();

            // buy items

            h.TABLE(o.items, d =>
            {
                h.TD_().T(d.name);
                if (d.unitw > 0)
                {
                    h.SP().SMALL_().T(Unit.Metrics[d.unitw])._SMALL();
                }
                h._TD();
                h.TD_(css: "uk-text-right").CNY(d.RealPrice)._TD();
                h.TD2(d.qty, d.unit, css: "uk-text-right");
                h.TD(d.SubTotal, true, true);
            });

            h.TOOLBAR(bottom: true, status: o.Status, state: o.ToState());
        });
    }
}

public class MyBuyVarWork : BuyVarWork
{
    public async Task ok(WebContext wc)
    {
        int id = wc[0];
        var prin = (User)wc.Principal;

        using var dc = NewDbContext();
        dc.Sql("UPDATE buys SET oked = @1, oker = @2, status = 4 WHERE id = @3 AND uid = @4 AND status = 2 RETURNING rtlid, pay");
        if (await dc.QueryTopAsync(p => p.Set(DateTime.Now).Set(prin.name).Set(id).Set(prin.id)))
        {
            dc.Let(out int rtlid);
            dc.Let(out decimal pay);

            var rtl = GrabTwin<int, Org>(rtlid);
            rtl.Notices.Put(OrgNoticePack.BUY_OKED, 1, pay);
        }

        wc.Give(200);
    }
}

public class RtllyBuyVarWork : BuyVarWork
{
    [Ui(tip: "回退到收单状态", icon: "reply", status: 2 | 4), Tool(ButtonConfirm, state: Buy.STA_REVERSABLE)]
    public async Task back(WebContext wc)
    {
        int id = wc[0];
        var org = wc[-2].As<Org>();

        using var dc = NewDbContext();
        dc.Sql("UPDATE buys SET adapted = NULL, adapter = NULL, oked = NULL, oker = NULL, status = 1 WHERE id = @1 AND rtlid = @2 AND (status = 2 OR status = 4)");
        await dc.ExecuteAsync(p => p.Set(id).Set(org.id));

        wc.Give(200);
    }

    [OrglyAuthorize(0, User.ROL_LOG)]
    [Ui("派发", "商户自行安排派发？", status: 1), Tool(ButtonConfirm)]
    public async Task ok(WebContext wc)
    {
        int id = wc[0];
        var org = wc[-2].As<Org>();
        var prin = (User)wc.Principal;

        using var dc = NewDbContext();
        dc.Sql("UPDATE buys SET oked = @1, oker = @2, status = 4 WHERE id = @3 AND rtlid = @4 AND status = 1 RETURNING uim, pay");
        if (await dc.QueryTopAsync(p => p.Set(DateTime.Now).Set(prin.name).Set(id).Set(org.id)))
        {
            dc.Let(out string uim);
            dc.Let(out decimal pay);

            await PostSendAsync(uim, $"商家自行派送，请留意收货（{org.name}，单号{id:D8}，￥{pay}）");
        }

        wc.Give(200);
    }

    [OrglyAuthorize(0, User.ROL_MGT)]
    [Ui("撤销", "确认撤销并退款？", status: 1 | 2), Tool(ButtonConfirm)]
    public async Task @void(WebContext wc)
    {
        int id = wc[0];
        var org = wc[-2].As<Org>();
        var prin = (User)wc.Principal;

        using var dc = NewDbContext(IsolationLevel.ReadCommitted);
        try
        {
            dc.Sql("UPDATE buys SET refund = pay, status = 0, adapted = @1, adapter = @2 WHERE id = @3 AND rtlid = @4 AND status = 1 RETURNING uim, topay, refund");
            if (await dc.QueryTopAsync(p => p.Set(DateTime.Now).Set(prin.name).Set(id).Set(org.id)))
            {
                dc.Let(out string uim);
                dc.Let(out decimal topay);
                dc.Let(out decimal refund);

                // remote call
                var trade_no = Buy.GetOutTradeNo(id, topay);
                string err = await PostRefundAsync(sup: false, trade_no, refund, refund, trade_no);
                if (err != null) // not success
                {
                    dc.Rollback();
                    Err(err);
                }
                else
                {
                    // notify user
                    await PostSendAsync(uim, "您的订单已经撤销，请查收退款（" + org.name + "　#" + trade_no + "　￥" + refund + "）");
                }
            }
        }
        catch (Exception)
        {
            dc.Rollback();
            Err("退款失败，订单号：" + id);
            return;
        }

        wc.Give(200);
    }
}

public class MktlyBuyVarWork : BuyVarWork
{
    public async Task com(WebContext wc)
    {
        string com = wc[0];
        var mkt = wc[-2].As<Org>();

        using var dc = NewDbContext();

        dc.Sql("SELECT localtimestamp(0); SELECT utel, first(uaddr), count(CASE WHEN status = 1 THEN 1 END), count(CASE WHEN status = 2 THEN 2 END) FROM buys WHERE mktid = @1 AND (status = 1 OR status = 2) AND typ = 1 AND ucom = @2 GROUP BY ucom, utel;");
        await dc.QueryTopAsync(p => p.Set(mkt.id).Set(com));

        // the time stamp to fence the range to update
        dc.Let(out DateTime stamp);
        var intstamp = MainUtility.ToInt2020(stamp);

        wc.GivePane(200, h =>
        {
            h.TABLE_();
            h.THEAD_().TH("地址").TH("收单", css: "uk-width-tiny").TH("集合", css: "uk-width-tiny")._THEAD();

            dc.NextResult();
            while (dc.Next())
            {
                dc.Let(out string utel);
                dc.Let(out string uaddr);
                dc.Let(out int created);
                dc.Let(out int adapted);

                h.TR_();
                h.TD_().PICK(utel).SP().SPAN_().A_TEL(utel, utel).SP().T(uaddr)._SPAN()._TD();
                h.TD_();
                if (created > 0)
                {
                    h.ADIALOG_(nameof(created), "?utel=", utel, mode: ToolAttribute.MOD_SHOW, false, css: "uk-link uk-button-link uk-flex-center").T(created)._A();
                }
                h._TD();
                h.TD_();
                if (adapted > 0)
                {
                    h.ADIALOG_(nameof(adapted), "?utel=", utel, mode: ToolAttribute.MOD_SHOW, false, css: "uk-link uk-button-link uk-flex-center").T(adapted)._A();
                }
                h._TD();
                h._TR();
            }

            h._TABLE();

            h.TOOLBAR(subscript: intstamp, toggle: true, bottom: true);
        });
    }

    public async Task created(WebContext wc)
    {
        string com = wc[0];
        var mkt = wc[-2].As<Org>();
        string utel = wc.Query[nameof(utel)];

        using var dc = NewDbContext();
        dc.Sql("SELECT ").collst(Buy.Empty).T(" FROM buys WHERE mktid = @1 AND status = 1 AND typ = 1 AND ucom = @2 AND utel = @3");
        var arr = await dc.QueryAsync<Buy>(p => p.Set(mkt.id).Set(com).Set(utel));

        wc.GivePane(200, h =>
        {
            h.MAINGRID(arr, o =>
            {
                h.UL_("uk-card-body uk-list uk-list-divider");
                h.LI_().H4_().T(o.name);
                if (o.tip != null)
                {
                    h.T('（').T(o.tip).T('）');
                }
                h._H4();
                h.SPAN_("uk-badge").T(o.created, time: 0).SP().T(Buy.Statuses[o.status])._SPAN()._LI();

                foreach (var it in o.items)
                {
                    h.LI_();

                    h.SPAN_("uk-width-expand").T(it.name);
                    if (it.unitw > 0)
                    {
                        h.SP().SMALL_().T(it.unitw).T(it.unit)._SMALL();
                    }

                    h._SPAN();

                    h.SPAN_("uk-width-1-5 uk-flex-right").CNY(it.RealPrice).SP().SUB(it.unit)._SPAN();
                    h.SPAN_("uk-width-tiny uk-flex-right").T(it.qty).SP().T(it.unit)._SPAN();
                    h.SPAN_("uk-width-1-5 uk-flex-right").CNY(it.SubTotal)._SPAN();
                    h._LI();
                }
                h._LI();

                h._UL();
            });
        }, false, 6);
    }

    public async Task adapted(WebContext wc)
    {
        string com = wc[0];
        var mkt = wc[-2].As<Org>();
        string utel = wc.Query[nameof(utel)];

        using var dc = NewDbContext();
        dc.Sql("SELECT ").collst(Buy.Empty).T(" FROM buys WHERE mktid = @1 AND status = 2 AND typ = 1 AND ucom = @2 AND utel = @3");
        var arr = await dc.QueryAsync<Buy>(p => p.Set(mkt.id).Set(com).Set(utel));

        wc.GivePane(200, h =>
        {
            h.MAINGRID(arr, o =>
            {
                h.UL_("uk-card-body uk-list uk-list-divider");
                h.LI_().H4_().T(o.name);
                if (o.tip != null)
                {
                    h.T('（').T(o.tip).T('）');
                }
                h._H4();
                h.SPAN_("uk-badge").T(o.created, time: 0).SP().T(Buy.Statuses[o.status])._SPAN()._LI();

                foreach (var it in o.items)
                {
                    h.LI_();

                    h.SPAN_("uk-width-expand").T(it.name);
                    if (it.unitw > 0)
                    {
                        h.SP().SMALL_().T(it.unitw).T(it.unit)._SMALL();
                    }

                    h._SPAN();

                    h.SPAN_("uk-width-1-5 uk-flex-right").CNY(it.RealPrice).SP().SUB(it.unit)._SPAN();
                    h.SPAN_("uk-width-tiny uk-flex-right").T(it.qty).SP().T(it.unit)._SPAN();
                    h.SPAN_("uk-width-1-5 uk-flex-right").CNY(it.SubTotal)._SPAN();
                    h._LI();
                }
                h._LI();

                h._UL();
            });
        }, false, 6);
    }

    [OrglyAuthorize(0, User.ROL_LOG)]
    [Ui("派发", "统一派发？"), Tool(ButtonPickConfirm)]
    public async Task ok(WebContext wc, int v2020)
    {
        string com = wc[0];
        var prin = (User)wc.Principal;
        var mkt = wc[-2].As<Org>();
        var stamp = MainUtility.ToDateTime(v2020);

        var f = await wc.ReadAsync<Form>();
        string[] key = f[nameof(key)];

        // Note: update only status = 2
        using var dc = NewDbContext();
        dc.Sql("UPDATE buys SET status = 4, oked = @1, oker = @2 WHERE mktid = @3 AND status = 2 AND typ = 1 AND ucom = @4 AND utel")._IN_(key).T(" AND adapted <= @5");
        await dc.ExecuteAsync(p => p.Set(DateTime.Now).Set(prin.name).Set(mkt.id).Set(com).SetForIn(key).Set(stamp));

        wc.GivePane(200);
    }
}