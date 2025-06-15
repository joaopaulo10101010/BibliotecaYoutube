using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using System.Diagnostics;

namespace Youtubelib
{
    public class YoutubeVideo
    {
        private readonly YoutubeClient _youtube = new YoutubeClient();
        public string url = "https://www.youtube.com/watch?v=VL0qz5l-zWo";
        private YoutubeExplode.Videos.Video video;
        private string ffmpeg = "C:/ffmpeg-7.1.1-essentials_build/bin/ffmpeg.exe";

        public YoutubeVideo(string Url)
        {
            this.url = Url;
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

        public async Task BaixarVideo(string path_saida = "audiovideo.mp4")
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
            await youtube.Videos.Streams.DownloadAsync(streamInfo, destino_com_nome);
            return true;
        }
    }
}
