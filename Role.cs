using JisResources.Sources;
using System;
using System.Linq;

namespace SpsSpolecne
{
    /// <summary>
    /// Role prihlaseneho uzivatele (users.frole - seznam kodu oddeleny carkou).
    /// <br>Spravce (001) ma vzdy pristup - stejne jako VFP xIsRole.</br>
    /// </summary>
    public static class Role
    {
        public const string Spravce = "001";
        public const string Vybrany = "100";      // vybrani uzivatele - sprava, opravy plateb, vsechny platby

        /// <summary>
        /// ma uzivatel nekterou z roli ? (seznam oddeleny carkou, napr. "006,005,100") - spravce vzdy
        /// </summary>
        public static bool Ma(string role)
        {
            var moje = Rozdel( Goa.Roles );
            return moje.Intersect( Rozdel( $"{Spravce},{role}" ) ).Any();
        }

        /// <summary>
        /// spravce (001) nebo vybrany uzivatel (100) - vidi vsechny platby, ponechava cislo dokladu, opravuje data plateb ...
        /// </summary>
        public static bool JeVybrany => Ma( Vybrany );

        /// <summary>
        /// jen spravce (001)
        /// </summary>
        public static bool JeSpravce => Rozdel( Goa.Roles ).Contains( Spravce );

        static string[] Rozdel(string role)
            => (role ?? "").Split( new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries ).Select( r => r.Trim() ).ToArray();
    }
}
