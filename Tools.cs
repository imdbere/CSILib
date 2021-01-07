using System;
using System.Numerics;

namespace CSIUserTool
{
    public static class Tools
    {
        public static String ToMathString(this Complex nr)
        {
            return $"({nr.Real}{nr.Imaginary.ToString("+0;-#")}j)";
        }
    }
}
