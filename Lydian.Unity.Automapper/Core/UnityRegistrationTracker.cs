using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Lydian.Unity.Automapper.Core
{
	/// <summary>
	/// Keeps track of registrations that have taken place on a Unity Container.
	/// </summary>
	internal sealed class UnityRegistrationTracker : IEqualityComparer<ContainerRegistration>
	{
		private readonly IEnumerable<ContainerRegistration> initialRegistrations;
		private readonly IUnityContainer target;

		/// <summary>
		/// Creates a new instance of the UnityRegistrationTracker and begins tracking changes immediately.
		/// </summary>
		/// <param name="target">The container to track changes of.</param>
		public UnityRegistrationTracker(IUnityContainer target)
		{
			Contract.Requires(target != null, "target is null.");
			Contract.Ensures(initialRegistrations != null);

			this.target = target;
			Contract.Assume(target.Registrations != null);
			initialRegistrations = target.Registrations.ToArray();
		}
		
		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Code Contracts")]
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Code Contracts")]
		[ContractInvariantMethod]
		private void ObjectInvariant()
		{
			Contract.Invariant(target != null);
			Contract.Invariant(initialRegistrations != null);
			Contract.Invariant(target.Registrations != null);
		}
		
		/// <summary>
		/// Gets the list of newly-added registrations since the registration tracker was created.
		/// </summary>
		/// <returns>The collection of new registrations.</returns>
		public IEnumerable<ContainerRegistration> GetNewRegistrations()
		{
			return target.Registrations.Except(initialRegistrations, this).ToArray();
		}

		public Boolean Equals(ContainerRegistration x, ContainerRegistration y)
		{
			return x.MappedToType.Equals(y.MappedToType)
				&& x.RegisteredType.Equals(y.MappedToType)
				&& String.Equals(x.Name, y.Name);
		}

		public Int32 GetHashCode(ContainerRegistration obj)
		{
			return (obj.MappedToType.FullName + obj.RegisteredType.FullName + obj.Name).GetHashCode();
		}
	}
}