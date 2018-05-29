using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace SCTest
{
    class SCViewModel : BaseViewModel
    {
        private Process process;
        private string text;

        public SCViewModel()
        {
            ClientCommand = new DelegateCommand(DoClientCommand);
            ClientOutput = new ObservableCollection<string>();
            EvalCommand = new DelegateCommand(DoEvalCommand);
        }

        public string ClientText
        {
            get { return process == null ? "Start" : "Stop"; }
        }

        public ICommand ClientCommand { get; }

        public ObservableCollection<string> ClientOutput { get; }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => ClientOutput.Add(e.Data));
        }

        public ICommand EvalCommand { get; }

        public string EvalText
        {
            get { return text; }
            set { SetProperty(ref text, value); }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => ClientOutput.Add(e.Data));
        }

        private void DoClientCommand()
        {
            if (process == null)
            {
                var folder = ConfigurationManager.AppSettings["SCFolder"];
                var arguments = ConfigurationManager.AppSettings["Arguments"];
                var startInfo = new ProcessStartInfo();
                startInfo.FileName = Path.Combine(folder, "sclang.exe");
                startInfo.Arguments = arguments;
                startInfo.WorkingDirectory = folder;
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                process = Process.Start(startInfo);
                process.ErrorDataReceived += Process_ErrorDataReceived;
                process.OutputDataReceived += Process_OutputDataReceived;
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
            }
            else
            {
                process.StandardInput.Write("0.exit");
                process.StandardInput.Write('\x1b');
                if (!process.WaitForExit(3000))
                {
                    process.Kill();
                    process.WaitForExit(1000);
                }
                process.ErrorDataReceived -= Process_ErrorDataReceived;
                process.OutputDataReceived -= Process_OutputDataReceived;
                process = null;
            }
            OnPropertyChanged("ClientText");
        }

        private void DoEvalCommand()
        {
            if (process != null && !string.IsNullOrEmpty(text))
            {
                process.StandardInput.Write(text);
                process.StandardInput.Write('\x0c');
            }
        }
    }
}
