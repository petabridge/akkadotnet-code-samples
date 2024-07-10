// -----------------------------------------------------------------------
//  <copyright file="AutofacModule.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2024 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Autofac;

namespace AutoFacIntegration;

public class AutofacModule
{
    public string TestString { get; } = nameof(TestString);
}