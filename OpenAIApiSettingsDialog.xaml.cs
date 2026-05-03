using System;
using System.Windows;

namespace PDFSplitterforCopilot
{
    public partial class OpenAIApiSettingsDialog : Window
    {
        public OpenAIApiSettingsDialog()
        {
            InitializeComponent();
            txtModel.Text = OpenAISettingsService.GetStoredOrDefaultModel();
            UpdateStatus();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenAISettingsService.Save(txtApiKey.Password, txtModel.Text);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save OpenAI settings:\n{ex.Message}", "OpenAI API Settings", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearKey_Click(object sender, RoutedEventArgs e)
        {
            OpenAISettingsService.ClearStoredApiKey();
            txtApiKey.Clear();
            UpdateStatus();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void UpdateStatus()
        {
            txtStatus.Text = OpenAISettingsService.HasApiKey()
                ? $"API key is configured. Settings file: {OpenAISettingsService.SettingsPath}"
                : $"API key is not configured. Settings file: {OpenAISettingsService.SettingsPath}";
        }
    }
}
