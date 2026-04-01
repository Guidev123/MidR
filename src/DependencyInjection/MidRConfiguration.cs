using Microsoft.Extensions.DependencyInjection;
using MidR.Behaviors;
using MidR.Core;
using MidR.Interfaces;
using System;
using System.Linq;
using System.Reflection;

namespace MidR.DependencyInjection
{
    /// <summary>
    /// Represents the configuration context for MidR, allowing customization of behaviors
    /// after the initial service registration.
    /// </summary>
    public sealed class MidRConfiguration
    {
        private readonly IServiceCollection _services;
        private readonly Assembly[] _assemblies;

        internal MidRConfiguration(IServiceCollection services, Assembly[] assemblies)
        {
            _services = services;
            _assemblies = assemblies;
        }

        /// <summary>
        /// Applies additional behavior configuration to the pipeline and updates the service collection accordingly.
        /// </summary>
        /// <param name="configure">
        /// A delegate used to configure the <see cref="BehaviorConfiguration"/> instance.
        /// </param>
        /// <returns>
        /// The same <see cref="MidRConfiguration"/> instance, enabling fluent configuration.
        /// </returns>
        public MidRConfiguration WithBehaviors(Action<BehaviorConfiguration> configure)
        {
            var oldRegistryDescriptor = _services.FirstOrDefault(s => s.ServiceType == typeof(HandlerRegistry));
            if (oldRegistryDescriptor is not null)
            {
                _services.Remove(oldRegistryDescriptor);
            }

            var behaviorConfigDescriptor = _services.FirstOrDefault(s => s.ServiceType == typeof(BehaviorConfiguration));
            var behaviorConfig = behaviorConfigDescriptor?.ImplementationInstance as BehaviorConfiguration
                                ?? new BehaviorConfiguration();

            configure(behaviorConfig);

            var registry = RegistryBuilder.BuildRegistry(_assemblies, behaviorConfig);
            _services.AddSingleton(registry);

            if (behaviorConfigDescriptor is not null)
            {
                _services.Remove(behaviorConfigDescriptor);
            }

            _services.AddSingleton(behaviorConfig);

            RegisterBehaviors(_services, behaviorConfig, _assemblies);

            return this;
        }

        private static void RegisterBehaviors(IServiceCollection services, BehaviorConfiguration behaviorConfig, Assembly[] assemblies)
        {
            var behaviorDescriptors = services.Where(s =>
                s.ServiceType.IsGenericType &&
                s.ServiceType.GetGenericTypeDefinition() == typeof(IRequestBehavior<,>)).ToList();

            foreach (var desc in behaviorDescriptors)
            {
                services.Remove(desc);
            }

            foreach (var behaviorDescriptor in behaviorConfig.Behaviors)
            {
                var behaviorType = behaviorDescriptor.BehaviorType;

                if (!behaviorType.IsGenericTypeDefinition)
                    continue;

                var genericParams = behaviorType.GetGenericArguments();

                if (genericParams.Length == 2)
                {
                    RegisterRequestBehaviors(services, behaviorType, assemblies);
                }
                else if (genericParams.Length == 1)
                {
                    RegisterNotificationBehaviors(services, behaviorType, assemblies);
                }
            }
        }

        public static void RegisterRequestBehaviors(IServiceCollection services, Type behaviorType, Assembly[] assemblies)
        {
            var requestTypes = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)))
                .ToList();

            foreach (var requestType in requestTypes)
            {
                var responseType = GetResponseType(requestType);
                if (responseType == null)
                {
                    continue;
                }

                if (!CanMakeGenericType(behaviorType, requestType, responseType))
                {
                    continue;
                }

                var concreteBehaviorType = behaviorType.MakeGenericType(requestType, responseType);
                var interfaceType = typeof(IRequestBehavior<,>).MakeGenericType(requestType, responseType);

                services.AddTransient(concreteBehaviorType);
                services.AddTransient(interfaceType, sp => sp.GetRequiredService(concreteBehaviorType));
            }
        }

        public static void RegisterNotificationBehaviors(IServiceCollection services, Type behaviorType, Assembly[] assemblies)
        {
            var genericParams = behaviorType.GetGenericArguments();
            if (genericParams.Length != 1)
            {
                return;
            }

            var notificationTypes = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
                .SelectMany(t => t.GetInterfaces())
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                .Select(c => c.GetGenericArguments()[0])
                .Distinct()
                .ToList();

            foreach (var notificationType in notificationTypes)
            {
                if (!CanMakeGenericType(behaviorType, notificationType))
                {
                    continue;
                }

                try
                {
                    var concreteBehaviorType = behaviorType.MakeGenericType(notificationType);
                    var interfaceType = typeof(INotificationBehavior<>).MakeGenericType(notificationType);

                    if (!interfaceType.IsAssignableFrom(concreteBehaviorType))
                    {
                        continue;
                    }

                    services.AddTransient(concreteBehaviorType);
                    services.AddTransient(interfaceType, sp => sp.GetRequiredService(concreteBehaviorType));
                }
                catch
                {
                    continue;
                }
            }
        }

        private static Type? GetResponseType(Type requestType)
        {
            var requestInterface = requestType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

            return requestInterface?.GetGenericArguments().FirstOrDefault();
        }

        private static bool CanMakeGenericType(Type genericTypeDefinition, params Type[] typeArguments)
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