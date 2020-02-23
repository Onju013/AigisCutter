using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace AigisCutter
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public static List<string> Args { get; private set; }
            = new List<string>();

        private void Application_Startup(
            object sender,
            StartupEventArgs e)
        {
            if(e.Args == null || e.Args.Count() <= 0)
                return;

            Args.AddRange(e.Args);
        }
    }
}