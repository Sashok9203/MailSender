using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Win32;
using MimeKit;

namespace MailSender.ModelViews
{
    internal class MailSenderModel :INotifyPropertyChanged
    {
        private string message = string.Empty;
        private bool isAuthorized = false;
        private SmtpClient? smtpClient ;

        private async void authorization()
        {
            try
            {
                if (smtpClient != null)
                {
                    if(smtpClient.IsConnected) smtpClient.Disconnect(true);
                    smtpClient.Dispose();
                    smtpClient = null;
                    IsAuthorized = false;
                    return;
                }
                smtpClient = new();
                smtpClient.MessageSent += OnMessageSent;
                await smtpClient.ConnectAsync("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
                await smtpClient.AuthenticateAsync(Email,Password);
                IsAuthorized = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                IsAuthorized = false;
                return;
            }
           
        }

        private void OnMessageSent(object? sender, MessageSentEventArgs e) => MessageBox.Show("Message sended","Send message");
        

        private void attachFile()
        {
            OpenFileDialog ofd = new();
            if (ofd.ShowDialog() == true)
                AttachedFiles.Add(new() { FilePath = ofd.FileName});
           
        }

        private async void sendMail()
        {
            BodyBuilder bodyBuilder = new();
            if (AttachedFiles.Count != 0)
                foreach (var item in AttachedFiles)
                   await bodyBuilder.Attachments.AddAsync(item.FilePath);
            var message = new MimeMessage
            {
                Date = DateTime.Now,
                Subject = Subject,
                Importance = Importance? MessageImportance.High: MessageImportance.Normal,
            };
            message.To.Add( new MailboxAddress(null,To));
            message.From.Add(new MailboxAddress(null, Email));
            bodyBuilder.TextBody = Message;
            message.Body = bodyBuilder.ToMessageBody();
            try { await smtpClient?.SendAsync(message); }
            catch (Exception ex){ MessageBox.Show(ex.Message, "Error"); }
        }

        public bool Importance { get; set; }

        public string ButtonName => IsAuthorized ? "Disconnect" : "Connect";

        public string Subject { get; set; } = string.Empty;

        public string To { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public bool IsAuthorized
        {
            get => isAuthorized;
            set 
            {
                isAuthorized = value;
                OnPropertyChanged(nameof(ButtonName));
                OnPropertyChanged(nameof(NotAuthorized));
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool NotAuthorized => !IsAuthorized;

        public string Message
        {
           get => message;
           set
           {
               message = value;
               OnPropertyChanged();
           }
        }

        public ObservableCollection<FilePathInfo> AttachedFiles { get; set; } = new();

        public RelayCommand Authorization => new((o)=> authorization(), (o)=> !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password));

        public RelayCommand AttachFile => new ((o) => attachFile());

        public RelayCommand SendMail => new((o) => sendMail(),(o)=> IsAuthorized && !string.IsNullOrWhiteSpace(To));

        public RelayCommand DeleteAttached => new((o) => AttachedFiles.Remove(o as FilePathInfo));

        public RelayCommand DeleteAllAttached => new((o) => AttachedFiles.Clear());

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
