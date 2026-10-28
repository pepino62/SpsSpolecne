using JisResources;
using JisResources.Sources;

namespace SpsSpolecne
{
    /// <summary>
    /// uzivatel pro editaci na obrazovce Sprava
    /// </summary>
    public class UzivatelEdit
    {
        public string FUKEY { get; set; } = "";
        public string FNAME { get; set; } = "";
        public string FUSER { get; set; } = "";
        public string FPASSWORD { get; set; } = "";
        public string FROLE { get; set; } = "003,006";
        public string FPHONE { get; set; } = "";
        public string FEMAIL { get; set; } = "@";       // prazdny e-mail = "@" (EmailBehavior)
        public string FCOMMENT { get; set; } = "";
        public string Nadpis => string.IsNullOrEmpty( FUKEY ) ? "NOVÝ UŽIVATEL" : $"UŽIVATEL  {FUKEY}";
    }

    /// <summary>
    /// text e-mailu (t1 EMAIL) pro editaci na obrazovce Sprava
    /// </summary>
    public class TextEmailuEdit
    {
        public string FITEM { get; set; } = "";
        public string FSORT { get; set; } = "";
        public string FPROMPT { get; set; } = "";
        public string FDATA { get; set; } = "";
        public string Nadpis => string.IsNullOrEmpty( FITEM ) ? "NOVÝ TEXT" : $"TEXT  {FITEM}";
    }


    /// <summary>
    /// Uzivatele (users - spolecna tabulka obou aplikaci) - bez dialogu, zapisy v transakci.
    /// <br>Pri chybe vyhodi JisException: s textem = upozorneni pro uzivatele, bez textu = chyba uz byla ohlasena (databaze).</br>
    /// </summary>
    public static class UzivatelSluzba
    {
        static string Trim(string s) => (s ?? "").Trim();

        static void Exec(string sql, object par)
        {
            if (!SpravaDat.Exec( sql, par ))
                throw new JisException();
        }

        /// <summary>
        /// ulozi uzivatele (VFP TAB05_1SAVE) - novy (FUKEY prazdne) dostane klic az po uspesnem zapisu; vraci klic
        /// </summary>
        public static string Ulozit(UzivatelEdit u)
        {
            if (string.IsNullOrWhiteSpace( u.FNAME ))
                throw new JisException( "Nezadáno jméno" );
            if (string.IsNullOrWhiteSpace( u.FUSER ))
                throw new JisException( "Nezadáno přihlašovací jméno" );

            if (SpravaDat.Scalar( "SELECT fukey FROM users WHERE fuser = @fuser AND fukey != @fukey LIMIT 1", new { fuser = Trim( u.FUSER ), fukey = Trim( u.FUKEY ) } ) != null)
                throw new JisException( "Uživatel s tímto přihlašovacím jménem již existuje" );
            if (SpravaDat.Scalar( "SELECT fukey FROM users WHERE fname = @fname AND fukey != @fukey LIMIT 1", new { fname = Trim( u.FNAME ), fukey = Trim( u.FUKEY ) } ) != null)
                throw new JisException( "Uživatel s tímto jménem již existuje" );

            var hodnoty = new
            {
                fuser = Trim( u.FUSER ),
                fname = Trim( u.FNAME ),
                frole = Trim( u.FROLE ),
                fpassword = Trim( u.FPASSWORD ),
                fphone = Trim( u.FPHONE ),
                femail = EmailBehavior.ProUlozeni( u.FEMAIL ),
                fcomment = (u.FCOMMENT ?? "").TrimEnd(),
                fukey = Trim( u.FUKEY )
            };

            if (!string.IsNullOrWhiteSpace( u.FUKEY ))
            {
                Exec( @"UPDATE users SET fuser = @fuser, fname = @fname, fform = 'SPORTX', foscis = 'Z00', funit = 'EXT',
                    frole = @frole, fpassword = @fpassword, fphone = @fphone, femail = @femail, fcomment = @fcomment
                    WHERE fukey = @fukey", hodnoty );
                return hodnoty.fukey;
            }

            string fukey;
            using (var tr = SpravaDat.Transakce())     // klic a zaznam spolecne - nezustane prazdny uzivatel
            {
                fukey = Cislovani.DalsiKlic36( "users", "fukey", 2, co: "uživatele" );
                Exec( @"INSERT INTO users (fukey, f0, f1, fuser, fname, foscis, funit, fform, frole, fphone, femail, fcomment, fpassword)
                    VALUES (@fukey, '', '', @fuser, @fname, 'Z00', 'EXT', 'SPORTX', @frole, @fphone, @femail, @fcomment, @fpassword)",
                    new { fukey, hodnoty.fuser, hodnoty.fname, hodnoty.frole, hodnoty.fphone, hodnoty.femail, hodnoty.fcomment, hodnoty.fpassword } );
                tr.Commit();
            }
            return fukey;
        }

        /// <summary>
        /// zneplatni (f0 = '1') nebo obnovi uzivatele (VFP TAB05_1DEL) - prihlaseneho uzivatele nelze zneplatnit
        /// </summary>
        public static void Platnost(string fukey, bool zneplatnit)
        {
            if (string.IsNullOrWhiteSpace( fukey ))
                throw new JisException( "Vyberte uživatele" );
            if (zneplatnit && Trim( fukey ) == Trim( Goa.Ukey ))
                throw new JisException( "Nelze zneplatnit právě přihlášeného uživatele" );

            Exec( "UPDATE users SET f0 = @f0 WHERE fukey = @fukey", new { f0 = zneplatnit ? "1" : "", fukey = Trim( fukey ) } );
        }
    }


    /// <summary>
    /// Texty e-mailu (t1 sekce EMAIL - spolecna tabulka obou aplikaci) - bez dialogu, zapisy v transakci.
    /// <br>Pri chybe vyhodi JisException: s textem = upozorneni pro uzivatele, bez textu = chyba uz byla ohlasena (databaze).</br>
    /// </summary>
    public static class TextEmailuSluzba
    {
        static string Trim(string s) => (s ?? "").Trim();

        // klic polozky t1 ma pevne 2 znaky
        static string Klic2(string s) => Trim( s ).PadRight( 2 ).Substring( 0, 2 );

        static void Exec(string sql, object par)
        {
            if (!SpravaDat.Exec( sql, par ))
                throw new JisException();
        }

        /// <summary>
        /// ulozi text e-mailu (VFP TAB06_1SAVE) - novy (FITEM prazdne) dostane klic az po uspesnem zapisu; vraci polozku
        /// <br>radky textu se ukladaji oddelene CR (jako ve VFP)</br>
        /// </summary>
        public static string Ulozit(TextEmailuEdit t)
        {
            if (string.IsNullOrWhiteSpace( t.FPROMPT ))
                throw new JisException( "Nezadán název textu" );
            if (string.IsNullOrWhiteSpace( t.FSORT ))
                throw new JisException( "Nezadáno pořadí textu" );

            string fdata = (t.FDATA ?? "").Replace( "\r\n", "\r" ).Replace( "\n", "\r" ).TrimEnd();

            if (!string.IsNullOrEmpty( t.FITEM ))
            {
                Exec( "UPDATE t1 SET fprompt = @fprompt, fsort = @fsort, fdata = @fdata WHERE fsection = 'EMAIL' AND fitem = @fitem",
                    new { fprompt = Trim( t.FPROMPT ), fsort = Trim( t.FSORT ), fdata, fitem = Klic2( t.FITEM ) } );
                return t.FITEM;
            }

            string fitem;
            using (var tr = SpravaDat.Transakce())
            {
                fitem = Cislovani.DalsiKlic36( "t1", "fitem", 2, "fsection = 'EMAIL' AND f0 != '*'", "textu" );
                Exec( @"INSERT INTO t1 (fsection, fitem, f0, f1, fsort, fcode, fprompt, fa201, fdata, frole, fcomment)
                    VALUES ('EMAIL', @fitem, '', '', @fsort, '', @fprompt, '', @fdata, '', '')",
                    new { fitem, fsort = Trim( t.FSORT ), fprompt = Trim( t.FPROMPT ), fdata } );
                tr.Commit();
            }
            return fitem;
        }

        /// <summary>
        /// zneplatni nebo obnovi text e-mailu (VFP TAB06_1DEL)
        /// </summary>
        public static void Platnost(string fitem, bool zneplatnit)
        {
            if (string.IsNullOrEmpty( fitem ))
                throw new JisException( "Vyberte text" );

            Exec( "UPDATE t1 SET f0 = @f0 WHERE fsection = 'EMAIL' AND fitem = @fitem", new { f0 = zneplatnit ? "1" : "", fitem = Klic2( fitem ) } );
        }
    }
}
