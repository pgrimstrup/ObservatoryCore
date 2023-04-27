using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Bridge
{
    internal class DockingPads
    {
        static Dictionary<int, string> PadLocations = new Dictionary<int, string>();

        static DockingPads()
        {
            PadLocations.Add(1, "6 o'clock near");
            PadLocations.Add(2, "6 o'clock mid-near");
            PadLocations.Add(3, "6 o'clock mid-far");
            PadLocations.Add(4, "6 o'clock far");

            PadLocations.Add(5, "7 o'clock near");
            PadLocations.Add(6, "7 o'clock mid-near");
            PadLocations.Add(7, "7 o'clock mid");
            PadLocations.Add(8, "7 o'clock far");

            PadLocations.Add(9, "8 o'clock near");
            PadLocations.Add(10, "8 o'clock far");

            PadLocations.Add(11, "9 o'clock near");
            PadLocations.Add(12, "9 o'clock mid-near");
            PadLocations.Add(13, "9 o'clock mid");
            PadLocations.Add(14, "9 o'clock mid-far");
            PadLocations.Add(15, "9 o'clock far");

            PadLocations.Add(16, "10 o'clock near");
            PadLocations.Add(17, "10 o'clock mid-near");
            PadLocations.Add(18, "10 o'clock mid-far");
            PadLocations.Add(19, "10 o'clock far");

            PadLocations.Add(20, "11 o'clock near");
            PadLocations.Add(21, "11 o'clock mid-near");
            PadLocations.Add(22, "11 o'clock mid");
            PadLocations.Add(23, "11 o'clock far");

            PadLocations.Add(24, "12 o'clock near");
            PadLocations.Add(25, "12 o'clock far");

            PadLocations.Add(26, "1 o'clock near");
            PadLocations.Add(27, "1 o'clock mid-near");
            PadLocations.Add(28, "1 o'clock mid");
            PadLocations.Add(29, "1 o'clock mid-far");
            PadLocations.Add(30, "1 o'clock far");

            PadLocations.Add(31, "2 o'clock near");
            PadLocations.Add(32, "2 o'clock mid-near");
            PadLocations.Add(33, "2 o'clock mid-far");
            PadLocations.Add(34, "2 o'clock far");

            PadLocations.Add(35, "3 o'clock near");
            PadLocations.Add(36, "3 o'clock mid-near");
            PadLocations.Add(37, "3 o'clock mid");
            PadLocations.Add(38, "3 o'clock far");

            PadLocations.Add(39, "4 o'clock near");
            PadLocations.Add(40, "4 o'clock far");

            PadLocations.Add(41, "5 o'clock near");
            PadLocations.Add(42, "5 o'clock mid-near");
            PadLocations.Add(43, "5 o'clock mid");
            PadLocations.Add(44, "5 o'clock mid-far");
            PadLocations.Add(45, "5 o'clock far");
        }

    }
}
