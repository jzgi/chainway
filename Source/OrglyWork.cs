using Chainly;
using Chainly.Web;
using static Chainly.Nodal.Store;

namespace Revital
{
    public abstract class OrglyWork : WebWork
    {
    }

    /// <summary>
    /// source and producer
    /// </summary>
    [Ui("供应产源业务")]
    public class PrvlyWork : OrglyWork
    {
        protected override void OnCreate()
        {
            // id of either current user or the specified
            CreateVarWork<PrvlyVarWork>((prin, key) =>
                {
                    var orgid = key?.ToInt() ?? ((User) prin).orgid;
                    return GrabObject<int, Org>(orgid);
                }
            );
        }
    }

#if ZHNT
    [Ui("市场商户业务")]
#else
    [Ui("驿站商户业务")]
#endif
    public class MrtlyWork : OrglyWork
    {
        protected override void OnCreate()
        {
            // id of either current user or the specified
            CreateVarWork<MrtlyVarWork>((prin, key) =>
                {
                    var orgid = key?.ToInt() ?? ((User) prin).orgid;
                    return GrabObject<int, Org>(orgid);
                }
            );
        }
    }
}