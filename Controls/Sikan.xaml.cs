using JisResources;
using JisResources.Sources;
using System;
using System.Windows;

namespace SpsSpolecne
{
    /// <summary>
    /// Sikana rezervace - prehled zmen (tabulka d9 / dz9)
    /// </summary>
    public partial class Sikan : JisUserControl
    {
        SikanBind dataBind;
        string f201 = "";

        public Sikan(string f201)
        {
            this.f201 = f201;
            InitializeComponent();

            DataContext = dataBind = new SikanBind();

            dataBind.ReadControls( this );
            dataBind.SetDataGrid( this, dataBind );
            Loaded += JisUserControl_Loaded;
        }

        public override void JisUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            base.JisUserControl_Loaded( sender, e );
            FillData( true );
        }

        private void FillData(bool _resetColums = false)
        {
            try
            {
                using (new WaitCursor())
                {
                    dataBind.CreateDataBind( f201 );
                    USERDATAGRID1.RestoreGrid( resetColumns: _resetColums );           // sestavi grid1 - pripoji zdroj dat, nastavi columns, sort, refresh ...
                }
            }
            catch (Exception ex)
            {
                Msg.Err( ex );
            }
        }
    }


    public class SikanBind : DataBindBasic
    {
        public SikanBind() : base()
        {
        }

        public void CreateDataBind(string f201)
        {
            try
            {
                DataSource( f201 );
            }
            catch (Exception ex)
            {
                Msg.Err( ex );
            }
        }


        public bool DataSource(string f201)
        {
            bool ret = false;
            try
            {
                SpravaDat.SelectX( "SIKAN", $@"
SELECT d9.* ,
 	CAST(coalesce(t1.fprompt,'')   As char(20)) As f903x ,
 	CAST(coalesce(users.fname,'')  As char(20)) As f906x
 from {Tab.D9} d9
 Left Join t1 ON t1.fsection = CAST('F903' AS char(20)) AND t1.fitem=d9.f903
 Left Join users ON users.fukey = d9.f906
 WHERE d9.f902 IN ('D2','D4') AND d9.f904 = @f201",   // jen zaznamy rezervace a jeji zalohy - zapujcky (D8) maji klice z jine rady (VFP SPORTY_TMP: f902='D2')
                    new { f201 = f201 ?? "" } ); // nacte data z databaze do tabulky SIKAN
                ret = true;
            }
            catch (Exception ex)
            {
                JisLog.Chyba( ex );
            }

            return ret;
        }
    }
}
