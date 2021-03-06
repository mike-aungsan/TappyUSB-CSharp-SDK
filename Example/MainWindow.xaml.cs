﻿using System;

namespace TapTrack.Demo
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;
    using System.Collections.ObjectModel;
    using System.Threading;
    using TappyUSB;
    using WpfAnimatedGif;
    using TappyUSB.Ndef;
    using System.Diagnostics;
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Driver tappyDriver;
        public ObservableCollection<Row> table;
        GridLength zeroHeight = new GridLength(0);

        public MainWindow()
        {
            InitializeComponent();
            tappyDriver = new Driver();
            table = new ObservableCollection<Row>();
            records.ItemsSource = table;
        }

        //
        // Read Ndef Message tab
        //

        private void ReadNdefButton_Click(object sender, RoutedEventArgs e)
        {
            ndefData.Text = "";
            tappyDriver.ReadNdef((byte)timeout.Value, AddNdefContent, errorHandler: ErrorCallback);
        }

        private void AddNdefContent(byte[] data)
        {
            byte[] temp = new byte[data.Length - data[1] - 2];

            Array.Copy(data, 2 + data[1], temp, 0, temp.Length);

            NdefParser parser = new NdefParser(temp);

            Action update = () =>
            {
                foreach (RecordData payload in parser.GetPayLoad())
                {
                    ndefData.Text += payload.Content + "\r\n";
                }
            };

            Dispatcher.BeginInvoke(update);
            ShowSuccessStatus();
        }

        //
        // Read UID Tab
        //

        private void ReadUIDButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPendingStatus("Waiting for tap");
            tappyDriver.ReadUID((byte)timeout.Value, AddUID, errorHandler: ErrorCallback);
        }

        private void AddUID(byte[] data)
        {
            Tag tag = new Tag(data);

            Action update = () =>
            {
                uidTextBox.Text = "";
                foreach (byte b in tag.UID)
                    uidTextBox.Text += string.Format("{0:X}", b).PadLeft(2, '0') + " ";
                typeTextBox.Text = string.Format("{0:X}", tag.TypeOfTag).PadLeft(2, '0');
            };
            ShowSuccessStatus();
            Dispatcher.Invoke(update);
        }

        //
        // Write URI Tab
        //

        private void WriteURLButton_Click(object sender, RoutedEventArgs e)
        {
            string url = string.Copy(urlTextBox.Text);

            ShowPendingStatus("Waiting for tap");
            tappyDriver.WriteContentToTag(ContentType.Uri, url, false, SuccessCallback, errorHandler: ErrorCallback);
        }

        //
        // Write Text Tab
        //

        private void WriteTextButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPendingStatus("Waiting for tap");
            TextRecordPayload textRecord = new TextRecordPayload("en", TextBox.Text);
            tappyDriver.WriteNdef((byte)timeout.Value, (bool)lockCheckBox.IsChecked, new NdefMessage(textRecord), SuccessCallback, errorHandler: ErrorCallback);
        }

        //
        // Write Multi Ndef Tab
        //

        private void WriteMultNdef(object send, RoutedEventArgs e)
        {
            List<RecordPayload> recs = new List<RecordPayload>();

            foreach (Row row in table)
            {
                if (row.Selected.Equals("Text"))
                {
                    recs.Add(new TextRecordPayload("en", (row.Content == null) ? "" : row.Content));
                }
                else
                {
                    recs.Add(new UriRecordPayload((row.Content == null) ? "" : row.Content));
                }
            }

            NdefMessage message = new NdefMessage(recs.ToArray());

            ShowPendingStatus("Waiting for tap");
            tappyDriver.WriteNdef((byte)timeout.Value, (bool)lockCheckBox.IsChecked, message, SuccessCallback, errorHandler: ErrorCallback);
        }

        private void AddTextRowButton_Click(object sender, RoutedEventArgs e)
        {
            Row row = new Row(table.Count);
            row.Selected = "Text";
            table.Add(row);
        }

        private void AddUriRowButton_Click(object sender, RoutedEventArgs e)
        {
            Row row = new Row(table.Count);
            row.Selected = "URI";
            table.Add(row);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            table.Clear();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Button removeButton = sender as Button;
            int row = (int)removeButton.Tag;
            for (int i = row + 1; i < table.Count; i++)
                table[i].Index = i - 1;
            table.RemoveAt(row);
        }

        //
        // vCard Tab
        //

        private void WriteVCardButton_Click(object sender, RoutedEventArgs e)
        {
            VCard info = new VCard(nameTextBox.Text, cellPhoneTextBox.Text, workPhoneTextBox.Text,
                homePhoneTextBox.Text, emailTextBox.Text, businessEmailTextBox.Text, homeAddrTextBox.Text,
                businessAddrTextBox.Text, companyTextBox.Text, titleTextBox.Text, websiteTextBox.Text);

            ShowPendingStatus("Waiting for tap");
            tappyDriver.WriteVCard(info, (bool)lockCheckBox.IsChecked, SuccessCallback, errorHandler: ErrorCallback);
        }

        private void ClearVCardButton_Click(object sender, RoutedEventArgs e)
        {
            nameTextBox.Text = "";
            emailTextBox.Text = "";
            cellPhoneTextBox.Text = "";
            homePhoneTextBox.Text = "";
            homeAddrTextBox.Text = "";
            websiteTextBox.Text = "";
            companyTextBox.Text = "";
            titleTextBox.Text = "";
            businessEmailTextBox.Text = "";
            workPhoneTextBox.Text = "";
            businessAddrTextBox.Text = "";
        }

        //
        // Detect Type 4B
        //

        private void ReadType4BWithAFI(object sender, RoutedEventArgs e)
        {
            ShowPendingStatus("Waiting for tap");
            tappyDriver.DetectTypeB((byte)timeout.Value, (byte)AFI.Value, UpdateDetTypeBForm, errorHandler: ErrorCallback);
        }

        private void ReadType4B(object sender, RoutedEventArgs e)
        {
            ShowPendingStatus("Waiting for tap");
            tappyDriver.DetectTypeB((byte)timeout.Value, UpdateDetTypeBForm, errorHandler: ErrorCallback);
        }

        private void UpdateDetTypeBForm(byte[] data)
        {
            Action update = () =>
            {
                byte atqbLen = data[0];
                byte attribLen = data[1];
                atqbTextBox.Text = "";
                attribTextBox.Text = "";


                for (int i = 2; i < 2 + data[0]; i++)
                    atqbTextBox.Text += string.Format("{0:X}", data[i]).PadLeft(2, '0') + " ";

                for (int i = data[0] + 2; i < data.Length; i++)
                    attribTextBox.Text += string.Format("{0:X}", data[i]).PadLeft(2, '0') + " ";
            };

            ShowSuccessStatus();
            Dispatcher.BeginInvoke(update);
        }

        //
        // Lock Tag Tab
        //

        public void LockButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPendingStatus("Waiting for tap");
            tappyDriver.LockCard((byte)timeout.Value, SuccessCallback, errorHandler: ErrorCallback);
        }

        //
        // Other
        //

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            HideStatus();
            tappyDriver.Stop(errorHandler: ErrorCallback);
        }

        private void AutoDetectButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPendingStatus("Searching for a TappyUSB");

            Task.Run(() =>
            {
                if (tappyDriver.AutoDetect())
                    ShowSuccessStatus();
                else
                    ShowFailStatus("No TappyUSB found");
            });
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (settingsContainer.Height.IsAuto)
                settingsContainer.Height = zeroHeight;
            else
                settingsContainer.Height = GridLength.Auto;
        }

        private void ShowPendingStatus(string message)
        {
            statusPopup.IsOpen = true;
            statusText.Content = "Pending";
            statusMessage.Content = message;
            ImageBehavior.SetAnimatedSource(statusImage, (BitmapImage)FindResource("Pending"));
        }

        private void ShowSuccessStatus()
        {
            Action show = () => _ShowSuccessStatus();

            Dispatcher.BeginInvoke(show);
        }

        private void _ShowSuccessStatus()
        {
            statusPopup.IsOpen = true;
            statusText.Content = "Success";
            statusMessage.Content = "";
            ImageBehavior.SetAnimatedSource(statusImage, (BitmapImage)FindResource("Success"));

            Task.Run(() =>
            {
                Thread.Sleep(750);
                HideStatus();
            });
        }

        private void ShowFailStatus(string message)
        {
            Action show = () => _ShowFailStatus(message);

            Dispatcher.BeginInvoke(show);
        }

        private void _ShowFailStatus(string message)
        {
            dismissButtonContainer.Height = new GridLength(50);
            dismissButton.Visibility = Visibility.Visible;
            statusPopup.IsOpen = true;
            statusText.Content = "Fail";
            statusMessage.Content = message;
            ImageBehavior.SetAnimatedSource(statusImage, (BitmapImage)FindResource("Error"));
        }

        private void HideStatus()
        {
            Action hide = () =>
            {
                statusPopup.IsOpen = false;
            };

            Dispatcher.Invoke(hide);
        }

        private void DismissButton_Click(object sender, RoutedEventArgs e)
        {
            HideStatus();
            dismissButton.Visibility = Visibility.Hidden;
            dismissButtonContainer.Height = zeroHeight;
        }

        public void SuccessCallback(byte[] data)
        {
            ShowSuccessStatus();
        }

        public void ErrorCallback(TappyError code, byte[] data)
        {
            if (code == TappyError.Application)
            {
                ShowFailStatus(tappyDriver.AppErrorLookUp(data[2]));
            }
            else if (code == TappyError.Hardware)
            {
                ShowFailStatus("TappyUSB is not connected");
            }
            else if (code == TappyError.Nack)
            {
                ShowFailStatus("NACK was received");
            }
        }

        private void launchUrlButton_Click(object sender, RoutedEventArgs e)
        {
            DetectandLaunch();
        }

        public void DetectandLaunch()
        {
            tappyDriver.ReadNdef(0, LaunchCallback);
        }

        private void LaunchCallback(byte[] data)
        {
            byte[] temp = new byte[data.Length - data[1] - 2];

            Array.Copy(data, 2 + data[1], temp, 0, temp.Length);

            NdefParser parser = new NdefParser(temp);
            RecordData[] payload = parser.GetPayLoad().ToArray();

            if (payload.Length > 0)
            {
                if (payload[0].NdefType.Equals("U"))
                {
                    Uri uri = new Uri(payload[0].Content);
                    if (uri.Scheme == 0)
                        return;
                    Process.Start(payload[0].Content);
                }
            }

            Task.Run(() =>
            {
                Thread.Sleep(500);
                DetectandLaunch();
            });
        }

        private void configureTagForPlatform_Click(object sender, RoutedEventArgs e)
        {
            tappyDriver.ConfigurePlatform(ConfigSuccess, null, ErrorCallback);
        }

        private void ConfigSuccess(byte[] data)
        {
            List<byte> temp = new List<byte>(data);
            Tag tag = new Tag(temp.GetRange(1, data.Length - 1).ToArray());
            Debug.WriteLine("Here "+BitConverter.ToString(tag.UID));
            string uid = BitConverter.ToString(tag.UID).Replace("-", "");
            Process.Start(string.Format($"https://members.taptrack.com/x.php?tag_code={uid}"));
        }
    }
}
