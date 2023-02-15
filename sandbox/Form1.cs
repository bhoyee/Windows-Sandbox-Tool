using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;


namespace sandbox
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1 = new OpenFileDialog();
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                programTextBox.Text = openFileDialog1.FileName;
            }
            
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            var path = programTextBox.Text;
            var arguments = argumentsTextBox.Text;

            // Create a new AppDomain for the application to run in
            AppDomain sandbox = AppDomain.CreateDomain("Sandbox", null, new AppDomainSetup
            {
                ApplicationBase = Path.GetDirectoryName(path),
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
                ApplicationName = Path.GetFileNameWithoutExtension(path)
            });

            try
            {
                // Load the application into the sandbox
                Assembly assembly = sandbox.Load(AssemblyName.GetAssemblyName(path));

                // Get a reference to the Main method of the application
                MethodInfo method = assembly.EntryPoint;
                if (method != null)
                {
                    // Execute the Main method in the sandbox
                    method.Invoke(null, new object[] { arguments.Split(' ') });
                }
            }
            finally
            {
                // Unload the sandbox
                AppDomain.Unload(sandbox);
            }
        }
    }

}
