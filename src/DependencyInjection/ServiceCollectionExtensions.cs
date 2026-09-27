using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MidR.Abstractions;
using MidR.Behaviors;
using MidR.Core;
using MidR.Interfaces;
using System;
using System.Linq;
using System.Reflection;

namespace MidR.DependencyInjection
{
    /// <summary>
    /// Provides extension methods for registering MidR components in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the MidR components, handlers, notifications and behaviors into the dependency injection container
        /// using the specified assemblies.
        /// </summary>
        /// <param name="services">The service collection used to register services.</param>
        /// <param name="args">
        /// Optional arguments used to resolve assemblies that contain request handlers, notification handlers,
        /// and behaviors.
        /// </param>
        /// <returns>
        /// A <see cref="MidRConfiguration"/> instance containing the registered services and resolved assemblies.
        /// </returns>
        /// <remarks>
        /// The in-memory bus defaults to an unbounded channel with <c>Environment.ProcessorCount</c> concurrency.
        /// Call <see cref="MidRConfiguration.WithMemoryBus"/> on the returned configuration to customize it
        /// (bounded channel, custom concurrency, etc.).
        /// </remarks>
        public static MidRConfiguration AddMidR(
            this IServiceCollection services,
            params object[] args)
        {
            if (args is null || args.Length == 0)
            {
                var knownTypes = new[]
                {
                    typeof(INotificationHandler<>),
                    typeof(IRequestHandler<,>)
                };

                var loadedAssemblies = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.FullName))
                    .ToArray();

                var assemblie = loadedAssemblies
                    .Where(assembly => assembly.GetTypes().Any(type => type.IsClass &&
                    !type.IsAbstract &&
                    type.GetInterfaces().Any(i => i.IsGenericType &&
                    knownTypes.Contains(i.GetGenericTypeDefinition())))).Cast<object>().ToArray();

                args = assemblie;
            }

            var assemblies = ResolveAssemblies(args);
            var behaviorConfig = new BehaviorConfiguration();

            var registry = RegistryBuilder.BuildRegistry(assemblies, behaviorConfig);

            var memoryBus = new MemoryBus(MemoryBusOptions.CreateUnbounded());

            services.AddSingleton(registry);
            services.AddScoped<IBehaviorPipeline, BehaviorPipeline>();
            services.AddScoped<INotificationBehaviorPipeline, NotificationBehaviorPipeline>();
            services.AddSingleton(memoryBus);
            services.AddSingleton(behaviorConfig);

            services.AddScoped<ISender, Sender>();
            services.AddScoped<IPublisher, Publisher>();
            services.AddScoped<IMediator, Mediator>();

            services.AddHostedService<MemoryEventBusDispatcher>();

            RegisterHandlers(services, assemblies, typeof(INotificationHandler<>), allowMultiple: true);
            RegisterHandlers(services, assemblies, typeof(IRequestHandler<,>), allowMultiple: false);
            RegisterDirectHandlers(services, assemblies);
            RegisterBehaviors(services, behaviorConfig, assemblies);

            return new MidRConfiguration(services, assemblies);
        }

        private static void RegisterBehaviors(IServiceCollection services, BehaviorConfiguration behaviorConfig, Assembly[] assemblies)
        {
            foreach (var behaviorDescriptor in behaviorConfig.Behaviors)
            {
                var behaviorType = behaviorDescriptor.BehaviorType;

                if (!behaviorType.IsGenericTypeDefinition)
                {
                    continue;
                }

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

        private static Type? GetResponseType(Type requestType)
        {
            var requestInterface = requestType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

            return requestInterface?.GetGenericArguments().FirstOrDefault();
        }

        private static Assembly[] ResolveAssemblies(object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.FullName))
                    .ToArray();
            }

            if (args.All(a => a is Assembly))
            {
                return args.Cast<Assembly>().ToArray();
            }

            if (args.All(a => a is string))
            {
                var prefixes = args.Cast<string>().ToArray();
                return AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(a =>
                        !a.IsDynamic &&
                        !string.IsNullOrWhiteSpace(a.FullName) &&
                        prefixes.Any(p => a.FullName!.StartsWith(p)))
                    .ToArray();
            }

            throw new ArgumentException("Invalid parameters for AddMidR(). Use: no arguments, Assembly[], or prefix strings.");
        }

        private static void RegisterHandlers(IServiceCollection services, Assembly[] assemblies, Type handlerInterface, bool allowMultiple)
        {
            var isNotificationHandler = handlerInterface == typeof(INotificationHandler<>);

            var types = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract)
                .ToList();

            foreach (var type in types)
            {
                // Direct Dispatch handlers are excluded from the normal fan-out registration so
                // they are only reachable via PublishAsync(..., RoutingKey). They are self-registered
                // by RegisterDirectHandlers instead.
                if (isNotificationHandler && type.GetCustomAttribute<DirectQueueAttribute>() is not null)
                {
                    continue;
                }

                var interfaces = type.GetInterfaces()
                    .Where(i =>
                        i.IsGenericType &&
                        i.GetGenericTypeDefinition() == handlerInterface);

                foreach (var iface in interfaces)
                {
                    if (allowMultiple)
                    {
                        services.AddTransient(iface, type);
                    }
                    else
                    {
                        services.TryAddTransient(iface, type);
                    }
                }
            }
        }

        private static void RegisterDirectHandlers(IServiceCollection services, Assembly[] assemblies)
        {
            var directHandlerTypes = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
                .Where(t => t.GetCustomAttribute<DirectQueueAttribute>() is not null)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
                .Distinct();

            foreach (var type in directHandlerTypes)
            {
                // Self-register the concrete type so the registry can resolve the exact handler
                // bound to a routing key via GetRequiredService(concreteType).
                services.TryAddTransient(type, type);
            }
        }
    }
}