using System.Linq;

namespace SpsSpolecne
{
    /// <summary>
    /// Nabidka opakovanych zakazniku z rezervaci (d2 / dz2) - jeden radek na zakaznika misto kazde kombinace jmeno + telefon + e-mail
    /// <br>zakaznik = telefon (bez predvolby a mezer); bez platneho telefonu e-mail; bez obou prijmeni + jmeno</br>
    /// <br>za zakaznika se vezme jmeno, telefon a e-mail z naposledy ulozene rezervace (f03) - oprava preklepu se v nabidce projevi hned</br>
    /// <br>e-mail bez telefonu se nenabizi zvlast, pokud uz je u zakaznika s telefonem</br>
    /// </summary>
    public static class NabidkaZakazniku
    {
        static readonly string[] Fiktivni = { "123456789", "0123456789", "987654321" };

        /// <summary>
        /// telefon bez predvolby a mezer - "+420 606 330 373" -> "606330373" (stejne jako SUBSTRING(f307,5) v SQL)
        /// </summary>
        public static string CisloTelefonu(string telefon)
        {
            string s = (telefon ?? "").Trim();
            if (s.StartsWith( "+" ))
                s = s.Length > 4 ? s.Substring( 4 ) : "";
            return new string( s.Where( char.IsDigit ).ToArray() );
        }

        /// <summary>
        /// telefon pouzitelny jako identifikace zakaznika - aspon 6 cislic, ne vyplnove cislo (111111111, 123456789 ...)
        /// </summary>
        public static bool PlatnyTelefon(string cislo)
            => cislo != null && cislo.Length >= 6 && cislo.Distinct().Count() > 1 && !Fiktivni.Contains( cislo );

        // SQL obdoba PlatnyTelefon pro sloupec s cislem bez predvolby a mezer
        static string SqlPlatnyTelefon(string sloupec)
            => $"(LENGTH({sloupec}) >= 6 AND {sloupec} NOT REGEXP '^(.)\\\\1*$' AND {sloupec} NOT IN ({string.Join( ",", Fiktivni.Select( f => $"'{f}'" ) )}))";

        /// <summary>
        /// SQL nabidky - sloupce F302, F303, F307, F308, FTEL (telefon bez predvolby), FTELC (bez mezer), FCOMPARE (RPAD prijmeni 20 + ...)
        /// <br>napln = false - prazdna nabidka (jen struktura); vcetneZrusenych - i kontakty ze zrusenych rezervaci</br>
        /// </summary>
        public static string Sql(bool napln = true, bool vcetneZrusenych = false)
        {
            string where = !napln ? "1 = 2"
                : $"d2.f302 != '' AND (LENGTH(TRIM(d2.f307)) > 4 OR TRIM(d2.f308) NOT IN ('','@'))" + (vcetneZrusenych ? "" : " AND d2.f0 = ''");

            return $@"
WITH k AS
(
    SELECT d2.f201, d2.f203, d2.f03, d2.f302, d2.f303, d2.f307, d2.f308,
           REPLACE(SUBSTRING(d2.f307,5),' ','') AS tel,
           LOWER(TRIM(d2.f308)) AS mail
    FROM {Tab.D2} d2
    WHERE {where}
),
k2 AS
(
    SELECT k.*,
           CASE WHEN {SqlPlatnyTelefon( "k.tel" )} THEN CONCAT('T', k.tel)
                WHEN k.mail NOT IN ('','@') THEN CONCAT('E', k.mail)
                ELSE CONCAT('J', LOWER(TRIM(k.f302)), '|', LOWER(TRIM(k.f303))) END AS fident
    FROM k
),
tm AS
(
    SELECT DISTINCT k2.mail FROM k2 WHERE k2.fident LIKE 'T%' AND k2.mail NOT IN ('','@')
),
k3 AS
(
    SELECT k2.*, ROW_NUMBER() OVER (PARTITION BY k2.fident ORDER BY k2.f03 IS NULL, k2.f03 DESC, k2.f203 DESC, k2.f201 DESC) AS poradi
    FROM k2
    LEFT JOIN tm ON k2.fident LIKE 'E%' AND tm.mail = k2.mail     -- e-mail uz je u zakaznika s telefonem
    WHERE tm.mail IS NULL
)
SELECT k3.f302, k3.f303, k3.f307, k3.f308,
       CAST(SUBSTRING(k3.f307,5) AS CHAR(50)) AS FTEL,
       CAST(k3.tel AS CHAR(16)) AS FTELC,
       CAST(CONCAT(RPAD(k3.f302,20,' '),RPAD(k3.f303,12,' '),RPAD(k3.f307,16,' '),RPAD(k3.f308,30,' ')) AS CHAR(78)) AS FCOMPARE
FROM k3
WHERE k3.poradi = 1
ORDER BY FCOMPARE";
        }
    }
}
