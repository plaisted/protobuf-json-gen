using Google.Protobuf.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plaisted.ProtobufJsonGen
{
    public class Message
    {
        public MessageDescriptor Descriptor { get; set; }
        private bool hasSpecialNumber;
        private bool hasLongNumber;
        private bool hasTimestamp;
        private bool hasMapped;

        public string MessageName { get { return Descriptor.Name; } }

        public List<string> ReferencedTypes { get; private set; } = new List<string>();

        public Message(Type message)
        {
            Descriptor = (MessageDescriptor)message.GetProperty("Descriptor").GetValue(null, null);
        }
        
        public string Constructor() =>
@"constructor(protoJson?: any) {
    if (protoJson === undefined) {
        return;
    }
    __protogen.Mapper.Construct(protoJson, this, __props_" + Descriptor.Name + @", __maps_" + Descriptor.Name + @");
}

static FromInterface(object: I" + Descriptor.Name + @"): " + Descriptor.Name + $@" {{
    return __protogen.Mapper.FromInterface<{Descriptor.Name}, I{Descriptor.Name}>(object, {Descriptor.Name});
}}

toJSON() {{
    return __protogen.Mapper.ToJSON(this,  __props_{Descriptor.Name}, __maps_{Descriptor.Name});
}}";

        public string GetContents()
        {
            return
$@"export interface I{Descriptor.Name} {{
{GetAllFieldsString().AddTabs(1)}
}}

export class {Descriptor.Name} implements I{Descriptor.Name} {{
{GetAllFieldsString().AddTabs(1)}
{Constructor().AddTabs(1)}

}}
{GetPropertyList()}
{GetSpecialFields()}

";
        }
        public string GetAllEnumsClasses()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var field in Descriptor.Fields.InDeclarationOrder())
            {
                if (field.FieldType == FieldType.Enum)
                {
                    builder.Append(GetEnumClass(field) + Environment.NewLine);
                }
            }
            return builder.ToString();

        }
        public string GetPropertyList()
        {
            return $"const __props_{Descriptor.Name}: string[] = [" + String.Join(", ", Descriptor.Fields.InDeclarationOrder().Select(f => "'" + f.JsonName + "'")) + "];";
        }

        public string GetSpecialFields()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"const __maps_{Descriptor.Name} = {{" + Environment.NewLine);
            var hadFields = false;
            foreach (var field in Descriptor.Fields.InDeclarationOrder())
            {
                string special = null;
                switch (field.FieldType)
                {
                    case FieldType.Double:
                    case FieldType.Float:
                        hasSpecialNumber = true;
                        special = "__protogen.specialNumber";
                        break;
                    case FieldType.Enum:
                        special = GenerateObjectMappers(field, $"{GetEnumClassName(field)}[value]", $"{GetEnumClassName(field)}[value]");
                        break;
                    case FieldType.Fixed64:
                    case FieldType.Int64:
                    case FieldType.UInt64:
                    case FieldType.SFixed64:
                    case FieldType.SInt64:
                        hasLongNumber = true;
                        special = "__protogen.LongNumber";
                        break;
                    case FieldType.Message:
                        if (field.MessageType.FullName.StartsWith("google.protobuf"))
                        {
                            try
                            {
                                GetFieldType(field.MessageType.Fields["value"]); //fine as is
                            }
                            catch (Exception e)
                            {
                                switch (field.MessageType.FullName)
                                {
                                    case "google.protobuf.Timestamp":
                                        hasTimestamp = true;
                                        special = "__protogen.Timestamp";
                                        break;
                                    case "google.protobuf.Duration":
                                        //leave as string
                                        break;
                                    default:
                                        special = field.MessageType.Name;
                                        break;
                                }

                            }
                        }
                        else
                        {
                            special = GenerateObjectMappers(field, $"new {field.MessageType.Name}(value)", "value");
                        }
                        break;
                }
                if (field.IsMap)
                {
                    hasMapped = true;
                    special = $"__protogen.MappedField('{GetFieldType(field.MessageType.Fields["value"])}')";
                }
            
                if (!string.IsNullOrEmpty(special))
                {
                    hadFields = true;
                    builder.Append($"    {field.JsonName}: {special}," + Environment.NewLine);
                }

            }
            builder.Length -= Environment.NewLine.Length;
            if (hadFields)
            {
                builder.Length -= 1;
                builder.Append(Environment.NewLine);
            }

            builder.Append("};" + Environment.NewLine);
            return builder.ToString();
        }

        private string GenerateObjectMappers(FieldDescriptor field, string fromObject, string toObject)
        {
            string special = null;
            if (field.IsRepeated)
            {
                special = RepeatedSpecialMapper(fromObject, toObject);
            }
            else
            {
                special = SpecialMapper(fromObject, toObject);
            }
            return special;
        }

        private string RepeatedSpecialMapper(string fromObject, string toObject) =>
@"{
        FromJSON: (val) => {
            const repeated: any[] = [];
            val.forEach(value => { repeated.push(" + fromObject + @"); });
            return repeated;
        },
        ToJSON: (val) => {
            const repeated: any[] = [];
            val.forEach(value => { repeated.push(" + toObject + @"); });
            return repeated;
        }
    }";

        private string SpecialMapper(string fromObject, string toObject) =>
$@"{{
        FromJSON: (value) => {fromObject},
        ToJSON: (value) => {toObject}
    }}";

        public string GetEnumClass(FieldDescriptor enumField)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("enum " + enumField.JsonName + " {" + Environment.NewLine);
            foreach (var value in enumField.EnumType.Values)
            {
                builder.Append($"    {value.Name} = {value.Number}," + Environment.NewLine);
            }
            builder.Length -= Environment.NewLine.Length + 1;
            builder.Append(Environment.NewLine + "}" + Environment.NewLine);
            return builder.ToString();
        }

        public string GetAllFieldsString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var field in Descriptor.Fields.InDeclarationOrder())
            {
                builder.Append(GetFieldString(field) + Environment.NewLine);
            }
            return builder.ToString();
        }
        private string GetFieldString(FieldDescriptor field)
        {
            var fieldName = field.JsonName;
            //var fieldType = field.MessageType;
            return $"{field.JsonName}{Optional(field)}: {GetFieldType(field)};";
        }

        private string GetFieldType(FieldDescriptor field)
        {
            string fieldType = null;
            switch (field.FieldType)
            {
                case FieldType.Bool:
                    fieldType = "boolean";
                    break;
                case FieldType.String:
                case FieldType.Bytes:
                    fieldType = "string";
                    break;
                case FieldType.Double:
                case FieldType.Float:
                    fieldType = "FloatNumber";
                    break;
                case FieldType.Enum:
                    fieldType = field.EnumType.FullName;
                    ReferencedTypes.Add(field.EnumType.FullName);
                    break;
                case FieldType.Fixed32:
                case FieldType.UInt32:
                case FieldType.Int32:
                case FieldType.SFixed32:
                case FieldType.SInt32:
                    fieldType = "number";
                    break;
                case FieldType.Fixed64:
                case FieldType.Int64:
                case FieldType.UInt64:
                case FieldType.SFixed64:
                case FieldType.SInt64:
                    fieldType = "LongNumber";
                    break;
                case FieldType.Message:
                    if (field.MessageType.FullName.StartsWith("google.protobuf"))
                    {
                        fieldType = GetFieldTypeForWellknownTypes(field);
                    }
                    else if (!field.IsMap)
                    {
                        fieldType = field.MessageType.FullName;
                        ReferencedTypes.Add(field.MessageType.FullName);
                    }
                    break;
                default:
                    //group
                    throw new NotSupportedException($"Field type {field.FieldType} is not supported.");
            }
            if (field.IsRepeated)
            {
                fieldType += "[]";
            }
            if (field.IsMap)
            {
                var mapType = GetFieldType(field.MessageType.Fields["value"]);
                fieldType = $"{{ [key: string]: {mapType} }}";
            }
            return fieldType;
        }

        private string GetFieldTypeForWellknownTypes(FieldDescriptor field)
        {
            switch (field.MessageType.FullName)
            {
                case "google.protobuf.Duration":
                    return "string";
                case "google.protobuf.Timestamp":
                    return "Date";
                default:
                    try
                    {
                        return GetFieldType(field.MessageType.Fields["value"]);
                    }
                    catch (Exception e)
                    {
                        return field.MessageType.FullName;
                    }
            }
        }
        private string GetEnumClassName(FieldDescriptor field)
        {
            return field.EnumType.FullName;
        }

        private string Optional(FieldDescriptor field)
        {
            switch (field.FieldType.ToString())
            {
                case "Message":
                    return "?";
                default:
                    return "";
            }
        }
    }
}
