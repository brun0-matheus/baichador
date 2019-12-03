using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

using CmdReturn = System.Tuple<int, string>;
using ThreadReturn = System.Tuple<int, System.Tuple<int, string>>;
using PlaylistReturn = System.Tuple<int, object>;

namespace Baichador {
    public partial class MainForm : Form {
        private ListForm otherForm;
        private BackgroundWorker downWorker;
        private Downloader dw;

        private readonly string DEF_SAVEDIR = Program.settings.DEFAULT_SAVEDIR;

        private const string DOWNLOAD_ERROR = "Erro durante download\n\nCódigo de saída {0}\n{1}";
        private const string DOWNLOAD_ERROR_TITLE = "Erro durante download";

        private const string OPT1_SUCCESS = "Música baixada com sucesso!";
        private const string OPT1_DOWNLOADING = "Baixando música...";

        private const string OPT2_SUCESS = "Todas (ou quase todas) músicas baixadas!";
        private const string OPT2_SUCCESS_TITLE = "Músicas baixadas!";
        private const string OPT2_DOWNLOADING = "Baixando músicas...";
        private const string OPT2_ABORTED = "Download abortado";

        private const string OPT3_LOADING_URLS = "Carregando playlist...";
        private const string OPT3_ERROR = "Não foi possível carregar a playlist\n\nCódigo de saída {0}\n{1}";

        private const string UPDATING = "Atualizando youtube-dl...";
        private const string UPDATED = "Youtube-dl atualizado!";
        private const string UPDATE_ERROR_TITLE = "Erro durante atualização";
        private const string UPDATE_ERROR_DESC = "ERRO! Tente executar o programa como administrador";

        /* Front-end */

        public MainForm() {
            InitializeComponent();

            comboBox.SelectedIndex = 0;
            lblInfo.Text = "";
            textBox1.Select();
        }

        private void DownloadList(string fname, string dir) {
            // Baixa um lista (em um arquivo) de musicas

            var toDownload = new List<Tuple<string, string>>();

            // Read file
            using(StreamReader reader = new StreamReader(fname)) {
                string line;

                while((line = reader.ReadLine()) != null) {
                    line = line.Trim(' ');
                    if(line == "")
                        continue;

                    string url = line.Split(new[] { ' ' })[0];
                    if(url.Length == 0)
                        continue;

                    string name = line.Substring(url.Length, line.Length - url.Length).Trim(' ');
                    if(name == "")
                        name = "%(title)s";

                    var tup = new Tuple<string, string>(url, name);
                    toDownload.Add(tup);
                }
            }

            otherForm = new ListForm(toDownload, this);
            otherForm.Show();

            downWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            downWorker.DoWork += otherForm.Download;
            downWorker.RunWorkerCompleted += CompletedMultipleDownload;
            downWorker.RunWorkerAsync(dir);
        }

        private void ChangeControlsState(bool state) {
            // Ativa ou desativa os controles

            foreach(Control ctrl in this.Controls) {
                if(ctrl is Label)
                    continue;
                else if(ctrl is TextBox txt)
                    txt.ReadOnly = !state;
                else
                    ctrl.Enabled = state;
            }

            if(!state)
                lblTitle.Select();
        }

        private void ShowMessage(string text, string title, bool changeLblInfo = true) {
            // Abre um pop-up com a mensagem escolhida a tb altera o texto de lblInfo
            MessageBox.Show(text, title);
            if(changeLblInfo)
                lblInfo.Text = title;
        }

        /* Events */

        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e) {
            // Muda o texto dos labels e a visibilidade dos botoes de acordo com as opcoes

            switch(comboBox.SelectedIndex) {
                case 0:
                    label1.Text = "Insira a url da música:";
                    label2.Text = "Digite o nome com que você quer salvar:";

                    btnFile.Visible = false;
                    btnDir.Visible = false;
                    break;
                case 1:
                    label1.Text = "Selecione a lista:";
                    label2.Text = "Selecione a pasta para salvar:";

                    btnFile.Visible = true;
                    btnDir.Visible = true;
                    break;
                case 2:
                    label1.Text = "Insira a url da playlist:";
                    label2.Text = "Selecione a pasta para salvar:";

                    btnFile.Visible = false;
                    btnDir.Visible = true;
                    break;
            }

            /*textBox1.Text = "";
            textBox2.Text = "";*/
        }

        private void BtnFile_Click(object sender, EventArgs e) {
            // Seleciona um arquivo (lista)

            using(OpenFileDialog dialog = new OpenFileDialog()) {
                if(dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dialog.FileName)) {
                    textBox1.Text = dialog.FileName;
                }
            }
        }

        private void BtnDir_Click(object sender, EventArgs e) {
            // Seleciona um diretorio (para salvar os arquivos)

            using(FolderBrowserDialog dialog = new FolderBrowserDialog()) {
                if(dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedPath)) {
                    textBox2.Text = dialog.SelectedPath;
                }
            }
        }

        private void BtnUpdate_Click(object sender, EventArgs e) {
            ChangeControlsState(false);

            lblInfo.Text = UPDATING;
            new Downloader(Downloader.MODE.UPDATE_YOUTUBE_DL, CompletedUpdate).Update();
        }

        private void BtnDown_Click(object sender, EventArgs e) {
            string url = textBox1.Text.Trim();
            string path = textBox2.Text.Trim();

            if(comboBox.SelectedIndex != 0 && String.IsNullOrEmpty(path))
                path = DEF_SAVEDIR;

            ChangeControlsState(false);

            try {
                switch(comboBox.SelectedIndex) {
                    case 0:
                        lblInfo.Text = OPT1_DOWNLOADING;
                        if(String.IsNullOrEmpty(path))
                            path = "%(title)s";

                        dw = new Downloader(Downloader.MODE.NORMAL_VIDEO, CompletedJustOneURL);
                        dw.Download(url, "", DEF_SAVEDIR + "\\" + path);

                        break;
                    case 1:
                        lblInfo.Text = OPT2_DOWNLOADING;
                        DownloadList(url, path);

                        break;
                    case 2:
                        lblInfo.Text = OPT3_LOADING_URLS;
                        dw = new Downloader(Downloader.MODE.GET_PLAYLIST, CompletedGetPlaylist);
                        dw.GetPlaylist(url, path);

                        break;
                }
            } catch(Exception ex) {
                ShowMessage(ex.Message, "ERRO: " + ex.GetType().ToString());
                ChangeControlsState(false);
            }
            
        }

        private void PictureBox1_Click(object sender, EventArgs e) {
            new SettingsForm().Show();
        }

        private void OnClose(object sender, FormClosedEventArgs e) {
            if(dw != null) {
                dw.Kill();
                dw = null;
            }
        }

        /* Threading */

        private void CompletedJustOneURL(object sender, RunWorkerCompletedEventArgs e) {
            // Quando terminar de baixar no modo apenas uma música

            ThreadReturn thRet = (ThreadReturn) e.Result;
            CmdReturn ret = thRet.Item2;

            if(ret.Item1 == 0)
                lblInfo.Text = OPT1_SUCCESS;
            else
                ShowMessage(String.Format(DOWNLOAD_ERROR, ret.Item1, ret.Item2), DOWNLOAD_ERROR_TITLE);

            ChangeControlsState(true);
        }

        private void CompletedGetPlaylist(object sender, RunWorkerCompletedEventArgs e) {
            // Quando terminar de buscar as urls no modo playlist

            PlaylistReturn ret = (PlaylistReturn) e.Result;

            if(ret.Item1 == 0) {
                var firstRet = ret.Item2 as Tuple<string, List<Tuple<string, string>>>;
                var toDownload = firstRet.Item2 as List<Tuple<string, string>>;

                otherForm = new ListForm(toDownload, this);
                otherForm.Show();

                downWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
                downWorker.DoWork += otherForm.Download;
                downWorker.RunWorkerCompleted += CompletedMultipleDownload;
                downWorker.RunWorkerAsync(firstRet.Item1 as string);
            } else {
                CmdReturn cmdRet = (CmdReturn) ret.Item2;
                ShowMessage(String.Format(OPT3_ERROR, cmdRet.Item1, cmdRet.Item2), DOWNLOAD_ERROR_TITLE);
                ChangeControlsState(true);
            }
        }

        private void CompletedMultipleDownload(object sender, RunWorkerCompletedEventArgs e) {
            // Quando terminar de baixar no modo uma lista de músicas

            if(!otherForm.exit)
                ShowMessage(OPT2_SUCESS, OPT2_SUCCESS_TITLE);
            else
                ShowMessage(OPT2_ABORTED, OPT2_ABORTED);

            ChangeControlsState(true);
        }

        private void CompletedUpdate(object sender, RunWorkerCompletedEventArgs e) {
            // Quando terminar de atualizar o youtube-dl

            ThreadReturn thRet = (ThreadReturn) e.Result;
            CmdReturn ret = (CmdReturn) thRet.Item2;

            Console.WriteLine("[DEBUG] " + ret.Item2);

            if(!ret.Item2.Contains("ERROR"))
                lblInfo.Text = UPDATED;
            else {
                lblInfo.Text = UPDATE_ERROR_DESC;
                ShowMessage(ret.Item2, UPDATE_ERROR_TITLE, false);
            }

            ChangeControlsState(true);
        }
    }
}
