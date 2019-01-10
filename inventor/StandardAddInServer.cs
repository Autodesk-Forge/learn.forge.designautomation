using Inventor;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Autodesk.Forge.Sample.DesignAutomation.Inventor
{
  /// <summary>
  /// This is the primary AddIn Server class that implements the ApplicationAddInServer interface
  /// that all Inventor AddIns are required to implement. The communication between Inventor and
  /// the AddIn is via the methods on this interface.
  /// </summary>
  [GuidAttribute("84fef6aa-abf5-43be-b176-bb6f0c1d6680")]
  [ComVisible(true)]
  public class StandardAddInServer : ApplicationAddInServer
  {

    // Inventor server object.
    private InventorServer m_server;
    private Commands m_automation;

    public StandardAddInServer()
    {
    }

    #region ApplicationAddInServer Members

    public void Activate(ApplicationAddInSite addInSiteObject, bool firstTime)
    {
      // This method is called by Inventor when it loads the addin.
      // The AddInSiteObject provides access to the Inventor Application object.
      // The FirstTime flag indicates if the addin is loaded for the first time.
      Trace.TraceInformation("Activate IPT Param");
      // Initialize AddIn members.
      m_server = addInSiteObject.InventorServer;
      m_automation = new Commands(m_server);
      // TODO: Add ApplicationAddInServer.Activate implementation.
      // e.g. event initialization, command creation etc.
    }

    public void Deactivate()
    {
      // This method is called by Inventor when the AddIn is unloaded.
      // The AddIn will be unloaded either manually by the user or
      // when the Inventor session is terminated

      // TODO: Add ApplicationAddInServer.Deactivate implementation

      // Release objects.
      Marshal.ReleaseComObject(m_server);
      m_server = null;

      GC.Collect();
      GC.WaitForPendingFinalizers();
    }


    public void ExecuteCommand(int commandID)
    {
      // Note:this method is now obsolete, you should use the 
      // ControlDefinition functionality for implementing commands.
    }

    public object Automation
    {
      // This property is provided to allow the AddIn to expose an API 
      // of its own to other programs. Typically, this  would be done by
      // implementing the AddIn's API interface in a class and returning 
      // that class object through this property.

      get
      {
        // TODO: Add ApplicationAddInServer.Automation getter implementation
        return m_automation;
      }
    }

    #endregion

  }
}
