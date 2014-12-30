// Guids.cs
// MUST match guids.h

using System;

namespace Leem.Testify
{
    internal static class GuidList
    {
        public const string GuidTestifyPkgString = "a9bce903-55a5-4a94-979c-2d4e5bb7a93e";
        private const string GuidTestifyCmdSetString = "91732380-9d7a-46ea-9128-757bdaee759f";
        public const string GuidTestifyCoverageWindowPersistanceString = "36c4a332-1b9b-49ce-9e45-da8bd399092c";

        // guid for the command set
        private const string GuidNumberedBookmarksCmdSetString = "c74fc9bd-32e1-4135-bddd-779021cc3630";
        public static readonly Guid GuidTestifyCmdSet = new Guid(GuidTestifyCmdSetString);

        // create a new Guid object with the guid string for the command string
        public static readonly Guid GuidNumberedBookmarksCmdSet = new Guid(GuidNumberedBookmarksCmdSetString);
    };
}