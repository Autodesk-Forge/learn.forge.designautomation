#region Copyright
//      .NET Sample
//
//      Copyright (c) 2019 by Autodesk, Inc.
//
//      Permission to use, copy, modify, and distribute this software
//      for any purpose and without fee is hereby granted, provided
//      that the above copyright notice appears in all copies and
//      that both that copyright notice and the limited warranty and
//      restricted rights notice below appear in all supporting
//      documentation.
//
//      AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
//      AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
//      MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
//      DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
//      UNINTERRUPTED OR ERROR FREE.
//
//      Use, duplication, or disclosure by the U.S. Government is subject to
//      restrictions set forth in FAR 52.227-19 (Commercial Computer
//      Software - Restricted Rights) and DFAR 252.227-7013(c)(1)(ii)
//      (Rights in Technical Data and Computer Software), as applicable.
//
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Newtonsoft.Json;

using Autodesk.Max;

namespace Autodesk.Forge.Sample.DesignAutomation.Max
{
    /// <summary>
    /// Used to hold the parameters to change
    /// </summary>
    public class InputParams
    {
        public float Width { get; set; }
        public float Height { get; set; }
    }
    /// <summary>
    /// Changes parameters in automted way. 
    /// Iterate entire scene to get all nodes
    /// In this example we specifically find Casement Windows wby object class ID
    /// Then modify the width and height based on inputs.
    /// 
    /// Could be expanded to find other window types, other objects, etc.
    /// </summary>
    static public class ParameterChanger
    {
        static List<IINode> m_sceneNodes = new List<IINode> { };

        /// <summary>
        /// Recursively go through the scene and get all nodes
        /// Use the Autodesk.Max APIs to get the children nodes
        /// </summary>
        static private void GetSceneNodes(IINode node)
        {
            m_sceneNodes.Add(node);

            for (int i = 0; i < node.NumberOfChildren; i++)
                GetSceneNodes(node.GetChildNode(i));
        }

        /// <summary>
        /// Function to specifically update Case Windows with input wedth and height parameters
        /// </summary>
        /// <param name="width">The new Width to set the Window</param>
        /// <param name="height">The new Height to set the Window</param>
        /// <returns></returns>
        static public int UpdateWindowNodes(float width, float height)
        {
            IGlobal globalInterface = Autodesk.Max.GlobalInterface.Instance;
            IInterface14 coreInterface = globalInterface.COREInterface14;

            IINode nodeRoot = coreInterface.RootNode;
            m_sceneNodes.Clear();
            GetSceneNodes(nodeRoot);

            // 3ds Max uses a class ID for all object types. This is easiest way to find specific type.
            // ClassID (1902665597L, 1593788199L) == 0x71685F7D, 0x5EFF4727 for casement window
            IClass_ID cidCasementWindow = globalInterface.Class_ID.Create(0x71685F7D, 0x5EFF4727);

            // Use LINQ to filter for windows only - in case scene has more than one, 
            // but this should still give us at least one for single window scene!
            var sceneWindows = from node in m_sceneNodes
                               where ((node.ObjectRef != null) && // In some cases the ObjectRef can be null for certain node types.
                                      (node.ObjectRef.ClassID.PartA == cidCasementWindow.PartA) && 
                                      (node.ObjectRef.ClassID.PartB == cidCasementWindow.PartB))
                               select node;

            // Iterate the casement windws and update the hight and width parameters.
            foreach (IINode item in sceneWindows)
            {
                // window is using old-style ParamArray rather than newer ParamBlk2
                IIParamArray pb = item.ObjectRef.ParamBlock;
                pb.SetValue(0, coreInterface.Time, height); // window height is at index zero.
                pb.SetValue(1, coreInterface.Time, width); // window width is at index one.
            }

            // If there are windows, save the window updates
            bool status;
            if (sceneWindows.Count() > 0)
            {
                status = coreInterface.FileSave;
                if (status == false)
                    return -1;
            }

            // This inidcates how many windows were modified.
            return sceneWindows.Count();
        }

    }

    /// <summary>
    /// This class is used to execute the automation. Above class could be connected to UI elements, or run by scripts directly.
    /// This class takes the iput from JSON input and uses those values. This way it is more cohesive to web development.
    /// </summary>
    static public class RuntimeExecute
    {
        static public int ModifyWindowWidthHeight()
        {
            int count = 0;

            // Run entire code block with try/catch to help determine errors
            try
            {

                // read input parameters from JSON file
                InputParams inputParams = JsonConvert.DeserializeObject<InputParams>(File.ReadAllText("params.json"));

                // Uncomment following to use if you want to test without JSON input.
                //InputParams inputParams = new InputParams();
                //inputParams.Height = 200.0f;
                //inputParams.Width = 150.0f;

                count = ParameterChanger.UpdateWindowNodes(inputParams.Width, inputParams.Height);

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Write(e.Message);
                return -1; //fail
            }



            return count; // 0+ means success, and how many objects were changed.
        }


        // Not used by this code, but is useful when exploring the APIs
        /// <summary>
        /// Input an obj to reflect
        /// </summary>
        /// <param name="obj"> Input object to reflect. </param>
        /// <returns> 1 </returns>
        static public int ReflectAPI(object obj)
        {
            try
            {


                System.Reflection.PropertyInfo[] propertyInfo = obj.GetType().GetProperties();
                string strOutput;

                strOutput = "\n The object is of type: " + obj.GetType().ToString() + " has the following reflected property info: ";
                for (int i = 0; i < propertyInfo.Length; i++)
                {
                    string name = propertyInfo[i].Name;
                    string s;
                    try
                    {
                        s = obj.GetType().InvokeMember(name, System.Reflection.BindingFlags.GetProperty, null, obj, null).ToString();
                        strOutput += "\n    " + name + ":  " + s + "  IsSpecialName == " + propertyInfo[i].IsSpecialName.ToString();
                    }
                    catch (System.Exception invokeMemberException)
                    {
                        strOutput += "\n" + name + ": EXCEPTION: " + invokeMemberException.Message;
                    }
                }
                System.Diagnostics.Debug.Write(strOutput);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Write(e.Message);
            }

            return 1;
        }
    }
}