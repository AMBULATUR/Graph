using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using Graph.Abstract;
using Graph.Commands;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Wpf;

namespace Graph
{
    public class ViewModel : INotifyPropertyChanged
    {
        public PlotModel Model { get; private set; }
        public ObservableCollection<string> ValueNames { get; private set; }
        public ICommand LoadCommand { get; private set; }
        public ICommand DrawCommand { get; private set; }
        public ICommand ListenCommand { get; private set; }
        public ICommand ClearCommand { get; private set; }


        public List<Serie> Series { get; private set; }
        private int xIndex = 0;
        private int yIndex = 0;
        public int XIndex { get { return xIndex; } set { xIndex = value; } }
        public int YIndex { get { return yIndex; } set { yIndex = value; } }

        private const int UpdateInterval = 20;
        private readonly Timer timer;

        public ViewModel()
        {
            LoadCommand = new DelegateCommand(Load);
            DrawCommand = new DelegateCommand(Draw);
            ListenCommand = new DelegateCommand(ListenPlot);
            ClearCommand = new DelegateCommand(Clear);

            ValueNames = new ObservableCollection<string>();
            Series = new List<Serie>();
            this.Model = new PlotModel { Title = "Graph" };
            this.timer = new Timer(OnTimerElapsed);
        }

        private void Clear(object obj)
        {
            var s = Model.Series;
             foreach (OxyPlot.Series.LineSeries serie in s)
            //foreach (OxyPlot.Series.ScatterSeries serie in s)
            {
                serie.Points.Clear();
            }
        }
        ConcurrentQueue<string> queu;
        private void Listen()
        {
            queu = new ConcurrentQueue<string>();
            int localPort = 8095;
            
                UdpClient receiver = new UdpClient(localPort);
                IPEndPoint remoteIp = null;
            try
            {
                while (true)
                {
                    byte[] data = receiver.Receive(ref remoteIp);
                    string message = Encoding.UTF8.GetString(data);
                    queu.Enqueue(message);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                receiver.Close();
            }
        }

        private void ListenPlot(Object obj)
        {
            this.timer.Change(Timeout.Infinite, Timeout.Infinite);
            this.Model = new PlotModel() { Title = "Online", };
            this.Model.Series.Add(new OxyPlot.Series.LineSeries { LineStyle = LineStyle.Dot });
           // this.Model.Series.Add(new OxyPlot.Series.ScatterSeries { MarkerType = MarkerType.Circle });
            this.RaisePropertyChanged("Model");
            var axes = this.Model.Axes;
            axes[1].Maximum = 10;
            this.RaisePropertyChanged("Model");

            this.timer.Change(1000, UpdateInterval);
            new Thread(Listen).Start();

        }
        private void OnTimerElapsed(object state)
        {
            lock (this.Model.SyncRoot)
            {
                this.Update();
            }

            this.Model.InvalidatePlot(true);
        }
        private void Update()
        {
             var s = (OxyPlot.Series.LineSeries)Model.Series[0];
            //var s = (OxyPlot.Series.ScatterSeries)Model.Series[0];
            string message;
            while (queu.TryDequeue(out message))
            {
                var split = message.Split('|');
                // s.Points.Add(new ScatterPoint(Double.Parse(split[0], CultureInfo.InvariantCulture), Double.Parse(split[1], CultureInfo.InvariantCulture)));

                 s.Points.Add(new DataPoint(Double.Parse(split[0], CultureInfo.InvariantCulture), Double.Parse(split[1], CultureInfo.InvariantCulture)));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string property)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }



        private void Draw(object obj)
        {
            this.Model.Series.Clear();
            this.Model.ResetAllAxes();
            var serie1 = Series.Find((x) => x.Id == XIndex);
            var serie2 = Series.Find((x) => x.Id == YIndex);
            //var lineSeries1 = new OxyPlot.Series.LineSeries();
            // var lineSeries1 = new OxyPlot.Series.ScatterSeries();
            var lineSeries1 = new OxyPlot.Series.ScatterSeries()
            {
            
            };
           // {//  InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline,};
             
            for (int i = 0; i < serie1.Values.Count; i++)
            {
                try
                {
                    var x = double.Parse((string)serie1.Values[i], CultureInfo.InvariantCulture);
                    var y = double.Parse((string)serie2.Values[i], CultureInfo.InvariantCulture);
                    lineSeries1.Points.Add(new ScatterPoint(x, y));
                   // lineSeries1.Points.Add(new DataPoint(x, y));
                }
                catch
                {
                    MessageBox.Show($"Can't combine {serie1.Name} and {serie2.Name}");
                    return;
                }
            }
            this.Model.Series.Add(lineSeries1);
            this.Model.InvalidatePlot(true);
        }
        private void Load(object obj)
        {
            ValueNames.Clear();
            Series.Clear();
            var path = OpenFileDialog();
            if (!String.IsNullOrEmpty(path))
            {
                var lines = File.ReadAllLines(path);

                for (int i = 0; i < lines.Count(); i++)
                {
                    for (int j = 0; j < lines[i].Split(',').Count(); j++)
                    {
                        if (i == 0)
                            Series.Add(new Serie(j, lines[i].Split(',')[j]));
                        else
                            Series.Find(x => x.Id == j).Values.Add(lines[i].Split(',')[j]);
                    }
                }
            }
            foreach (var item in Series)
            {
                ValueNames.Add(item.Name);
            }
        }

        private string OpenFileDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                DereferenceLinks = false
            };
            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }
            return null;
        }

    }
}
