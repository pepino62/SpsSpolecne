using JisResources;
using JisResources.Sources;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SpsSpolecne
{
    /// <summary>
    /// Karta zakaznika - prehled predchozich rezervaci zakaznika (chata / pokoj, terminy, zrusene, poznamky)
    /// <br>zakaznik se hleda podle telefonu, e-mailu nebo prijmeni a jmena (udaje jsou na kazde rezervaci d2 / dz2)</br>
    /// </summary>
    public partial class KartaZakaznika : JisUserControl
    {
        KartaZakaznikaBind dataBind;

        public KartaZakaznika(string prijmeni = "", string jmeno = "", string telefon = "", string email = "")
        {
            InitializeComponent();

            DataContext = dataBind = new KartaZakaznikaBind();
            dataBind.Prijmeni = prijmeni?.Trim() ?? "";
            dataBind.Jmeno = jmeno?.Trim() ?? "";
            dataBind.Telefon = KartaZakaznikaBind.CisloTelefonu( telefon );
            dataBind.Email = email?.Trim() ?? "";

            dataBind.ReadControls( this );
            dataBind.SetDataGrid( this, dataBind );
            Loaded += JisUserControl_Loaded;
        }

        public override void JisUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            base.JisUserControl_Loaded( sender, e );
            try
            {
                using (new WaitCursor())
                {
                    bool zadano = dataBind.JeZadano();
                    dataBind.DataSource( zadano );    // bez zadanych udaju prazdny seznam (jen sloupce gridu)
                    USERDATAGRID1.RestoreGrid( resetColumns: true );
                    dataBind.SpocitejSouhrn();
                }
                if (!dataBind.JeZadano())
                    TxtPrijmeni.Focus();    // samostatne volani (zalozka OSOBY) - vstup jmena
            }
            catch (Exception ex)
            {
                Msg.Err( ex );
            }
        }

        void Nacti()
        {
            using (new WaitCursor())
            {
                if (dataBind.DataSource( true ))
                {
                    USERDATAGRID1.RefreshSourceData( false );  // false - bez znovunaplneni dat
                    dataBind.SpocitejSouhrn();
                }
            }
        }

        private void BtnNacti_Click(object sender, RoutedEventArgs e)
        {
            Nacti();
        }

        private void BtnVymaz_Click(object sender, RoutedEventArgs e)
        {
            dataBind.Vymaz();
            dataBind.DataSource( false );
            USERDATAGRID1.RefreshSourceData( false );
            dataBind.SpocitejSouhrn();
            TxtPrijmeni.Focus();
        }

        // nabidka zakazniku se nacte az pri vstupu do pole (desitky tisic rezervaci)
        private void NaplnNabidku(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (((JisTextBoxGrid)sender).IsLoaded && dataBind.Nabidka == null)
            {
                using (new WaitCursor())
                    dataBind.NaplnNabidku();
            }
        }

        // vyber zakaznika z nabidky (prijmeni nebo telefon) - doplni ostatni udaje a nacte rezervace
        private void Nabidka_OnSelectKeyValue(object sender, UserControlEventArgs e)
        {
            JisTextBoxGrid textBoxGrid = (JisTextBoxGrid)sender;
            if (dataBind.Plneni || textBoxGrid.ItemIndex < 0 || textBoxGrid.SelectedDataRow == null)
                return;

            dataBind.Predvolit( textBoxGrid.SelectedDataRow );
            Nacti();
        }
    }


    public class KartaZakaznikaBind : DataBindBasic
    {
        string _Prijmeni = "";
        public string Prijmeni { get => _Prijmeni; set => SetField( ref _Prijmeni, value ); }

        string _Jmeno = "";
        public string Jmeno { get => _Jmeno; set => SetField( ref _Jmeno, value ); }

        string _Telefon = "";
        public string Telefon { get => _Telefon; set => SetField( ref _Telefon, value ); }

        string _Email = "";
        public string Email { get => _Email; set => SetField( ref _Email, value ); }

        string _KlicJmeno = "";
        public string KlicJmeno { get => _KlicJmeno; set => SetField( ref _KlicJmeno, value ); }

        string _KlicTelefon = "";
        public string KlicTelefon { get => _KlicTelefon; set => SetField( ref _KlicTelefon, value ); }

        DataView _Nabidka;
        public DataView Nabidka { get => _Nabidka; set => SetField( ref _Nabidka, value ); }

        string _Souhrn = "";
        public string Souhrn { get => _Souhrn; set => SetField( ref _Souhrn, value ); }

        string _SouhrnZrusene = "";
        public string SouhrnZrusene { get => _SouhrnZrusene; set => SetField( ref _SouhrnZrusene, value ); }

        /// <summary>
        /// probiha predvyplneni z nabidky - zmena textu v poli s nabidkou nema vyvolat dalsi vyber
        /// </summary>
        public bool Plneni { get; private set; }


        public KartaZakaznikaBind() : base()
        {
        }

        public static string CisloTelefonu(string telefon) => NabidkaZakazniku.CisloTelefonu( telefon );

        string EmailHledany => (Email ?? "").Trim() == "@" ? "" : (Email ?? "").Trim();

        public bool JeZadano()
            => !string.IsNullOrWhiteSpace( Prijmeni ) || CisloTelefonu( Telefon ).Length > 0 || EmailHledany.Length > 0;

        public void Vymaz()
        {
            Plneni = true;
            Prijmeni = Jmeno = Telefon = KlicJmeno = KlicTelefon = "";
            Email = "";
            Plneni = false;
        }

        public void Predvolit(DataRow dr)
        {
            Plneni = true;
            try
            {
                Prijmeni = $"{dr[ "F302" ]}".Trim();
                Jmeno = $"{dr[ "F303" ]}".Trim();
                Telefon = $"{dr[ "FTELC" ]}".Trim();
                Email = $"{dr[ "F308" ]}".Trim();
            }
            finally
            {
                Plneni = false;
            }
        }

        // seznam zakazniku (jako nabidka jmen ve vstupu rezervace) + telefon bez predvolby a mezer pro hledani podle cisla
        public void NaplnNabidku()
        {
            try
            {
                SpravaDat.Select( "kartajmena", NabidkaZakazniku.Sql( vcetneZrusenych: true ), hide: true );    // i zakaznik, ktery jen rusil
                Nabidka = Goa.TableView( "kartajmena" );
            }
            catch (Exception ex)
            {
                JisLog.Chyba( ex );
            }
        }


        /// <summary>
        /// rezervace zakaznika - podle telefonu, bez telefonu podle e-mailu, bez obou podle prijmeni a jmena (pripadne jen prijmeni)
        /// <br>hledat = false - prazdny seznam (jen struktura pro grid)</br>
        /// </summary>
        public bool DataSource(bool hledat)
        {
            try
            {
                string tel = CisloTelefonu( Telefon );
                string email = EmailHledany;
                string prijmeni = (Prijmeni ?? "").Trim();
                string jmeno = (Jmeno ?? "").Trim();

                var podminky = new List<string>();
                if (hledat)
                {
                    // telefon povazujeme za jedinecny - je-li zadan, hleda se jen podle nej (jinak by se pridali
                    // zakaznici s jinym telefonem a stejnym e-mailem / jmenem); bez telefonu e-mail, bez obou jmeno
                    if (NabidkaZakazniku.PlatnyTelefon( tel ))     // vyplnove cislo (111111111, 123456789) by vratilo cizi rezervace
                        podminky.Add( "REPLACE(SUBSTRING(d2.f307,5),' ','') = @tel" );
                    else if (email.Contains( "@" ) && email.Length > 1)
                        podminky.Add( "d2.f308 = @email" );
                    else if (prijmeni != "" && jmeno != "")
                        podminky.Add( "(d2.f302 = @prijmeni AND d2.f303 = @jmeno)" );
                    else if (prijmeni != "")
                        podminky.Add( "d2.f302 = @prijmeni" );

                    if (podminky.Count == 0)
                        throw new JisException( "Zadejte příjmení, telefon (alespoň 6 číslic, ne výplňové číslo) nebo e-mail" );
                }
                string where = podminky.Count == 0 ? "1 = 2" : string.Join( " OR ", podminky );

                string cmd = $@"
SELECT FALSE AS fmark,
       d2.f201,
       CAST(d2.f202 AS UNSIGNED) AS frez,
       CAST(COALESCE(NULLIF(TRIM(t4.ft402),''), t1.fprompt, '') AS CHAR(30)) AS fubyt,
       d2.f203,
       d2.f204,
       d2.f212,
       (SELECT COUNT(*) FROM {Tab.D6} d6 WHERE d6.f201 = d2.f201 AND d6.f0 = '') AS fosob,
       CAST(IF(d2.f0 = '1','ANO','') AS CHAR(3)) AS fzrusena,
       CAST(REPLACE(d2.f215,'~',' ') AS CHAR(500)) AS fpozn,
       d2.f302,
       d2.f303,
       d2.f307,
       d2.f308
FROM {Tab.D2} d2
LEFT JOIN {Tab.T4} t4 ON t4.ft401 = d2.ft401
LEFT JOIN t1 ON t1.fsection = CAST('F217' AS CHAR(20)) AND t1.fitem = d2.f217 AND d2.ft401 = ''
WHERE {where}
ORDER BY d2.f203 DESC, d2.f202, d2.ft401";

                return SpravaDat.Select( "KARTA", cmd, new { tel, email, prijmeni, jmeno }, hide: true );
            }
            catch (JisException e1)
            {
                Msg.Ok( e1.Text );
            }
            catch (Exception ex)
            {
                JisLog.Chyba( ex );
                Msg.Err( ex );
            }
            return false;
        }

        // pocet rezervaci (cisel rezervace - hromadna ma vic radku) a z toho zrusenych
        public void SpocitejSouhrn()
        {
            var radky = Goa.TableEnum( "KARTA" ).ToList();
            int rezervaci = radky.Select( r => $"{r[ "frez" ]}" ).Distinct().Count();
            int zrusenych = radky.Where( r => $"{r[ "fzrusena" ]}" == "ANO" ).Select( r => $"{r[ "frez" ]}" ).Distinct().Count();
            Souhrn = radky.Count == 0 ? "" : $"rezervací : {rezervaci}";
            SouhrnZrusene = zrusenych == 0 ? "" : $"z toho zrušených : {zrusenych}";
        }
    }


    /// <summary>
    /// okno Karta zakaznika - ze vstupu rezervace s udaji zakaznika, ze zalozky OSOBY bez udaju
    /// </summary>
    public class KartaZakaznikaOkno : JisWindow
    {
        public KartaZakaznikaOkno(string prijmeni = "", string jmeno = "", string telefon = "", string email = "")
        {
            jisBackground = new SolidColorBrush( (Color)ColorConverter.ConvertFromString( "#62709C" ) );  // fialova
            SetBlur( 100, br: new SolidColorBrush( (Color)ColorConverter.ConvertFromString( "#CAD3EF" ) ) );
            Title = "Karta zákazníka";
            Name = "KARTAZAKAZNIKA";

            Grid grid = new Grid();
            Content = grid;
            grid.Children.Add( new KartaZakaznika( prijmeni, jmeno, telefon, email ) );

            Width = 1000;
            Height = 600;
            SpravaForms.SetForm( "read", this, "" );
            Closing += (s, e) => SpravaForms.SetForm( "save", this, "" );

            ShowDialog();
        }
    }
}
