using System.ServiceProcess;
using BixPlugins.BixLIFX;

namespace BixLIFXService
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            BixLIFX.Init();
        }

        protected override void OnStop()
        {
        }
    }
}