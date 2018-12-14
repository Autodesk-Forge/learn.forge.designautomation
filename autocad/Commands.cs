using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(Autodesk.Forge.Sample.DesignAutomation.AutoCAD.MainEntry))]
[assembly: ExtensionApplication(null)]

namespace Autodesk.Forge.Sample.DesignAutomation.AutoCAD
{
  public class MainEntry
  {
    [CommandMethod("FPDCommands", "UpdateWindowParam", CommandFlags.Modal)]
    public static void UpdateParam()
    {
      //Get active document of drawing with Dynamic block
      var doc = Application.DocumentManager.MdiActiveDocument;
      var db = doc.Database;
      var ed = doc.Editor;
      var pso = new PromptStringOptions("\nEnter dynamic block name:")
      {
        AllowSpaces = true
      };
      PromptResult pr = ed.GetString(pso);
      if (pr.Status != PromptStatus.OK)
        return;
      using (Transaction t = db.TransactionManager.StartTransaction())
      {
        var bt = t.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
        if (!bt.Has(pr.StringResult))
        {
          ed.WriteMessage("\nBlock \"" + pr.StringResult + "\" does not exist.");
          return;
        }
        //get the blockDef and check if is anonymous
        BlockTableRecord btr = (BlockTableRecord)t.GetObject(bt[pr.StringResult], OpenMode.ForRead);
        if (btr.IsDynamicBlock)
        {
          //get all anonymous blocks from this dynamic block
          ObjectIdCollection anonymousIds = btr.GetAnonymousBlockIds();
          ObjectIdCollection dynBlockRefs = new ObjectIdCollection();
          foreach (ObjectId anonymousBtrId in anonymousIds)
          {
            //get the anonymous block
            BlockTableRecord anonymousBtr = (BlockTableRecord)t.GetObject(anonymousBtrId, OpenMode.ForRead);
            //and all references to this block
            ObjectIdCollection blockRefIds = anonymousBtr.GetBlockReferenceIds(true, true);
            foreach (ObjectId id in blockRefIds)
            {
              dynBlockRefs.Add(id);
            }

          }
          if (dynBlockRefs.Count > 0)
          {
            //Get the first dynamic block reference, we have only one Dyanmic Block reference in Drawing
            var dBref = t.GetObject(dynBlockRefs[0], OpenMode.ForWrite) as BlockReference;
            UpdateDynamicProperties(ed, dBref);
          }
        }
        // Committing is cheaper than aborting
        t.Commit();

      }

    }
    /// <summary>
    /// This updates the Dyanmic Blockreference with given Width and Height
    /// The initial parameters of Dynamic Blockrefence, Width =20.00 and Height =40.00
    /// </summary>
    /// <param Editor="ed"></param>
    /// <param BlockReference="br"></param>
    /// <param String="name"></param>

    private static void UpdateDynamicProperties(Editor ed, BlockReference br)
    {
      PromptDoubleResult widthResult = ed.GetDouble("\nEnter Width of Window");
      if (widthResult.Status != PromptStatus.OK) return;
      double width = widthResult.Value;
      PromptDoubleResult heightResult = ed.GetDouble("\nEnter Height of Window");
      if (heightResult.Status != PromptStatus.OK) return;
      double height = heightResult.Value;
      // Only continue is we have a valid dynamic block
      if (br != null && br.IsDynamicBlock)
      {
        // Get the dynamic block's property collection
        DynamicBlockReferencePropertyCollection pc = br.DynamicBlockReferencePropertyCollection;
        foreach (DynamicBlockReferenceProperty prop in pc)
        {
          switch (prop.PropertyName)
          {
            case "Width":
              prop.Value = width;
              break;
            case "Height":
              prop.Value = height;
              break;
            default:
              break;
          }
        }
      }
    }
  }
}


