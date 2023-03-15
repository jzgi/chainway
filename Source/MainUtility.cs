using System;
using ChainFx;
using ChainFx.Web;
using static ChainFx.CryptoUtility;

namespace ChainSmart
{
    public static class MainUtility
    {
        public static double ComputeDistance(double lat1, double lng1, double lat2, double lng2)
        {
            const int EARTH_RADIUS_KM = 6371;

            var dlat = ToRadians(lat2 - lat1);
            var dlng = ToRadians(lng2 - lng1);

            lat1 = ToRadians(lat1);
            lat2 = ToRadians(lat2);

            var a = Math.Sin(dlat / 2) * Math.Sin(dlat / 2) + Math.Sin(dlng / 2) * Math.Sin(dlng / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EARTH_RADIUS_KM * c;

            static double ToRadians(double degrees)
            {
                return degrees * Math.PI / 180;
            }
        }


        public static HtmlBuilder A_POI(this HtmlBuilder h, double x, double y, string title, string addr, string tel = null, bool active = true)
        {
            if (active)
            {
                h.T("<a class=\"uk-icon-link\" uk-icon=\"location\" href=\"http://apis.map.qq.com/uri/v1/marker?marker=coord:").T(y).T(',').T(x).T(";title:").T(title).T(";addr:").T(addr);
                if (tel != null)
                {
                    h.T(";tel:").T(tel);
                }

                h.T("\" onclick=\"return dialog(this,32,false,'").T(title).T("');\"></a>");
            }
            else
            {
                h.T("<span class=\"uk-icon-link uk-inactive\" uk-icon=\"location\"></span>");
            }

            return h;
        }

        public static HtmlBuilder SELECT_SPEC(this HtmlBuilder h, string name, JObj specs, string css = null)
        {
            h.SELECT_(name, local: name, css: css);

            for (int i = 0; i < specs.Count; i++)
            {
                var spec = specs.EntryAt(i);
                var v = spec.Value;
                if (v.IsObject)
                {
                    h.OPTGROUP_(spec.Key);

                    var sub = (JObj)v;
                    for (int k = 0; k < sub.Count; k++)
                    {
                        var e = sub.EntryAt(k);
                        h.OPTION(e.Key, e.Value);
                    }

                    h._OPTGROUP();
                }
            }

            h.OPTION(string.Empty, "非派送区");

            h._SELECT();

            return h;
        }


        public static HtmlBuilder RECEIVER(this HtmlBuilder h, string tel)
        {
            h.T("<a class=\"uk-icon-button uk-light\" href=\"tel:").T(tel).T("\" uk-icon=\"icon: receiver\"></a>");
            return h;
        }

        public static HtmlBuilder A_ICON(this HtmlBuilder h, string url, string icon)
        {
            h.T("<a class=\"uk-icon-button uk-light\" href=\"").T(url).T("\" uk-icon=\"icon: \"").T(icon).T("\"></a>");
            return h;
        }

        public static HtmlBuilder A_TEL(this HtmlBuilder h, string name, string tel, string css = null)
        {
            h.T("<a ");
            if (css != null)
            {
                h.T(" class=\"").T(css).T("\" ");
            }

            h.T("href=\"tel:").T(tel).T("\">").T(name).T("</a>");
            return h;
        }

        public static HtmlBuilder ATEL(this HtmlBuilder h, string tel = null, string css = null)
        {
            h.T("<a class=\"uk-icon-link uk-circle");
            if (css != null)
            {
                h.T(' ');
                h.T(css);
            }

            h.T("\" uk-icon=\"receiver\" href=\"tel:").T(tel).T("\"></a>");
            return h;
        }

        public static HtmlBuilder POI_(this HtmlBuilder h, double x, double y, string title, string addr, string tel = null)
        {
            h.T("<a class=\"\" href=\"http://apis.map.qq.com/uri/v1/marker?marker=coord:").T(y).T(',').T(x).T(";title:").T(title).T(";addr:").T(addr);
            if (tel != null)
            {
                h.T(";tel:").T(tel);
            }

            h.T("&referer=\">");
            return h;
        }

        public static HtmlBuilder _POI(this HtmlBuilder h)
        {
            h.T("</a>");
            return h;
        }

        public static HtmlBuilder MASKNAME(this HtmlBuilder h, string name)
        {
            for (int i = 0; i < name?.Length; i++)
            {
                var c = name[i];
                if (i == 0)
                {
                    h.T(c);
                }
                else
                {
                    h.T('Ｘ');
                }
            }

            return h;
        }


        public static string ComputeCredential(string tel, string password)
        {
            string v = tel + ":" + password;
            return MD5(v);
        }


        public static void SetUserCookies(this WebContext wc, User o, int maxage = 3600 * 24 * 3)
        {
            // token cookie
            var token = AuthenticateAttribute.ToToken(o, 0x0fff);
            var tokenStr = WebUtility.BuildSetCookie(nameof(token), token, maxage: maxage, httponly: true);

            // cookie for vip, o means none
            var vipStr = WebUtility.BuildSetCookie(nameof(o.vip), TextUtility.ToString(o.vip), maxage: maxage);
            var nameStr = WebUtility.BuildSetCookie(nameof(o.name), (o.name), maxage: maxage);
            var telStr = WebUtility.BuildSetCookie(nameof(o.tel), (o.tel), maxage: maxage);

            // multiple cookie
            wc.SetHeader("Set-Cookie", tokenStr, vipStr, nameStr, telStr);
        }

        public static void ViewAgrmt(this HtmlBuilder h, JObj jo)
        {
            string title = null;
            string a = null, b = null;
            string[] terms = null;
            jo.Get(nameof(title), ref title);
            jo.Get(nameof(a), ref a);
            jo.Get(nameof(b), ref b);
            jo.Get(nameof(terms), ref terms);

            h.OL_();
            for (int i = 0; i < terms.Length; i++)
            {
                var o = terms[i];
                h.LI_().T(o)._LI();
            }

            h._OL();
            h.FIELD("甲方", a).FIELD("乙方", b);
        }
    }
}