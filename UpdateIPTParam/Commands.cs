using Inventor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Autodesk.Forge.Sample.DesignAutomation.Inventor
{
  [ComVisible(true)]
  public class Commands
  {
    private InventorServer m_server;
    public Commands(InventorServer app)
    {
      m_server = app;
    }

    public void Run(Document doc)
    {
      LogTrace("Run called with {0}", doc.DisplayName);
      RunWithArguments(doc);
    }

    private class HeartBeat : IDisposable
    {
      // default is 50s
      public HeartBeat(int intervalMillisec = 50000)
      {
        t = new Thread(() =>
        {

          LogTrace("HeartBeating every {0}ms.", intervalMillisec);

          for (; ; )
          {
            Thread.Sleep((int)intervalMillisec);
            LogTrace("HeartBeat {0}.", (long)(new TimeSpan(DateTime.Now.Ticks - ticks).TotalSeconds));
          }

        });

        ticks = DateTime.Now.Ticks;
        t.Start();
      }

      public void Dispose()
      {
        Dispose(true);
        GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool disposing)
      {
        if (disposing)
        {
          if (t != null)
          {
            LogTrace("Ending HeartBeat");
            t.Abort();
            t = null;
          }
        }
      }

      private Thread t;
      private long ticks;
    }

    public void RunWithArguments(Document doc)
    {
      try
      {
        // load processing parameters
        string paramsJson = System.IO.File.ReadAllText("params.json");

        // update parameters in the doc
        // start HeartBeat around ChangeParameters, it could be a long operation
        using (new HeartBeat())
        {
          ChangeParameters(doc, paramsJson);
        }

        // generate outputs
        var docDir = System.IO.Path.GetDirectoryName(doc.FullFileName);

        var documentType = doc.DocumentType;
        if (documentType == DocumentTypeEnum.kPartDocumentObject)
        {
          var fileName = System.IO.Path.Combine(docDir, "outputFile.ipt"); // the name must be in sync with OutputIpt localName in Activity
          // start HeartBeat around Save, it could be a long operation
          using (new HeartBeat())
          {
            doc.SaveAs(fileName, false);
          }
          LogTrace("Saved as {0}", fileName);

        }
      }
      catch (Exception e)
      {
        LogTrace("Processing failed: {0}", e.ToString());
      }
    }

    /// <summary>
    /// Change parameters in Inventor document.
    /// </summary>
    /// <param name="doc">The Inventor document.</param>
    /// <param name="json">JSON with changed parameters.</param>
    public void ChangeParameters(Document doc, string json)
    {
      var theParams = GetParameters(doc);

      Dictionary<string, string> parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
      foreach (KeyValuePair<string, string> entry in parameters)
      {
        var parameterName = entry.Key;
        var newExpression = entry.Value;

        try
        {
          Parameter param = theParams[parameterName.ToLower()];
          LogTrace("Parameter {0} from {1} to {2}", parameterName, param.Expression, newExpression);
          param.Expression = newExpression;
        }
        catch (Exception e)
        {
          LogTrace("Cannot update {0}: {1}", parameterName, e.Message);
        }
      }

      doc.Update();
      doc.Save();
    }
    /// <summary>
    /// Get parameters for the document.
    /// </summary>
    /// <returns>Parameters. Throws exception if parameters are not found.</returns>
    private static Parameters GetParameters(Document doc)
    {
      var docType = doc.DocumentType;
      switch (docType)
      {
        case DocumentTypeEnum.kAssemblyDocumentObject:
          var asm = doc as AssemblyDocument;
          return asm.ComponentDefinition.Parameters;

        case DocumentTypeEnum.kPartDocumentObject:
          var ipt = doc as PartDocument;
          return ipt.ComponentDefinition.Parameters;

        default:
          throw new ApplicationException(string.Format("Unexpected document type ({0})", docType));
      }
    }

    #region Logging utilities

    /// <summary>
    /// Log message with 'trace' log level.
    /// </summary>
    private static void LogTrace(string format, params object[] args)
    {
      Trace.TraceInformation(format, args);
    }

    #endregion
  }
}
