﻿using CommonExtensionLib.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using XmlDocumentParser;
using XmlDocumentParser.CsXmlDocument;
using XMLDocumentToHtmlCUI.Crypto;

namespace XMLDocumentToHtmlCUI
{
    public class CsXmlToHtmlWriter
    {

        private readonly Element root;

        public string BaseTemplatePath { get; set; } = "BaseTemplate/BaseTemplate.html";
        public string BaseMethodTemplate { get; set; } = "BaseTemplate/BaseMethodTemplate.html";
        public string BasePropertyTemplate { get; set; } = "BaseTemplate/BasePropertyTemplate.html";
        public string ParameterTableTemplate { get; set; } = "BaseTemplate/BaseParameterTemplate.html";

        public CsXmlToHtmlWriter(Element root)
        {
            this.root = root;
        }

        public void WriteToDisk()
        {
            var menu = CreateMenu(root);
            CreateDirectory(root);
            CreateClassFile(root, menu);
        }

        private void CreateDirectory(Element element, string suffix = "")
        {
            if (element != null)
            {
                if (element.Namespaces != null)
                {
                    var name = suffix + element.Name;
                    var di = new DirectoryInfo(name);
                    if (!di.Exists)
                        di.Create();
                    foreach (var elem in element.Namespaces)
                        CreateDirectory(elem, name + "/");
                }
            }
        }

        private void CreateClassFile(Element element, string menu, string suffix = "")
        {
            if (element != null)
            {
                if (element.Namespaces != null)
                {
                    var name = suffix + element.Name;
                    foreach (var elem in element.Namespaces)
                        CreateClassFile(elem, menu, name + "/");
                }
                else
                {
                    var name = suffix + element.Name + ".html";
                    using (var fs = new FileStream(name, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        WriteHtml(fs, element.Members, element, menu);
                    }
                }
            }
        }

        private string CreateMenu(Element element, string suffix = "", string link = "")
        {
            if (element.Type == ElementType.Root)
            {
                var sb2 = new StringBuilder();
                sb2.AppendLine("<ul>");
                foreach (var elem in element.Namespaces)
                    sb2.Append(CreateMenu(elem, "    "));
                sb2.AppendLine("</ul>");
                return sb2.ToString();
            }

            if (element == null)
                return string.Empty;

            var sb = new StringBuilder();
            if (element.Namespaces != null)
            {
                var name = suffix + "<li>" + element.Name;
                sb.AppendLine(name);
                sb.AppendLine(suffix + "    <ul>");
                foreach (var elem in element.Namespaces)
                    sb.Append(CreateMenu(elem, suffix + "        ", link + "../"));
                sb.AppendLine(suffix + "    </ul>");
                sb.AppendLine(suffix + "</li>");
            }
            else
            {
                var namespacePath = element.Namespace.ToString().Replace(".", "/");
                var name = "{0}<li><a href=\"{1}{2}/{3}.html\">{3}</a></li>".FormatString(suffix, link, namespacePath, element.Name); //suffix + "<li><a href=\"#\">" + element.Name + "</a></li>";
                sb.AppendLine(name);
            }
            return sb.ToString();
        }

        private void WriteHtml(Stream stream, List<Member> members, Element parent, string menu)
        {
            var loader = new Template.TemplateLoader(BaseTemplatePath);
            loader.Assign("ClassName", "{0} {1}".FormatString(parent.Name, parent.Type.ToString()));
            loader.Assign("Title", "{0} {1}".FormatString(parent.Name, parent.Type.ToString()));
            loader.Assign("Namespace", parent.Namespace.ToString());
            loader.Assign("Menu", menu, true);

            var methods = new StringBuilder();
            var properties = new StringBuilder();
            foreach (var member in members)
            {
                if (member.Type == MethodType.Method)
                {
                    var parametersStr = ResolveMethodParameter(member);
                    var paramStr = ResolveParameterTable(member, ParameterTableTemplate);

                    var methodLoader = new Template.TemplateLoader(BaseMethodTemplate);
                    methodLoader.Assign("MethodHash", Sha256.GetSha256(member.Name + parametersStr));
                    methodLoader.Assign("MethodName", member.Name);
                    methodLoader.Assign("MethodParameters", parametersStr);
                    methodLoader.Assign("MethodComment", member.Value);
                    if (!string.IsNullOrEmpty(paramStr))
                        methodLoader.Assign("Parameters", paramStr, true);
                    else
                        methodLoader.Assign("ParameterStyle", "display:none");
                    methods.Append(methodLoader.ToString());
                }
                else if (member.Type == MethodType.Property)
                {
                    var propertyLoader = new Template.TemplateLoader(BasePropertyTemplate);
                    propertyLoader.Assign("PropertyHash", Sha256.GetSha256(member.Name));
                    propertyLoader.Assign("PropertyName", member.Name);
                    propertyLoader.Assign("PropertyComment", member.Value);
                    properties.Append(propertyLoader.ToString());
                }
            }

            loader.Assign("MethodItems", methods.ToString(), true);
            loader.Assign("PropertyItems", properties.ToString(), true);
            var template = loader.ToString();
            var templateBytes = Encoding.UTF8.GetBytes(template);
            stream.Write(templateBytes, 0, templateBytes.Length);
        }

        private static string ResolveMethodParameter(Member member)
        {
            var parameters = member.MethodParameters.Zip(member.Parameters.Keys, (type, name) => new { Type = type, Name = name });
            var parametersStr = "(";
            foreach (var param in parameters.Select((v, i) => new { Index = i, Value = v }))
            {
                if (param.Index < member.Parameters.Count - 1)
                    parametersStr += "{0} {1}, ".FormatString(ResolveType(param.Value.Type), param.Value.Name);
                else
                    parametersStr += "{0} {1}".FormatString(ResolveType(param.Value.Type), param.Value.Name);
            }
            parametersStr += ")";

            return parametersStr;
        }

        private static string ResolveParameterTable(Member member, string templatePath)
        {
            var paramStr = "";
            var parameterLoader = new Template.TemplateLoader(templatePath);
            var p1 = member.MethodParameters.Zip(member.Parameters.Keys, (type, name) => new { Type = type, Name = name });
            var p2 = member.Parameters.Values.Zip(p1, (comment, parameter) => new { Comment = comment, Parameter = parameter });
            foreach (var parameter in p2)
            {
                parameterLoader.Assign("Type", ResolveType(parameter.Parameter.Type));
                parameterLoader.Assign("TypeName", parameter.Parameter.Name);
                parameterLoader.Assign("TypeComment", parameter.Comment);
                paramStr += parameterLoader.ToString();
                parameterLoader.Reset();
            }
            return paramStr;
        }

        private static string ResolveType(string text)
        {
            return text.Replace("{", "&lt;").Replace("}", "&gt;");
        }

        private void ShowElements(Element element, string suffix = "")
        {
            if (element != null)
            {
                if (element.Namespaces != null)
                {
                    var name = suffix + "> " + element.Name;
                    Console.WriteLine(name);
                    foreach (var elem in element.Namespaces)
                        ShowElements(elem, suffix + " ");
                }
                else
                {
                    var name = suffix + "> " + element.Name;
                    Console.WriteLine(name);
                }

                if (element.Members != null)
                {
                    foreach (var elem in element.Members)
                    {
                        string parametersStr = "";
                        if (elem.Type == MethodType.Method)
                        {
                            parametersStr += "(";
                            var parameters = elem.MethodParameters.Zip(elem.Parameters.Keys, (type, name) => new { Type = type, Name = name });
                            foreach (var tuple in parameters.Select((v, i) => new { Index = i, Value = v }))
                            {
                                if (tuple.Index < elem.Parameters.Count - 1)
                                    parametersStr += "{0} {1}, ".FormatString(tuple.Value.Type, tuple.Value.Name);
                                else
                                    parametersStr += "{0} {1})".FormatString(tuple.Value.Type, tuple.Value.Name);

                            }
                        }
                        Console.WriteLine("{0} > {1}: {2}{3}", suffix, elem.Type, elem.Name, parametersStr);
                    }
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {

			//var loader = new Template.TemplateLoader("base_template.html");
			//loader.Assign("title", "test");
   //         loader.Assign("body", "test");
			//Console.WriteLine(loader);

            var parser = new CsXmlDocumentParser("base.xml");
			var root = parser.TreeElement;

            var converter = new CsXmlToHtmlWriter(root);
            converter.WriteToDisk();
            //         foreach (var member in root.Namespaces)
            //             ShowElements(member);

            //Console.ReadLine();
        }
    }
}
