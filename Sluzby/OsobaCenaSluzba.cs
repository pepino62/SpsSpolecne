using JisResources;
using JisResources.Sources;
using System;

namespace SpsSpolecne
{
    /// <summary>
    /// Osoby rezervace (d6 / dz6) - bez dialogu. Dotaz na chybejici doklad dela obrazovka (ChybiDoklad).
    /// <br>Pri chybe vyhodi JisException: s textem = upozorneni pro uzivatele, bez textu = chyba uz byla ohlasena (databaze).</br>
    /// </summary>
    public static class OsobaSluzba
    {
        static void Exec(string sql, object par)
        {
            if (!SpravaDat.Exec( sql, par ))
                throw new JisException();
        }

        /// <summary>
        /// povinne udaje osoby (VFP SPORTR6_SAVEO / SPORTZ6_SAVEO)
        /// </summary>
        public static void Kontrola(IOsoba o)
        {
            if (string.IsNullOrWhiteSpace( o.F602 ))
                throw new JisException( "Není zadané PŘÍJMENÍ !" );
            if (string.IsNullOrWhiteSpace( o.F603 ))
                throw new JisException( "Není zadané JMÉNO !" );
            if (o.F604 == null || o.F604 < new DateTime( 1900, 1, 1 ))
                throw new JisException( "Není zadané DATUM NAROZENÍ !" );
            if (string.IsNullOrWhiteSpace( o.F606 ))
                throw new JisException( "Není zadaná ADRESA !" );
            if (o.F609 < 1 || o.F609 > 110)
                throw new JisException( "Chybně zadané DATUM NAROZENÍ !" );
            if (o.F609 >= 18 && (o.F608 + o.F610 + o.F611 + o.F612 == 0))
                throw new JisException( "Nelze uložit - označte jednu z položek :\n\n   - zaplatí rekreační poplatek\n   - ZTP / P\n   - obyvatel Žďáru nad Sázavou\n   - student 18-26 let" );
        }

        /// <summary>
        /// osoba od 15 let bez dokladu - obrazovka se zepta, zda pokracovat
        /// </summary>
        public static bool ChybiDoklad(IOsoba o)
            => o.F609 >= 15 && string.IsNullOrWhiteSpace( o.F605 );

        /// <summary>
        /// ulozi osobu - nova (F601 prazdne) dostane klic az po uspesnem zapisu
        /// </summary>
        public static void Ulozit(IOsoba o)
        {
            Kontrola( o );
            var udaje = new
            {
                f201 = o.F201 ?? "", f602 = o.F602 ?? "", f603 = o.F603 ?? "", f604 = o.F604?.Date, f605 = o.F605 ?? "", f606 = o.F606 ?? "", f607 = o.F607 ?? "",
                f608 = o.F608, f609 = o.F609, f610 = o.F610, f611 = o.F611, f612 = o.F612, ukey = Goa.Ukey ?? "", f601 = o.F601 ?? ""
            };

            if (!string.IsNullOrWhiteSpace( o.F601 ))
            {
                Exec( $@"UPDATE {Tab.D6} d6 SET f0 = ' ', f201 = @f201, f602 = @f602, f603 = @f603, f604 = @f604, f605 = @f605, f606 = @f606, f607 = @f607,
                    f608 = @f608, f609 = @f609, f610 = @f610, f611 = @f611, f612 = @f612, f02 = @ukey, f03 = NOW()
                    WHERE d6.f601 = @f601", udaje );
                return;
            }

            string f601;
            using (var tr = SpravaDat.Transakce())     // klic a zaznam spolecne
            {
                f601 = Cislovani.NovyKlic( "F601" );
                Exec( $@"INSERT INTO {Tab.D6} (f0, f601, f201, f602, f603, f604, f605, f606, f607, f608, f609, f610, f611, f612, f02, f03)
                    VALUES (' ', @f601, @f201, @f602, @f603, @f604, @f605, @f606, @f607, @f608, @f609, @f610, @f611, @f612, @ukey, NOW())",
                    new
                    {
                        f601, udaje.f201, udaje.f602, udaje.f603, udaje.f604, udaje.f605, udaje.f606, udaje.f607,
                        udaje.f608, udaje.f609, udaje.f610, udaje.f611, udaje.f612, udaje.ukey
                    } );
                tr.Commit();
            }
            o.F601 = f601;
        }

        /// <summary>
        /// vymaze osobu z rezervace
        /// </summary>
        public static void Smazat(string f601)
        {
            if (string.IsNullOrWhiteSpace( f601 ))
                throw new JisException( "Není vybrána osoba" );
            Exec( $"DELETE FROM {Tab.D6} WHERE f601 = @f601", new { f601 } );
        }
    }


    /// <summary>
    /// Polozky ceny rezervace (d5 / dz5) - bez dialogu, zapisy v transakci.
    /// <br>Pri chybe vyhodi JisException: s textem = upozorneni pro uzivatele, bez textu = chyba uz byla ohlasena (databaze).</br>
    /// </summary>
    public static class CenaSluzba
    {
        static void Exec(string sql, object par)
        {
            if (!SpravaDat.Exec( sql, par ))
                throw new JisException();
        }

        static string SqlInsert => $@"INSERT INTO {Tab.D5} (f0, f501, f201, ft501, f502, f503, f504, f505, f506, f507, f02, f03)
            VALUES (' ', @f501, @f201, @ft501, @f502, @f503, @f504, @f505, @f506, @f507, @ukey, NOW())";

        /// <summary>
        /// ulozi polozku ceny (VFP SPORTR6_SAVEC / SPORTZ6_SAVEC) - nova polozka pres sezonu i mimosezonu = dva radky;
        /// nova dostane klic az po uspesnem zapisu
        /// <br>kemp: SPZ povinna u polozky typu S, mimosezona ma stejnou SPZ; penzion: polozka povinna i pri oprave, SPZ mimosezony F507_1</br>
        /// </summary>
        public static void Ulozit(ICena c, IRezervace d)
        {
            if (string.IsNullOrWhiteSpace( c.FT501 ) && (d.F217 != "04" || (Tab.Penzion && !string.IsNullOrWhiteSpace( c.F501 ))))
                throw new JisException( "Není vybrána POLOŽKA !" );

            if (!string.IsNullOrWhiteSpace( c.F501 ))
            {
                if (!Tab.Penzion && string.IsNullOrWhiteSpace( c.F507 ) && c.FT508 == "S")
                    throw new JisException( "Není zadana SPZ (karavanu,auta) !" );
                Exec( $@"UPDATE {Tab.D5} d5 SET f0 = ' ', f201 = @f201, f502 = @f502, f503 = @f503, f504 = @f504, f505 = @f505, f506 = @f506, f507 = @f507, f02 = @ukey, f03 = NOW()
                    WHERE d5.f501 = @f501",
                    new { f201 = c.F201 ?? "", f502 = c.F502, f503 = c.F503, f504 = c.F504, f505 = c.F505, f506 = c.F506 ?? "", f507 = c.F507 ?? "", ukey = Goa.Ukey ?? "", f501 = c.F501 } );
                return;
            }

            string f501;
            using (var tr = SpravaDat.Transakce())     // sezona i mimosezona spolecne
            {
                f501 = Cislovani.NovyKlic( "F501" );
                Exec( SqlInsert, new { f501, f201 = c.F201 ?? "", ft501 = c.FT501 ?? "", f502 = c.F502, f503 = c.F503, f504 = c.F504, f505 = c.F505, f506 = c.F506 ?? "", f507 = c.F507 ?? "", ukey = Goa.Ukey ?? "" } );

                if (!string.IsNullOrWhiteSpace( c.F506_1 ))    // cena pres sezonu i mimosezonu - dalsi radek (mimosezona)
                    Exec( SqlInsert, new
                    {
                        f501 = Cislovani.NovyKlic( "F501" ), f201 = c.F201 ?? "", ft501 = c.FT501 ?? "", f502 = c.F502_1, f503 = c.F503_1, f504 = c.F504, f505 = c.F505_1, f506 = c.F506_1 ?? "",
                        f507 = (Tab.Penzion ? c.F507_1 : c.F507) ?? "", ukey = Goa.Ukey ?? ""
                    } );

                tr.Commit();
            }
            c.F501 = f501;
        }

        /// <summary>
        /// vymaze polozku ceny
        /// </summary>
        public static void Smazat(string f501)
        {
            if (string.IsNullOrWhiteSpace( f501 ))
                throw new JisException( "Není vybrána položka ceny" );
            Exec( $"DELETE FROM {Tab.D5} WHERE f501 = @f501", new { f501 } );
        }

        /// <summary>
        /// vymaze vsechny polozky ceny rezervace (cena jeste neni evidovana)
        /// </summary>
        public static void SmazatVse(string f201)
        {
            if (string.IsNullOrWhiteSpace( f201 ))
                throw new JisException( "Rezervace není uložená" );
            if (SpravaDat.Scalar( $"SELECT 1 FROM {Tab.D2} d2 WHERE d2.f201 = @f201 AND d2.f213 IS NOT NULL", new { f201 } ) != null)
                throw new JisException( "NELZE - cena rezervace už je zaevidovaná (zaplacená) !" );
            Exec( $"DELETE FROM {Tab.D5} WHERE f201 = @f201", new { f201 } );
        }
    }
}
