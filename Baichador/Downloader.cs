using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Threading;

using CmdReturn = System.Tuple<int, string>;
using ThreadReturn = System.Tuple<int, System.Tuple<int, string>>;
using PlaylistReturn = System.Tuple<int, object>;


namespace Baichador {
    class Downloader {
        private struct Argument {
            public string url;
            public string dir;
            public string fname;
        }

        /*private class JSONReturn {
            public List<Dictionary<string, string>> entries;
            public string uploader_url, webpage_url_basename, uploader_id, extractor_key, id, extractor, _type, title, uploader, webpage_url;
        }*/

        internal enum MODE { NORMAL_VIDEO, GET_PLAYLIST, GET_TITLE, UPDATE_YOUTUBE_DL };
        private delegate void ReadData(object sender, DataReceivedEventArgs e);

        private readonly string CMD_NORMAL = Program.settings.CMD_NORMAL;
        private readonly string CMD_PLAYLIST = Program.settings.CMD_GET_PLAYLIST_VIDEOS;
        private readonly string CMD_TITLE = Program.settings.CMD_GET_TITLE;
        private readonly string CMD_UPDATE = "--update";
        private readonly string BASE_URL = Program.settings.BASE_URL;

        private readonly BackgroundWorker worker;
        private readonly int id;
        private readonly MODE mode;
        private Process proc = null;
        private bool started = false, exit = false;

        public Downloader(MODE mode, RunWorkerCompletedEventHandler handler, int id = 0) {
            worker = new BackgroundWorker {
                WorkerSupportsCancellation = true
            };

            this.mode = mode;
            if(mode == MODE.NORMAL_VIDEO)
                worker.DoWork += ThDownloadUrl;
            else if(mode == MODE.GET_PLAYLIST)
                worker.DoWork += ThGetPlaylist;
            else if(mode == MODE.GET_TITLE)
                worker.DoWork += TthGetTitle;
            else if(mode == MODE.UPDATE_YOUTUBE_DL)
                worker.DoWork += ThUpdate;
            else
                return;

            worker.RunWorkerCompleted += handler;
            this.id = id;
        }

        /* Public methods */

        public void Download(string url, string dir, string fname) {
            if(mode != MODE.NORMAL_VIDEO)
                return;

            if(fname.EndsWith(".mp3"))
                fname = fname.Remove(fname.Length - 4);

            Argument args = new Argument {
                url = url,
                dir = dir,
                fname = fname
            };

            worker.RunWorkerAsync(args);
        }

        public void GetPlaylist(string url, string dir) {
            if(mode != MODE.GET_PLAYLIST)
                return;

            Argument args = new Argument {
                url = url,
                dir = dir
            };
            worker.RunWorkerAsync(args);
        }

        public void GetTitle(string url) {
            if(mode != MODE.GET_TITLE)
                return;

            worker.RunWorkerAsync(url);
        }

        public void Update() {
            if(mode != MODE.UPDATE_YOUTUBE_DL)
                return;

            worker.RunWorkerAsync();
        }

        public void Kill() {
            if(proc != null && started && !proc.HasExited) {
                proc.Kill();
                proc = null;
            }

            exit = true;
        }

        /* Thread methods */

        private void TthGetTitle(object sender, DoWorkEventArgs e) {
            string url = (string) e.Argument;
            e.Result = new ThreadReturn(id, RunCmd(String.Format(CMD_TITLE, url), true));
        }

        private void ThUpdate(object sender, DoWorkEventArgs e) {
            e.Result = new ThreadReturn(id, RunCmd(CMD_UPDATE, true));
        }

        private void ThDownloadUrl(object sender, DoWorkEventArgs e) {
            Argument args = (Argument) e.Argument;
            e.Result = new ThreadReturn(id, DownloadUrl(args.url, args.dir, args.fname));
        }

        private void ThGetPlaylist(object sender, DoWorkEventArgs e) {
            Argument args = (Argument) e.Argument;
            e.Result = GetPlaylistURLS(args.url, args.dir);
        }

        /* Core Methods */

        private CmdReturn RunCmd(string cmd, bool getStdout = false) {
            int code = 0;
            string text = "";

            proc = new Process();
            ProcessStartInfo info = new ProcessStartInfo {
                FileName = "youtube-dl.exe",
                Arguments = cmd,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = getStdout,
                RedirectStandardError = !getStdout
            };
            proc.StartInfo = info;
                
            StringBuilder output = new StringBuilder();
            using(AutoResetEvent waitHandle = new AutoResetEvent(false)) {
                ReadData method = (sender, e) => {
                    if(e.Data == null)
                        waitHandle.Set();
                    else
                        output.Append(e.Data);
                };

                if(getStdout)
                    proc.OutputDataReceived += new DataReceivedEventHandler(method);
                else
                    proc.ErrorDataReceived += new DataReceivedEventHandler(method);

                if(exit) {
                    if(proc != null)
                        proc.Dispose();
                    proc = null;
                    return null;
                }
                    
                proc.Start();
                started = true;

                if(getStdout)
                    proc.BeginOutputReadLine();
                else
                    proc.BeginErrorReadLine();

                proc.WaitForExit();
                waitHandle.WaitOne();

                started = false;
                if(proc == null)
                    return null;

                code = proc.ExitCode;
                text = output.ToString();
            }

            proc.Dispose();
            proc = null;
            return new CmdReturn(code, text);
        }

        private CmdReturn DownloadUrl(string url, string dir, string fname) {
            try {
                if(!String.IsNullOrEmpty(dir)) {
                    dir = dir.Trim('\\');
                    dir += "\\";
                }

                if(String.IsNullOrEmpty(fname))
                    fname = "%(title)s";

                CmdReturn ret = RunCmd(String.Format(CMD_NORMAL, dir, fname, url));
                return ret;
            } catch(Exception ex) {
                return new CmdReturn(-7, ex.Message);
            }
        }

        private PlaylistReturn GetPlaylistURLS(string url, string dir) {
            try {
                CmdReturn ret = RunCmd(String.Format(CMD_PLAYLIST, url), true);
                object toReturn = ret;

                if(ret.Item1 == 0) {
                    var urls = new List<Tuple<string, string>>();
                    string data = ret.Item2.Trim(new[] { '{', '}' });
                    // Console.WriteLine(ret.Item2);

                    foreach(string base_line in data.Split(new [] { "}{" }, StringSplitOptions.RemoveEmptyEntries)) {
                        string line = '{' + base_line + '}';
                        if(String.IsNullOrEmpty(line))
                            continue;

                        var json = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(line);
                        string title = "%(title)s";
                        if(json.ContainsKey("title"))
                            title = json["title"];

                        urls.Add(new Tuple<string, string>(BASE_URL + json["url"], title));
                    }

                    toReturn = new Tuple<string, List<Tuple<string, string>>>(dir, urls);

                    /*var json = new JavaScriptSerializer().Deserialize<JSONReturn>(ret.Item2);
                    var urls = new List<Tuple<string, string>>();

                    foreach(var obj in json.entries) {
                        string title = "%(title)s";
                        if(obj.ContainsKey("title"))
                            title = obj["title"];

                        urls.Add(new Tuple<string, string>(BASE_URL + obj["url"], title));
                    }
                        

                    toReturn = new Tuple<string, List<Tuple<string, string>>>(dir, urls);*/
                }

                return new PlaylistReturn(ret.Item1, toReturn);
            } catch(Exception ex) {
                return new PlaylistReturn(-7, new CmdReturn(-7, ex.Message));
            }
        }
    }
}
