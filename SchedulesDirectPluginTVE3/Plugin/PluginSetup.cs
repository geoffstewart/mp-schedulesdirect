using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using SetupTv;

namespace SchedulesDirect.Plugin
{
  public class PluginSetup
  {
    internal const string PLUGIN_NAME = "SchedulesDirect EPG Client";
    internal const string PLUGIN_VERSION = "1.2.3.2";

    #region ISetupForm Members

    // Returns the name of the plugin which is shown in the plugin menu
    public string Name
    {
      get { return PLUGIN_NAME; }
    }

    // Returns the name of the plugin which is shown in the plugin menu
    public string Version
    {
      //get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(2); }
      get { return PLUGIN_VERSION; }
    }

    // Returns the author of the plugin which is shown in the plugin menu
    public string Author
    {
      get { return "Patrick"; }
    }

    // Returns the author of the plugin which is shown in the plugin menu
    public bool MasterOnly
    {
      get { return true; }
    }

    // Returns the description of the plugin is shown in the plugin menu
    public string Description()
    {
      return "Electronic Program Guide client for Schedules Direct US and Canada free TV listing service. Signup at schedulesdirect.org";
    }

    // indicates if a plugin has its own setup screen
    public bool HasSetup()
    {
      return true;
    }

    // show the setup dialog
    public SetupTv.SectionSettings Setup
    {
      get
      {
        SchedulesDirectPluginConfig configForm = new SchedulesDirectPluginConfig();
        return configForm;
      }
    }
    #endregion
  }
}
