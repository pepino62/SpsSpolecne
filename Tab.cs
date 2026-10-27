using System;

namespace SpsSpolecne
{
    /// <summary>
    /// Nazvy tabulek podle aplikace - SpsLite (kemp) d2, d4 ..., SPSZS (penzion) dz2, dz4 ...
    /// <br>Nastavit pri startu aplikace (Tab.Init), v SQL pouzivat alias, aby zbytek dotazu byl pro obe aplikace stejny:</br>
    /// <code>
    /// SpravaDat.Exec( $"UPDATE {Tab.D4} d4 SET f0 = '1' WHERE d4.f401 = @f401", new { f401 } );
    /// </code>
    /// </summary>
    public static class Tab
    {
        static string _predpona;

        /// <summary>
        /// predpona tabulek - "d" SpsLite, "dz" SPSZS
        /// </summary>
        public static void Init(string predpona)
        {
            if (predpona != "d" && predpona != "dz")
                throw new ArgumentException( $"Neznámá předpona tabulek '{predpona}' (d nebo dz)", nameof( predpona ) );
            _predpona = predpona;
        }

        static string Nazev(string cislo)
            => (_predpona ?? throw new InvalidOperationException( "Tab.Init nebyl zavolán" )) + cislo;

        public static string D2 => Nazev( "2" );    // rezervace
        public static string D4 => Nazev( "4" );    // zalohy
        public static string D5 => Nazev( "5" );    // polozky ceny
        public static string D6 => Nazev( "6" );    // osoby
        public static string D9 => Nazev( "9" );    // sikana (evidence zmen)
    }
}
