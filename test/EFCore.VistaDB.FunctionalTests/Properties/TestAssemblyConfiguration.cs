// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// VistaDB's static engine state (VistaDBEngine.Connections, IntraProcessLockManager) is not
// thread-safe. xUnit's default test parallelization causes intermittent "Cannot open data storage
// or file" errors mid-run and corrupts the SharedStore lifecycle — Visual Studio Test Explorer
// reports ~3,000 phantom failures under parallel execution while a single-threaded CLI run shows
// the real failure count (~15). Force serial execution at the assembly level so VS Test Explorer
// (which doesn't accept `dotnet test`'s `-- xUnit.MaxParallelThreads=1` argument) gets the same
// behavior. xunit.runner.json in the project root configures the same thing for runners that
// read JSON config; this attribute is the belt-and-suspenders backstop.

[assembly: CollectionBehavior(DisableTestParallelization = true)]
