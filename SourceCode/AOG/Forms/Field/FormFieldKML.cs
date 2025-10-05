﻿using AOG.Classes;

using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AOG
{
    public partial class FormFieldKML : Form
    {
        //class variables
        private readonly FormGPS mf = null;

        private double easting, northing, latK, lonK;

        public FormFieldKML(Form _callingForm)
        {
            //get copy of the calling main form
            mf = _callingForm as FormGPS;

            InitializeComponent();

            label1.Text = gStr.Get(gs.gsEnterFieldName);
            this.Text = gStr.Get(gs.gsCreateNewField);
        }

        private void FormFieldDir_Load(object sender, EventArgs e)
        {
            btnSave.Enabled = false;

            if (!mf.IsOnScreen(Location, Size, 1))
            {
                Top = 0;
                Left = 0;
            }
        }

        private void tboxFieldName_TextChanged(object sender, EventArgs e)
        {
            TextBox textboxSender = (TextBox)sender;
            int cursorPosition = textboxSender.SelectionStart;
            textboxSender.Text = Regex.Replace(textboxSender.Text, glm.fileRegex, "");
            textboxSender.SelectionStart = cursorPosition;

            if (String.IsNullOrEmpty(tboxFieldName.Text.Trim()))
            {
                btnLoadKML.Enabled = false;
            }
            else
            {
                btnLoadKML.Enabled = true;
            }
        }

        private void btnSerialCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void tboxFieldName_Click(object sender, EventArgs e)
        {
            if (Settings.User.setDisplay_isKeyboardOn)
            {
                mf.KeyboardToText((TextBox)sender, this);
                btnSerialCancel.Focus();
            }
        }

        private void btnLoadKML_Click(object sender, EventArgs e)
        {
            tboxFieldName.Enabled = false;
            btnAddDate.Enabled = false;
            btnAddTime.Enabled = false;

            //create the dialog instance
            OpenFileDialog ofd = new OpenFileDialog
            {
                //set the filter to text KML only
                Filter = "KML files (*.KML)|*.KML",

                //the initial directory, fields, for the open dialog
                InitialDirectory = RegistrySettings.fieldsDirectory
            };

            //was a file selected
            if (ofd.ShowDialog() == DialogResult.Cancel) return;

            //get lat and lon from boundary in kml
            FindLatLon(ofd.FileName);

            //reset sim and world to kml position
            CreateNewField();

            //Load the outer boundary
            LoadKMLBoundary(ofd.FileName);
        }

        private void btnAddDate_Click(object sender, EventArgs e)
        {
            tboxFieldName.Text += " " + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private void btnAddTime_Click(object sender, EventArgs e)
        {
            tboxFieldName.Text += " " + DateTime.Now.ToString("HH-mm", CultureInfo.InvariantCulture);
        }

        private void LoadKMLBoundary(string filename)
        {
            string coordinates = null;
            int startIndex;

            using (System.IO.StreamReader reader = new System.IO.StreamReader(filename))
            {
                try
                {
                    while (!reader.EndOfStream)
                    {
                        //start to read the file
                        string line = reader.ReadLine();

                        startIndex = line.IndexOf("<coordinates>");

                        if (startIndex != -1)
                        {
                            while (true)
                            {
                                int endIndex = line.IndexOf("</coordinates>");

                                if (endIndex == -1)
                                {
                                    //just add the line
                                    if (startIndex == -1) coordinates += line.Substring(0);
                                    else coordinates += line.Substring(startIndex + 13);
                                }
                                else
                                {
                                    if (startIndex == -1) coordinates += line.Substring(0, endIndex);
                                    else coordinates += line.Substring(startIndex + 13, endIndex - (startIndex + 13));
                                    break;
                                }
                                line = reader.ReadLine();
                                line = line.Trim();
                                startIndex = -1;
                            }

                            line = coordinates;
                            string[] numberSets = line.Split();

                            //at least 3 points
                            if (numberSets.Length > 2)
                            {
                                CBoundaryList newBnd = new CBoundaryList();

                                foreach (string item in numberSets)
                                {
                                    if (item.Length < 3)
                                        continue;
                                    string[] fix = item.Split(',');
                                    double.TryParse(fix[0], NumberStyles.Float, CultureInfo.InvariantCulture, out lonK);
                                    double.TryParse(fix[1], NumberStyles.Float, CultureInfo.InvariantCulture, out latK);

                                    mf.pn.ConvertWGS84ToLocal(latK, lonK, out northing, out easting);

                                    //add the point to boundary
                                    newBnd.fenceLine.Add(new vec3(easting, northing, 0));
                                }

                                mf.bnd.AddToBoundList(newBnd, mf.bnd.bndList.Count);

                                coordinates = "";
                            }
                            else
                            {
                                mf.TimedMessageBox(2000, gStr.Get(gs.gsErrorreadingKML), gStr.Get(gs.gsChooseBuildDifferentone));
                                Log.EventWriter("New Field, Error Reading KML");
                            }
                            break;
                        }
                    }
                    mf.FileSaveBoundary();

                    btnSave.Enabled = true;
                    btnLoadKML.Enabled = false;
                }
                catch (Exception ee)
                {
                    btnSave.Enabled = false;
                    btnLoadKML.Enabled = false;
                    mf.TimedMessageBox(2000, gStr.Get(gs.gsErrorreadingKML), gStr.Get(gs.gsChooseBuildDifferentone));
                    Log.EventWriter("New Field, Error Reading KML" + ee.ToString());
                    return;
                }
            }

            mf.bnd.isOkToAddPoints = false;
        }

        private void FindLatLon(string filename)
        {
            string coordinates = null;
            int startIndex;

            using (System.IO.StreamReader reader = new System.IO.StreamReader(filename))
            {
                try
                {
                    while (!reader.EndOfStream)
                    {
                        //start to read the file
                        string line = reader.ReadLine();

                        startIndex = line.IndexOf("<coordinates>");

                        if (startIndex != -1)
                        {
                            while (true)
                            {
                                int endIndex = line.IndexOf("</coordinates>");

                                if (endIndex == -1)
                                {
                                    //just add the line
                                    if (startIndex == -1) coordinates += line.Substring(0);
                                    else coordinates += line.Substring(startIndex + 13);
                                }
                                else
                                {
                                    if (startIndex == -1) coordinates += line.Substring(0, endIndex);
                                    else coordinates += line.Substring(startIndex + 13, endIndex - (startIndex + 13));
                                    break;
                                }
                                line = reader.ReadLine();
                                line = line.Trim();
                                startIndex = -1;
                            }

                            line = coordinates;
                            char[] delimiterChars = { ' ', '\t', '\r', '\n' };
                            string[] numberSets = line.Split(delimiterChars);

                            //at least 3 points
                            if (numberSets.Length > 2)
                            {
                                double counter = 0, lat = 0, lon = 0;
                                latK = lonK = 0;
                                foreach (string item in numberSets)
                                {
                                    if (item.Length < 3)
                                        continue;
                                    string[] fix = item.Split(',');
                                    double.TryParse(fix[0], NumberStyles.Float, CultureInfo.InvariantCulture, out lonK);
                                    double.TryParse(fix[1], NumberStyles.Float, CultureInfo.InvariantCulture, out latK);
                                    lat += latK;
                                    lon += lonK;
                                    counter += 1;
                                }
                                lonK = lon / counter;
                                latK = lat / counter;

                                coordinates = "";
                            }
                            else
                            {
                                mf.TimedMessageBox(2000, gStr.Get(gs.gsErrorreadingKML), gStr.Get(gs.gsChooseBuildDifferentone));
                                Log.EventWriter("New Field, Error Reading KML ");
                            }
                            //if (button.Name == "btnLoadBoundaryFromGE")
                            //{
                            break;
                            //}
                        }
                    }
                }
                catch (Exception et)
                {
                    mf.TimedMessageBox(2000, "Exception", "Error Finding Lat Lon");
                    Log.EventWriter("Lat Lon Exception Reading KML " + et.ToString());
                    return;
                }
            }

            mf.bnd.isOkToAddPoints = false;
        }

        private void CreateNewField()
        {
            //fill something in
            if (String.IsNullOrEmpty(tboxFieldName.Text.Trim()))
            {
                Close();
                return;
            }

            //append date time to name
            mf.currentFieldDirectory = tboxFieldName.Text.Trim();

            //get the directory and make sure it exists, create if not
            string directoryName = Path.Combine(RegistrySettings.fieldsDirectory, mf.currentFieldDirectory);

            mf.menustripLanguage.Enabled = false;
            //if no template set just make a new file.
            try
            {
                //start a new field
                mf.FieldNew();

                //create it for first save
                if ((!string.IsNullOrEmpty(directoryName)) && (Directory.Exists(directoryName)))
                {
                    MessageBox.Show(gStr.Get(gs.gsChooseADifferentName), gStr.Get(gs.gsDirectoryExists), MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }
                else
                {
                    mf.pn.SetLocalMetersPerDegree(latK, lonK);

                    //make sure directory exists, or create it
                    if ((!string.IsNullOrEmpty(directoryName)) && (!Directory.Exists(directoryName)))
                    { Directory.CreateDirectory(directoryName); }

                    mf.displayFieldName = mf.currentFieldDirectory;

                    //create the field file header info
                    if (!mf.isFieldStarted)
                    {
                        mf.TimedMessageBox(3000, gStr.Get(gs.gsFieldNotOpen), gStr.Get(gs.gsCreateNewField));
                        return;
                    }
                    string myFileName;

                    //get the directory and make sure it exists, create if not
                    directoryName = Path.Combine(RegistrySettings.fieldsDirectory, mf.currentFieldDirectory);

                    if ((directoryName.Length > 0) && (!Directory.Exists(directoryName)))
                    { Directory.CreateDirectory(directoryName); }

                    myFileName = "Field.txt";

                    using (StreamWriter writer = new StreamWriter(Path.Combine(directoryName, myFileName)))
                    {
                        //Write out the date
                        writer.WriteLine(DateTime.Now.ToString("yyyy-MMMM-dd hh:mm:ss tt", CultureInfo.InvariantCulture));

                        writer.WriteLine("$FieldDir");
                        writer.WriteLine("KML Derived");

                        //write out the easting and northing Offsets
                        writer.WriteLine("$Offsets");
                        writer.WriteLine("0,0");

                        writer.WriteLine("Convergence");
                        writer.WriteLine("0");

                        writer.WriteLine("StartFix");
                        writer.WriteLine(CNMEA.latStart.ToString(CultureInfo.InvariantCulture) + "," + CNMEA.lonStart.ToString(CultureInfo.InvariantCulture));
                    }

                    directoryName = Path.Combine(RegistrySettings.fieldsDirectory, mf.currentFieldDirectory, "Jobs");

                    if ((directoryName.Length > 0) && (!Directory.Exists(directoryName)))
                    { Directory.CreateDirectory(directoryName); }
                    
                    mf.FileSaveFlags();
                }
            }
            catch (Exception ex)
            {
                Log.EventWriter("Creating new kml field " + ex.ToString());

                MessageBox.Show(gStr.Get(gs.gsError), ex.ToString());
                mf.currentFieldDirectory = "";
            }
        }
    }
}