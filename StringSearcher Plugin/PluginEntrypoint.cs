using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DotNetResolver.Plugins;

namespace StringSearcher
{
    public class PluginEntrypoint : Plugin
    {
        public override void Entrypoint()
        {
            IConnector connector = this.StartUpContext.Connector;
            IControlManager manager = connector.ControlManager;

            StringSearcherControl control = new StringSearcherControl(this.StartUpContext.Connector);
            control.Dock = DockStyle.Fill;
            TabPage tabPage = new TabPage("String Searcher");
            tabPage.Controls.Add(control);

            manager.AddTabPage(tabPage);
        }

        public override string Name
        {
            get { return "String Searcher"; }
        }

        public override Version MinimumVersion
        {
            get
            {
                return new Version(3, 3, 0, 0);
            }
        }
    }
}
