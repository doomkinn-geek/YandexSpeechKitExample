using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System.Text;
using System.Windows.Controls;
using System.Net.Http.Headers;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using System.Threading;

namespace YandexSpeechKitExample_WPF
{
    public enum VoiceOption
    {
        alena,
        jane,
        omazh,
        filipp,
        ermil,
        madirus,
        zahar
    }

    public enum EmotionOption
    {
        neutral,
        good,
        evil
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Button btnStart;
        private CancellationTokenSource _cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
        }
        private async void BtnCreateAudioFiles_Click(object sender, RoutedEventArgs e)
        {
            VoiceOption selectedVoice = (VoiceOption)((ComboBoxItem)voiceComboBox.SelectedItem).Tag;
            EmotionOption selectedEmotion = (EmotionOption)((ComboBoxItem)emotionComboBox.SelectedItem).Tag;
            var directoryPath = txtDirectory.Text.Trim();

            if (string.IsNullOrEmpty(directoryPath))
            {
                MessageBox.Show("Выберите папку с текстовыми файлами.");
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            BtnCancel.IsEnabled = true; // Включаем кнопку отмены
            BtnCreateAudioFiles.IsEnabled = false;
            string mode = singleFileRadio.IsChecked.GetValueOrDefault() ? "file" : "folder";
            try
            {
                await CreateAudioFilesFromTextFiles(directoryPath, selectedVoice.ToString(), selectedEmotion.ToString(), cancellationToken, mode);
                txtStatus.Content = "Готово!";
            }
            catch (OperationCanceledException)
            {
                txtStatus.Content = "Операция отменена пользователем.";
            }
            catch (Exception ex)
            {
                txtStatus.Content = $"Ошибка: {ex.Message}";
            }
            BtnCancel.IsEnabled = false; // Отключаем кнопку отмены
            BtnCreateAudioFiles.IsEnabled = true;
        }


        private void UpdateProgress(int value, int max)
        {
            progressBar.Value = value;
            progressBar.Maximum = max;
            progressLabel.Content = $"{(int)(((double)value / max) * 100)}%";
        }


        private void RadioButtons_Checked(object sender, RoutedEventArgs e)
        {
            if (txtDirectory != null)
            {
                txtDirectory.Text = "";
            }
        }

        private void BtnSelectDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                if (singleFileRadio.IsChecked == true)
                {
                    dialog.Filters.Add(new CommonFileDialogFilter("Text Files", ".txt"));
                    dialog.IsFolderPicker = false;
                }
                else
                {
                    dialog.IsFolderPicker = true;
                }

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    txtDirectory.Text = dialog.FileName;
                }
            }
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private async Task CreateAudioFilesFromTextFiles(string directoryPath, string selectedVoice, string selectedEmotion, CancellationToken cancellationToken, string mode = "folder")
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();
            var apiKey = config.GetSection("YandexApi:ApiKey").Value;
            var apiUrl = "https://tts.api.cloud.yandex.net/speech/v1/tts:synthesize";
            const int maxTextLength = 4500;

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Api-Key", apiKey);

                var filePaths = mode == "folder" ? Directory.GetFiles(directoryPath, "*.txt") : new string[] { directoryPath };
                progressBar.Minimum = 0;
                progressBar.Maximum = filePaths.Length;
                progressBar.Value = 0;

                for (int i = 0; i < filePaths.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string text = await File.ReadAllTextAsync(filePaths[i]);
                    string outputFilePath = Path.ChangeExtension(filePaths[i], ".mp3");

                    var paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                    progressBar.Minimum = 0;
                    progressBar.Maximum = paragraphs.Length;
                    progressBar.Value = 0;

                    using (var outputFile = File.Create(outputFilePath))
                    {
                        foreach (var paragraph in paragraphs)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var textChunks = new List<string>();
                            for (int j = 0; j < paragraph.Length; j += maxTextLength)
                            {
                                textChunks.Add(paragraph.Substring(j, Math.Min(maxTextLength, paragraph.Length - j)));
                            }

                            foreach (var chunk in textChunks)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                                {
                                    {"text", chunk},
                                    {"lang", "ru-RU"},
                                    {"voice", selectedVoice},
                                    {"emotion", selectedEmotion},
                                    {"format", "mp3" },
                                    {"speed", "1.2" },
                                    {"sampleRateHertz", "48000" }
                                });

                                using (var response = await httpClient.PostAsync(apiUrl, content, cancellationToken))
                                {
                                    if (response.IsSuccessStatusCode)
                                    {
                                        using (var responseStream = await response.Content.ReadAsStreamAsync())
                                        {
                                            await responseStream.CopyToAsync(outputFile);
                                        }
                                    }
                                    else
                                    {
                                        txtStatus.Content = $"Ошибка: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}";
                                        break;
                                    }
                                }
                            }

                            progressBar.Value++;
                            progressLabel.Content = $"{((progressBar.Value / progressBar.Maximum) * 100):F1}%";
                        }
                    }

                    progressBar.Value++;
                    progressLabel.Content = $"{((progressBar.Value / progressBar.Maximum) * 100):F1}%";
                }
            }

            txtStatus.Content = "Завершено!";
        }


        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
        }
    }
}
