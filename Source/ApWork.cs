﻿using System;
using System.Data;
using System.Threading.Tasks;
using ChainFx.Web;
using static ChainFx.Web.Modal;
using static ChainFx.Nodal.Nodality;

namespace ChainSmart;

/// <summary>
/// Accounts Payable.
/// </summary>
/// <typeparam name="V"></typeparam>
public abstract class ApWork<V> : WebWork where V : ApVarWork, new()
{
    protected override void OnCreate()
    {
        CreateVarWork<V>(state: State);
    }

    protected static void MainTable(HtmlBuilder h, Ap[] arr, bool orgname)
    {
        h.TABLE_();
        DateTime last = default;
        foreach (var o in arr)
        {
            if (o.dt != last)
            {
                h.TR_().TD_("uk-padding-tiny-left", colspan: 4).T(o.dt, time: 0)._TD()._TR();
            }

            h.TR_();
            if (orgname)
            {
                var org = GrabTwin<int, Org>(o.orgid);
                h.TD(org.name);
            }

            h.TD(Ap.Typs[o.typ]);
            h.TD_("uk-text-right").T(o.trans)._TD();
            if (!orgname)
            {
                h.TD_("uk-text-right").CNY(o.amt)._TD();
            }

            h.TD_("uk-text-right").CNY(o.topay)._TD();
            h._TR();

            last = o.dt;
        }

        h._TABLE();
    }
}

[Ui("市场端应付帐款")]
public class AdmlyBuyApWork : ApWork<AdmlyBuyApVarWork>
{
    [Ui("应付帐款", status: 1), Tool(Anchor)]
    public async Task @default(WebContext wc, int page)
    {
        using var dc = NewDbContext();
        dc.Sql("SELECT ").collst(Ap.Empty).T(" FROM buyaps WHERE status BETWEEN 1 AND 2 ORDER BY id LIMIT 40 OFFSET @1 * 40");
        var arr = await dc.QueryAsync<Ap>(p => p.Set(page));

        wc.GivePage(200, h =>
        {
            h.TOOLBAR();
            if (arr == null)
            {
                h.ALERT("尚无结款");
                return;
            }

            MainTable(h, arr, true);

            h.PAGINATION(arr.Length == 40);
        }, false, 60);
    }

    [Ui(tip: "已付款", icon: "credit-card", status: 2), Tool(Anchor)]
    public async Task oked(WebContext wc, int page)
    {
        using var dc = NewDbContext();
        dc.Sql("SELECT ").collst(Ap.Empty).T(" FROM buyaps WHERE typ BETWEEN ").T(Ap.TYP_RTL).T(" AND ").T(Ap.TYP_MKT).T(" AND status = 4 ORDER BY id LIMIT 40 OFFSET @1 * 40");
        var arr = await dc.QueryAsync<Ap>(p => p.Set(page));

        wc.GivePage(200, h =>
        {
            h.TOOLBAR();
            if (arr == null)
            {
                h.ALERT("尚无结款");
                return;
            }

            MainTable(h, arr, true);

            h.PAGINATION(arr.Length == 40);
        }, false, 3);
    }

}

[AdmlyAuthorize(User.ROL_FIN)]
[Ui("供应端应付帐款")]
public class AdmlyPurApWork : ApWork<AdmlyPurApVarWork>
{
    [Ui("应付帐款", status: 1), Tool(Anchor)]
    public async Task @default(WebContext wc, int page)
    {
        using var dc = NewDbContext();
        dc.Sql("SELECT ").collst(Ap.Empty).T(" FROM puraps WHERE typ BETWEEN 1 AND 3 AND status = 1 ORDER BY id LIMIT 40 OFFSET @1 * 40");
        var arr = await dc.QueryAsync<Ap>();

        wc.GivePage(200, h =>
        {
            h.TOOLBAR();
            if (arr == null)
            {
                h.ALERT("尚无结算");
                return;
            }

            MainTable(h, arr, true);

            h.PAGINATION(arr.Length == 40);
        }, false, 3);
    }

    [Ui(tip: "历史", icon: "history", status: 2), Tool(AnchorPrompt)]
    public async Task past(WebContext wc, int page)
    {
        var topOrgs = Grab<int, Org>();
        bool inner = wc.Query[nameof(inner)];
        int prv = 0;
        if (inner)
        {
            wc.GivePane(200, h =>
            {
                h.FORM_().FIELDSUL_("按供应版块");
                // h.LI_().SELECT("版块", nameof(prv), prv, topOrgs, filter: (k, v) => v.EqZone, required: true);
                h._FIELDSUL()._FORM();
            });
        }
        else
        {
            using var dc = NewDbContext();
            dc.Sql("SELECT ").collst(Ap.Empty).T(" FROM clears WHERE typ = ").T(Ap.TYP_SUP).T(" AND sprid = @1 AND status > 0 ORDER BY id DESC LIMIT 40 OFFSET 40 * @2");
            var arr = await dc.QueryAsync<Ap>(p => p.Set(prv).Set(page));
            wc.GivePage(200, h =>
            {
                h.TOOLBAR();
                if (arr == null)
                {
                    return;
                }

                h.PAGINATION(arr.Length == 40);
            }, false, 3);
        }
    }

}

[Ui("业务结款")]
public class OrglyBuyApWork : ApWork<PtylyApVarWork>
{
    [Ui("业务结款", status: 1), Tool(Anchor)]
    public void @default(WebContext wc, int page)
    {
        var isOrg = (bool)State;
        var org = isOrg ? wc[-1].As<Org>() : null;

        using var dc = NewDbContext();
        dc.Sql("SELECT ").collst(Ap.Empty).T(" FROM buyaps WHERE orgid = @1 AND status BETWEEN 1 AND 2 ORDER BY id DESC LIMIT 40 OFFSET @2 * 40");
        var arr = dc.Query<Ap>(p =>
        {
            if (org == null)
            {
                p.SetNull();
            }
            else
            {
                p.Set(org.id);
            }

            p.Set(page);
        });

        wc.GivePage(200, h =>
        {
            h.TOOLBAR();
            if (arr == null)
            {
                h.ALERT("尚无结款");
                return;
            }

            MainTable(h, arr, false);

            h.PAGINATION(arr?.Length == 20);
        }, false, 3);
    }
}

[Ui("业务结款")]
public class OrglyPurApWork : ApWork<PtylyApVarWork>
{
    [Ui("业务收入", status: 1), Tool(Anchor)]
    public void @default(WebContext wc, int page)
    {
        var isOrg = (bool)State;

        var org = isOrg ? wc[-1].As<Org>() : null;

        using var dc = NewDbContext();
        dc.Sql("SELECT ").collst(Ap.Empty).T(" FROM puraps WHERE orgid = @1 AND status BETWEEN 1 AND 2 ORDER BY id DESC LIMIT 40 OFFSET @2 * 40");
        var arr = dc.Query<Ap>(p =>
        {
            if (org == null)
            {
                p.SetNull();
            }
            else
            {
                p.Set(org.id);
            }

            p.Set(page);
        });

        wc.GivePage(200, h =>
        {
            h.TOOLBAR();
            if (arr == null)
            {
                h.ALERT("尚无结款");
                return;
            }

            MainTable(h, arr, false);

            h.PAGINATION(arr?.Length == 20);
        }, false, 3);
    }
}