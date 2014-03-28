// PkgCmdID.cs
// MUST match PkgCmdID.h

namespace Leem.Testify
{
    internal static class PkgCmdIDList
    {
        public const uint cmdidTestCommand = 0x100;
        public const uint cmdidTestTool = 0x101;
        public const uint cmdidSolutionTests = 0x102;
        public const uint cmdidProjectTests = 0x103;

        // command IDs for all numbered bookmarks
        // just to keep the values in sync I opted for a multiple of 5
        public const uint cmdBookmark0 = 0x0005;

        public const uint cmdBookmark1 = 0x0015;
        public const uint cmdBookmark2 = 0x0025;
        public const uint cmdBookmark3 = 0x0035;
        public const uint cmdBookmark4 = 0x0045;
        public const uint cmdBookmark5 = 0x0055;
        public const uint cmdBookmark6 = 0x0065;
        public const uint cmdBookmark7 = 0x0075;
        public const uint cmdBookmark8 = 0x0085;
        public const uint cmdBookmark9 = 0x0095;

        // command ID for Clear Bookmarks command
        public const uint cmdClearBookmarks = 0x0105;
    };
}