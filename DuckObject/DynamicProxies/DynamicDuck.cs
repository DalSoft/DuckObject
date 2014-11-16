using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Dynamic;

//Thanks and credit to http://weblogs.asp.net/britchie/archive/2010/08/03/dynamicduck-duck-typing-in-a-dynamic-world.aspx
namespace DalSoft.Dynamic.DynamicProxies
{
    /// <summary>
    /// Extension methods for duck typing and dynamic casts.
    /// </summary>
    internal static class DynamicDuck
    {
        static MethodInfo GetPropertyMethod = typeof(DynamicProxyBase).GetMethod("GetProperty");
        static MethodInfo SetPropertyMethod = typeof(DynamicProxyBase).GetMethod("SetProperty");
        static MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.SpecialName;

        /// <summary>
        /// Module builder singleton.
        /// </summary>
        private static ModuleBuilder s_moduleBuilder;

        /// <summary>
        /// Creates a wrapper object of type <typeparamref name="T">T</typeparamref> around the specified <paramref name="target">target</paramref> object.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        /// <param name="target">Object to be wrapped.</param>
        /// <returns>Wrapper around the specified object.</returns>
        /// <exception cref="InvalidOperationException">Occurs when the specified target type is not an interface.</exception>
        /// <remarks>
        /// This method allows a form of duck typing where interfaces are used to specify a late-bound contract morphed over an existing object.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "By design.")]
        public static T AsIf<T>(IDynamicMetaObjectProvider target) where T : class
        {
            return (T)AsIf(target, typeof (T));
        }

        public static object AsIf(IDynamicMetaObjectProvider target, Type type)
        {
            //
            // Get the thunk type.
            //
            var targetType = type;
            string thunkTypeName = "<>__Thunks." + targetType.FullName;

            //
            // Don't regenerate the thunk type if it already exists for the specified target type.
            //
            Type thunkType = ModuleBuilder.GetType(thunkTypeName, false, false);
            if (thunkType == null)
            {
                thunkType = BuildThunkType(targetType, thunkTypeName);
            }

            //
            // Create and return the thunk instance.
            //
            return Activator.CreateInstance(thunkType, target);
        }

        /// <summary>
        /// Builds a thunk type definition with the specified <paramref name="thunkTypeName">name</paramref> for the specified <paramref name="targetType">target type</paramref>.
        /// </summary>
        /// <param name="targetType">Target type to create a thunk type definition for.</param>
        /// <param name="thunkTypeName">Name to be used for the created thunk type definition.</param>
        /// <returns>Thunk type definition for the specified <paramref name="targetType">target type</paramref>.</returns>
        private static Type BuildThunkType(Type targetType, string thunkTypeName)
        {
            TypeBuilder typeBuilder = GetTypeBuilder(thunkTypeName);

            //
            // Set the parent type to Dynamic.
            //
            typeBuilder.SetParent(typeof(DynamicProxyBase));

            //
            // Implement constructor for thunked object.
            //
            ImplementConstructor(typeBuilder);

            //
            // Implement all interfaces.
            //
            foreach (Type interfaceType in GetInterfaces(targetType))
            {
                ImplementInterface(interfaceType, typeBuilder);
            }

            return typeBuilder.CreateType();
        }

        /// <summary>
        /// Implements the constructor for a thunk type definition.
        /// </summary>
        /// <param name="typeBuilder">Type builder to emit to.</param>
        private static void ImplementConstructor(TypeBuilder typeBuilder)
        {
            //
            // public <class>(object @object) : base(@object)
            // {
            // }
            //
            ConstructorBuilder ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard, new Type[] { typeof(object) });
            ILGenerator ctorILGen = ctorBuilder.GetILGenerator();
            ctorILGen.Emit(OpCodes.Ldarg_0);
            ctorILGen.Emit(OpCodes.Ldarg_1);
            ctorILGen.Emit(OpCodes.Call, typeof(DynamicProxyBase).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(object) }, null));
            ctorILGen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Implements the specified <paramref name="interfaceType">interface type</paramref>.
        /// </summary>
        /// <param name="interfaceType">Interface type to implement.</param>
        /// <param name="typeBuilder">Type builder to emit to.</param>
        /// <param name="siteCounter">Global counter for site fields used in the thunk type being generated.</param>
        private static void ImplementInterface(Type interfaceType, TypeBuilder typeBuilder)
        {
            //
            // Add implements clause.
            //
            typeBuilder.AddInterfaceImplementation(interfaceType);

            //
            // Implement all properties.
            //
            foreach (MemberInfo member in interfaceType.GetMembers())
            {
                if (member.MemberType == MemberTypes.Property)
                {
                    var propertyInfo = member as PropertyInfo;
                    Type returnType = propertyInfo.PropertyType;
                    Type[] parameterTypes = propertyInfo.GetIndexParameters().Select(parameter => parameter.ParameterType).ToArray();
                    PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyInfo.Name, PropertyAttributes.None, returnType, parameterTypes);
                    if (propertyInfo.CanRead)
                        AddGetPropertyMethod(propertyBuilder, typeBuilder, propertyInfo.Name);
                    if (propertyInfo.CanWrite)
                        AddSetPropertyMethod(propertyBuilder, typeBuilder, propertyInfo.Name);
                }
            }
        }

        private static void AddGetPropertyMethod(PropertyBuilder propertyBuilder, TypeBuilder typeBuilder, string methodName)
        {
            const string GETPREFIX = "get_";
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(GETPREFIX + methodName, attributes, CallingConventions.HasThis, propertyBuilder.PropertyType, null);

            ILGenerator methodILGen = methodBuilder.GetILGenerator();
            methodILGen.DeclareLocal(propertyBuilder.PropertyType);
            methodILGen.Emit(OpCodes.Ldarg_0);
            methodILGen.Emit(OpCodes.Ldstr, methodName);
            methodILGen.Emit(OpCodes.Call, GetPropertyMethod);
            methodILGen.Emit(OpCodes.Unbox_Any, propertyBuilder.PropertyType);
            methodILGen.Emit(OpCodes.Stloc_0);
            methodILGen.Emit(OpCodes.Ldloc_0);
            methodILGen.Emit(OpCodes.Ret);
            propertyBuilder.SetGetMethod(methodBuilder);
        }

        private static void AddSetPropertyMethod(PropertyBuilder propertyBuilder, TypeBuilder typeBuilder, string methodName)
        {
            const string SETPREFIX = "set_";
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(SETPREFIX + methodName, attributes, CallingConventions.HasThis, typeof(void), new Type[] { propertyBuilder.PropertyType });

            ILGenerator methodILGen = methodBuilder.GetILGenerator();
            methodILGen.Emit(OpCodes.Ldarg_0);
            methodILGen.Emit(OpCodes.Ldstr, methodName);
            methodILGen.Emit(OpCodes.Ldarg_1);
            methodILGen.Emit(OpCodes.Call, SetPropertyMethod);
            methodILGen.Emit(OpCodes.Ret);
            propertyBuilder.SetSetMethod(methodBuilder);
        }

        /// <summary>
        /// Gets the closure of all interfaces types implemented by the specified <paramref name="interfaceType">interface type</paramref>.
        /// </summary>
        /// <param name="interfaceType">Interface type to calculate the closure of implemented interface types for.</param>
        /// <returns>Closure of implemented interface types.</returns>
        /// <remarks>No particular order is guaranteed.</remarks>
        /// <example>
        ///    interface IBar {}
        ///    interface IFoo1 : IBar {}
        ///    interface IFoo2 : IBar {}
        ///    interface ISample : IFoo1, IFoo2 {}
        /// |-
        ///    CollectionAssert.AreEquivalent(GetInterfaces(typeof(ISample)), new Type[] { typeof(ISample), typeof(IFoo1), typeof(IFoo2), typeof(IBar) })
        /// </example>
        private static Type[] GetInterfaces(Type interfaceType)
        {
            HashSet<Type> interfaces = new HashSet<Type>();

            //
            // Call helper function to find closure of all interfaces to implement.
            //
            GetInterfacesInternal(interfaces, interfaceType);

            return interfaces.ToArray();
        }

        /// <summary>
        /// Helper method to calculate the closure of implemented interfaces for the specified <paramref name="interfaceType">interface type</paramref> recursively.
        /// </summary>
        /// <param name="interfaces">Collected set of interface types.</param>
        /// <param name="interfaceType">Interface type to find all implemented interfaces for recursively.</param>
        private static void GetInterfacesInternal(HashSet<Type> interfaces, Type interfaceType)
        {
            //
            // Avoid duplication.
            //
            if (!interfaces.Contains(interfaceType))
            {
                interfaces.Add(interfaceType);

                //
                // Recursive search.
                //
                foreach (Type subInterfaceType in interfaceType.GetInterfaces())
                {
                    GetInterfacesInternal(interfaces, subInterfaceType);
                }
            }
        }

        /// <summary>
        /// Gets a type builder for a type with the specified <paramref name="thunkTypeName">type name</paramref>.
        /// </summary>
        /// <param name="thunkTypeName">Name of the type to create a type builder for.</param>
        /// <returns>Type builder for the specified <paramref name="thunkTypeName">name</paramref>.</returns>
        private static TypeBuilder GetTypeBuilder(string thunkTypeName)
        {
            return ModuleBuilder.DefineType(thunkTypeName, TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
        }

        /// <summary>
        /// Ensures the module builder singleton is available.
        /// </summary>
        private static ModuleBuilder ModuleBuilder
        {
            get
            {
                if (s_moduleBuilder == null)
                {
                    AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DuckTaperGen"), AssemblyBuilderAccess.Run);
                    s_moduleBuilder = assemblyBuilder.DefineDynamicModule("Thunks");
                }
                return s_moduleBuilder;
            }
        }   
    }
}
