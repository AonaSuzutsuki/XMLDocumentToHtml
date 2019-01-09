﻿using CommonCoreLib.Bool;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlDocumentParser.CsXmlDocument
{
    /// <summary>
    /// It expresses the lowest element such as method and property.
    /// </summary>
    public class Member
    {
        public string Id { get; set; } = string.Empty;
        public Accessibility Accessibility { get; set; } = Accessibility.Public;

        public string Difinition { get; set; } = string.Empty;

        /// <summary>
        /// Method type of member.
        /// </summary>
        public MethodType Type { get; set; }

        /// <summary>
        /// Namespace of member.
        /// </summary>
        public NamespaceItem Namespace { get; set; }

        /// <summary>
        /// Name of member.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Parameter types of member.
        /// </summary>
        public List<string> ParameterTypes { get; set; } = new List<string>();

        public string ReturnType { get; set; } = Constants.SystemVoid;

        /// <summary>
        /// Value of member.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Return comment of member.
        /// </summary>
        public string ReturnComment { get; set; } = string.Empty;

        /// <summary>
        /// Parameter names of member.
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();



        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var member = (Member)obj;
            var boolcollector = new BoolCollector();

            boolcollector.ChangeBool("Accessibility", Accessibility == member.Accessibility);
            boolcollector.ChangeBool("Difinition", Difinition.Equals(member.Difinition));
            boolcollector.ChangeBool("Id", Id.Equals(member.Id));
            boolcollector.ChangeBool("Name", Name.Equals(member.Name));
            boolcollector.ChangeBool("Namespace", Namespace.Equals(member.Namespace));
            boolcollector.ChangeBool("Parameters", Parameters.SequenceEqual(member.Parameters));
            boolcollector.ChangeBool("ParameterTypes", ParameterTypes.SequenceEqual(member.ParameterTypes));
            boolcollector.ChangeBool("ReturnComment", ReturnComment.Equals(member.ReturnComment));
            boolcollector.ChangeBool("ReturnType", ReturnType.Equals(member.ReturnType));
            boolcollector.ChangeBool("Type", Type == member.Type);
            boolcollector.ChangeBool("Value", Value.Equals(Value));

            return boolcollector.Value;
        }
    }
}
