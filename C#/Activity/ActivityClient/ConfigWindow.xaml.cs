using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ActivityClient
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindow : Window
    {
        public Settings sets;

        public ConfigWindow()
        {
            InitializeComponent();
            OpenSettings();
        }

        public void OpenSettings()
        {
            Serializer serializer = new Serializer();
            var deserializedObject = serializer.DeSerializeObject("Settings.activity");

            if (deserializedObject != null)
            {
                sets = deserializedObject;
            }
            else
            {
                sets = new Settings
                {
                    IPAddress = "",
                    TeamDisplayEnabled = true,
                    MusicEffectsEnabled = true,
                    AnimationsEnabled = true,
                    FontSize = "large"
                };
            }

            txtIPAddress.Text = sets.IPAddress;
            chbTeamDisplay.IsChecked = sets.TeamDisplayEnabled;
            chbMusicEffects.IsChecked = sets.MusicEffectsEnabled;
            chbAnimations.IsChecked = sets.AnimationsEnabled;

            cmbFontSize.SelectionChanged -= cmbFontSize_SelectionChanged;
            cmbFontSize.SelectedValue = sets.FontSize;
            cmbFontSize.SelectionChanged += cmbFontSize_SelectionChanged;
        }

        public void SaveSettings()
        {
            Serializer serializer = new Serializer();
            serializer.SerializeObject("Settings.activity", sets);
        }

        private void txtIPAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            sets.IPAddress = txtIPAddress.Text;
        }

        private void chbTeamDisplay_Checked(object sender, RoutedEventArgs e)
        {
            if (chbTeamDisplay.IsChecked == true)
            {
                sets.TeamDisplayEnabled = true;
            }
            else
            {
                sets.TeamDisplayEnabled = false;
            }
        }

        private void chbMusicEffects_Checked(object sender, RoutedEventArgs e)
        {
            if (chbMusicEffects.IsChecked == true)
            {
                sets.MusicEffectsEnabled = true;
            }
            else
            {
                sets.MusicEffectsEnabled = false;
            }
        }

        private void chbAnimations_Checked(object sender, RoutedEventArgs e)
        {
            if (chbAnimations.IsChecked == true)
            {
                sets.AnimationsEnabled = true;
            }
            else
            {
                sets.AnimationsEnabled = false;
            }
        }

        private void cmbFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            sets.FontSize = cmbFontSize.SelectedValue.ToString();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            this.DialogResult = true;
            this.Close();
        }

    }
}
