﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by xsd, Version=4.0.30319.33440.
// 
namespace SqlPad.Oracle {
    using System.Xml.Serialization;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://husqvik.com/SqlPad/2014/08/Oracle")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://husqvik.com/SqlPad/2014/08/Oracle", IsNullable=false)]
    public partial class OracleConfiguration {
        
        private string startupScriptField;
        
        private string tKProfPathField;
        
        private OracleConfigurationConnection[] connectionsField;
        
        private OracleConfigurationFormatter formatterField;
        
        /// <remarks/>
        public string StartupScript {
            get {
                return this.startupScriptField;
            }
            set {
                this.startupScriptField = value;
            }
        }
        
        /// <remarks/>
        public string TKProfPath {
            get {
                return this.tKProfPathField;
            }
            set {
                this.tKProfPathField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Connection", IsNullable=false)]
        public OracleConfigurationConnection[] Connections {
            get {
                return this.connectionsField;
            }
            set {
                this.connectionsField = value;
            }
        }
        
        /// <remarks/>
        public OracleConfigurationFormatter Formatter {
            get {
                return this.formatterField;
            }
            set {
                this.formatterField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://husqvik.com/SqlPad/2014/08/Oracle")]
    public partial class OracleConfigurationConnection {
        
        private string startupScriptField;
        
        private OracleConfigurationConnectionExecutionPlan executionPlanField;
        
        private string connectionNameField;
        
        private string remoteTraceDirectoryField;
        
        /// <remarks/>
        public string StartupScript {
            get {
                return this.startupScriptField;
            }
            set {
                this.startupScriptField = value;
            }
        }
        
        /// <remarks/>
        public OracleConfigurationConnectionExecutionPlan ExecutionPlan {
            get {
                return this.executionPlanField;
            }
            set {
                this.executionPlanField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ConnectionName {
            get {
                return this.connectionNameField;
            }
            set {
                this.connectionNameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string RemoteTraceDirectory {
            get {
                return this.remoteTraceDirectoryField;
            }
            set {
                this.remoteTraceDirectoryField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://husqvik.com/SqlPad/2014/08/Oracle")]
    public partial class OracleConfigurationConnectionExecutionPlan {
        
        private TypeSchemaObject targetTableField;
        
        /// <remarks/>
        public TypeSchemaObject TargetTable {
            get {
                return this.targetTableField;
            }
            set {
                this.targetTableField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://husqvik.com/SqlPad/2014/08/Oracle")]
    public partial class TypeSchemaObject {
        
        private string schemaField;
        
        private string nameField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Schema {
            get {
                return this.schemaField;
            }
            set {
                this.schemaField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://husqvik.com/SqlPad/2014/08/Oracle")]
    public partial class OracleConfigurationFormatter {
        
        private OracleConfigurationFormatterFormatOptions formatOptionsField;
        
        /// <remarks/>
        public OracleConfigurationFormatterFormatOptions FormatOptions {
            get {
                return this.formatOptionsField;
            }
            set {
                this.formatOptionsField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://husqvik.com/SqlPad/2014/08/Oracle")]
    public partial class OracleConfigurationFormatterFormatOptions {
        
        private FormatOption identifierField;
        
        private bool identifierFieldSpecified;
        
        private FormatOption aliasField;
        
        private bool aliasFieldSpecified;
        
        private FormatOption keywordField;
        
        private bool keywordFieldSpecified;
        
        private FormatOption reservedWordField;
        
        private bool reservedWordFieldSpecified;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public FormatOption Identifier {
            get {
                return this.identifierField;
            }
            set {
                this.identifierField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool IdentifierSpecified {
            get {
                return this.identifierFieldSpecified;
            }
            set {
                this.identifierFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public FormatOption Alias {
            get {
                return this.aliasField;
            }
            set {
                this.aliasField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool AliasSpecified {
            get {
                return this.aliasFieldSpecified;
            }
            set {
                this.aliasFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public FormatOption Keyword {
            get {
                return this.keywordField;
            }
            set {
                this.keywordField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool KeywordSpecified {
            get {
                return this.keywordFieldSpecified;
            }
            set {
                this.keywordFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public FormatOption ReservedWord {
            get {
                return this.reservedWordField;
            }
            set {
                this.reservedWordField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ReservedWordSpecified {
            get {
                return this.reservedWordFieldSpecified;
            }
            set {
                this.reservedWordFieldSpecified = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://husqvik.com/SqlPad/2014/08/Oracle")]
    public enum FormatOption {
        
        /// <remarks/>
        Keep,
        
        /// <remarks/>
        Upper,
        
        /// <remarks/>
        Lower,
        
        /// <remarks/>
        InitialCapital,
    }
}
