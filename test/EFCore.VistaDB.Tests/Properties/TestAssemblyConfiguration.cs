// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Match the FunctionalTests project: serialize tests for consistency and to avoid any VistaDB
// engine static-state issues if unit tests ever touch the engine. See companion
// xunit.runner.json in the project root.

[assembly: CollectionBehavior(DisableTestParallelization = true)]
