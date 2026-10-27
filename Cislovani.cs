using JisResources;
using JisResources.Sources;

namespace SpsSpolecne
{
    /// <summary>
    /// Klice a cislovani dokladu z tabulky DA / DZA (rady se zakladaji automaticky, generovani je bezpecne pri soubeznem ukladani).
    /// <br>Pri chybe vyhodi JisException - volat uvnitr try / transakce.</br>
    /// </summary>
    public static class Cislovani
    {
        // rady cisel - kazdy rok nova rada (klic rady = nazev + rok)
        public static string CisloRezervace => Tab.Penzion ? "F202Z" : "F202R";
        public const string CisloDokladu = "F220";
        public const string CisloDokladuZapujcky = "F806";     // jen SpsLite

        /// <summary>
        /// dalsi cislo v rade pro dany rok, napr. Cislovani.NoveCislo( Cislovani.CisloDokladu, F203.Year )
        /// </summary>
        public static string NoveCislo(string rada, int rok, string co = "dokladu")
        {
            string key = $"{rada}{rok}";
            if (!LibSys.AddNewGenNum( Tab.DA, key ))
                throw new JisException( $"Chyba při generování čísla {co} - nelze uložit !" );

            string cislo = LibSys.GenNum( Tab.DA, key );
            if (string.IsNullOrWhiteSpace( cislo ))
                throw new JisException( $"Chyba při generování čísla {co} - nelze uložit !" );
            return cislo;
        }

        /// <summary>
        /// dalsi klic podle nejvyssiho klice v tabulce (tabulky bez rady v DA: d7.f701, t6.ft601, t7.ft701)
        /// <br>volat uvnitr SpravaDat.Transakce() - FOR UPDATE zamkne posledni klic do konce transakce, soubezne ulozeni pocka</br>
        /// </summary>
        public static string DalsiKlicMax(string tabulka, string pole)
            => LibSys.NextKey( $"{SpravaDat.Scalar( $"SELECT MAX({pole}) FROM {tabulka} FOR UPDATE" )}" );

        /// <summary>
        /// dalsi klic v soustave 0-9A-Z pevne delky (jako VFP xnextkey), napr. 1J -> 1K, 0Z -> 10 - users.fukey, t1.fitem, t5.ft501
        /// <br>volat uvnitr SpravaDat.Transakce() - FOR UPDATE zamkne posledni klic, soubezne ulozeni pocka</br>
        /// </summary>
        public static string DalsiKlic36(string tabulka, string pole, int delka, string where = "1=1", string co = "záznamu")
        {
            const string znaky = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string max = $"{SpravaDat.Scalar( $"SELECT MAX({pole}) FROM {tabulka} WHERE {where} FOR UPDATE" )}";
            char[] k = max.Trim().ToUpper().PadLeft( delka, '0' ).ToCharArray();
            for (int i = k.Length - 1; i >= 0; i--)
            {
                int p = znaky.IndexOf( k[ i ] );
                if (p < znaky.Length - 1)
                {
                    k[ i ] = znaky[ p + 1 ];
                    return new string( k );
                }
                k[ i ] = '0';
            }
            throw new JisException( $"Nelze vytvořit klíč nového {co} - klíče jsou vyčerpány" );
        }

        /// <summary>
        /// novy klic zaznamu (F201, F301, F401, F501, F601, F801, FB01, FE01 ...)
        /// </summary>
        public static string NovyKlic(string pole)
        {
            string klic = LibSys.GenKey( Tab.DA, pole );
            if (string.IsNullOrWhiteSpace( klic ))
                throw new JisException( $"Chyba při generování klíče {pole} - nelze uložit !" );
            return klic;
        }
    }
}
