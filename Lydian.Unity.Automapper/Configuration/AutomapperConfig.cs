using Lydian.Unity.Automapper.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Lydian.Unity.Automapper
{
	/// <summary>
	/// Represents a set of configuration instructions that guide the Automapper regarding mapping of specific types such as whether to register as a singleton, use policy injection or multimapping etc.
	/// </summary>
	public sealed class AutomapperConfig
	{
		private readonly List<Type> doNotMapTypes = new List<Type>();
		private readonly List<Tuple<Type, String>> explicitNamedMappings = new List<Tuple<Type, String>>();
		private readonly List<Type> multimapTypes = new List<Type>();
		private readonly List<Type> policyInjectionTypes = new List<Type>();
		private readonly List<Type> singletonTypes = new List<Type>();

		private AutomapperConfig()
		{
		}

		/// <summary>
		/// Creates a new UnityAutomapperConfig that can be composed using chained fluent-API style methods.
		/// </summary>
		/// <returns>An empty instance of the configuration.</returns>
		public static AutomapperConfig Create()
		{
			Contract.Ensures(Contract.Result<AutomapperConfig>() != null);
			return new AutomapperConfig();
		}

		/// <summary>
		/// Creates an automapper configuration using Attributes on the types to dictate the set of configuration.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		internal static AutomapperConfig Create(IEnumerable<Type> source)
		{
			Contract.Requires(source != null, "source is null.");

			var configuration = Create()
									.AndDoNotMapFor(source.Where(Lydian.Unity.Automapper.Core.Utils.HasAttribute<DoNotMapAttribute>).ToArray())
									.AndMapAsSingleton(source.Where(Lydian.Unity.Automapper.Core.Utils.HasAttribute<SingletonAttribute>).ToArray())
									.AndUseMultimappingFor(source.Where(Lydian.Unity.Automapper.Core.Utils.HasAttribute<MultimapAttribute>).ToArray())
									.AndUsePolicyInjectionFor(source.Where(Lydian.Unity.Automapper.Core.Utils.HasAttribute<PolicyInjectionAttribute>).ToArray());

			var namedMappings = source
									.Select(t => Tuple.Create(t, t.GetMapAsName()))
									.Where(pair => pair.Item2 != null);

			foreach (var namedMapping in namedMappings)
				configuration.AndUseNamedMappingFor(namedMapping.Item1, namedMapping.Item2);

			return configuration;
		}

		/// <summary>
		/// Indicates that the provided types should not participate in automapping.
		/// </summary>
		/// <param name="types">The set of types to ignore.</param>
		/// <returns></returns>
		public AutomapperConfig AndDoNotMapFor(params Type[] types)
		{
			Contract.Requires(types != null, "types is null.");

			doNotMapTypes.AddRange(types);
			return this;
		}
		/// <summary>
		/// Indicates that the provided type should be mapped using a specific name.
		/// </summary>
		/// <param name="type">The concrete type to map.</param>
		/// <param name="name">The name to use for the mapping.</param>
		/// <returns></returns>
		public AutomapperConfig AndUseNamedMappingFor(Type type, String name)
		{
			Contract.Requires(type != null, "type is null.");
			Contract.Requires(!String.IsNullOrEmpty(name), "name is null or empty.");
			
			explicitNamedMappings.Add(Tuple.Create(type, name));
			return this;
		}
		/// <summary>
		/// Indicates that the specified interfaces should use multimapping if many concretes are found for them.
		/// </summary>
		/// <param name="types">The set of interfaces to register as potential multimaps.</param>
		/// <returns></returns>
		public AutomapperConfig AndUseMultimappingFor(params Type[] types)
		{
			Contract.Requires(types != null, "types is null.");

			multimapTypes.AddRange(types);
			return this;
		}
		/// <summary>
		/// Indicates that the specified types should partake in policy injection.
		/// </summary>
		/// <param name="types">The set of types to register with policy injection.</param>
		/// <returns></returns>
		public AutomapperConfig AndUsePolicyInjectionFor(params Type[] types)
		{
			Contract.Requires(types != null, "types is null.");

			policyInjectionTypes.AddRange(types);
			return this;
		}
		/// <summary>
		/// Indicates that the specified types should be registered as singletons, that is using the ContainerControlledLifetimeManager.
		/// </summary>
		/// <param name="types">The set of types to register as singletons.</param>
		/// <returns></returns>
		public AutomapperConfig AndMapAsSingleton(params Type[] types)
		{
			Contract.Requires(types != null, "types is null.");

			singletonTypes.AddRange(types);
			return this;
		}

		private void DoMerge(AutomapperConfig config)
		{
			Contract.Requires(config.doNotMapTypes != null);
			Contract.Requires(config.explicitNamedMappings != null);
			Contract.Requires(config.multimapTypes != null);
			Contract.Requires(config.policyInjectionTypes != null);
			Contract.Requires(config.singletonTypes != null);

			doNotMapTypes.AddRange(config.doNotMapTypes);
			explicitNamedMappings.AddRange(config.explicitNamedMappings);
			multimapTypes.AddRange(config.multimapTypes);
			policyInjectionTypes.AddRange(config.policyInjectionTypes);
			singletonTypes.AddRange(config.singletonTypes);
		}

		internal AutomapperConfig MergeWith(AutomapperConfig sourceConfig)
		{
			DoMerge(sourceConfig);			
			return this;
		}
		internal Boolean IsMultimap(Type type)
		{
			type = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

			return multimapTypes
					.Any(t => t == type);
		}
		internal Boolean IsNamedMapping(Type type)
		{
			return explicitNamedMappings.Any(t => t.Item1 == type);
		}
		internal Boolean PolicyInjectionRequired()
		{
			 return policyInjectionTypes.Any();
		}
		internal Boolean IsPolicyInjection(Type type)
		{
			return policyInjectionTypes.Any(t => t == type);
		}
		internal Boolean IsSingleton(Type type)
		{
			return singletonTypes.Any(t => t == type);
		}
		internal Boolean IsMappable(Type type)
		{
			return !doNotMapTypes.Any(t => t == type);
		}
		internal String GetNamedMapping(Lydian.Unity.Automapper.Core.TypeMapping mapping)
		{
			var explicitNamedMapping = explicitNamedMappings
										.Where(t => t.Item1 == mapping.To)
										.Select(t => t.Item2)
										.FirstOrDefault();

			if (explicitNamedMapping != null)
				return explicitNamedMapping;

			return mapping.To.FullName;
		}

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		[ContractInvariantMethod]
		private void Assumptions()
		{
			Contract.Invariant(policyInjectionTypes != null);
			Contract.Invariant(singletonTypes != null);
			Contract.Invariant(doNotMapTypes != null);
			Contract.Invariant(explicitNamedMappings != null);
			Contract.Invariant(multimapTypes != null);
		}
	}
}