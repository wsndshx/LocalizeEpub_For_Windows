using MahApps.Metro.Controls;
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.IO.Compression;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;

namespace LocalizeEpub_For_Windows
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        // 创建一个ObservableCollection对象, 用于文件列表
        private ObservableCollection<FileInfo> fileInfos = new ObservableCollection<FileInfo>();
        // 创建一个ObservableCollection对象, 用于日志输出
        ObservableCollection<string> cmd = new ObservableCollection<string>();
        // 用于控制日志
        Logs logs;
        FileListData ListData;
        // 
        Dictionary<int, string> fanhuajiMode = new Dictionary<int, string> {
            {0, "Simplified"},
            {1, "Traditional" },
            {2, "China"},
            {3, "Hongkong"},
            {4, "Taiwan"},
            {5, "Mars"}
        };

        public MainWindow()
        {
            InitializeComponent();
        }

        public class FanhuajiOutput
        {
            public int converter { get; set; }
            public string text { get; set; }
        }

        public class FileInfo
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string Status { get; set; }
        }
        public class FileListData
        {
            DataGrid list;
            ObservableCollection<FileInfo> data;

            public FileListData(DataGrid List,ObservableCollection<FileInfo> ts)
            {
                list = List;
                data = ts;
                list.ItemsSource = data;
            }

            public void UpdataStatus(int index,string status)
            {
                data[index].Status = status;
                list.ItemsSource = null;
                list.ItemsSource = data;
            }
        }

        public class Logs
        {
            TextBox cmd;

            public Logs(TextBox textBlock)
            {
                cmd = textBlock;
            }
            public void Print(string log)
            {
                cmd.AppendText(log);
            }

            public void Println(string log)
            {
                cmd.AppendText(log+"\n");
            }

            public void Clear()
            {
                cmd.Text = "";
            }
        }

        public bool TestNetwork(string ip)
        {
            //构造Ping实例  
            Ping pingSender = new Ping();
            //Ping 选项设置  
            PingOptions options = new PingOptions();
            options.DontFragment = true;
            //测试数据  
            string data = "test data abcabc";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            //设置超时时间  
            int timeout = 120;
            //调用同步 send 方法发送数据,将返回结果保存至PingReply实例  
            PingReply reply = pingSender.Send(ip, timeout, buffer, options);
            if (reply.Status == IPStatus.Success)
            {
                logs.Print("("+reply.Address.ToString() + ")["+reply.RoundtripTime+"ms]");
                return true;
                //lst_PingResult.Items.Add("答复的主机地址：" + reply.Address.ToString());
                //lst_PingResult.Items.Add("往返时间：" + reply.RoundtripTime);
                //lst_PingResult.Items.Add("生存时间（TTL）：" + reply.Options.Ttl);
                //lst_PingResult.Items.Add("是否控制数据包的分段：" + reply.Options.DontFragment);
                //lst_PingResult.Items.Add("缓冲区大小：" + reply.Buffer.Length);
            }
            else return false;
        }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 这里捕获鼠标抬起后的动作
            // 开启一个文件选择窗口用于选择文件
            // Configure open file dialog box
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Multiselect = true;
            dialog.FileName = "Document"; // Default file name
            dialog.DefaultExt = ".epub"; // Default file extension
            dialog.Filter = "EPUB (.epub)|*.epub"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // 这里开始变更布局
                Grid_0.Background = null;
                Grid_0.Opacity = 1;
                // 隐藏原先的导入窗口
                Import.Visibility=Visibility.Collapsed;

                // 获取用户选中的文件
                {
                    int i = 0;
                    foreach (String path in dialog.FileNames)
                    {
                        fileInfos.Add(new FileInfo
                        {
                            Name = dialog.SafeFileNames[i++],
                            Path = path,
                            Status = "等待"
                        });
                    };
                }
                // 将导入的文件放置在文件列表当中
                ListData = new FileListData(FileList, fileInfos);
                // 初始化日志界面
                logs = new Logs(Log);
                logs.Clear();

                // 显示操作窗口
                Grid_1.Visibility=Visibility.Visible;
            }
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            Grid_0.Opacity = 0.8;
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            Grid_0.Opacity = 0.4;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = sender as Hyperlink;
            Process.Start(new ProcessStartInfo(link.NavigateUri.AbsoluteUri));
        }

        private void Fanhuaji_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == false)
            {
                return;
            }
            // 测试繁化姬API连通性
            logs.Print("API连通状态: api.zhconvert.org");
            if (TestNetwork("api.zhconvert.org"))
            {
                logs.Println("[√]");
            }
            else
            {
                logs.Println("[×]");
            }
        }

        private void Fanhuaji_Button_Click(object sender, RoutedEventArgs e)
        {
            // 禁用按钮点击
            Fanhuaji_Start.IsEnabled = false;
            // 开始对列表中的文件进行转换
            switch (Fanhuaji_mode.SelectedIndex)
            {
                case -1:
                    logs.Println("请选择需要的转换模式desu");
                    break;
                default:
                    logs.Println("当前选择的模式为" + Fanhuaji_mode.SelectionBoxItem + ", 开始转换...");
                    Action<int> action = new Action<int>((int mode) =>
                    {
                        // 遍历列表
                        int i = 0;
                        foreach (var item in fileInfos)
                        {
                            this.Dispatcher.Invoke(new Action(() => {
                                logs.Println("开始处理文件[" + item.Name + "]:");
                                ListData.UpdataStatus(i, "转换中");
                            }));
                            // Remove temp file
                            try
                            {
                                Directory.Delete("./temp", true);
                            }
                            catch (Exception)
                            {
                            }
                            // Unzip
                            try
                            {
                                ZipFile.ExtractToDirectory(item.Path, "./temp");
                            }
                            catch (Exception)
                            {
                            }
                            // Get files list
                            var allowedExtensions = new[] { ".xhtml", ".html", ".ncx", ".opf" };
                            var files = Directory
                                .GetFiles("./temp","*", SearchOption.AllDirectories)
                                .Where(file => allowedExtensions.Any(file.ToLower().EndsWith))
                                .ToList();
                            // Working with files
                            foreach (string file in files)
                            {
                                this.Dispatcher.Invoke(new Action(() => {
                                    logs.Println(file);
                                }));
                                // Read file content
                                string content = File.ReadAllText(file, Encoding.UTF8);
                                // Build Json data
                                var fanhuajiOutput = new
                                {
                                    converter = fanhuajiMode[mode],
                                    text = content
                                };
                                string jsonString = JsonSerializer.Serialize(fanhuajiOutput);
                                // Send a request
                                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://api.zhconvert.org/convert");
                                req.Method = "POST";
                                req.Timeout = 8000;
                                req.ContentType = "application/json";
                                byte[] data = Encoding.UTF8.GetBytes(jsonString);
                                req.ContentLength = data.Length;
                                using (Stream reqStream = req.GetRequestStream())
                                {
                                    reqStream.Write(data, 0, data.Length);

                                    reqStream.Close();
                                }
                                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                                Stream stream = resp.GetResponseStream();

                                //获取响应内容
                                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                                {
                                    content = reader.ReadToEnd();
                                }
                                JsonDocument jsonDocument = JsonDocument.Parse(content);
                                content = jsonDocument.RootElement.GetProperty("data").GetProperty("text").GetString();

                                // Write to the original file
                                File.WriteAllText(file, content);
                            }
                            // Zip
                            Directory.CreateDirectory("./LocalizeEpub_output");
                            ZipFile.CreateFromDirectory("./temp", "./LocalizeEpub_output/" + item.Name, CompressionLevel.NoCompression, false);
                            this.Dispatcher.Invoke(new Action(() => {
                                logs.Println("文件[" + item.Name + "]处理完成.");
                                ListData.UpdataStatus(i++, "已完成");
                            }));
                        }
                        try
                        {
                            Directory.Delete("./temp", true);
                        }
                        catch (Exception)
                        {
                        }
                        this.Dispatcher.Invoke(new Action(() => {
                            logs.Println("列表中的文件已全部处理完成.");
                            Fanhuaji_Start.IsEnabled = true;
                        }));
                        
                    });
                    action.BeginInvoke(Fanhuaji_mode.SelectedIndex,null,null);
                    break;
            }
        }

        private void Log_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Log.IsMouseOver == true)
            {
                return ;
            }
            Log.ScrollToEnd();
        }
    }
}
