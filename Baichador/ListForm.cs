using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using CmdReturn = System.Tuple<int, string>;
using ThreadReturn = System.Tuple<int, System.Tuple<int, string>>;

namespace Baichador {
    public partial class ListForm : Form {
        private readonly int idIndex, titleIndex, statusIndex, urlIndex;
        private readonly List<Tuple<string, string>> musics;
        private readonly List<int> retries = new List<int>();
        private readonly List<Downloader> downloaders = new List<Downloader>();
        private readonly MainForm mainForm;
        private readonly BackgroundWorker titleFinder = new BackgroundWorker() { WorkerSupportsCancellation = true };

        private readonly int MAX_THREADS = Program.settings.MAX_THREADS;
        private readonly int MAX_RETRIES = Program.settings.MAX_RETRIES;
        private readonly int MAX_TITLE_THREADS = Program.settings.MAX_FIND_TITLE_THREADS;
        private int activeThreads = 0, activeTitleThreads = 0;
        internal bool exit = false;
        private string dir;

        private readonly Color SUCESS_COLOR = Color.FromArgb(95, 210, 10);
        private readonly Color TRYING_COLOR = Color.FromArgb(255, 255, 105);
        private readonly Color ERROR_COLOR = Color.FromArgb(255, 45, 45);

        private const string WAITING_TO_SEARCH_TITLE = "Esperando para procurar título...";
        private const string SEARCHING_TITLE = "Procurando por título...";
        private const string TITLE_NOT_FOUND = "Título não encontrado";
        private const string PENDING_STATUS = "Esperando...";
        private const string DOWNLOADING_STATUS = "Baixando...";
        private const string DOWNLOADED_STATUS = "Baixado!";
        private const string TRYING_AGAIN = "Tentando mais {0} vezes...";
        private const string DOWNLOAD_FAILED = "Erro número {0}";
        private const string DOWNLOAD_FAILED_NO_LOG = "Erro. Sem log";

        public ListForm(List<Tuple<string, string>> musics, MainForm mainForm) {
            InitializeComponent();

            idIndex = table.Columns["cID"].Index;
            titleIndex = table.Columns["cTitle"].Index;
            statusIndex = table.Columns["cStatus"].Index;
            urlIndex = table.Columns["cURL"].Index;

            this.musics = musics;
            this.mainForm = mainForm;
            mainForm.Enabled = false;

            Height = Program.settings.LIST_FORM_MAX_HEIGHT;
            Width = Program.settings.LIST_FORM_WIDTH;

            Show();
            GenerateTable();
            table.ClearSelection();
        }

        public void Download(object sender, DoWorkEventArgs e) {
            dir = (string) e.Argument;

            for(int i = 0; i < musics.Count; i++) {
                while(activeThreads >= MAX_THREADS) {
                    if(exit)
                        return;
                }
                ++activeThreads;

                var music = musics[i];
                GetRow(i).Cells[statusIndex].Value = DOWNLOADING_STATUS;
                GetRow(i).DefaultCellStyle.BackColor = TRYING_COLOR;

                string illegal = Path.DirectorySeparatorChar + new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                string path = music.Item2;

                foreach(char c in illegal)
                    path = path.Replace(c.ToString(), "");
                
                Downloader dw = new Downloader(Downloader.MODE.NORMAL_VIDEO, CompletedURL, i);
                dw.Download(music.Item1, dir, path);
                downloaders.Add(dw);
            }

            while(activeThreads > 0) {
                if(exit)
                    return;
            }
        }

        /* Thread Event Methods */

        private void FindTitles(object sender, DoWorkEventArgs e) {
            List<Tuple<int, string>> toFind = (List<Tuple<int, string>>) e.Argument;

            foreach(var music in toFind) {
                while(activeTitleThreads >= MAX_TITLE_THREADS) {
                    if(exit)
                        return;
                }

                activeTitleThreads++;
                GetRow(music.Item1).Cells[titleIndex].Value = SEARCHING_TITLE;

                Downloader dw = new Downloader(Downloader.MODE.GET_TITLE, FoundTitle, music.Item1);
                dw.GetTitle(music.Item2);
                downloaders.Add(dw);
            }
        }

        private void CompletedURL(object sender, RunWorkerCompletedEventArgs e) {
            // Quando terminar de baixar uma música

            if(exit)
                return;

            ThreadReturn thRet = (ThreadReturn) e.Result;
            CmdReturn ret = thRet.Item2;
            int index = thRet.Item1;

            if(ret.Item1 == 0) {
                GetRow(index).Cells[statusIndex].Value = DOWNLOADED_STATUS;
                GetRow(index).DefaultCellStyle.BackColor = SUCESS_COLOR;
                activeThreads--;
            } else if(retries[index] < MAX_RETRIES) {
                retries[index]++;
                GetRow(index).Cells[statusIndex].Value = String.Format(TRYING_AGAIN, retries[index]);

                Downloader dw = new Downloader(Downloader.MODE.NORMAL_VIDEO, CompletedURL, index);
                dw.Download(musics[index].Item1, dir, musics[index].Item2);
                downloaders.Add(dw);
            } else {
                GetRow(index).DefaultCellStyle.BackColor = ERROR_COLOR;
                ErrorReport(ret.Item2, index);
                activeThreads--;
            }
        }

        private void FoundTitle(object sender, RunWorkerCompletedEventArgs e) {
            // Quando encontrar o titulo de uma musica (quando o user nao define o titulo)
            activeTitleThreads--;
            if(exit)
                return;

            ThreadReturn ret = (ThreadReturn) e.Result;

            string title;
            if(ret.Item2.Item1 == 0)
                title = ret.Item2.Item2;
            else
                title = TITLE_NOT_FOUND;

            GetRow(ret.Item1).Cells[titleIndex].Value = title;
        }

        /* Other Methods */

        private void GenerateTable() {
            Tuple<string, string> music;
            var toFindTitle = new List<Tuple<int, string>>(); 
            string[] row = new string[4];

            for(int i = 0; i < musics.Count; i++) {
                music = musics[i];

                if(music.Item2 == "%(title)s") {
                    row[titleIndex] = WAITING_TO_SEARCH_TITLE;
                    toFindTitle.Add(new Tuple<int, string>(i, music.Item1));
                    // new Downloader(Downloader.MODE.GET_TITLE, FoundTitle, i).GetTitle(music.Item1);
                }
                else
                    row[titleIndex] = music.Item2;

                row[idIndex] = i.ToString();
                row[statusIndex] = PENDING_STATUS;
                row[urlIndex] = music.Item1;

                table.Rows.Add(row);
                retries.Add(0);
            }

            titleFinder.DoWork += FindTitles;
            titleFinder.RunWorkerAsync(toFindTitle);

            int usedHeight = 60;
            foreach(DataGridViewRow r in table.Rows) {
                usedHeight += r.Height + 2;
            }

            if(usedHeight < Height)
                Height = usedHeight;
            table.Height = Height - 20;
        }

        private void OnClose(object sender, FormClosedEventArgs e) {
            exit = true;
            foreach(Downloader dw in downloaders)
                dw.Kill();

            mainForm.Enabled = true;
        }

        private void ErrorReport(string msg, int index) {
            // Gera um log de erro

            string fname;

            for(int i = 1; i < 100; i++) {
                fname = String.Format("{0}\\error-{1}.log", dir, i);
                if(File.Exists(fname))
                    continue;

                try {
                    File.WriteAllText(fname, msg);
                    GetRow(index).Cells[statusIndex].Value = String.Format(DOWNLOAD_FAILED, i);
                } catch(Exception) {
                    GetRow(index).Cells[statusIndex].Value = DOWNLOAD_FAILED_NO_LOG;
                }

                break;
            }
        }

        private DataGridViewRow GetRow(int id) {
            foreach(DataGridViewRow row in table.Rows) {
                if(row.Cells[idIndex].Value.ToString() == id.ToString())
                    return row;
            }

            return null;
        }

    }
}
