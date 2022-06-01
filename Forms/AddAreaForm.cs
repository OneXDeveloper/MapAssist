using MapAssist.Settings;
using MapAssist.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MapAssist
{
    public partial class AddAreaForm : Form
    {
        public string listToAddTo;
        public AddAreaForm()
        {
            InitializeComponent();
        }

        private void AddAreaForm_Load(object sender, EventArgs e)
        {
            var areas = new Dictionary<Area, string>();

            foreach (var area in Enum.GetValues(typeof(Area)).Cast<Area>()){
                if (area != Area.None && area.IsValid() && !MapAssistConfiguration.Loaded.HiddenAreas.Contains(area))
                {
                    areas.Add(area, area.Name());
                }
            }

            lstAreas.DataSource = new BindingSource(areas, null);
            lstAreas.ValueMember = "Key";
            lstAreas.DisplayMember = "Value";
        }

        private void AddSelectedArea(Area areaToAdd)
        {
            var areaName = areaToAdd.NameInternal();

            var formParent = (ConfigEditor)Owner;
            var list = formParent.Controls.Find(listToAddTo, true).FirstOrDefault() as ListBox;
            if (!list.Items.Contains(areaName))
            {
                list.Items.Add(areaToAdd.Name());
                switch (listToAddTo)
                {
                    case "lstHidden":
                        MapAssistConfiguration.Loaded.HiddenAreas = MapAssistConfiguration.Loaded.HiddenAreas.Append(areaToAdd).ToArray();
                        break;
                }
                Close();
            }
        }

        private void btnAddArea_Click(object sender, EventArgs e)
        {
            var areaToAdd = (Area)lstAreas.SelectedValue;
            AddSelectedArea(areaToAdd);
        }

        private void lstAreas_MouseDoubleClick(object sender, EventArgs e)
        {
            var areaToAdd = (Area)lstAreas.SelectedValue;
            AddSelectedArea(areaToAdd);
        }
    }
}
