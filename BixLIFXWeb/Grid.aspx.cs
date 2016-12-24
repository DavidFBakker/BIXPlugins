using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Web.UI.WebControls;
using BixPlugins.BixLIFX;
using LifxNet;
using Telerik.Web.UI;

namespace BixLIFXWeb
{
    public partial class Grid : System.Web.UI.Page
    {
     
        protected void RadGrid1_NeedDataSource(object sender, Telerik.Web.UI.GridNeedDataSourceEventArgs e)

        {
            var bulbs = new ObservableCollection<LightBulb>();

            RadGrid1.DataSource = bulbs;
        }

    }
}
