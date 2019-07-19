using System.Windows.Forms;

namespace ASCOM.Meade.net
{
    public partial class frmMain : Form
    {
        delegate void SetTextCallback(string text);

        public frmMain()
        {
            InitializeComponent();
        }

    }
}