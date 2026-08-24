using System.Collections;
using System.Reflection;
using Dynamicweb.CoreUI.Actions;

namespace Truvio.Commerce.PowerTools.Core.Rights.Dw;

/// <summary>
/// Every capability-control read, through reflection.
/// <para>
/// Capability control is a DW <b>10.19+</b> feature: <c>Dynamicweb.CoreUI.CapabilityControl</c> —
/// <c>CapabilityHelper</c>, <c>CapabilityKey</c>, <c>CapabilityServices</c> and
/// <c>ActionNode.Capability</c> — does not exist in 10.18 or below (verified by bisecting the
/// packages: absent at 10.18.11, present at 10.19.6). The suite compiles against a 10.8.4 floor and
/// ships one binary for every supported host, so binding to those types at compile time would make
/// the whole assembly unloadable on an older host — DW's AddInManager skips such an assembly
/// silently, taking every other tool with it.
/// </para>
/// <para>
/// So the tool binds late instead, and reports "capability data unavailable" wherever the API is
/// missing rather than failing. Every member is resolved once and cached; a resolution failure is
/// permanent for the process, never retried per row.
/// </para>
/// </summary>
internal static class CapabilityReflection
{
    private static readonly Lazy<Surface> Api = new(Resolve, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>True when this host carries the capability-control API at all.</summary>
    public static bool Available => Api.Value.Ok;

    /// <summary>
    /// <c>CapabilityHelper.IsCapabilityControlActive()</c>. Call ONCE per report — it resolves a DI
    /// service on every call. False on a host without the API, which is also how it behaves for a
    /// solution that simply never turned the feature on.
    /// </summary>
    public static bool IsCapabilityControlActive()
    {
        var api = Api.Value;
        if (!api.Ok)
            return false;

        try
        {
            return api.IsActive!.Invoke(null, null) is true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Every declared capability, with the RequiredCapabilities that drive the cascade.</summary>
    public static IReadOnlyList<CapabilityMeta> GetCapabilities()
    {
        var api = Api.Value;
        if (!api.Ok)
            return [];

        try
        {
            if (api.GetCapabilities!.Invoke(null, null) is not IEnumerable capabilities)
                return [];

            var list = new List<CapabilityMeta>();
            foreach (var capability in capabilities)
            {
                if (capability is null)
                    continue;

                var key = KeyValue(api, api.CapabilityKeyProperty?.GetValue(capability));
                if (string.IsNullOrEmpty(key))
                    continue;

                var name = api.CapabilityNameProperty?.GetValue(capability) as string ?? key;
                var required = new List<string>();
                if (api.RequiredProperty?.GetValue(capability) is IEnumerable requiredKeys)
                {
                    foreach (var requiredKey in requiredKeys)
                    {
                        var value = KeyValue(api, requiredKey);
                        if (!string.IsNullOrEmpty(value))
                            required.Add(value);
                    }
                }

                list.Add(new CapabilityMeta(key, name, required));
            }

            return list;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// The stored limitations (denies) for the given keys, resolved through DW's own service so the
    /// <c>CapabilityLimitation</c> table is never read directly — a solution that never enabled the
    /// feature may not even have it.
    /// </summary>
    public static IReadOnlyList<(int UserGroupId, string Key)> GetLimitations(IEnumerable<string> keys)
    {
        var api = Api.Value;
        if (!api.Ok || api.GetAccesses is null)
            return [];

        try
        {
            var service = api.CapabilitiesProperty!.GetValue(null);
            if (service is null)
                return [];

            var typedKeys = (IList)Activator.CreateInstance(api.KeyListType!)!;
            foreach (var key in keys.Distinct(StringComparer.OrdinalIgnoreCase))
                typedKeys.Add(MakeKey(api, key));

            if (api.GetAccesses.Invoke(service, [typedKeys]) is not IEnumerable accesses)
                return [];

            var list = new List<(int, string)>();
            foreach (var access in accesses)
            {
                if (access is null)
                    continue;

                var groupId = api.AccessGroupProperty?.GetValue(access) as int? ?? 0;
                var key = KeyValue(api, api.AccessKeyProperty?.GetValue(access));
                if (groupId > 0 && !string.IsNullOrEmpty(key))
                    list.Add((groupId, key));
            }

            return list;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// DW's own verdict for one user and key — kept beside the evaluator's computation so the two
    /// can be compared. Null when it could not be read.
    /// </summary>
    public static bool? UserHasCapability(int userId, string key)
    {
        var api = Api.Value;
        if (!api.Ok || api.UserHasCapability is null || string.IsNullOrEmpty(key))
            return null;

        try
        {
            var service = api.CapabilitiesProperty!.GetValue(null);
            if (service is null)
                return null;

            return api.UserHasCapability.Invoke(service, [userId, MakeKey(api, key)]) as bool?;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>The capability an area, section or node declares — empty when it declares none.</summary>
    public static string CapabilityOf(ActionNode? node)
    {
        var api = Api.Value;
        if (!api.Ok || node is null || api.NodeCapabilityProperty is null)
            return string.Empty;

        try
        {
            return KeyValue(api, api.NodeCapabilityProperty.GetValue(node));
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// <c>NavigationActionNode.PermissionLevelCurrentUser</c> — the level DW resolved for whoever
    /// was current when the node was built. Newer than the 10.8.4 floor, so it is read late like
    /// the capability surface. Null when absent or unset, which DW itself treats as "All".
    /// </summary>
    public static int? PermissionLevelOf(object? node) => LevelProperty(node, "PermissionLevelCurrentUser");

    /// <summary>
    /// <c>ActionNode.PermissionLevelRequired</c> — what a node needs from its parent before the
    /// tree keeps it (<c>PermissionLevelExtension.WithPermission</c>).
    /// </summary>
    public static int? RequiredLevelOf(object? node) => LevelProperty(node, "PermissionLevelRequired");

    private static int? LevelProperty(object? node, string propertyName)
    {
        if (node is null)
            return null;

        try
        {
            var value = node.GetType().GetProperty(propertyName)?.GetValue(node);
            // PermissionLevel is an enum; both properties are nullable.
            return value is null ? null : Convert.ToInt32(value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// <c>PermissionHierarchyFeature</c> (also newer than the floor) — it changes what
    /// <c>HasPermission</c> means. Null when it cannot be read.
    /// </summary>
    public static bool? IsPermissionHierarchyActive()
    {
        var api = Api.Value;
        if (api.FeatureManagerIsActive is null || api.PermissionHierarchyFeatureType is null)
            return null;

        try
        {
            var manager = api.FeatureManagerAccessor?.Invoke();
            if (manager is null)
                return null;

            return api.FeatureManagerIsActive
                .MakeGenericMethod(api.PermissionHierarchyFeatureType)
                .Invoke(manager, null) as bool?;
        }
        catch
        {
            return null;
        }
    }

    private static object MakeKey(Surface api, string key) => Activator.CreateInstance(api.KeyType!, key)!;

    private static string KeyValue(Surface api, object? key) =>
        key is null ? string.Empty : api.KeyValueProperty?.GetValue(key) as string ?? string.Empty;

    private static Surface Resolve()
    {
        try
        {
            // CoreUI is referenced at compile time for ActionNode, so its assembly is always the
            // right one to look in — no assembly scan, no version guessing.
            var coreUi = typeof(ActionNode).Assembly;
            var ns = "Dynamicweb.CoreUI.CapabilityControl.";

            var helper = coreUi.GetType(ns + "CapabilityHelper");
            var keyType = coreUi.GetType(ns + "CapabilityKey");
            var services = coreUi.GetType(ns + "CapabilityServices");
            var serviceType = coreUi.GetType(ns + "CapabilityService");
            var capabilityType = coreUi.GetType(ns + "Capability");
            var accessType = coreUi.GetType(ns + "CapabilityAccess");

            if (helper is null || keyType is null || services is null || serviceType is null || capabilityType is null || accessType is null)
                return Surface.Missing;

            var surface = new Surface
            {
                Ok = true,
                KeyType = keyType,
                KeyListType = typeof(List<>).MakeGenericType(keyType),
                KeyValueProperty = keyType.GetProperty("Value"),
                IsActive = helper.GetMethod("IsCapabilityControlActive", BindingFlags.Public | BindingFlags.Static),
                GetCapabilities = helper.GetMethod("GetCapabilities", BindingFlags.Public | BindingFlags.Static),
                CapabilitiesProperty = services.GetProperty("Capabilities", BindingFlags.Public | BindingFlags.Static),
                CapabilityKeyProperty = capabilityType.GetProperty("Key"),
                CapabilityNameProperty = capabilityType.GetProperty("Name"),
                RequiredProperty = capabilityType.GetProperty("RequiredCapabilities"),
                AccessGroupProperty = accessType.GetProperty("UserGroupId"),
                AccessKeyProperty = accessType.GetProperty("Key"),
                GetAccesses = serviceType.GetMethod("GetCapabilityAccesses"),
                UserHasCapability = serviceType.GetMethod("UserHasCapability"),
                NodeCapabilityProperty = typeof(ActionNode).GetProperty("Capability")
            };

            ResolveFeatureManager(surface);

            return surface.KeyValueProperty is null || surface.IsActive is null ? Surface.Missing : surface;
        }
        catch
        {
            return Surface.Missing;
        }
    }

    /// <summary>
    /// The feature manager is reached the same way DW reaches it (DI), so no assumption is made
    /// about how features are stored. Optional: a null here only costs the hierarchy note.
    /// </summary>
    private static void ResolveFeatureManager(Surface surface)
    {
        try
        {
            var core = typeof(Dynamicweb.Core.Converter).Assembly;
            var featureManagerType = core.GetType("Dynamicweb.Core.FeatureManager");
            surface.PermissionHierarchyFeatureType =
                core.GetType("Dynamicweb.Security.Permissions.PermissionHierarchyFeature")
                ?? core.GetType("Dynamicweb.Core.PermissionHierarchyFeature");

            if (featureManagerType is null || surface.PermissionHierarchyFeatureType is null)
                return;

            surface.FeatureManagerIsActive = featureManagerType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "IsActive" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);

            var resolverType = core.GetType("Dynamicweb.Extensibility.Dependencies.DependencyResolver");
            var current = resolverType?.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (current is IServiceProvider provider)
                surface.FeatureManagerAccessor = () => provider.GetService(featureManagerType);
        }
        catch
        {
            // Optional signal only.
        }
    }

    private sealed class Surface
    {
        public static readonly Surface Missing = new();

        public bool Ok { get; init; }
        public Type? KeyType { get; init; }
        public Type? KeyListType { get; init; }
        public PropertyInfo? KeyValueProperty { get; init; }
        public MethodInfo? IsActive { get; init; }
        public MethodInfo? GetCapabilities { get; init; }
        public PropertyInfo? CapabilitiesProperty { get; init; }
        public PropertyInfo? CapabilityKeyProperty { get; init; }
        public PropertyInfo? CapabilityNameProperty { get; init; }
        public PropertyInfo? RequiredProperty { get; init; }
        public PropertyInfo? AccessGroupProperty { get; init; }
        public PropertyInfo? AccessKeyProperty { get; init; }
        public MethodInfo? GetAccesses { get; init; }
        public MethodInfo? UserHasCapability { get; init; }
        public PropertyInfo? NodeCapabilityProperty { get; init; }

        public Type? PermissionHierarchyFeatureType { get; set; }
        public MethodInfo? FeatureManagerIsActive { get; set; }
        public Func<object?>? FeatureManagerAccessor { get; set; }
    }
}
