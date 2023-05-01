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

            try
            {
                await CreateAudioFilesFromTextFiles(directoryPath, selectedVoice.ToString(), selectedEmotion.ToString());
                MessageBox.Show("Готово!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void UpdateProgress(int value, int max)
        {
            progressBar.Value = value;
            progressBar.Maximum = max;
            progressLabel.Content = $"{(int)(((double)value / max) * 100)}%";
        }


        private void BtnSelectDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    txtDirectory.Text = dialog.FileName;
                }
            }
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            
        }       

        private async Task CreateAudioFilesFromTextFiles(string directoryPath, string selectedVoice, string selectedEmotion)
        {
            var apiKey = "AQVNz7LJrIiaQHv22qhEWV7Ef0UPBUgUFlH-waHV";
            var apiUrl = "https://tts.api.cloud.yandex.net/speech/v1/tts:synthesize";
            const int maxTextLength = 4500;

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Api-Key", apiKey);

                var filePaths = Directory.GetFiles(directoryPath, "*.txt");
                progressBar.Minimum = 0;
                progressBar.Maximum = filePaths.Length;
                progressBar.Value = 0;

                for (int i = 0; i < filePaths.Length; i++)
                {
                    string text = await File.ReadAllTextAsync(filePaths[i]);
                    string outputFilePath = Path.ChangeExtension(filePaths[i], ".mp3");

                    var textChunks = new List<string>();
                    for (int j = 0; j < text.Length; j += maxTextLength)
                    {
                        textChunks.Add(text.Substring(j, Math.Min(maxTextLength, text.Length - j)));
                    }

                    using (var outputFile = File.Create(outputFilePath))
                    {
                        foreach (var chunk in textChunks)
                        {
                            var content = new FormUrlEncodedContent(new Dictionary<string, string>
                            {
                                {"text", chunk},
                                {"lang", "ru-RU"},
                                {"voice", selectedVoice},
                                {"emotion", selectedEmotion},
                                {"format", "mp3" },
                                {"speed", "1.25" },
                                {"sampleRateHertz", "48000" }                                
                            });

                            using (var response = await httpClient.PostAsync(apiUrl, content))
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
                    }

                    progressBar.Value++;
                    progressLabel.Content = $"{((progressBar.Value / progressBar.Maximum) * 100):F1}%";
                }
            }

            txtStatus.Content = "Завершено!";
        }

    }
}
