﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ASCOM.Meade.net.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ASCOM.Meade.net.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Drawing.Bitmap.
        /// </summary>
        internal static System.Drawing.Bitmap ASCOM {
            get {
                object obj = ResourceManager.GetObject("ASCOM", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The {0} was not {1} because you did not allow it..
        /// </summary>
        internal static string Server_ElevateSelf_The__0__was_not__1__because_you_did_not_allow_it_ {
            get {
                return ResourceManager.GetString("Server_ElevateSelf_The__0__was_not__1__because_you_did_not_allow_it_", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to load served COM class assembly {0} - {1}.
        /// </summary>
        internal static string Server_LoadComObjectAssemblies_Failed_to_load_served_COM_class_assembly__0_____1_ {
            get {
                return ResourceManager.GetString("Server_LoadComObjectAssemblies_Failed_to_load_served_COM_class_assembly__0_____1_" +
                        "", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown argument: {0}
        ///Valid are : -register, -unregister and -embedding.
        /// </summary>
        internal static string Server_ProcessArguments_ {
            get {
                return ResourceManager.GetString("Server_ProcessArguments_", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to register class factory for {0}.
        /// </summary>
        internal static string Server_RegisterClassFactories_Failed_to_register_class_factory_for__0_ {
            get {
                return ResourceManager.GetString("Server_RegisterClassFactories_Failed_to_register_class_factory_for__0_", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error while registering the server:
        ///{0}.
        /// </summary>
        internal static string Server_RegisterObjects_ {
            get {
                return ResourceManager.GetString("Server_RegisterObjects_", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} Settings ({1}).
        /// </summary>
        internal static string SetupDialogForm_SetupDialogForm__0__Settings___1__ {
            get {
                return ResourceManager.GetString("SetupDialogForm_SetupDialogForm__0__Settings___1__", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ({0:00.0}% of sidereal rate).
        /// </summary>
        internal static string SetupDialogForm_TextBox1_TextChanged___0_00_0___of_sidereal_rate_ {
            get {
                return ResourceManager.GetString("SetupDialogForm_TextBox1_TextChanged___0_00_0___of_sidereal_rate_", resourceCulture);
            }
        }
    }
}
