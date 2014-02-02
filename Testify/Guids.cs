// Guids.cs
// MUST match guids.h
using System;

namespace Leem.Testify
{
    static class GuidList
    {
        public const string guidTestifyPkgString = "a9bce903-55a5-4a94-979c-2d4e5bb7a93e";
        public const string guidTestifyCmdSetString = "91732380-9d7a-46ea-9128-757bdaee759f";
        public const string guidToolWindowPersistanceString = "36c4a332-1b9b-49ce-9e45-da8bd399092c";

        public static readonly Guid guidTestifyCmdSet = new Guid(guidTestifyCmdSetString);
    };
}