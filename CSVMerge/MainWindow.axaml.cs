using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace CSVMerge
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            if (!File.Exists("csvMerge.config"))
            {
                return;
            }

            OpenAfterCheckBox.IsEnabled = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            try
            {
                string[] t = File.ReadAllText("csvMerge.config").Split(',');
                FullPathCheckBox.IsChecked = bool.Parse(t[0]);
                OpenAfterCheckBox.IsChecked = bool.Parse(t[1]);
                HeadersCheckBox.IsChecked = bool.Parse(t[2]);
            }
            catch (FormatException)
            {
                File.Delete("csvMerge.config");
            }
        }

        private async void SaveButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (FileList.Count == 0)
            {
                return;
            }

            SaveButton.IsEnabled = false;

            SaveFileDialog s = new()
            {
                DefaultExtension = ".csv",
                Title = "Save where?",
                Filters = new List<FileDialogFilter>
                {
                    new()
                    {
                        Name = "CSV Files (*.csv)",
                        Extensions = { "csv" },
                    }
                }
            };
            string? savePath = await s.ShowAsync(this);
            if (savePath is null)
            {
                return;
            }

            List<string> newFile = new List<string>();
            bool headers = HeadersCheckBox.IsChecked is true;

            if (headers)
            {
                newFile.Add((await File.ReadAllLinesAsync(FileList.First())).First());
            }

            foreach (List<string> file in FileList.Select(File.ReadAllLines).Select(z => z.ToList()))
            {
                if (headers)
                {
                    file.RemoveAt(0);
                }
                newFile.AddRange(file);
            }

            await File.WriteAllLinesAsync(savePath, newFile);

            SaveButton.IsEnabled = true;

            if (OpenAfterCheckBox.IsChecked is true)
            {
                OpenWithDefaultProgram(savePath);
            }
        }

        public static void OpenWithDefaultProgram(string path)
        {
            using Process fileOpener = new();

            fileOpener.StartInfo.FileName = "explorer";
            fileOpener.StartInfo.Arguments = "\"" + path + "\"";
            fileOpener.Start();
        }

        private List<string> FileList = new();
        private List<string> FileListNameOnly => FileList.Select(Path.GetFileNameWithoutExtension).ToList()!;

        private async void AddButton_OnClick(object? sender, RoutedEventArgs e)
        {
            OpenFileDialog o = new()
            {
                AllowMultiple = true,
                Filters = new List<FileDialogFilter>
                {
                    new()
                    {
                        Name = "CSV Files (*.csv)", 
                        Extensions = { "csv" },
                    }
                },
                Title = "Select files"
            };
            string[]? items = await o.ShowAsync(this);
            if (items is null)
            {
                return;
            }

            FileList.AddRange(items.Where(z => File.ReadAllLines(z).Length > 0));

            //Files.BeginBatchUpdate();

            RenderGrid();

            //Files.EndBatchUpdate();
        }

        private void RenderGrid()
        {
            Files.RowDefinitions.Clear();
            Files.Children.Clear();
            FileList = FileList.Distinct().OrderBy(z => z).ToList();

            SaveButton.IsEnabled = FileList.Count > 0;

            List<string> listToRender = FullPathCheckBox.IsChecked is true ? FileList : FileListNameOnly;


            foreach (string item in listToRender)
            {
                Files.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                TextBlock t = new()
                {
                    Text = item,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Left,
                    FontSize = 20,
                };
                Files.Children.Add(t);
                int rowNum = Files.RowDefinitions.Count - 1;
                Grid.SetRow(t, rowNum);
                Grid.SetColumn(t, 0);

                Button b = new()
                {
                    Content = "Remove",
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                };
                b.Click += B_Click;
                Files.Children.Add(b);
                Grid.SetRow(b, rowNum);
                Grid.SetColumn(b, 2);
            }

            Files.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
        }

        private void B_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button b)
            {
                return;
            }

            int row = Grid.GetRow(b);
            FileList.RemoveAt(row);

            RenderGrid();
        }

        private void FullPathCheckBox_OnChecked(object? sender, RoutedEventArgs e)
        {
            RenderGrid();
        }

        private void FullPathCheckBox_OnUnchecked(object? sender, RoutedEventArgs e)
        {
            RenderGrid();
        }

        private void Window_OnClosing(object? sender, CancelEventArgs e)
        {
            File.WriteAllText("csvMerge.config", $"{FullPathCheckBox.IsChecked is true},{OpenAfterCheckBox.IsChecked is true},{HeadersCheckBox.IsChecked is true}");
        }
    }
}