using System.Windows.Forms;

namespace ASCOM.Meade.net
{
    public partial class FrmMain : Form
    {
        delegate void SetTextCallback(string text);

        public FrmMain()
        {
            InitializeComponent();
        }

    }
}