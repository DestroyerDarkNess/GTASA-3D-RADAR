using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Samp.Radar.SDK
{
    public static class API
    {
        // HUD / Radar -----------------------------------------------------------
        public static unsafe void SetHudState(bool enable)
            => *(byte*)Offsets.R(Offsets.RAD_STATE_ADDR) = (byte)(enable ? 1 : 0);

        public static unsafe void SetVanillaRadarVisible(bool visible)
            => *(byte*)Offsets.R(Offsets.RADAR_FLAG) = (byte)(visible ? 1 : 0);

        public static unsafe bool IsVanillaRadarVisible()
            => *(byte*)Offsets.R(Offsets.RADAR_FLAG) != 0;

        public static unsafe float GetRadarRotation()
            => *(float*)Offsets.R(Offsets.RAD_ROT_ADDR);

        // Player position -------------------------------------------------------
        public static unsafe bool TryGetPlayerPosition(out Vector3 pos)
        {
            uint ped = *(uint*)Offsets.R(Offsets.PED_PTR_ADDR);
            if (ped == 0) { pos = default; return false; }

            uint matrix = *(uint*)(ped + Offsets.MAT_OFFSET);
            if (matrix == 0) { pos = default; return false; }

            float x = *(float*)(matrix + Offsets.X_OFFSET);
            float y = *(float*)(matrix + Offsets.Y_OFFSET);
            float z = *(float*)(matrix + Offsets.Z_OFFSET);

            pos = new Vector3(x, y, z);
            return true;
        }

        // Misc ------------------------------------------------------------------
        public static unsafe bool IsEscMenuOpen()
            => *(byte*)Offsets.R(Offsets.ADDR_PLAYER_IN_MENU) != 0;

        public static unsafe int GetPlayerInteriorId()
            => *(int*)Offsets.R(Offsets.ADDR_CURRENT_INTERIOR);

        public static bool IsPlayerInsideInterior()
            => GetPlayerInteriorId() > 0;
    }
}