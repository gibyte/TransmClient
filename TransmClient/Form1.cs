using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Forms;
using Transmission.API.RPC;
using Transmission.API.RPC.Entity;

namespace TransmClient
{
    public partial class Form1 : Form
    {
        private Client transmissionClient;
        private ListView listViewDownloads;
        private Button pauseButton;
        private Button stopButton;
        private Timer refreshTimer;
        public Form1(string filePath = "")
        {
            InitializeComponent();
            InitializeListViewDownloads();
            InitializeTransmissionClient();
            InitializeTorrentList();
            InitializeButtons();

            if (filePath != string.Empty) SendToTransmission(filePath);
        }

        private void SendToTransmission(string filePath)
        {
            try
            {
                // Открываем файл и считываем его содержимое
                byte[] torrentFileBytes = File.ReadAllBytes(filePath);

                // Создаем объект NewTorrent для передачи данных о торрент-файле
                NewTorrent newTorrent = new NewTorrent
                {
                    Metainfo = Convert.ToBase64String(torrentFileBytes)
                };

                // Отправляем торрент-файл на загрузку в Transmission
                transmissionClient.TorrentAdd(newTorrent);

                // Обновляем список загрузок после добавления нового торрента
                InitializeTorrentList();

                MessageBox.Show($"Файл {filePath} успешно добавлен в Transmission для загрузки.", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении файла {filePath} в Transmission: {ex.Message}", "Ошибка");
            }
        }

        private void InitializeTransmissionClient()
        {
            transmissionClient = new Client("http://NAS.local:9091/transmission/rpc", Guid.NewGuid().ToString("N"), "admin", "90Azedan");
        }

        private void InitializeTorrentList()
        {
            try
            {
                var torrentInfo = transmissionClient.TorrentGet(new[] { TorrentFields.ID, TorrentFields.NAME, TorrentFields.PERCENT_DONE });

                foreach (var torrent in torrentInfo.Torrents)
                {
                    // Проверяем, есть ли торрент с таким же ID уже в списке
                    bool found = false;
                    foreach (ListViewItem item in listViewDownloads.Items)
                    {
                        if (item.Tag != null && (int)item.Tag == torrent.ID)
                        {
                            // Если найден, обновляем его данные и помечаем как найденный
                            item.SubItems[0].Text = torrent.Name;
                            item.SubItems[1].Text = torrent.PercentDone.ToString("P0");
                            //item.SubItems[4].Text = FormatTimeSpan(torrent.EstimatedTimeLeft); // Оставшееся время
                            found = true;
                            break;
                        }
                    }

                    // Если торрент не найден в списке, добавляем новый элемент
                    if (!found)
                    {
                        ListViewItem newItem = new ListViewItem(new string[] { torrent.Name, torrent.PercentDone.ToString("P0") });
                        newItem.Tag = torrent.ID; // Сохраняем ID торрента как Tag элемента
                        listViewDownloads.Items.Add(newItem);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        // Метод для форматирования TimeSpan в строку времени
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 0)
            {
                return "Unknown";
            }
            else
            {
                return timeSpan.ToString(@"hh\:mm\:ss");
            }
        }

        private void InitializeListViewDownloads()
        {
            listViewDownloads = new ListView();
            listViewDownloads.Dock = DockStyle.Fill;
            listViewDownloads.View = View.Details;
            listViewDownloads.FullRowSelect = true;
            listViewDownloads.DoubleClick += listViewDownloads_DoubleClick;

            listViewDownloads.Columns.Add("File Name", 200);
            listViewDownloads.Columns.Add("Progress", 70);
            listViewDownloads.Columns.Add("Download Rate", 20);
            listViewDownloads.Columns.Add("Upload Rate", 20);
            listViewDownloads.Columns.Add("Remaining Time", 700); // Новая колонка

            refreshTimer = new Timer();
            refreshTimer.Interval = 5000; // Обновление каждые 5 секунд (в миллисекундах)
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();

            Controls.Add(listViewDownloads);
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            InitializeTorrentList();
        }

        private void InitializeButtons()
        {
            FlowLayoutPanel buttonPanel = new FlowLayoutPanel();
            buttonPanel.FlowDirection = FlowDirection.LeftToRight;
            buttonPanel.Dock = DockStyle.Top;

            pauseButton = new Button();
            pauseButton.Text = "Пауза";
            pauseButton.Click += pauseButton_Click;
            pauseButton.Dock = DockStyle.Bottom;

            stopButton = new Button();
            stopButton.Text = "Остановка";
            stopButton.Click += stopButton_Click;
            stopButton.Dock = DockStyle.Bottom;

            Button openButton = new Button();
            openButton.Text = "Открыть";
            openButton.Click += openButton_Click;
            openButton.Dock = DockStyle.Bottom;

            Button registerButton = new Button();
            registerButton.Text = "Register";
            registerButton.Click += RegisterButton_Click;
            registerButton.Dock = DockStyle.Top; // Помещаем кнопку сверху

            // Добавляем кнопку на форму
            buttonPanel.Controls.Add(pauseButton);
            buttonPanel.Controls.Add(stopButton);
            buttonPanel.Controls.Add(openButton);
            buttonPanel.Controls.Add(registerButton);

            Controls.Add(buttonPanel);
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Торрент файлы (*.torrent)|*.torrent";
            openFileDialog.Title = "Выберите торрент файл";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                SendToTransmission(filePath);
            }
        }

        private void pauseButton_Click(object sender, EventArgs e)
        {
            if (listViewDownloads.SelectedItems.Count > 0)
            {
                //var selectedTorrentId = (int)listViewDownloads.SelectedItems[0].Tag;
                //transmissionClient.TorrentStop(new[] { selectedTorrentId });
                MessageBox.Show("Загрузка приостановлена.", "Пауза");
            }
            else
            {
                MessageBox.Show("Выберите загрузку для приостановки.", "Предупреждение");
            }
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            if (listViewDownloads.SelectedItems.Count > 0)
            {
                //var selectedTorrentId = (int)listViewDownloads.SelectedItems[0].Tag;
                //transmissionClient.TorrentRemove(selectedTorrentId, true);
                MessageBox.Show("Загрузка остановлена и удалена.", "Остановка");
            }
            else
            {
                MessageBox.Show("Выберите загрузку для остановки и удаления.", "Предупреждение");
            }
        }

        private void listViewDownloads_DoubleClick(object sender, EventArgs e)
        {
            if (listViewDownloads.SelectedItems.Count > 0)
            {
                var selectedFile = listViewDownloads.SelectedItems[0].Text;
                OpenFile(selectedFile);
            }
        }

        private void OpenFile(string filePath)
        {
            try
            {
                System.Diagnostics.Process.Start(@"\\NAS\torrent\" + filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии файла: {ex.Message}", "Ошибка");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void RegisterApplicationWithRegistry()
        {
            try
            {
                // Создаем ключ реестра для расширения .torrent
                RegistryKey key = Registry.ClassesRoot.CreateSubKey(".torrent");
                key.SetValue("", "YourApplicationName");

                // Создаем подключение для нашего приложения
                RegistryKey appKey = Registry.ClassesRoot.CreateSubKey("YourApplicationName");
                appKey.SetValue("", "URL: Your Application");
                appKey.SetValue("URL Protocol", "");

                // Создаем подключение для иконки нашего приложения
                RegistryKey iconKey = appKey.CreateSubKey("DefaultIcon");
                iconKey.SetValue("", "YourApplication.exe,1");

                // Создаем подключение для команды запуска
                RegistryKey commandKey = appKey.CreateSubKey(@"shell\open\command");
                commandKey.SetValue("", "YourApplication.exe \"%1\"");

                MessageBox.Show("Приложение успешно зарегистрировано в реестре.", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации приложения: {ex.Message}", "Ошибка");
            }
        }
        private void RegisterButton_Click(object sender, EventArgs e)
        {
            RegisterApplicationWithRegistry();
        }
    }
}
