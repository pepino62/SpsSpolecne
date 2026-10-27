using JisResources;
using JisResources.Sources;

namespace SpsSpolecne
{
    /// <summary>
    /// Zaloha rezervace (d4 / dz4) - bez dialogu, zaloha a sikan v jedne transakci.
    /// <br>Pri chybe vyhodi JisException: s textem = upozorneni pro uzivatele, bez textu = chyba uz byla ohlasena (databaze).</br>
    /// </summary>
    public static class ZalohaSluzba
    {
        static void Exec(string sql, object par)
        {
            if (!SpravaDat.Exec( sql, par ))
                throw new JisException();
        }

        /// <summary>
        /// kontrola pred ulozenim zalohy (VFP SPORTR6_SAVEZ / SPORTZ6_SAVEZ)
        /// </summary>
        public static void Kontrola(IZaloha z, IRezervace d)
        {
            if (string.IsNullOrWhiteSpace( d.F201 ))
                throw new JisException( "Rezervace není uložená" );
            if (d.F213 != null)
                throw new JisException( "NELZE uložit zálohu - Rezervace byla zaplacena" );
            if (z.F404 == 0)
                throw new JisException( "Záloha nesmí být nulová !" );
            if (string.IsNullOrWhiteSpace( z.F401 ) && z.F404 < 0)
                throw new JisException( "Záloha nesmí být záporná !" );
            if (z.F404 > 0 && z.F405 == 0 && string.IsNullOrWhiteSpace( d.F304 ))
                throw new JisException( "Nelze uložit - zadána ZÁLOHA, nutno zadat i adresu bydliště !" );
            if (z.F404 > 0 && z.F405 == 1 && string.IsNullOrWhiteSpace( d.F218 ))
                throw new JisException( "Nelze uložit - zadána ZÁLOHA, nutno zadat i fakturační adresu !" );
        }

        /// <summary>
        /// ulozi zalohu rezervace - nova (F401 prazdne) dostane klic az po uspesnem zapisu; sikan D2/05
        /// </summary>
        public static void Ulozit(IZaloha z, IRezervace d)
        {
            Kontrola( z, d );
            bool nova = string.IsNullOrWhiteSpace( z.F401 );
            string f401 = z.F401;

            using (var tr = SpravaDat.Transakce())     // klic, zaloha a sikan spolecne
            {
                var udaje = new
                {
                    f201 = nova ? d.F201 : (z.F201 ?? ""), f402 = z.F402?.Date, f403 = z.F403?.Date, f404 = z.F404, f405 = z.F405,
                    f406 = z.F406 ?? "", f407 = z.F407 ?? "", ukey = Goa.Ukey ?? ""
                };

                if (nova)
                {
                    f401 = Cislovani.NovyKlic( "F401" );
                    Exec( $@"INSERT INTO {Tab.D4} (f0, f401, f201, f402, f403, f404, f405, f406, f407, f02, f03)
                        VALUES (' ', @f401, @f201, @f402, @f403, @f404, @f405, @f406, @f407, @ukey, NOW())",
                        new { f401, udaje.f201, udaje.f402, udaje.f403, udaje.f404, udaje.f405, udaje.f406, udaje.f407, udaje.ukey } );
                }
                else
                    Exec( $@"UPDATE {Tab.D4} d4 SET f0 = ' ', f201 = @f201, f402 = @f402, f403 = @f403, f404 = @f404, f405 = @f405, f406 = @f406, f407 = @f407,
                            f02 = @ukey, f03 = NOW()
                        WHERE d4.f401 = @f401",
                        new { udaje.f201, udaje.f402, udaje.f403, udaje.f404, udaje.f405, udaje.f406, udaje.f407, udaje.ukey, f401 } );

                // sikan - ulozeni zalohy (VFP _SIK 'D2','05')
                if (!SikanZapis.Zapis( SikanZapis.Rezervace, SikanZapis.UlozilZalohu, d.F201 ))
                    throw new JisException();

                tr.Commit();
            }
            z.F401 = f401;
        }

        /// <summary>
        /// zneplatni zalohu (f0 = '1') - jen u nezaplacene rezervace; sikan D4/09
        /// </summary>
        public static void Smazat(IZaloha z, IRezervace d)
        {
            if (string.IsNullOrWhiteSpace( z.F401 ))
                throw new JisException( "Záloha není uložená" );
            if (d.F213 != null)
                throw new JisException( "NELZE smazat zálohu - Rezervace byla zaplacena" );

            using (var tr = SpravaDat.Transakce())
            {
                Exec( $"UPDATE {Tab.D4} d4 SET f0 = '1', f02 = @ukey, f03 = NOW() WHERE d4.f401 = @f401", new { ukey = Goa.Ukey ?? "", f401 = z.F401 } );

                // sikan - smazani zalohy (VFP _SIK 'D4','09')
                if (!SikanZapis.Zapis( SikanZapis.Zaloha, SikanZapis.SmazalZalohu, d.F201 ))
                    throw new JisException();

                tr.Commit();
            }
        }
    }
}
