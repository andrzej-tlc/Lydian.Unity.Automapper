using Lydian.Unity.Automapper.Core;
using Lydian.Unity.Automapper.Core.Handling;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.InterceptionExtension;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;

namespace Lydian.Unity.Automapper.Test.Core
{

	[TestClass]
	public class TypeMappingHandlerTests
	{
		private Mock<IRegistrationNameFactory> registrationNameFactory;
		private Mock<IConfigLifetimeManagerFactory> configLifetimeManagerFactory;
		private Mock<IInjectionMemberFactory> injectionMemberFactory;
		private Mock<ITypeMappingValidator> typeMappingValidator;
		private Mock<IUnityContainer> target;
		private TypeMappingHandler handler;
		
		[TestInitialize]
		public void Setup()
		{
			registrationNameFactory = new Mock<IRegistrationNameFactory>();
			configLifetimeManagerFactory = new Mock<IConfigLifetimeManagerFactory>();
			injectionMemberFactory = new Mock<IInjectionMemberFactory>();
			typeMappingValidator = new Mock<ITypeMappingValidator>();
			target = new Mock<IUnityContainer>();

			handler = new TypeMappingHandler(registrationNameFactory.Object, configLifetimeManagerFactory.Object, injectionMemberFactory.Object, typeMappingValidator.Object); 
		}

		[TestMethod]
		public void PerformRegistrations_NoMappingsSupplied_NoRegistrationsCreated()
		{
			// Act
			handler.PerformRegistrations(target.Object, new TypeMapping[0]);
			
			// Assert
			target.Verify(t => t.RegisterType(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<String>(), It.IsAny<LifetimeManager>(), It.IsAny<InjectionMember[]>()), Times.Never());
		}

		[TestMethod]
		public void PerformRegistrations_SingleMappingSupplied_CallsValidator()
		{
			var mapping = new TypeMapping(typeof(String), typeof(Boolean));

			// Act
			handler.PerformRegistrations(target.Object, new [] { mapping });

			// Assert
			typeMappingValidator.Verify(v => v.ValidateTypeMapping(mapping));
		}

		[TestMethod]
		public void PerformRegistrations_SingleMappingSupplied_CallsInjectionMemberFactory()
		{
			var mapping = new TypeMapping(typeof(String), typeof(Boolean));

			// Act
			handler.PerformRegistrations(target.Object, new[] { mapping });

			// Assert
			injectionMemberFactory.Verify(m => m.CreateInjectionMembers(mapping));
		}

		[TestMethod]
		public void PerformRegistrations_SingleMappingSupplied_CallsLifetimeManagerFactory()
		{
			var mapping = new TypeMapping(typeof(String), typeof(Boolean));

			// Act
			handler.PerformRegistrations(target.Object, new[] { mapping });

			// Assert
			configLifetimeManagerFactory.Verify(c => c.CreateLifetimeManager(mapping));
		}

		[TestMethod]
		public void PerformRegistrations_SingleMappingSupplied_CallsRegistrationNameFactory()
		{
			var mapping = new TypeMapping(typeof(String), typeof(Boolean));

			// Act
			handler.PerformRegistrations(target.Object, new[] { mapping });

			// Assert
			registrationNameFactory.Verify(r => r.GetRegistrationName(mapping));
		}

		[TestMethod]
		public void PerformRegistrations_CreatesAllParts_RegistersItIntoUnity()
		{
			var lifetimeManager = new ContainerControlledLifetimeManager();
			var injectionMembers = new InjectionMember[] { new InterceptionBehavior<PolicyInjectionBehavior>() };

			StubCreateInjectionMembers(injectionMembers);
			registrationNameFactory.Setup(f => f.GetRegistrationName(It.IsAny<TypeMapping>())).Returns("TEST");
			configLifetimeManagerFactory.Setup(l => l.CreateLifetimeManager(It.IsAny<TypeMapping>())).Returns(lifetimeManager);

			// Act
			handler.PerformRegistrations(target.Object, new[] { new TypeMapping(typeof(String), typeof(Boolean)) });

			// Assert
			target.Verify(t => t.RegisterType(typeof(String), typeof(Boolean), "TEST", lifetimeManager, injectionMembers));
		}
		
		[TestMethod]
		public void PerformRegistrations_CreatedARegistration_ReturnsIt()
		{
			var realContainer = new UnityContainer();

			// Act
			var registrations = handler.PerformRegistrations(realContainer, new[] { new TypeMapping(typeof(Object), typeof(String)) });

			// Assert
			var registration = registrations.Single();
			Assert.AreEqual(typeof(Object), registration.RegisteredType);
			Assert.AreEqual(typeof(String), registration.MappedToType);
		}

		[TestMethod]
		public void PerformRegistrations_ExistingRegistrationSameSourceDifferentName_DoesNotReturnIt()
		{
			var realContainer = new UnityContainer();
			realContainer.RegisterType<Object, String>("TEST");
			registrationNameFactory.Setup(f => f.GetRegistrationName(It.IsAny<TypeMapping>())).Returns("TEST2");
			
			// Act
			var registrations = handler.PerformRegistrations(realContainer, new[] { new TypeMapping(typeof(Object), typeof(String)) });

			// Assert
			var registration = registrations.Single();
			Assert.AreEqual(typeof(Object), registration.RegisteredType);
			Assert.AreEqual(typeof(String), registration.MappedToType);
		}

		[TestMethod]
		public void PerformRegistrations_ExistingRegistrationDifferentSourceSameName_DoesNotReturnIt()
		{
			var realContainer = new UnityContainer();
			realContainer.RegisterType<String, String>("TEST2");
			registrationNameFactory.Setup(f => f.GetRegistrationName(It.IsAny<TypeMapping>())).Returns("TEST2");

			// Act
			var registrations = handler.PerformRegistrations(realContainer, new[] { new TypeMapping(typeof(Object), typeof(String)) });

			// Assert
			var registration = registrations.Single();
			Assert.AreEqual(typeof(Object), registration.RegisteredType);
			Assert.AreEqual(typeof(String), registration.MappedToType);
		}

		[TestMethod]
		public void PerformRegistrations_RegistrationCreatedInjectionMembers_AddsPolicyExtension()
		{
			var container = new Mock<IUnityContainer>();
			StubCreateInjectionMembers(new InterceptionBehavior<PolicyInjectionBehavior>());

			// Act
			var registrations = handler.PerformRegistrations(container.Object, new[] { new TypeMapping(typeof(Object), typeof(String)) });

			// Assert
			container.Verify(tar => tar.AddExtension(It.IsAny<UnityContainerExtension>()), Times.Once());
		}

		[TestMethod]
		public void PerformRegistrations_RegistrationCreatedNoInjectionMembers_DoesNotAddPolicyExtension()
		{
			var container = new Mock<IUnityContainer>();
			StubCreateInjectionMembers();

			// Act
			var registrations = handler.PerformRegistrations(container.Object, new[] { new TypeMapping(typeof(Object), typeof(String)) });

			// Assert
			container.Verify(tar => tar.AddExtension(It.IsAny<UnityContainerExtension>()), Times.Never());
		}

		[TestMethod]
		public void PerformRegistrations_MultipleRegistrationsWithInjectionMembers_OnlyAddsPolicyExtensionOnce()
		{
			var container = new Mock<IUnityContainer>();
			StubCreateInjectionMembers(new InterceptionBehavior<PolicyInjectionBehavior>());

			// Act
			var registrations = handler.PerformRegistrations(container.Object, new[] { new TypeMapping(typeof(Object), typeof(String)), new TypeMapping(typeof(Object), typeof(String)) });

			// Assert
			container.Verify(tar => tar.AddExtension(It.IsAny<UnityContainerExtension>()), Times.Once());
		}



		private void StubCreateInjectionMembers(params InjectionMember[] members)
		{
			injectionMemberFactory.Setup(m => m.CreateInjectionMembers(It.IsAny<TypeMapping>()))
											  .Returns(members);
		}
	}
}
