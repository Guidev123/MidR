using MidR.Abstractions;
using MidR.Behaviors;
using MidR.Core;
using MidR.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MidR.DependencyInjection
{
    internal static class RegistryBuilder
    {
        public static HandlerRegistry BuildRegistry(Assembly[] assemblies, BehaviorConfiguration behaviorConfig)
        {
            var registry = new HandlerRegistry();

            var handlerMappings = RegisterHandlers(registry, assemblies);

            var notificationTypes = RegisterNotificationHandlers(registry, assemblies);

            RegisterDirectNotificationHandlers(registry, assemblies);

            RegisterBehaviors(registry, handlerMappings, behaviorConfig);

            RegisterNotificationBehaviors(registry, notificationTypes, behaviorConfig);

            return registry;
        }

        private static Dictionary<Type, Type> RegisterHandlers(HandlerRegistry registry, Assembly[] assemblies)
        {
            var handlerMappings = new Dictionary<Type, Type>();
            var types = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
                .ToList();

            foreach (var type in types)
            {
                var interfaces = type.GetInterfaces();

                foreach (var iface in interfaces)
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                    {
                        var genericArgs = iface.GetGenericArguments();
                        var requestType = genericArgs[0];
                        var responseType = genericArgs[1];

                        var method = typeof(HandlerRegistry).GetMethod(nameof(HandlerRegistry.RegisterHandler))!
                            .MakeGenericMethod(requestType, responseType);
                        method.Invoke(registry, null);

                        handlerMappings[requestType] = responseType;
                    }
                }
            }

            return handlerMappings;
        }

        private static void RegisterBehaviors(HandlerRegistry registry, Dictionary<Type, Type> handlerMappings, BehaviorConfiguration behaviorConfig)
        {
            foreach (var behaviorDescriptor in behaviorConfig.Behaviors)
            {
                var behaviorType = behaviorDescriptor.BehaviorType;

                if (!behaviorType.IsGenericTypeDefinition)
                    continue;

                foreach (var kvp in handlerMappings)
                {
                    var requestType = kvp.Key;
                    var responseType = kvp.Value;

                    if (!SatisfiesGenericConstraints(behaviorType, requestType, responseType))
                    {
                        continue;
                    }

                    var method = typeof(HandlerRegistry).GetMethod(nameof(HandlerRegistry.RegisterBehavior))!
                        .MakeGenericMethod(requestType, responseType);

                    method.Invoke(registry, new object[] { behaviorType, behaviorDescriptor.Priority });
                }
            }
        }

        private static List<Type> RegisterNotificationHandlers(HandlerRegistry handlerRegistry, Assembly[] assemblies)
        {
            var notificationTypes = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
                .SelectMany(t => t.GetInterfaces())
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                .Select(i => i.GetGenericArguments()[0])
                .Distinct()
                .ToList();

            var registerMethod = typeof(HandlerRegistry)
                .GetMethod(nameof(HandlerRegistry.RegisterNotificationHandler))!;

            foreach (var notificationType in notificationTypes)
            {
                var method = registerMethod.MakeGenericMethod(notificationType);
                method.Invoke(handlerRegistry, null);
            }

            return notificationTypes;
        }

        private static void RegisterDirectNotificationHandlers(HandlerRegistry registry, Assembly[] assemblies)
        {
            var registerMethod = typeof(HandlerRegistry)
                .GetMethod(nameof(HandlerRegistry.RegisterDirectNotificationHandler))!;

            var directHandlerTypes = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
                .Select(t => new { Type = t, Attribute = t.GetCustomAttribute<DirectQueueAttribute>() })
                .Where(x => x.Attribute is not null);

            foreach (var entry in directHandlerTypes)
            {
                var notificationTypes = entry.Type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                    .Select(i => i.GetGenericArguments()[0]);

                foreach (var notificationType in notificationTypes)
                {
                    var method = registerMethod.MakeGenericMethod(notificationType);

                    try
                    {
                        method.Invoke(registry, new object[] { new RoutingKey(entry.Attribute!.Key), entry.Type });
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException is not null)
                    {
                        throw ex.InnerException;
                    }
                }
            }
        }

        private static void RegisterNotificationBehaviors(HandlerRegistry handlerRegistry, List<Type> notificationTypes, BehaviorConfiguration behaviorConfiguration)
        {
            foreach (var behaviorDescriptor in behaviorConfiguration.Behaviors)
            {
                var behaviorType = behaviorDescriptor.BehaviorType;

                if (!behaviorType.IsGenericTypeDefinition)
                {
                    continue;
                }

                var genericParams = behaviorType.GetGenericArguments();
                if (genericParams.Length != 1)
                {
                    continue;
                }

                foreach (var notificationType in notificationTypes)
                {
                    if (!SatisfiesGenericConstraints(behaviorType, notificationType))
                    {
                        continue;
                    }

                    try
                    {
                        var concreteBehaviorType = behaviorType.MakeGenericType(notificationType);
                        var notificationBehaviorInteface = typeof(INotificationBehavior<>).MakeGenericType(notificationType);

                        if (!notificationBehaviorInteface.IsAssignableFrom(concreteBehaviorType))
                        {
                            continue;
                        }

                        var method = typeof(HandlerRegistry).GetMethod(nameof(HandlerRegistry.RegisterNotificationBehavior))!
                            .MakeGenericMethod(notificationType);

                        method.Invoke(handlerRegistry, new object[] { behaviorType, behaviorDescriptor.Priority });
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
        }

        private static bool SatisfiesGenericConstraints(Type genericTypeDefinition, params Type[] typeArguments)
        {
            var genericParams = genericTypeDefinition.GetGenericArguments();

            if (genericParams.Length != typeArguments.Length)
            {
                return false;
            }

            for (int i = 0; i < genericParams.Length; i++)
            {
                var constraints = genericParams[i].GetGenericParameterConstraints();

                foreach (var constraint in constraints)
                {
                    try
                    {
                        var resolvedConstraint = ResolveConstraint(constraint, genericParams, typeArguments);

                        if (!resolvedConstraint.IsAssignableFrom(typeArguments[i]))
                        {
                            return false;
                        }
                    }
                    catch (ArgumentException)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static Type ResolveConstraint(Type constraint, Type[] genericParams, Type[] typeArguments)
        {
            if (!constraint.ContainsGenericParameters)
            {
                return constraint;
            }

            if (constraint.IsGenericType)
            {
                var args = constraint.GetGenericArguments();
                var resolvedArgs = new Type[args.Length];

                for (int j = 0; j < args.Length; j++)
                {
                    var idx = Array.IndexOf(genericParams, args[j]);
                    resolvedArgs[j] = idx >= 0 ? typeArguments[idx] : args[j];
                }

                return constraint.GetGenericTypeDefinition().MakeGenericType(resolvedArgs);
            }

            var paramIdx = Array.IndexOf(genericParams, constraint);
            return paramIdx >= 0 ? typeArguments[paramIdx] : constraint;
        }
    }
}