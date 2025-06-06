using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Samp.Radar.SDK
{
    internal static unsafe class Offsets
    {
        // === STATIC OFFSETS
        public const int RAD_STATE_ADDR = 0xBA6769;

        public const int RADAR_FLAG = 0xBAA420;
        public const int RAD_ROT_ADDR = 0xBA8310;

        public const int PED_PTR_ADDR = 0xB6F5F0;
        public const int MAT_OFFSET = 0x14;
        public const int X_OFFSET = 0x30;
        public const int Y_OFFSET = 0x34;
        public const int Z_OFFSET = 0x38;

        public const int ADDR_PLAYER_IN_MENU = 0xBA67A4;
        public const int ADDR_CURRENT_INTERIOR = 0xA4ACE8;

        // ---------------------------------------------------------------------
        private static readonly int _delta;   // signed 32-bit; realBase - preferredBase

        static Offsets()
        {
            IntPtr realBase = Process.GetCurrentProcess().MainModule.BaseAddress;

            // --- Read PE header in memory to get the preferred ImageBase -----
            byte* pBase = (byte*)realBase;
            int peOffset = *(int*)(pBase + 0x3C);          // DOS->e_lfanew
            bool pe32Plus = (*(ushort*)(pBase + peOffset + 0x18) == 0x20B);

            uint preferredBase =
                pe32Plus
                    ? *(uint*)(pBase + peOffset + 0x18 + 0x08)  // PE32+: 8 bytes after Magic
                    : *(uint*)(pBase + peOffset + 0x34);        // PE32 : 0x34 after PE header

            _delta = (int)realBase.ToInt32() - (int)preferredBase;
        }

        /// <summary>
        /// Translates an absolute GTA offset (based on preferred ImageBase)
        /// into the *actual* address for this run, compensating ASLR/rebases.
        /// </summary>
        public static IntPtr R(int absoluteOffset) => (IntPtr)(absoluteOffset + _delta);
    }
}