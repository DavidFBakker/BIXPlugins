using System;
using BixPlugins.BixLIFX;

namespace BixLIFXWeb
{
    public partial class MasterPage : System.Web.UI.MasterPage
    {
        public MasterPage()
        {
            BixLIFX.Init(false);
        }
        protected void Page_Load(object sender, EventArgs e)
        {

        }
    }
}