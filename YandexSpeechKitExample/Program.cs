using System;
using System.IO;
using System.Net;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace YandexSpeechKitExample
{
    class Program
    {
        static void Main(string[] args)
        {
            string apiKey = "ВАШ API-КЛЮЧ";
            string url = "https://tts.api.cloud.yandex.net/speech/v1/tts:synthesize";
            string directoryPath = "ПУТЬ К ДИРЕКТОРИИ С ФАЙЛАМИ"; // путь к директории с файлами
            string languageCode = "ru-RU"; // код языка (например, ru-RU для русского языка)
            string voiceName = "oksana"; // имя голоса
            double speed = 1.0; // скорость озвучивания
            string format = "oggopus"; // формат аудиофайла
            int quality = 1; // качество аудиофайла (от 0 до 1)

            // получаем список всех файлов с расширением .txt в директории
            string[] files = Directory.GetFiles(directoryPath, "*.txt");

            foreach (string file in files)
            {
                string text = File.ReadAllText(file); // читаем текст из файла

                // создаем JSON-объект с параметрами запроса
                JObject json = new JObject();
                json.Add("text", text);
                json.Add("lang", languageCode);
                json.Add("voice", voiceName);
                json.Add("speed", speed);
                json.Add("format", format);
                json.Add("quality", quality);

                // создаем объект WebRequest и настраиваем его параметры
                WebRequest request = WebRequest.Create(url);
                request.Method = "POST";
                request.Headers.Add("Authorization", "Api-Key " + apiKey);
                request.ContentType = "application/json";

                // отправляем запрос и получаем ответ
                using (Stream stream = request.GetRequestStream())
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.Write(json.ToString());
                    }
                }

                using (WebResponse response = request.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        string outputFilePath = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(file) + "." + format);
                        // сохраняем аудиофайл в ту же директорию, где находится текстовый файл
                        using (FileStream fileStream = File.Create(outputFilePath))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                }
            }
        }
    }
}
