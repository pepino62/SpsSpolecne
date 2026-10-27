using JisResources.Sources;

namespace SpsSpolecne
{
    /// <summary>
    /// Evidence zmen - sikana (tabulka d9 / dz9, VFP _SIK).
    /// <code>
    /// if (!SikanZapis.Zapis( SikanZapis.Rezervace, SikanZapis.PrijalPlatbu, F201 ))
    ///     throw new JisException();
    /// </code>
    /// </summary>
    public static class SikanZapis
    {
        // co se meni (f902)
        public const string Rezervace = "D2";
        public const string Zaloha = "D4";
        public const string Zapujcka = "D8";        // jen SpsLite

        // udalost (f903, popisy v t1 sekce F903)
        public const string Zalozil = "01";
        public const string Editoval = "02";
        public const string ZrusilRezervaci = "03";
        public const string ObnovilRezervaci = "04";
        public const string UlozilZalohu = "05";
        public const string PrijalPlatbu = "06";
        public const string ZrusilPlatbu = "07";
        public const string SmazalCenu = "08";
        public const string SmazalZalohu = "09";     // neni v t1 F903 (VFP _SIK 'D4','09')

        /// <summary>
        /// zapise udalost (uzivatel a cas aktualni); klic - f201 rezervace, f801 zapujcky ...
        /// <br>Vraci false pri chybe (chyba je ohlasena a zapsana do protokolu).</br>
        /// </summary>
        public static bool Zapis(string tabulka, string udalost, object klic)
            => SpravaDat.Exec( $"INSERT INTO {Tab.D9} (f901, f902, f903, f904, f905, f906) VALUES ('0', @f902, @f903, @f904, CURRENT_TIMESTAMP(), @ukey)",
                new { f902 = tabulka, f903 = udalost, f904 = $"{klic}", ukey = Goa.Ukey ?? "" } );
    }
}
