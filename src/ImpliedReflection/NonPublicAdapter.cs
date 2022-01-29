using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using AdapterSet = System.Management.Automation.PSObject.AdapterSet;

namespace ImpliedReflection
{
    public partial class NonPublicAdapter : DotNetAdapter
    {
        private const BindingFlags InstanceBindingFlags =
            BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.IgnoreCase | BindingFlags.Instance;

        private const BindingFlags StaticBindingFlags =
            BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.IgnoreCase | BindingFlags.Static;

        private static readonly NonPublicAdapter s_customInstance = new(isStatic: false);

        private static readonly NonPublicAdapter s_customStatic = new(isStatic: true);

        private static readonly HashSet<Type> s_processedTypes = new();

        private static readonly object s_syncObject = new();

        private static DotNetAdapter s_originalInstance;

        private static DotNetAdapter s_originalStatic;

        private static Patches s_patches;

        private static bool s_isActive;

        /// This static is thread safe based on the lock in GetInstancePropertyReflectionTable
        /// <summary>
        /// CLR reflection property cache for instance properties.
        /// </summary>
        private static readonly Dictionary<Type, CacheTable> s_instancePropertyCacheTable = new();

        // This static is thread safe based on the lock in GetStaticPropertyReflectionTable
        /// <summary>
        /// CLR reflection property cache for static properties.
        /// </summary>
        private static readonly Dictionary<Type, CacheTable> s_staticPropertyCacheTable = new();

        // This static is thread safe based on the lock in GetInstanceMethodReflectionTable
        /// <summary>
        /// CLR reflection method cache for instance methods.
        /// </summary>
        private static readonly Dictionary<Type, CacheTable> s_instanceMethodCacheTable = new();

        // This static is thread safe based on the lock in GetStaticMethodReflectionTable
        /// <summary>
        /// CLR reflection method cache for static methods.
        /// </summary>
        private static readonly Dictionary<Type, CacheTable> s_staticMethodCacheTable = new();

        // This static is thread safe based on the lock in GetInstanceMethodReflectionTable
        /// <summary>
        /// CLR reflection method cache for instance events.
        /// </summary>
        private static readonly Dictionary<Type, Dictionary<string, EventCacheEntry>> s_instanceEventCacheTable = new();

        // This static is thread safe based on the lock in GetStaticMethodReflectionTable
        /// <summary>
        /// CLR reflection method cache for static events.
        /// </summary>
        private static readonly Dictionary<Type, Dictionary<string, EventCacheEntry>> s_staticEventCacheTable = new();

        private static Collection<CollectionEntry<PSMemberInfo>> s_originalMemberCollection;

        private static Collection<CollectionEntry<PSMethodInfo>> s_originalMethodCollection;

        private static Collection<CollectionEntry<PSPropertyInfo>> s_originalPropertyCollection;

        private readonly bool _isStatic;

        public NonPublicAdapter(bool isStatic) : base(isStatic)
        {
            _isStatic = isStatic;
        }

        public static void Bind()
        {
            ref bool isActive = ref s_isActive;
            if (isActive)
            {
                return;
            }

            lock (s_syncObject)
            {
                if (isActive)
                {
                    return;
                }

                ref DotNetAdapter instance = ref PSObjectProxy.s_dotNetInstanceAdapter;
                s_originalInstance = instance;
                instance = s_customInstance;

                ref DotNetAdapter staticAdapter = ref PSObjectProxy.s_dotNetStaticAdapter;
                s_originalStatic = staticAdapter;
                staticAdapter = s_customStatic;

                PSObjectProxy.s_dotNetInstanceAdapterSet = new AdapterSet(s_customInstance, null);
                DotNetAdapterProxy.s_instancePropertyCacheTable = s_instancePropertyCacheTable;
                DotNetAdapterProxy.s_staticPropertyCacheTable = s_staticPropertyCacheTable;
                DotNetAdapterProxy.s_instanceMethodCacheTable = s_instanceMethodCacheTable;
                DotNetAdapterProxy.s_staticMethodCacheTable = s_staticMethodCacheTable;
                DotNetAdapterProxy.s_instanceEventCacheTable = s_instanceEventCacheTable;
                DotNetAdapterProxy.s_staticEventCacheTable = s_staticEventCacheTable;

                static void ReplaceClrMembers<T>(Collection<CollectionEntry<T>> entries)
                    where T : PSMemberInfo
                {
                    for (int i = 0; i < entries.Count; i++)
                    {
                        const string ClrMembersCollectionName = "clr members";
                        if (entries[i].CollectionNameForTracing is ClrMembersCollectionName)
                        {
                            entries[i] = CreateCollectionEntry<T>(
                                DotNetGetMembersDelegate<T>,
                                DotNetGetMemberDelegate<T>,
                                DotNetGetFirstMemberOrDefaultDelegate<T>,
                                shouldReplicateWhenReturning: false,
                                shouldCloneWhenReturning: false,
                                collectionNameForTracing: ClrMembersCollectionName);
                            return;
                        }
                    }
                }

                ref var member = ref PSObjectProxy.s_memberCollection;
                ref var method = ref PSObjectProxy.s_methodCollection;
                ref var property = ref PSObjectProxy.s_propertyCollection;
                s_originalMemberCollection = new(member.ToArray());
                s_originalMethodCollection = new(method.ToArray());
                s_originalPropertyCollection = new(property.ToArray());
                ReplaceClrMembers(member);
                ReplaceClrMembers(method);
                ReplaceClrMembers(property);

                PSObjectProxy.s_adapterMapping.Clear();

                PSBinaryOperationBinder.InvalidateCache();
                PSConvertBinder.InvalidateCache();
                PSGetIndexBinder.InvalidateCache();
                PSSetIndexBinder.InvalidateCache();
                PSSetMemberBinder.InvalidateCache();
                PSInvokeMemberBinder.InvalidateCache();
                PSCreateInstanceBinder.InvalidateCache();

                Binders.CreateInstance.s_binderCache.Clear();
                Binders.GetDynamicMember.s_binderCache.Clear();
                Binders.InvokeDynamicMember.s_binderCache.Clear();
                Binders.SetDynamicMember.s_binderCache.Clear();
                lock (Binders.GetMember.s_binderCache)
                {
                    foreach (KeyValuePair<Tuple<string, Type, bool, bool>, PSGetMemberBinder> binder in Binders.GetMember.s_binderCache)
                    {
                        binder.Value._version++;
                    }
                }

                lock (Binders.GetMember.s_binderCacheIgnoringCase)
                {
                    foreach (KeyValuePair<string, List<PSGetMemberBinder>> binders in Binders.GetMember.s_binderCacheIgnoringCase)
                    {
                        foreach (PSGetMemberBinder binder in binders.Value)
                        {
                            binder._version++;
                        }
                    }
                }

                Binders.InvokeMember.s_binderCache.Clear();
                s_patches = Patches.Create();
                isActive = true;
            }
        }

        // Doesn't really work at the moment. I'd make it private to communicate that,
        // but that probably won't help too much in this case.
        public static void Unbind()
        {
            ref bool isActive = ref s_isActive;
            if (!isActive)
            {
                return;
            }

            lock (s_syncObject)
            {
                if (!isActive)
                {
                    return;
                }

                PSObjectProxy.s_dotNetInstanceAdapter = s_originalInstance;
                PSObjectProxy.s_dotNetInstanceAdapterSet = new AdapterSet(s_originalInstance, null);
                s_originalInstance = null;

                PSObjectProxy.s_dotNetStaticAdapter = s_originalStatic;
                s_originalStatic = null;

                PSObjectProxy.s_memberCollection = s_originalMemberCollection;
                PSObjectProxy.s_methodCollection = s_originalMethodCollection;
                PSObjectProxy.s_propertyCollection = s_originalPropertyCollection;
                s_originalMemberCollection = null;
                s_originalMethodCollection = null;
                s_originalPropertyCollection = null;

                PSObjectProxy.s_adapterMapping.Clear();

                PSBinaryOperationBinder.InvalidateCache();
                PSConvertBinder.InvalidateCache();
                PSGetIndexBinder.InvalidateCache();
                PSSetIndexBinder.InvalidateCache();
                PSSetMemberBinder.InvalidateCache();
                PSInvokeMemberBinder.InvalidateCache();
                PSCreateInstanceBinder.InvalidateCache();

                Binders.CreateInstance.s_binderCache.Clear();
                Binders.GetDynamicMember.s_binderCache.Clear();
                Binders.InvokeDynamicMember.s_binderCache.Clear();
                Binders.SetDynamicMember.s_binderCache.Clear();
                lock (Binders.GetMember.s_binderCache)
                {
                    foreach (KeyValuePair<Tuple<string, Type, bool, bool>, PSGetMemberBinder> binder in Binders.GetMember.s_binderCache)
                    {
                        binder.Value._version++;
                    }
                }

                lock (Binders.GetMember.s_binderCacheIgnoringCase)
                {
                    foreach (KeyValuePair<string, List<PSGetMemberBinder>> binders in Binders.GetMember.s_binderCacheIgnoringCase)
                    {
                        foreach (PSGetMemberBinder binder in binders.Value)
                        {
                            binder._version++;
                        }
                    }
                }
                Binders.InvokeMember.s_binderCache.Clear();

                s_patches.Dispose();
                s_patches = null;
                isActive = false;
            }
        }

        private static PSMemberInfoInternalCollection<T> DotNetGetMembersDelegate<T>(PSObject msjObj) where T : PSMemberInfo
        {
            Adapter adapter = msjObj.InternalBaseDotNetAdapter;
            if (adapter is null || adapter.GetType() == typeof(DotNetAdapter))
            {
                adapter = s_customInstance;
            }

            PSMemberInfoInternalCollection<T> retValue = adapter.BaseGetMembers<T>(
                msjObj.ImmediateBaseObject);

            Polyfill.MemberResolution.WriteLine("DotNet members: {0}.", retValue.VisibleCount);
            return retValue;
        }

        private static T DotNetGetMemberDelegate<T>(PSObject msjObj, string name) where T : PSMemberInfo
        {
            Adapter adapter = msjObj.InternalBaseDotNetAdapter;
            if (adapter is null || adapter.GetType() == typeof(DotNetAdapter))
            {
                adapter = s_customInstance;
            }

            T retValue = adapter.BaseGetMember<T>(msjObj.ImmediateBaseObject, name);
            Polyfill.MemberResolution.WriteLine("DotNet member: {0}.", retValue is null ? "not found" : retValue.Name);
            return retValue;
        }

        protected override T GetMember<T>(object obj, string memberName)
        {
            T returnValue = GetDotNetProperty<T>(obj, memberName);
            return returnValue ?? GetDotNetMethod<T>(obj, memberName);
        }

        protected override PSMemberInfoInternalCollection<T> GetMembers<T>(object obj)
        {
            PSMemberInfoInternalCollection<T> returnValue = new();
            AddAllProperties(obj, returnValue, false);
            AddAllMethods(obj, returnValue, false);
            AddAllEvents(obj, returnValue, false);
            AddAllDynamicMembers(obj, returnValue);

            return returnValue;
        }

        private new void AddAllProperties<T>(
            object obj,
            PSMemberInfoInternalCollection<T> members,
            bool ignoreDuplicates)
            where T : PSMemberInfo
        {
            bool lookingForProperties = typeof(T).IsAssignableFrom(typeof(PSProperty));
            bool lookingForParameterizedProperties = IsTypeParameterizedProperty(typeof(T));
            if (!lookingForProperties && !lookingForParameterizedProperties)
            {
                return;
            }

            CacheTable table = _isStatic
                ? GetStaticPropertyReflectionTable((Type)obj)
                : GetInstancePropertyReflectionTable(obj.GetType());

            for (int i = 0; i < table.memberCollection.Count; i++)
            {
                if (table.memberCollection[i] is PropertyCacheEntry propertyEntry)
                {
                    if (lookingForProperties)
                    {
                        if (!ignoreDuplicates || (members[propertyEntry.member.Name] is null))
                        {
                            members.Add(
                                new PSProperty(
                                    name: propertyEntry.member.Name,
                                    adapter: this,
                                    baseObject: obj,
                                    adapterData: propertyEntry)
                                { IsHidden = propertyEntry.GetIsHidden() } as T);
                        }
                    }
                }
                else if (lookingForParameterizedProperties)
                {
                    ParameterizedPropertyCacheEntry parameterizedPropertyEntry = (ParameterizedPropertyCacheEntry)table.memberCollection[i];
                    if (!ignoreDuplicates || (members[parameterizedPropertyEntry.propertyName] is null))
                    {
                        // TODO: check for HiddenAttribute
                        // We can't currently write a parameterized property in a PowerShell class so this isn't too important,
                        // but if someone added the attribute to their C#, it'd be good to set isHidden correctly here.
                        members.Add(new PSParameterizedProperty(parameterizedPropertyEntry.propertyName,
                            this, obj, parameterizedPropertyEntry) as T);
                    }
                }
            }
        }

        private new void AddAllMethods<T>(
            object obj,
            PSMemberInfoInternalCollection<T> members,
            bool ignoreDuplicates)
            where T : PSMemberInfo
        {
            if (!typeof(T).IsAssignableFrom(typeof(PSMethod)))
            {
                return;
            }

            CacheTable table = _isStatic
                ? GetStaticMethodReflectionTable((Type)obj)
                : GetInstanceMethodReflectionTable(obj.GetType());

            for (int i = 0; i < table.memberCollection.Count; i++)
            {
                MethodCacheEntry method = (MethodCacheEntry)table.memberCollection[i];
                bool isCtor = method[0].method is ConstructorInfo;
                string name = isCtor ? "new" : method[0].method.Name;

                if (!ignoreDuplicates || (members[name] is null))
                {
                    bool isSpecial = !isCtor && method[0].method.IsSpecialName;
                    members.Add(Polyfill.CreatePSMethod(name, this, obj, method, isSpecial, method.GetIsHidden()) as T);
                }
            }
        }

        private new void AddAllEvents<T>(object obj, PSMemberInfoInternalCollection<T> members, bool ignoreDuplicates) where T : PSMemberInfo
        {
            if (!typeof(T).IsAssignableFrom(typeof(PSEvent)))
            {
                return;
            }

            Dictionary<string, EventCacheEntry> table = _isStatic
                ? GetStaticEventReflectionTable((Type)obj)
                : GetInstanceEventReflectionTable(obj.GetType());

            foreach (EventCacheEntry psEvent in table.Values)
            {
                if (!ignoreDuplicates || (members[psEvent.events[0].Name] is null))
                {
                    members.Add(new PSEvent(psEvent.events[0]) as T);
                }
            }
        }

        private static void AddAllDynamicMembers<T>(
            object obj,
            PSMemberInfoInternalCollection<T> members)
            where T : PSMemberInfo
        {
            if (obj is not IDynamicMetaObjectProvider idmop || obj is PSObject)
            {
                return;
            }

            if (!typeof(T).IsAssignableFrom(typeof(PSDynamicMember)))
            {
                return;
            }

            foreach (string name in idmop.GetMetaObject(Expression.Variable(idmop.GetType())).GetDynamicMemberNames())
            {
                members.Add(new PSDynamicMember(name) as T);
            }
        }

        internal static CacheTable GetStaticPropertyReflectionTable(Type type)
        {
            lock (s_staticPropertyCacheTable)
            {
                if (s_processedTypes.Contains(type) && s_staticPropertyCacheTable.TryGetValue(type, out CacheTable typeTable))
                {
                    return typeTable;
                }

                typeTable = new CacheTable();
                PopulatePropertyReflectionTable(type, typeTable, StaticBindingFlags);
                s_staticPropertyCacheTable[type] = typeTable;
                s_processedTypes.Add(type);
                return typeTable;
            }
        }

        /// <summary>
        /// Retrieves the table for static methods.
        /// </summary>
        /// <param name="type">Type to load methods for.</param>
        internal static CacheTable GetStaticMethodReflectionTable(Type type)
        {
            lock (s_staticMethodCacheTable)
            {
                if (s_processedTypes.Contains(type) && s_staticMethodCacheTable.TryGetValue(type, out CacheTable typeTable))
                {
                    return typeTable;
                }

                typeTable = new CacheTable();
                PopulateMethodReflectionTable(type, typeTable, StaticBindingFlags);
                s_staticMethodCacheTable[type] = typeTable;
                s_processedTypes.Add(type);
                return typeTable;
            }
        }

        /// <summary>
        /// Retrieves the table for static events.
        /// </summary>
        /// <param name="type">Type containing properties to load in typeTable.</param>
        internal static Dictionary<string, EventCacheEntry> GetStaticEventReflectionTable(Type type)
        {
            lock (s_staticEventCacheTable)
            {
                if (s_processedTypes.Contains(type) && s_staticEventCacheTable.TryGetValue(type, out Dictionary<string, EventCacheEntry> typeTable))
                {
                    return typeTable;
                }

                typeTable = new Dictionary<string, EventCacheEntry>();
                PopulateEventReflectionTable(type, typeTable, StaticBindingFlags);
                s_staticEventCacheTable[type] = typeTable;
                s_processedTypes.Add(type);
                return typeTable;
            }
        }

        /// <summary>
        /// Retrieves the table for instance events.
        /// </summary>
        /// <param name="type">Type containing methods to load in typeTable.</param>
        internal static Dictionary<string, EventCacheEntry> GetInstanceEventReflectionTable(Type type)
        {
            lock (s_instanceEventCacheTable)
            {
                if (s_processedTypes.Contains(type) && s_instanceEventCacheTable.TryGetValue(type, out Dictionary<string, EventCacheEntry> typeTable))
                {
                    return typeTable;
                }

                typeTable = new Dictionary<string, EventCacheEntry>(StringComparer.OrdinalIgnoreCase);
                PopulateEventReflectionTable(type, typeTable, InstanceBindingFlags);
                s_instanceEventCacheTable[type] = typeTable;
                s_processedTypes.Add(type);
                return typeTable;
            }
        }

        /// <summary>
        /// Called from GetProperty and GetProperties to populate the
        /// typeTable with all public properties and fields
        /// of type.
        /// </summary>
        /// <param name="type">Type with properties to load in typeTable.</param>
        internal static CacheTable GetInstancePropertyReflectionTable(Type type)
        {
            lock (s_instancePropertyCacheTable)
            {
                if (s_processedTypes.Contains(type) && s_instancePropertyCacheTable.TryGetValue(type, out CacheTable typeTable))
                {
                    return typeTable;
                }

                typeTable = new CacheTable();
                PopulatePropertyReflectionTable(type, typeTable, InstanceBindingFlags);
                s_instancePropertyCacheTable[type] = typeTable;
                s_processedTypes.Add(type);
                return typeTable;
            }
        }

        /// <summary>
        /// Retrieves the table for instance methods.
        /// </summary>
        /// <param name="type">Type with methods to load in typeTable.</param>
        internal static CacheTable GetInstanceMethodReflectionTable(Type type)
        {
            lock (s_instanceMethodCacheTable)
            {
                if (s_processedTypes.Contains(type) && s_instanceMethodCacheTable.TryGetValue(type, out CacheTable typeTable))
                {
                    return typeTable;
                }

                typeTable = new CacheTable();
                PopulateMethodReflectionTable(type, typeTable, InstanceBindingFlags);
                s_instanceMethodCacheTable[type] = typeTable;
                s_processedTypes.Add(type);
                return typeTable;
            }
        }

        /// <summary>
        /// Called from GetEventReflectionTable within a lock to fill the
        /// event cache table.
        /// </summary>
        /// <param name="type">Type to get events from.</param>
        /// <param name="typeEvents">Table to be filled.</param>
        /// <param name="bindingFlags">BindingFlags to use.</param>
        internal static void PopulateEventReflectionTable(
            Type type,
            Dictionary<string, EventCacheEntry> typeEvents,
            BindingFlags bindingFlags)
        {
            // Assemblies in CoreCLR might not allow reflection execution on their internal types. In such case, we walk up
            // the derivation chain to find the first public parent, and use reflection events on the public parent.
            // if (!TypeResolver.IsPublic(type) && DisallowPrivateReflection(type))
            // {
            //     type = GetFirstPublicParentType(type);
            // }

            // In CoreCLR, "GetFirstPublicParentType" may return null if 'type' is an interface
            if (type is not null)
            {
                EventInfo[] events = type.GetEvents(bindingFlags);
                Dictionary<string, List<EventInfo>> tempTable = new(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < events.Length; i++)
                {
                    EventInfo typeEvent = events[i];
                    string eventName = typeEvent.Name;
                    if (!tempTable.TryGetValue(eventName, out List<EventInfo> previousEntry))
                    {
                        List<EventInfo> eventEntry = new() { typeEvent };
                        tempTable.Add(eventName, eventEntry);
                    }
                    else
                    {
                        previousEntry.Add(typeEvent);
                    }
                }

                foreach (KeyValuePair<string, List<EventInfo>> entry in tempTable)
                {
                    typeEvents.Add(entry.Key, new EventCacheEntry(entry.Value.ToArray()));
                }
            }
        }

        /// <summary>
        /// Called from GetPropertyReflectionTable within a lock to fill the
        /// property cache table.
        /// </summary>
        /// <param name="type">Type to get properties from.</param>
        /// <param name="typeProperties">Table to be filled.</param>
        /// <param name="bindingFlags">BindingFlags to use.</param>
        internal static void PopulatePropertyReflectionTable(Type type, CacheTable typeProperties, BindingFlags bindingFlags)
        {
            Dictionary<string, List<PropertyInfo>> tempTable = new(StringComparer.OrdinalIgnoreCase);
            Type typeToGetPropertyAndField = type;

            // Assemblies in CoreCLR might not allow reflection execution on their internal types. In such case, we walk up the
            // derivation chain to find the first public parent, and use reflection properties/fields on the public parent.
            // if (!TypeResolver.IsPublic(type) && DisallowPrivateReflection(type))
            // {
            //     typeToGetPropertyAndField = GetFirstPublicParentType(type);
            // }

            // In CoreCLR, "GetFirstPublicParentType" may return null if 'type' is an interface
            PropertyInfo[] properties;
            if (typeToGetPropertyAndField is not null)
            {
                properties = typeToGetPropertyAndField.GetProperties(bindingFlags);
                for (int i = 0; i < properties.Length; i++)
                {
                    PopulateSingleProperty(type, properties[i], tempTable, properties[i].Name);
                }
            }

            Type[] interfaces = type.GetInterfaces();
            for (int interfaceIndex = 0; interfaceIndex < interfaces.Length; interfaceIndex++)
            {
                Type interfaceType = interfaces[interfaceIndex];
                // if (!TypeResolver.IsPublic(interfaceType))
                // {
                //     continue;
                // }

                properties = interfaceType.GetProperties(bindingFlags);
                for (int propertyIndex = 0; propertyIndex < properties.Length; propertyIndex++)
                {
                    PopulateSingleProperty(type, properties[propertyIndex], tempTable, properties[propertyIndex].Name);
                }
            }

            foreach (KeyValuePair<string, List<PropertyInfo>> pairs in tempTable)
            {
                List<PropertyInfo> propertiesList = pairs.Value;
                PropertyInfo firstProperty = propertiesList[0];
                if ((propertiesList.Count > 1) || (firstProperty.GetIndexParameters().Length != 0))
                {
                    typeProperties.Add(pairs.Key, new ParameterizedPropertyCacheEntry(propertiesList));
                }
                else
                {
                    PropertyCacheEntry cacheEntry = new(firstProperty);
                    if (cacheEntry.writeOnly && firstProperty.GetGetMethod(nonPublic: true) is not null)
                    {
                        cacheEntry.writeOnly = false;
                    }

                    typeProperties.Add(pairs.Key, cacheEntry);
                }
            }

            // In CoreCLR, "GetFirstPublicParentType" may return null if 'type' is an interface
            if (typeToGetPropertyAndField is not null)
            {
                FieldInfo[] fields = typeToGetPropertyAndField.GetFields(bindingFlags);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    string fieldName = field.Name;
                    PropertyCacheEntry previousMember = (PropertyCacheEntry)typeProperties[fieldName];
                    if (previousMember is null)
                    {
                        typeProperties.Add(fieldName, new PropertyCacheEntry(field));
                    }
                    else
                    {
                        // A property/field declared with new in a derived class might appear twice
                        // if (!string.Equals(previousMember.member.Name, fieldName))
                        // {
                        //     throw new ExtendedTypeSystemException(
                        //         "NotACLSComplaintField",
                        //         null,
                        //         ExtendedTypeSystem.NotAClsCompliantFieldProperty,
                        //         fieldName,
                        //         type.FullName,
                        //         previousMember.member.Name);
                        // }
                    }
                }
            }
        }

        private static void PopulateSingleProperty(
            Type type,
            PropertyInfo property,
            Dictionary<string, List<PropertyInfo>> tempTable,
            string propertyName)
        {
            if (!tempTable.TryGetValue(propertyName, out List<PropertyInfo> previousPropertyEntry))
            {
                previousPropertyEntry = new List<PropertyInfo> { property };
                tempTable.Add(propertyName, previousPropertyEntry);
            }
            else
            {
                // PropertyInfo firstProperty = previousPropertyEntry[0];
                // if (!string.Equals(property.Name, firstProperty.Name, StringComparison.Ordinal))
                // {
                //     throw new ExtendedTypeSystemException(
                //         "NotACLSComplaintProperty",
                //         null,
                //         ExtendedTypeSystem.NotAClsCompliantFieldProperty,
                //         property.Name,
                //         type.FullName,
                //         firstProperty.Name);
                // }

                if (PropertyAlreadyPresent(previousPropertyEntry, property))
                {
                    return;
                }

                previousPropertyEntry.Add(property);
            }
        }

        /// <summary>
        /// This method is necessary because an overridden property in a specific class derived from a generic one will
        /// appear twice. The second time, it should be ignored.
        /// </summary>
        private static bool PropertyAlreadyPresent(List<PropertyInfo> previousProperties, PropertyInfo property)
        {
            // The loop below
            bool returnValue = false;
            ParameterInfo[] propertyParameters = property.GetIndexParameters();
            int propertyIndexLength = propertyParameters.Length;

            for (int propertyIndex = 0; propertyIndex < previousProperties.Count; propertyIndex++)
            {
                PropertyInfo previousProperty = previousProperties[propertyIndex];
                ParameterInfo[] previousParameters = previousProperty.GetIndexParameters();
                if (previousParameters.Length == propertyIndexLength)
                {
                    bool parametersAreSame = true;
                    for (int parameterIndex = 0; parameterIndex < previousParameters.Length; parameterIndex++)
                    {
                        ParameterInfo previousParameter = previousParameters[parameterIndex];
                        ParameterInfo propertyParameter = propertyParameters[parameterIndex];
                        if (previousParameter.ParameterType != propertyParameter.ParameterType)
                        {
                            parametersAreSame = false;
                            break;
                        }
                    }

                    if (parametersAreSame)
                    {
                        returnValue = true;
                        break;
                    }
                }
            }

            return returnValue;
        }

        internal static void PopulateMethodReflectionTable(Type type, MethodInfo[] methods, CacheTable typeMethods)
        {
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.DeclaringType == type)
                {
                    string methodName = method.Name;
                    List<MethodBase> previousMethodEntry = (List<MethodBase>)typeMethods[methodName];
                    if (previousMethodEntry is null)
                    {
                        List<MethodBase> methodEntry = new() { method };
                        typeMethods.Add(methodName, methodEntry);
                    }
                    else
                    {
                        AddOverload(previousMethodEntry, method);
                    }
                }
            }

            if (type.BaseType is not null)
            {
                PopulateMethodReflectionTable(type.BaseType, methods, typeMethods);
            }
        }

        /// <summary>
        /// Adds an overload to a list of MethodInfo.  Before adding to the list, the
        /// list is searched to make sure we don't end up with 2 functions with the
        /// same signature.  This can happen when there is a newslot method.
        /// </summary>
        private static void AddOverload(List<MethodBase> previousMethodEntry, MethodInfo method)
        {
            for (int i = 0; i < previousMethodEntry.Count; i++)
            {
                if (SameSignature(previousMethodEntry[i], method))
                {
                    return;
                }
            }

            previousMethodEntry.Add(method);
        }

        /// <summary>
        /// Compare the signatures of the methods, returning true if the methods have
        /// the same signature.
        /// </summary>
        private static bool SameSignature(MethodBase method1, MethodBase method2)
        {
            if (method1.GetGenericArguments().Length != method2.GetGenericArguments().Length)
            {
                return false;
            }

            ParameterInfo[] parameters1 = method1.GetParameters();
            ParameterInfo[] parameters2 = method2.GetParameters();
            if (parameters1.Length != parameters2.Length)
            {
                return false;
            }

            for (int i = 0; i < parameters1.Length; ++i)
            {
                if (parameters1[i].ParameterType != parameters2[i].ParameterType
                    || parameters1[i].IsOut != parameters2[i].IsOut
                    || parameters1[i].IsOptional != parameters2[i].IsOptional)
                {
                    return false;
                }
            }

            return true;
        }

        private static void PopulateMethodReflectionTable(ConstructorInfo[] ctors, CacheTable typeMethods)
        {
            foreach (ConstructorInfo ctor in ctors)
            {
                List<MethodBase> previousMethodEntry = (List<MethodBase>)typeMethods["new"];
                if (previousMethodEntry is null)
                {
                    List<MethodBase> methodEntry = new();
                    methodEntry.Add(ctor);
                    typeMethods.Add("new", methodEntry);
                    continue;
                }

                previousMethodEntry.Add(ctor);
            }
        }

        /// <summary>
        /// Called from GetMethodReflectionTable within a lock to fill the
        /// method cache table.
        /// </summary>
        /// <param name="type">Type to get methods from.</param>
        /// <param name="typeMethods">Table to be filled.</param>
        /// <param name="bindingFlags">BindingFlags to use.</param>
        internal static void PopulateMethodReflectionTable(Type type, CacheTable typeMethods, BindingFlags bindingFlags)
        {
            Type typeToGetMethod = type;

            // Assemblies in CoreCLR might not allow reflection execution on their internal types. In such case, we walk up
            // the derivation chain to find the first public parent, and use reflection methods on the public parent.
            // if (!TypeResolver.IsPublic(type) && DisallowPrivateReflection(type))
            // {
            //     typeToGetMethod = GetFirstPublicParentType(type);
            // }

            // In CoreCLR, "GetFirstPublicParentType" may return null if 'type' is an interface
            if (typeToGetMethod is not null)
            {
                MethodInfo[] methods = typeToGetMethod.GetMethods(bindingFlags);
                PopulateMethodReflectionTable(typeToGetMethod, methods, typeMethods);
            }

            Type[] interfaces = type.GetInterfaces();
            for (int interfaceIndex = 0; interfaceIndex < interfaces.Length; interfaceIndex++)
            {
                Type interfaceType = interfaces[interfaceIndex];
                // if (!TypeResolver.IsPublic(interfaceType))
                // {
                //     continue;
                // }

                if (interfaceType.IsGenericType && type.IsArray)
                {
                    // GetInterfaceMap is not supported in this scenario...
                    // not sure if we need to do something special here...
                    continue;
                }

                MethodInfo[] methods;
                if (type.IsInterface)
                {
                    methods = interfaceType.GetMethods(bindingFlags);
                }
                else
                {
                    InterfaceMapping interfaceMapping = type.GetInterfaceMap(interfaceType);
                    methods = interfaceMapping.InterfaceMethods;
                }

                for (int methodIndex = 0; methodIndex < methods.Length; methodIndex++)
                {
                    MethodInfo interfaceMethodDefinition = methods[methodIndex];

                    if (interfaceMethodDefinition.IsStatic != ((BindingFlags.Static & bindingFlags) != 0))
                    {
                        continue;
                    }

                    List<MethodBase> previousMethodEntry = (List<MethodBase>)typeMethods[interfaceMethodDefinition.Name];
                    if (previousMethodEntry is null)
                    {
                        List<MethodBase> methodEntry = new() { interfaceMethodDefinition };
                        typeMethods.Add(interfaceMethodDefinition.Name, methodEntry);
                    }
                    else
                    {
                        if (!previousMethodEntry.Contains(interfaceMethodDefinition))
                        {
                            previousMethodEntry.Add(interfaceMethodDefinition);
                        }
                    }
                }
            }

            if ((bindingFlags & BindingFlags.Static) != 0)
            {
                // We don't add constructors if there was a static method named new.
                // We don't add constructors if the target type is not public, because it's useless to an internal type.
                List<MethodBase> previousMethodEntry = (List<MethodBase>)typeMethods["new"];
                if (previousMethodEntry is null)
                {
                    BindingFlags ctorBindingFlags = bindingFlags & ~(BindingFlags.FlattenHierarchy | BindingFlags.Static);
                    ctorBindingFlags |= BindingFlags.Instance;
                    ConstructorInfo[] ctorInfos = type.GetConstructors(ctorBindingFlags);
                    PopulateMethodReflectionTable(ctorInfos, typeMethods);
                }
            }

            for (int i = 0; i < typeMethods.memberCollection.Count; i++)
            {
                typeMethods.memberCollection[i] =
                    new MethodCacheEntry(((List<MethodBase>)typeMethods.memberCollection[i]).ToArray());
            }
        }

        private new T GetDotNetProperty<T>(object obj, string propertyName) where T : PSMemberInfo
        {
            return GetDotNetPropertyImpl<T>(obj, propertyName, predicate: null);
        }

        private new T GetDotNetMethod<T>(object obj, string methodName) where T : PSMemberInfo
        {
            return GetDotNetMethodImpl<T>(obj, methodName, predicate: null);
        }
    }
}
