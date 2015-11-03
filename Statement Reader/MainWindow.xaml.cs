using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using Statement_Reader.Annotations;
using Statement_Reader.Properties;

namespace Statement_Reader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private readonly string[] _statementTypes = new[] {"DES Visa", "RBC Chequing", "RBC Visa"};

        public MainWindow()
        {
            InitializeComponent();

            var settings = new Settings();
            InputFolderTextBox.Text = settings.Path;

            if (DateTime.Today.Month < 6)
                Year.Value = DateTime.Today.Year - 1;
            else
                Year.Value = DateTime.Today.Year;

            StatementTypeComboBox.ItemsSource = _statementTypes;
            StatementTypeComboBox.SelectedItem = settings.StatementType;
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SelectInputFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog {SelectedPath = InputFolderTextBox.Text};
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)

                InputFolderTextBox.Text = dialog.SelectedPath;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            IStatementParser parser = null;
            switch (StatementTypeComboBox.SelectionBoxItem.ToString())
            {
                case "DES Visa":
                    parser = new StatementParserForDejardingsVisa();
                    break;

                case "RBC Chequing":
                    parser = new StatementParserForRbcChecking();
                    break;

                case "RBC Visa":
                    parser = new StatementParserForRbcVisa();
                    break;

            }

            if (parser == null)
            {
                MessageBox.Show("Statement type is not supported.");
                return;
            }

            LogTextBox.Clear();
            Log("Started...");
            var path = InputFolderTextBox.Text;

            //Saving the path
            var s = new Settings
            {
                Path = path,
                StatementType = StatementTypeComboBox.SelectedItem.ToString(),
            };
            s.Save();

            ProcessFiles(Path.Combine(InputFolderTextBox.Text), parser, GeneratePdfTextFilesCheckBox.IsChecked == true);

            Log("Done.");
        }

        private void ProcessFiles(string path, IStatementParser parser, bool generateInterimFiles)
        {
            if (!Directory.Exists(path))
            {
                Log("ERROR: Path does not exist: " + path);
                return;
            }


            var outputFilename = Path.Combine(path, "output.txt");

            using (var outputFile = new StreamWriter(outputFilename, false, Encoding.UTF8))
            {
                foreach (var filename in Directory.GetFiles(path, "*.pdf"))
                {
                    Log(filename);

                    var statement = parser.ExtractStatement(filename, generateInterimFiles);
                    statement.WriteStatementToCsv(outputFile);
                }
            }
        }

        private void Log(string str)
        {
            LogTextBox.AppendText(DateTime.Now + " " + str + "\r\n");
        }

        private void InputFolderTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var folder = Path.GetFileName(InputFolderTextBox.Text);
            if (_statementTypes.Contains(folder))
                StatementTypeComboBox.SelectedItem = folder;
        }

        private void Year_ValueChanged(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {


        }
    }
}
