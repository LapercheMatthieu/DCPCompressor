﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Ce code a été généré par un outil.
//     Version du runtime :4.0.30319.42000
//
//     Les modifications apportées à ce fichier peuvent provoquer un comportement incorrect et seront perdues si
//     le code est régénéré.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DCPCompressor {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.13.0.0")]
    internal sealed partial class CompressorSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static CompressorSettings defaultInstance = ((CompressorSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new CompressorSettings())));
        
        public static CompressorSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("100000")]
        public int PARALLEL_THRESHOLD {
            get {
                return ((int)(this["PARALLEL_THRESHOLD"]));
            }
            set {
                this["PARALLEL_THRESHOLD"] = value;
            }
        }
    }
}
