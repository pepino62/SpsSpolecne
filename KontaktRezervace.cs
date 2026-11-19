using JisResources;
using JisResources.Sources;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SpsSpolecne
{
    /// <summary>
    /// Udaje zakaznika na rezervaci (jmeno, adresa, stat, telefon, e-mail, fakturacni adresa) a jejich preneseni
    /// na ostatni rezervace hromadne rezervace (stejne cislo rezervace v roce, kazda chata / pokoj je samostatny radek d2 / dz2).
    /// <br>Pouziti: pred ulozenim Nacti (puvodni stav), po ulozeni Zmenen + OstatniHromadne, po potvrzeni Prenes.</br>
    /// </summary>
    public static class KontaktRezervace
    {
        static readonly string[] Sloupce = { "f302", "f303", "f304", "f305", "f306", "f307", "f308", "f218" };

        /// <summary>
        /// ulozene udaje zakaznika rezervace (null = rezervace neexistuje)
        /// </summary>
        public static Dictionary<string, string> Nacti(string f201)
        {
            if (string.IsNullOrWhiteSpace( f201 ))
                return null;
            SpravaDat.Select( "_kontakt", $"SELECT {string.Join( ", ", Sloupce.Select( s => "d2." + s ) )} FROM {Tab.D2} d2 WHERE d2.f201 = @f201",
                new { f201 }, hide: true );
            DataRow r = Goa.TableEnum( "_kontakt" ).FirstOrDefault();
            return r == null ? null : Sloupce.ToDictionary( s => s, s => $"{r[ s ]}".Trim() );
        }

        public static bool Zmenen(Dictionary<string, string> puvodni, Dictionary<string, string> novy)
            => puvodni != null && novy != null && Sloupce.Any( s => !string.Equals( puvodni[ s ], novy[ s ], StringComparison.Ordinal ) );

        /// <summary>
        /// ostatni platne rezervace hromadne rezervace (stejne cislo, rok a druh) se stejnym puvodnim zakaznikem (jmeno, telefon, e-mail)
        /// <br>rezervace s jinym zakaznikem pod stejnym cislem se nemeni</br>
        /// </summary>
        public static List<string> OstatniHromadne(string f201, Dictionary<string, string> puvodni)
        {
            if (puvodni == null)
                return new List<string>();
            SpravaDat.Select( "_kontaktskup", $@"
SELECT o.f201
FROM {Tab.D2} d
JOIN {Tab.D2} o ON o.f202 = d.f202 AND YEAR(o.f203) = YEAR(d.f203) AND o.f01 = d.f01 AND o.f201 != d.f201 AND o.f0 = ''
WHERE d.f201 = @f201 AND TRIM(d.f205) != ''
  AND TRIM(o.f302) = @f302 AND TRIM(o.f303) = @f303 AND TRIM(o.f307) = @f307 AND TRIM(o.f308) = @f308
ORDER BY o.f205",
                new { f201, f302 = puvodni[ "f302" ], f303 = puvodni[ "f303" ], f307 = puvodni[ "f307" ], f308 = puvodni[ "f308" ] }, hide: true );
            return Goa.TableEnum( "_kontaktskup" ).Select( r => $"{r[ "f201" ]}" ).ToList();
        }

        /// <summary>
        /// prenese udaje zakaznika z rezervace f201 na rezervace cile (jedna transakce, sikan u kazde zmenene rezervace)
        /// </summary>
        public static void Prenes(string f201, List<string> cile)
        {
            if (cile == null || cile.Count == 0)
                return;
            using (var tr = SpravaDat.Transakce())
            {
                string sloupce = string.Join( ", ", Sloupce.Select( s => $"o.{s} = d.{s}" ) );
                if (!SpravaDat.Exec( $@"UPDATE {Tab.D2} o JOIN {Tab.D2} d ON d.f201 = @f201
                        SET {sloupce}, o.f02 = @ukey, o.f03 = CURRENT_TIMESTAMP()
                        WHERE o.f202 = d.f202 AND FIND_IN_SET(o.f201, @cile) > 0",
                    new { f201, ukey = Goa.Ukey ?? "", cile = string.Join( ",", cile ) } ))
                    throw new JisException();

                foreach (string c in cile)
                    if (!SikanZapis.Zapis( SikanZapis.Rezervace, SikanZapis.Editoval, c ))
                        throw new JisException();

                tr.Commit();
            }
        }
    }
}
