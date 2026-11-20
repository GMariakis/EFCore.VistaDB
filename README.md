# EFCore.VistaDB

An Entity Framework Core provider for the VistaDB embedded database. It integrates VistaDB with EF Core through `UseVistaDB`.

## Compatibility

Provider version 11.0.0 targets .NET 11 and Entity Framework Core 11. It uses the VistaDB 6.6.2 engine.

## VistaDB license required

You need a valid VistaDB license to use this provider to develop applications with VistaDB. Obtain VistaDB 6 from Gibraltar Software and restore the `VistaDB.6` engine package from the NuGet source installed with your licensed copy or from a private feed authorized under your VistaDB license.

The provider package references the engine as a separate dependency. See the vendor’s terms for details. [VistaDB licensing](https://doc.vistadb.com/VistaDB_Licensing.html) · [VistaDB NuGet package setup](https://doc.vistadb.com/DevelopersReference_NuGetPackagesforVistaDB.html)
