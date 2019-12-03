using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baichador {
    internal class Settings {
        private const string CONFIG_FNAME = "Baichador.config";

        public int MAX_THREADS = 5, MAX_FIND_TITLE_THREADS = 3, MAX_RETRIES = 2, LIST_FORM_WIDTH = 730, LIST_FORM_MAX_HEIGHT = 805;
        public string CMD_NORMAL = @"--no-playlist -o ""{0}{1}.%(ext)s"" -qx --audio-format mp3 ""{2}""", 
            CMD_GET_PLAYLIST_VIDEOS = @"--flat-playlist -j ""{0}""",
            CMD_GET_TITLE = @"--no-playlist --skip-download --get-title --no-warnings ""{0}""",
            BASE_URL = @"https://youtu.be/", DEFAULT_SAVEDIR = @"%userprofile%\Music";

        public Settings(bool tryRead = true) {
            if(!tryRead)
                return;

            try {
                Settings temp = new Settings(false);

                using(StreamReader reader = new StreamReader(CONFIG_FNAME)) {
                    string line;

                    while((line = reader.ReadLine()) != null) {
                        line = line.Trim(' ');
                        if(line == "")
                            continue;

                        List<string> split = line.Split(' ').ToList();

                        string name = split[0];

                        split.RemoveAt(0);
                        string data = String.Join(" ", split);

                        switch(name) {
                            case "MAX_THREADS":
                                temp.MAX_THREADS = int.Parse(data);
                                break;
                            case "MAX_FIND_TITLE_THREADS":
                                temp.MAX_FIND_TITLE_THREADS = int.Parse(data);
                                break;
                            case "MAX_RETRIES":
                                temp.MAX_RETRIES = int.Parse(data);
                                break;
                            case "LIST_FORM_WIDTH":
                                temp.LIST_FORM_WIDTH = int.Parse(data);
                                break;
                            case "LIST_FORM_MAX_HEIGHT":
                                temp.LIST_FORM_MAX_HEIGHT = int.Parse(data);
                                break;
                            case "CMD_NORMAL":
                                temp.CMD_NORMAL = data;
                                break;
                            case "CMD_GET_PLAYLIST_VIDEOS":
                                temp.CMD_GET_PLAYLIST_VIDEOS = data;
                                break;
                            case "CMD_GET_TITLE":
                                temp.CMD_GET_TITLE = data;
                                break;
                            case "BASE_URL":
                                temp.BASE_URL = data;
                                break;
                            case "DEFAULT_SAVEDIR":
                                temp.DEFAULT_SAVEDIR = data;
                                break;
                        }
                    }
                }

                MAX_THREADS = temp.MAX_THREADS;
                MAX_FIND_TITLE_THREADS = temp.MAX_FIND_TITLE_THREADS;
                MAX_RETRIES = temp.MAX_RETRIES;
                LIST_FORM_WIDTH = temp.LIST_FORM_WIDTH;
                LIST_FORM_MAX_HEIGHT = temp.LIST_FORM_MAX_HEIGHT;
                CMD_NORMAL = temp.CMD_NORMAL;
                CMD_GET_PLAYLIST_VIDEOS = temp.CMD_GET_PLAYLIST_VIDEOS;
                CMD_GET_TITLE = temp.CMD_GET_TITLE;
                BASE_URL = temp.BASE_URL;
                DEFAULT_SAVEDIR = temp.DEFAULT_SAVEDIR;
            } catch(IOException) {
                Console.Error.WriteLine("Não foi possível carregar as configurações do arquivo. Usando valores padrões.");
            }
        }
    }
}
