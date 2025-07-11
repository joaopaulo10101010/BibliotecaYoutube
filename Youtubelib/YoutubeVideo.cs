using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using System.Diagnostics;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Configuration;



namespace Youtubelib
{
    public class YoutubeHelp
    {
        public string RequisitosString()
        {
            return "1 - Biblioteca Youtube Explode(NuGet)\n2 - SDK FFMPEG (https://ffmpeg.org/download.html)";
        }
    }

    public class YoutubePesquisa
    {
        private string _apikey;
        public YoutubePesquisa()
        {
            var doc = new System.Xml.XmlDocument();
            doc.Load("youtube.config");
            _apikey = doc.SelectSingleNode("//add[@key='MinhaChaveAPI']")?.Attributes["value"].Value;
        }

        public void ApiKey(string key)
        {
            _apikey = key;
        }

        public async Task<Video[]> PesquisaVideos(string pesquisa,int maxresultados)
        {
            string maxresult = Convert.ToString(maxresultados);
            HttpClient client = new HttpClient();
            string youtubeconnection = $"https://www.googleapis.com/youtube/v3/search?part=snippet&q={Uri.EscapeDataString(pesquisa)}&type=video&maxResults={maxresult}&key={_apikey}";

            try
            {
                var resposta = await client.GetStringAsync(youtubeconnection);
                var json = JsonDocument.Parse(resposta);
                var items = json.RootElement.GetProperty("items");
                var resuPesquisa = new Video[items.GetArrayLength()];

                for (int i = 0; i < items.GetArrayLength(); i++)
                {
                    var item = items[i];
                    var id = item.GetProperty("id").GetProperty("videoId").GetString();
                    var title = item.GetProperty("snippet").GetProperty("title").GetString();
                    resuPesquisa[i] = new Video { Id = id, titulo = title };
                }
                client.Dispose();
                return resuPesquisa;
            }
            catch (Exception ex) 
            {
                client.Dispose();
                Console.WriteLine($"Ocorreu um erro ao realizar a pesquisa: {ex.Message}");
                return null;
            }
        }
    }
    public class Video
    {
        public string titulo { get; set; } = "Sem titulo";
        public string Id { get; set; } = "Sem Id";
        public string video_path {  get; set; }
        public void Play()
        {
            if (video_path != null)
            {
                try
                {
                    ProcessStartInfo processo = new ProcessStartInfo() { FileName = video_path, UseShellExecute = true };
                    Process.Start(processo);
                }
                catch (Exception erro)
                {
                    Console.WriteLine($"Nao foi possivel reproduzir o video, erro: {erro.Message}");
                }
            }
            else 
            { 
                Console.WriteLine("Objeto sem caminho para o video");
            }
        }
    }

    public class YoutubeVideo
    {
        private readonly YoutubeClient _youtube = new YoutubeClient();
        public string url = "https://www.youtube.com/watch?v=VL0qz5l-zWo";
        private YoutubeExplode.Videos.Video video;
        private string ffmpeg = "C:/ffmpeg-7.1.1-essentials_build/bin/ffmpeg.exe";
        public IProgress<double> progressoAudio = new Progress<double>();
        public IProgress<double> progressoVideo = new Progress<double>();

        public YoutubeVideo(string Url)
        {
            url = Url;
        }

        /// <summary>
        /// Esse Metodo serve para alterar o caminho do SDK ffmpeg, que é utilizado nessa classe para fazer a junção dos arquivos de video, com os de audio.
        /// </summary>
        /// <param name="path">caminho para o SDK ffmpeg exe: "C:/ffmpeg-7.1.1-essentials_build/bin/ffmpeg.exe".</param>
        public void AlterarFfmpeg(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                ffmpeg = path;
            }
        }

        private async Task<bool> Atualizar()
        {
            if (video != null)
                return true;

            await CarregarVideo(url, _youtube);
            return video != null;
        }

        public async Task<string> GetTitulo()
        {
            if (await Atualizar())
                return video.Title;
            return "Titulo do Video Indisponivel";
        }

        /// <summary>
        /// Metodo Responsavel por retornar uma string contendo o nome do Canal dono do video, que esta atrelado a esse objeto.
        /// </summary>
        public async Task<string> GetAutor()
        {
            if (await Atualizar())
                return video.Author.ChannelTitle;
            return "Nome do Canal Indisponivel";
        }

        /// <summary>
        /// Metodo Responsavel por retornar uma string contendo a duração do video, que esta atrelado a esse objeto.
        /// </summary>
        public async Task<string> GetDuracao()
        {
            if (await Atualizar())
                return video.Duration?.ToString() ?? "";
            return "Duracao Indisponivel";
        }

        private async Task CarregarVideo(string Url, YoutubeClient youtubeClient)
        {
            video = await youtubeClient.Videos.GetAsync(Url);
        }

        public string GetUrl()
        {
            return url;
        }

        public async Task<bool> TrocarUrl(string Url)
        {
            try
            {
                url = Url;
                return await Atualizar();
            }
            catch
            {
                return false;
            }
        }

        public async Task BaixarVideo(string path_saida = "audiovideo.mp4", bool force = false)
        {
            if (System.IO.File.Exists(path_saida) == false)
            {
                bool completo = false;
                string tempname = System.DateTime.Now.ToString("yyyyMMddHHmmss");
                string tempaudio = $"{tempname}audio.mp4";
                string tempvideo = $"{tempname}video.mp4";

                if (await DownloadAudioLocal(tempaudio) && await DownloadVideoLocal(tempvideo))
                {
                    completo = await JuntarAudioVideo(tempvideo, tempaudio, path_saida);
                }

                if (completo)
                {
                    System.IO.File.Delete(tempaudio);
                    System.IO.File.Delete(tempvideo);
                }
            }
            else
            {
                if (force == true)
                {
                    System.IO.File.Delete(path_saida);
                    bool completo = false;
                    string tempname = System.DateTime.Now.ToString("yyyyMMddHHmmss");
                    string tempaudio = $"{tempname}audio.mp4";
                    string tempvideo = $"{tempname}video.mp4";

                    if (await DownloadAudioLocal(tempaudio) && await DownloadVideoLocal(tempvideo))
                    {
                        completo = await JuntarAudioVideo(tempvideo, tempaudio, path_saida);
                    }

                    if (completo)
                    {
                        System.IO.File.Delete(tempaudio);
                        System.IO.File.Delete(tempvideo);
                    }
                }
                else
                {
                    Console.WriteLine("Ja possui um arquivo com o nome do video na pasta");
                }
            }

        }

        private async Task<bool> JuntarAudioVideo(string video, string audio, string saida)
        {
            var ffmpegArgs = $"-i \"{video}\" -i \"{audio}\" -c copy \"{saida}\"";

            var processo = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = ffmpegArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            processo.Start();
            string resultado = await processo.StandardError.ReadToEndAsync();
            await processo.WaitForExitAsync();
            return true;
        }

        public async Task<bool> DownloadVideoLocal(string destino_com_nome)
        {
            var youtube = _youtube;
            var videoUrl = url;
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoUrl);
            var streamInfo = streamManifest.GetVideoOnlyStreams().GetWithHighestVideoQuality();
            await youtube.Videos.Streams.DownloadAsync(streamInfo, destino_com_nome);
            return true;
        }


        public async Task<bool> DownloadAudioLocal(string destino_com_nome)
        {

            var youtube = _youtube;
            var videoUrl = url;
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoUrl);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            await youtube.Videos.Streams.DownloadAsync(streamInfo, destino_com_nome, progressoAudio);
            return true;
        }

        
    }
}
