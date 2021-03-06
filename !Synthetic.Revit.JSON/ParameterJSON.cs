﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.DesignScript.Runtime;

using Autodesk.Revit.DB;
using RevitParam = Autodesk.Revit.DB.Parameter;
using RevitElem = Autodesk.Revit.DB.Element;
using revitDoc = Autodesk.Revit.DB.Document;

using Newtonsoft.Json;

namespace Synthetic.Revit.JSON
{
    public class ParameterJSON
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string StorageType { get; set; }
        public int Id { get; set; }
        public string GUID { get; set; }
        public bool IsShared { get; set; }
        public bool IsReadOnly { get; set; }


        [JsonConstructor]
        public ParameterJSON(string Name, string Value, string StorageType, int Id, string GUID, bool IsShared, bool IsReadOnly)
        {
            this.Name = Name;
            this.Value = Value;
            this.StorageType = StorageType;
            this.Id = Id;
            this.GUID = GUID;
            this.IsShared = IsShared;
            this.IsReadOnly = IsReadOnly;
        }

        public ParameterJSON(RevitParam param,
            [DefaultArgument("Synthetic.Revit.Document.Current()")] revitDoc Document)
        {
            this.Name = param.Definition.Name;
            this.StorageType = param.StorageType.ToString();
            switch (this.StorageType)
            {
                case "Double":
                    this.Value = param.AsDouble().ToString();
                    break;
                case "ElementId":
                    ElementIdJSON id = new ElementIdJSON(param.AsElementId(), Document);
                    this.Value = ElementIdJSON.ToJSON(id);
                    break;
                case "Integer":
                    this.Value = param.AsInteger().ToString();
                    break;
                case "String":
                default:
                    this.Value = param.AsString();
                    break;
            }

            this.Id = param.Id.IntegerValue;

            if (IsShared)
            {
                this.GUID = param.GUID.ToString();
            }
            else
            {
                this.GUID = "";
            }

            this.IsShared = param.IsShared;
            this.IsReadOnly = param.IsReadOnly;

        }

        public static string ToJSON(ParameterJSON parameter)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(parameter, Formatting.Indented);
        }

        public static RevitElem ModifyParameter(ParameterJSON paramJSON, RevitElem Elem)
        {
            RevitParam param = null;

            if (paramJSON.IsShared)
            {
                param = Elem.get_Parameter(new Guid(paramJSON.GUID));
            }
            else if (paramJSON.Id < 0)
            {
                param = Elem.get_Parameter((BuiltInParameter)paramJSON.Id);
            }
            else if (paramJSON.Id > 0)
            {
                revitDoc doc = Elem.Document;
                ParameterElement paramElem = (ParameterElement)doc.GetElement(new ElementId(paramJSON.Id));
                if (paramElem != null)
                {
                    Definition def = paramElem.GetDefinition();
                    param = Elem.get_Parameter(def);
                }
            }

            if (param != null && !param.IsReadOnly)
            {
                switch (paramJSON.StorageType)
                {
                    case "Double":
                        param.Set(Convert.ToDouble(paramJSON.Value));
                        break;
                    case "ElementId":
                        param.Set(new ElementId(Convert.ToInt32(paramJSON.Value)));
                        break;
                    case "Integer":
                        param.Set(Convert.ToInt32(paramJSON.Value));
                        break;
                    case "String":
                    default:
                        param.Set(paramJSON.Value);
                        break;
                }
            }

            return Elem;
        }
    }
}
