using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OnTopReplica {
    public partial class TestForm : Form {

        private static TestForm _Form;

        public static TestForm Instance {
            get {
                if(_Form != null)
                    return _Form;

                _Form = new TestForm();
                _Form.Show();
                _Form.FormClosed += (s, e) => _Form = null;
                return _Form;
            }
        }

        public TestForm() {
            InitializeComponent();
        }

        public void View(Image img) {
            ImageView.Image = img;
        }
    }
}
