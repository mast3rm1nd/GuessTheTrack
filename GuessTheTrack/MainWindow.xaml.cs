using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;

using System.IO;
using System.Windows.Forms;
using NAudio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace GuessTheTrack
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static List<string> mediaExtensions = new List<string> { ".mp3" };
        static List<string> filesFound = new List<string>();
        static List<MP3_File> filesForQuestions = new List<MP3_File>();
        static List<MP3_File> usedFilesForQuestions = new List<MP3_File>();

        Random rnd = new Random();

        const int _SECONDS_TO_GUESS = 5;

        public MainWindow()
        {
            InitializeComponent();

            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;

            SetUI(false);

            //var reader = new Mp3FileReader(@"C:\vk_bak\music\Toto - Paul Meets Chani (Dune OST).mp3");
            //var totalSeconds = reader.TotalTime.TotalSeconds;
            //reader.CurrentTime = new TimeSpan(0, 0, (int)( (totalSeconds - _SECONDS_TO_GUESS) / 2) );//часы, миуты, секунды // середина проигрываемого участка ровно на середине трека
            //var waveOut = new WaveOut(); // or WaveOutEvent()
            //waveOut.Init(reader); //Just subscribe to WaveOut's PlaybackStopped event.
            //waveOut.Play();

            //var test = Directory.GetDirectories(@"C:\vk_bak\music");
            //DirSearch(@"C:\vk_bak\music");
            //FillQuestionsList();            
        }


               

        

        private void BrowseAudioCollection_Button_Click(object sender, RoutedEventArgs e)
        {
            filesFound.Clear();
            filesForQuestions.Clear();

            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                DirSearch(fbd.SelectedPath);
                FillQuestionsList();

                TracksCount_Label.Content = String.Format("Пригодных MP3 файлов найдено: {0}", filesForQuestions.Count);

                if(filesForQuestions.Count < 4)
                {
                    System.Windows.MessageBox.Show(
                        String.Format("Всего пригодных файлов для игры: {0}. Этого не достаточно даже для одного вопроса. Задайте другую коллекцию файлов.",
                        filesForQuestions.Count));

                    SetUI(false);
                    return;
                }

                SetUI(true);
                StartEndGame_Button.IsEnabled = true;
            }            
        }



        void ResetRadioButtonsSelection()
        {
            Variant0_Radio_Button.IsChecked = false;
            Variant1_Radio_Button.IsChecked = false;
            Variant2_Radio_Button.IsChecked = false;
            Variant3_Radio_Button.IsChecked = false;
        }



        static string currentTrackPath;
        static int rightButton;
        static int usersAnswer;
        void AskQuestion()
        {
            ResetRadioButtonsSelection();

            //usersAnswer = -1;

            var question = GetRandomQuestion();
            currentTrackPath = question[0].FilePath;

            rightButton = rnd.Next(4);

            switch(rightButton)
            {
                case 0: Variant0_Radio_Button.Content = String.Format("{0} - {1}", question[0].Artist, question[0].Title); break;
                case 1: Variant1_Radio_Button.Content = String.Format("{0} - {1}", question[0].Artist, question[0].Title); break;
                case 2: Variant2_Radio_Button.Content = String.Format("{0} - {1}", question[0].Artist, question[0].Title); break;
                case 3: Variant3_Radio_Button.Content = String.Format("{0} - {1}", question[0].Artist, question[0].Title); break;
            }

            var wrongAnswerIndex = 1;
            for(int i = 0; i < 4; i++)
            {
                if (i == rightButton)
                    continue;

                switch (i)
                {
                    case 0: Variant0_Radio_Button.Content = String.Format("{0} - {1}", question[wrongAnswerIndex].Artist, question[wrongAnswerIndex].Title); break;
                    case 1: Variant1_Radio_Button.Content = String.Format("{0} - {1}", question[wrongAnswerIndex].Artist, question[wrongAnswerIndex].Title); break;
                    case 2: Variant2_Radio_Button.Content = String.Format("{0} - {1}", question[wrongAnswerIndex].Artist, question[wrongAnswerIndex].Title); break;
                    case 3: Variant3_Radio_Button.Content = String.Format("{0} - {1}", question[wrongAnswerIndex].Artist, question[wrongAnswerIndex].Title); break;
                }

                wrongAnswerIndex++;
            }

            PlayCurrentAnswer();
        }


        static OffsetSampleProvider trimmed;
        static WaveOut waveOut = new WaveOut();
        void PlayCurrentAnswer()
        {
            MakeAnswer_Button.IsEnabled = false;
            PlayOneMoreTime_Button.IsEnabled = false;

            //var reader = new Mp3FileReader(currentTrackPath);
            //var totalSeconds = reader.TotalTime.TotalSeconds;
            //reader.CurrentTime = new TimeSpan(0, 0, (int)((totalSeconds - _SECONDS_TO_GUESS) / 2));//часы, миуты, секунды // середина проигрываемого участка ровно на середине трека
            ////var waveOut = new WaveOut(); // or WaveOutEvent()

            ////waveOut.PlaybackStopped += WaveOut_PlaybackStopped;

            //waveOut.Init(reader); //Just subscribe to WaveOut's PlaybackStopped event.
            //waveOut.Play();
            AudioFileReader reader = null;

            try
            {
                reader = new AudioFileReader(currentTrackPath);
            }
            catch
            {
                System.Windows.MessageBox.Show(String.Format("Не удалось воспроизвести {0}, будет задан другой вопрос.)", currentTrackPath));
                AskQuestion();
                return;
            }
            
            var totalSeconds = reader.TotalTime.TotalSeconds;
            reader.CurrentTime = TimeSpan.FromSeconds((int)((totalSeconds - _SECONDS_TO_GUESS) / 2));//часы, миуты, секунды // середина проигрываемого участка ровно на середине трека
            trimmed = new OffsetSampleProvider(reader);
            var takeDuration = TimeSpan.FromSeconds(_SECONDS_TO_GUESS);
            trimmed.TakeSamples = (int)(trimmed.WaveFormat.SampleRate * takeDuration.TotalSeconds) * trimmed.WaveFormat.Channels;
            waveOut = new WaveOut(); // or WaveOutEvent()

            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;

            waveOut.Init(new SampleToWaveProvider16(trimmed)); //Just subscribe to WaveOut's PlaybackStopped event.
            waveOut.Play();
        }

        private void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            //MakeAnswer_Button.IsEnabled = true;
            PlayOneMoreTime_Button.IsEnabled = true;
        }

        static int rightAnswersCount = 0;
        static int wrongAnswersCount = 0;
        private void MakeAnswer_Button_Click(object sender, RoutedEventArgs e)
        {
            if (usersAnswer == rightButton)
            {
                rightAnswersCount++;
                RightAnswersCount_Label.Content = rightAnswersCount.ToString();
            }
            else
            {
                wrongAnswersCount++;
                WrongAnswersCount_Label.Content = wrongAnswersCount.ToString();
            }

            if(filesForQuestions.Count < 4)
            {
                System.Windows.MessageBox.Show("Осталось всего 3 файла для игры. Продолжение не возможно. Вы можете начать заного.");
                SetUI(false);
                return;
            }

            waveOut.Pause();
            waveOut = new WaveOut();

            PlayOneMoreTime_Button.IsEnabled = false;

            AskQuestion();
        }



        #region TechnicalMethodsAndStuff
        void DirSearch(string sDir)
        {
            foreach (string f in Directory.GetFiles(sDir, "*.*"))
            {
                //var test = Path.GetExtension(f).ToLower();
                if (mediaExtensions.Contains(Path.GetExtension(f).ToLower()))
                    filesFound.Add(f);
            }

            foreach (string d in Directory.GetDirectories(sDir))
            {
                DirSearch(d);
            }
        }


        void FillQuestionsList()
        {
            foreach (var path in filesFound)
            {
                TagLib.File tagFile;

                try
                {
                tagFile = TagLib.File.Create(path);
                }
                catch
                {
                    continue;
                }

                string artist = tagFile.Tag.FirstPerformer;                
                //string album = tagFile.Tag.Album;
                string title = tagFile.Tag.Title;

                if(title != "" && title != null)
                    if(artist != "" && artist != null)
                    {
                        filesForQuestions.Add(new MP3_File { FilePath = path, Artist = artist, Title = title });
                    }
            }
        }


        class MP3_File
        {
            public string FilePath { get; set; }
            public string Artist { get; set; }
            public string Title { get; set; }
        }

        /// <summary>
        /// Первый эллемент возвращённого массива - верный (удалён из списка для угадывания).
        /// </summary>
        MP3_File[] GetRandomQuestion()
        {
            var fourTracks = new List<MP3_File>();
            var usedIndexes = new List<int>();

            var indexOfAnswer = rnd.Next(filesForQuestions.Count);

            fourTracks.Add(filesForQuestions[indexOfAnswer]);

            filesForQuestions.RemoveAt(indexOfAnswer);

            
                 
            do
            {
                var randomIndex = rnd.Next(filesForQuestions.Count);

                if (usedIndexes.Contains(randomIndex))
                    continue;

                usedIndexes.Add(randomIndex);
                fourTracks.Add(filesForQuestions[randomIndex]);

            }while(usedIndexes.Count < 3);// находим три альтернативных варианта


            return fourTracks.ToArray();
        }


        void SetUI(bool enable)
        {
            Variant0_Radio_Button.IsEnabled = enable;
            Variant1_Radio_Button.IsEnabled = enable;
            Variant2_Radio_Button.IsEnabled = enable;
            Variant3_Radio_Button.IsEnabled = enable;

            PlayOneMoreTime_Button.IsEnabled = enable;
            MakeAnswer_Button.IsEnabled = enable;
        }
        #endregion

        #region SettingUserAnswer
        
        private void Variant0_Radio_Button_Checked(object sender, RoutedEventArgs e)
        {
            usersAnswer = 0;
            MakeAnswer_Button.IsEnabled = true;
        }

        private void Variant1_Radio_Button_Checked(object sender, RoutedEventArgs e)
        {
            usersAnswer = 1;
            MakeAnswer_Button.IsEnabled = true;
        }
        

        private void Variant2_Radio_Button_Checked(object sender, RoutedEventArgs e)
        {
            usersAnswer = 2;
            MakeAnswer_Button.IsEnabled = true;
        }

        private void Variant3_Radio_Button_Checked(object sender, RoutedEventArgs e)
        {
            usersAnswer = 3;
            MakeAnswer_Button.IsEnabled = true;
        }

        #endregion


        void ResetCountersAndLabels()
        {
            wrongAnswersCount = 0;
            rightAnswersCount = 0;

            WrongAnswersCount_Label.Content = "0";
            RightAnswersCount_Label.Content = "0";
        }


        static bool isGameStarted = false;
        private void StartAndRestartGame_Button_Click(object sender, RoutedEventArgs e)
        {
            if(isGameStarted)
            {
                //SetUI(false);
                //isGameStarted = !isGameStarted;

                filesForQuestions.AddRange(usedFilesForQuestions);
                usedFilesForQuestions.Clear();

                waveOut.Pause();
                waveOut = new WaveOut();

                ResetCountersAndLabels();

                AskQuestion();
            }
            else
            {
                StartEndGame_Button.Content = "Начать заного";

                SetUI(true);
                AskQuestion();
                

                isGameStarted = true;
            }
        }

        private void PlayOneMoreTime_Button_Click(object sender, RoutedEventArgs e)
        {
            MakeAnswer_Button.IsEnabled = false;
            PlayOneMoreTime_Button.IsEnabled = false;

            //waveOut = new WaveOut();
            //waveOut.Init(new SampleToWaveProvider16(trimmed));
            //waveOut.Play();
            var reader = new AudioFileReader(currentTrackPath);
            var totalSeconds = reader.TotalTime.TotalSeconds;
            reader.CurrentTime = TimeSpan.FromSeconds((int)((totalSeconds - _SECONDS_TO_GUESS) / 2));//часы, миуты, секунды // середина проигрываемого участка ровно на середине трека
            trimmed = new OffsetSampleProvider(reader);
            var takeDuration = TimeSpan.FromSeconds(_SECONDS_TO_GUESS);
            trimmed.TakeSamples = (int)(trimmed.WaveFormat.SampleRate * takeDuration.TotalSeconds) * trimmed.WaveFormat.Channels;
            waveOut = new WaveOut(); // or WaveOutEvent()

            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;

            waveOut.Init(new SampleToWaveProvider16(trimmed)); //Just subscribe to WaveOut's PlaybackStopped event.
            waveOut.Play();
        }
    }
}
