﻿#r @"C:\Users\isaac\Source\Repos\Lydian.Unity.Automapper\packages\Unity.2.1.505.2\lib\NET35\Microsoft.Practices.Unity.dll"
#r @"C:\Users\isaac\Source\Repos\Lydian.Unity.Automapper\packages\Unity.Interception.2.1.505.2\lib\NET35\Microsoft.Practices.Unity.Interception.dll"

#load "Types.fs"
#load "AutomapperConfig.fs"
#load "Attributes.fs"
open Lydian.Unity.Automapper
#load "ConfigurationBuilder.fs"
open Lydian.Unity.Automapper
#load "TypeMappingFactory.fs"
#load "ContainerRegistrar.fs"
open Lydian.Unity.Automapper
#load "MappingController.fs"
open Lydian.Unity.Automapper.MappingController
#load "Mapper.fs"
open Lydian.Unity.Automapper
open Microsoft.Practices.Unity
open System
open System.Reflection

type IInterface = 
    interface
    end
type InterfaceImplementation() = 
    interface IInterface
type InterfaceImplementationTwo() = 
    interface IInterface

let container = new UnityContainer()
let types = [ typeof<IInterface>; typeof<InterfaceImplementation>; typeof<InterfaceImplementationTwo> ]

Mapper.AutomapTypes(container, { Behaviors = MappingBehaviors.CollectionRegistration }, types)

container.Registrations |> Seq.map(fun r -> sprintf "%s -> %s (%s)" r.RegisteredType.FullName r.MappedToType.FullName r.Name)