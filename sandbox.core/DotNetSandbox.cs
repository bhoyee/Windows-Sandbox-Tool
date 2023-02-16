using System;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting;
using System.Collections.Generic;

using System.Security;
using System.Security.Permissions;

namespace Sandbox.Core.Abstractions
{
    /// <summary>
    /// Sandbox class which will be created in AppDomain.
    /// </summary>
    public class DotNetSandbox : MarshalByRefObject
    {
        /// <summary>
        /// Contains reference to Assembly which will be loaded to sandbox.
        /// </summary>
        private Assembly _sandboxAssembly;

        /// <summary>
        /// Contains reference to Assembly's entry point method.
        /// </summary>
        private MethodInfo _sandboxAssemblyEntry;

        /// <summary>
        /// Contains mappings for Permission type, with it's enum argument (in constructor).
        /// This is needed for Permission checkbox tree generation.
        /// </summary>
        private Dictionary<Type, Type> _providedPermissions = new Dictionary<Type, Type>()
        {
            [typeof(SecurityPermission)]                = typeof(SecurityPermissionFlag),
            [typeof(FileIOPermission)]                  = typeof(PermissionState),
            [typeof(WebPermission)]                     = typeof(PermissionState),
            [typeof(ReflectionPermission)]              = typeof(ReflectionPermissionFlag),
            [typeof(RegistryPermission)]                = typeof(PermissionState),
            [typeof(GacIdentityPermission)]             = typeof(PermissionState),
            [typeof(EnvironmentPermission)]             = typeof(PermissionState),
            [typeof(IsolatedStorageFilePermission)]     = typeof(PermissionState),
            [typeof(KeyContainerPermission)]            = typeof(KeyContainerPermissionFlags),
            [typeof(PrincipalPermission)]               = typeof(PermissionState),
            [typeof(SiteIdentityPermission)]            = typeof(PermissionState),
            [typeof(PublisherIdentityPermission)]       = typeof(PermissionState),
            [typeof(StorePermission)]                   = typeof(StorePermissionFlags),
            [typeof(StrongNameIdentityPermission)]      = typeof(PermissionState),
            [typeof(TypeDescriptorPermission)]          = typeof(TypeDescriptorPermissionFlags),
            [typeof(UIPermission)]                      = typeof(PermissionState),
            [typeof(UrlIdentityPermission)]             = typeof(PermissionState),
            [typeof(ZoneIdentityPermission)]            = typeof(SecurityZone),
        };

        /// <summary>
        /// Returns Assembly's entry point.
        /// </summary>
        public MethodInfo SandboxAssemblyEntry
        {
            get => this._sandboxAssemblyEntry;
        }

        /// <summary>
        /// Returns current provided permissions.
        /// </summary>
        public Dictionary<Type, Type> ProvidedPermissions
        {
            get => this._providedPermissions;
        }

        /// <summary>
        /// Stores current Assembly and it's entry point.
        /// </summary>
        /// <param name="assembly">Loaded Assembly from outside.</param>
        private void SetSandboxAssembly(Assembly assembly)
        {
            this._sandboxAssembly = assembly;
            this._sandboxAssemblyEntry = this._sandboxAssembly.EntryPoint;
        }

        /// <summary>
        /// Invokes entry point with specified arguments.
        /// It is capable to determine if 'string[] args' are necessary or not.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        private void InvokeEntryPoint(string[] args)
        {
            var count = this._sandboxAssemblyEntry.GetParameters().Length;
            this._sandboxAssemblyEntry.Invoke(null, 
                count == 0 ? null : new object[] { args });
        }

        /// <summary>
        /// Executes untrusted Assembly.
        /// </summary>
        /// <param name="assembly">Loaded Assembly instance to be executed.</param>
        /// <param name="args">Command line arguments which will be passed to entry point.</param>
        public void ExecuteUntrusted(Assembly assembly, string[] args)
        {
            try
            {
                this.SetSandboxAssembly(assembly);
                this.InvokeEntryPoint(args);
            }
            catch
            {
                // these two lines are needed for SecurityException,
                // to make it more informative
                new PermissionSet(PermissionState.Unrestricted).Assert();
                CodeAccessPermission.RevertAssert();
                // then rethrow current exception
                throw;
            }
        }

        /// <summary>
        /// Creates ObjectHandle to DotNetSandbox objec in another AppDomain.
        /// </summary>
        /// <param name="domain">Domain where sandbox instance will be created.</param>
        /// <returns>ObjectHandle to created instance.</returns>
        public static ObjectHandle CreateHandle(AppDomain domain)
        {
            var fullName = typeof(DotNetSandbox).FullName;
            var fullyQualifiedName = typeof(DotNetSandbox).Assembly
                .ManifestModule.FullyQualifiedName;
            return Activator.CreateInstanceFrom(
                domain, fullyQualifiedName, fullName);
        }

        /// <summary>
        /// Loads Assembly and checks for it's entry point.
        /// </summary>
        /// <param name="assemblyPath">Path to Assembly to be resolved.</param>
        /// <returns>Loaded Assembly from path.</returns>
        /// <exception cref="ArgumentNullException">Thrown when Assembly.EntryPoint is null.</exception>
        public static Assembly ResolveAssemblyPath(string assemblyPath)
        {
            var assembly = Assembly.LoadFile(assemblyPath);
            if (assembly.EntryPoint == null)
                throw new ArgumentNullException("There are no entry point!\n" +
                    "It seems that this assembly is class library.");
            return assembly;
        }
    }
}
